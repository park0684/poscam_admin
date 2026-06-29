namespace poscam.AdminWeb.Models.Updates;

/// <summary>
/// JavaScript 직접 업로드 시작 옵션.
/// File 객체와 파일 바이트는 이 DTO에 포함하지 않는다.
/// </summary>
public sealed class ArtifactUploadStartOptions
{
    public string UploadKey { get; set; } = "";

    public string InputElementId { get; set; } = "";

    public string Url { get; set; } = "";

    public string Token { get; set; } = "";

    public string RequestId { get; set; } = "";

    public string Os { get; set; } = "";

    public string Architecture { get; set; } = "";

    public string PackageType { get; set; } = "";
}

/// <summary>
/// JavaScript가 업로드 시작 여부를 즉시 반환하는 결과.
/// </summary>
public sealed class ArtifactUploadStartResult
{
    public bool Started { get; set; }

    public string Message { get; set; } = "";
}

/// <summary>
/// 선택된 browser File의 표시용 메타데이터.
/// 파일 내용은 .NET으로 전달하지 않는다.
/// </summary>
public sealed class ArtifactSelectedFileInfo
{
    public bool HasFile { get; set; }

    public string Name { get; set; } = "";

    public long Size { get; set; }

    public string Type { get; set; } = "";
}

public sealed class ArtifactUploadProgress
{
    public long Loaded { get; set; }

    public long Total { get; set; }

    public int Percent { get; set; }
}

/// <summary>
/// XHR 완료 결과. JSON 응답에서 필요한 안전한 값만 전달한다.
/// 비JSON 응답 원문이나 HTML은 포함하지 않는다.
/// </summary>
public sealed class ArtifactUploadResult
{
    public bool Success { get; set; }

    public bool Cancelled { get; set; }

    public bool NetworkError { get; set; }

    public int HttpStatus { get; set; }

    public int ErrorCode { get; set; }

    public string Message { get; set; } = "";

    public string? RequestId { get; set; }

    public long? ArtifactCode { get; set; }

    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    public string? Sha256 { get; set; }

    public bool Replaced { get; set; }
}

/// <summary>
/// 직접 업로드 URL·Target·오류 메시지 정책.
/// Component와 자동 테스트가 동일한 규칙을 사용한다.
/// </summary>
public static class ArtifactUploadUiPolicy
{
    public const long DefaultMaximumFileSize = 1_073_741_824L;

    public static string BuildUploadUrl(
        string publicBaseUrl,
        long releaseCode)
    {
        if (releaseCode <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseCode));
        }

        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp
                && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "PublicBaseUrl은 유효한 HTTP 또는 HTTPS 절대 URL이어야 합니다.",
                nameof(publicBaseUrl));
        }

        return $"{publicBaseUrl.TrimEnd('/')}/api/v1/admin/releases/{releaseCode}/artifacts";
    }

    public static bool HasActiveTarget(
        IEnumerable<ReleaseArtifactSummaryResponse>? artifacts,
        string os,
        string architecture,
        string packageType)
    {
        if (artifacts is null)
        {
            return false;
        }

        return artifacts.Any(artifact =>
            artifact.Status == 1
            && string.Equals(artifact.Os, os, StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                artifact.Architecture,
                architecture,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                artifact.PackageType,
                packageType,
                StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsUnauthorized(ArtifactUploadResult result)
    {
        return result.HttpStatus == 401
               || result.ErrorCode is 5001 or 5003 or 5004;
    }

    public static bool IsForbidden(ArtifactUploadResult result)
    {
        return result.HttpStatus == 403
               || result.ErrorCode == 7001;
    }

    public static string GetFailureMessage(ArtifactUploadResult result)
    {
        if (result.Cancelled)
        {
            return "업로드를 취소했습니다.";
        }

        if (result.NetworkError)
        {
            return "네트워크 오류로 업로드하지 못했습니다.";
        }

        if (IsUnauthorized(result))
        {
            return "로그인이 만료되었거나 유효하지 않습니다.";
        }

        if (IsForbidden(result))
        {
            return "업데이트 관리 권한이 없습니다.";
        }

        return (result.HttpStatus, result.ErrorCode) switch
        {
            (409, _) or (_, 8022) =>
                "동일한 Target의 Artifact가 다른 작업으로 변경되었습니다. 상세정보를 다시 확인해 주세요.",
            (413, _) or (_, 8031) =>
                "업로드 파일이 허용된 최대 크기를 초과했습니다.",
            (415, _) or (_, 8030) =>
                "유효한 ZIP 패키지가 아닙니다.",
            (503, _) or (_, 9003) =>
                "관리자 권한 확인 서비스를 사용할 수 없습니다. 잠시 후 다시 시도해 주세요.",
            (500, _) or (_, 8032) or (_, 9999) =>
                "Artifact 업로드 처리 중 서버 오류가 발생했습니다.",
            _ when !string.IsNullOrWhiteSpace(result.Message) =>
                result.Message.Trim(),
            _ => "Artifact를 업로드하지 못했습니다."
        };
    }
}
