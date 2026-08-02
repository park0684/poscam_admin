[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "http://localhost:5000",

    [Parameter(Mandatory = $true)]
    [string]$StoreId,

    [Parameter(Mandatory = $true)]
    [string]$Hwid,

    [Parameter(Mandatory = $false)]
    [string]$DeviceName = "CamViewer Token Renewal Test",

    [Parameter(Mandatory = $false)]
    [string]$ProgramVersion = "3.2.0.0"
)

$ErrorActionPreference = "Stop"

function ConvertTo-PlainText {
    param(
        [Parameter(Mandatory = $true)]
        [Security.SecureString]$SecureValue
    )

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)

    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Get-HttpErrorResponseBody {
    param(
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    $response = $ErrorRecord.Exception.Response

    if ($null -eq $response) {
        return ""
    }

    try {
        $stream = $response.GetResponseStream()

        if ($null -eq $stream) {
            return ""
        }

        $reader = New-Object System.IO.StreamReader($stream)

        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    }
    catch {
        return ""
    }
}

function Invoke-JsonPost {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [hashtable]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 10

    try {
        return Invoke-RestMethod `
            -Method Post `
            -Uri $Uri `
            -ContentType "application/json; charset=utf-8" `
            -Body $json
    }
    catch {
        $statusCode = "unknown"

        if ($null -ne $_.Exception.Response) {
            try {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }
            catch {
                $statusCode = "unknown"
            }
        }

        $responseBody = Get-HttpErrorResponseBody -ErrorRecord $_
        $message = "HTTP request failed. StatusCode=$statusCode, Uri=$Uri"

        if (-not [string]::IsNullOrWhiteSpace($responseBody)) {
            $message += "`r`nResponseBody:`r`n$responseBody"
        }

        throw $message
    }
}

if ([string]::IsNullOrWhiteSpace($StoreId) -or
    $StoreId -eq "테스트 매장 ID" -or
    $StoreId -eq "실제 매장 ID") {
    throw "-StoreId에는 예시 문구가 아니라 stores.store_id의 실제 매장 ID를 입력해야 합니다."
}

if ([string]::IsNullOrWhiteSpace($Hwid) -or
    $Hwid -eq "기존 캠뷰어 HWID" -or
    $Hwid -eq "실제 기존 캠뷰어 HWID") {
    throw "-Hwid에는 예시 문구가 아니라 기존 캠뷰어가 사용하는 실제 HWID를 입력해야 합니다."
}

$normalizedBaseUrl = $BaseUrl.TrimEnd('/')
$securePassword = Read-Host "Store password" -AsSecureString
$storePassword = ConvertTo-PlainText -SecureValue $securePassword

try {
    Write-Host "[1/2] Requesting Viewer login token..."

    $loginResponse = Invoke-JsonPost `
        -Uri "$normalizedBaseUrl/api/viewer/login" `
        -Body @{
            storeId = $StoreId
            storePassword = $storePassword
            hwid = $Hwid
            deviceName = $DeviceName
            programVersion = $ProgramVersion
        }

    if (-not $loginResponse.success) {
        throw "Viewer login failed. ErrorCode=$($loginResponse.errorCode), Message=$($loginResponse.message)"
    }

    $loginToken = $loginResponse.data.token.token
    $loginExpiresAt = [DateTime]$loginResponse.data.token.expiresAt
    $loginOfflineUntil = [DateTime]$loginResponse.data.token.offlineUntil

    if ([string]::IsNullOrWhiteSpace($loginToken)) {
        throw "Viewer login succeeded but token was empty."
    }

    $wasExpiredBeforeVerify = $loginExpiresAt.ToUniversalTime() -lt [DateTime]::UtcNow

    Write-Host "Login succeeded."
    Write-Host "  DeviceCode: $($loginResponse.data.deviceCode)"
    Write-Host "  ExpiresAt: $($loginExpiresAt.ToUniversalTime().ToString('o'))"
    Write-Host "  OfflineUntil: $($loginOfflineUntil.ToUniversalTime().ToString('o'))"
    Write-Host "  Expired before verify: $wasExpiredBeforeVerify"

    if (-not $wasExpiredBeforeVerify) {
        throw @"
The login token is not expired, so the expired-token renewal path cannot be proven.
Start the local AuthServer with AuthPolicy__TokenExpireHours=-1 and run this script again.
"@
    }

    Write-Host "[2/2] Verifying and rotating the expired Viewer token..."

    $verifyResponse = Invoke-JsonPost `
        -Uri "$normalizedBaseUrl/api/viewer/verify-token" `
        -Body @{
            token = $loginToken
            hwid = $Hwid
            programVersion = $ProgramVersion
        }

    if (-not $verifyResponse.success) {
        throw "Viewer token renewal failed. ErrorCode=$($verifyResponse.errorCode), Message=$($verifyResponse.message)"
    }

    if (-not $verifyResponse.data.isValid) {
        throw "Viewer token renewal response returned IsValid=false."
    }

    $renewedToken = $verifyResponse.data.token.token
    $renewedExpiresAt = [DateTime]$verifyResponse.data.token.expiresAt
    $renewedOfflineUntil = [DateTime]$verifyResponse.data.token.offlineUntil

    if ([string]::IsNullOrWhiteSpace($renewedToken)) {
        throw "Viewer token renewal succeeded but the renewed token was empty."
    }

    if ($renewedToken -eq $loginToken) {
        throw "Viewer token renewal returned the same token instead of a rotated token."
    }

    $offlineDays =
        ($renewedOfflineUntil.ToUniversalTime() - [DateTime]::UtcNow).TotalDays

    if ($offlineDays -lt 6.9 -or $offlineDays -gt 7.1) {
        throw "Renewed Viewer OfflineUntil is not approximately 7 days. RemainingDays=$offlineDays"
    }

    Write-Host ""
    Write-Host "Viewer token renewal integration test PASSED."
    Write-Host "  Message: $($verifyResponse.message)"
    Write-Host "  StoreCode: $($verifyResponse.data.storeCode)"
    Write-Host "  DeviceCode: $($verifyResponse.data.deviceCode)"
    Write-Host "  Renewed ExpiresAt: $($renewedExpiresAt.ToUniversalTime().ToString('o'))"
    Write-Host "  Renewed OfflineUntil: $($renewedOfflineUntil.ToUniversalTime().ToString('o'))"
}
finally {
    $storePassword = $null
    $securePassword.Dispose()
}
