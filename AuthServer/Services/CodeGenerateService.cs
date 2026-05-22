using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Options;

namespace poscam.AuthServer.Services;

/// <summary>
/// 백엔드 코드 생성 서비스.
/// 
/// 매장 ID, 계약번호, 로그번호, 설정 버전 등을 생성한다.
/// DB는 코드를 생성하지 않고, 저장과 중복 방지만 담당한다.
/// </summary>
public class CodeGenerateService
{
    private readonly AuthPolicyOptions _options;

    /// <summary>
    /// 혼동될 수 있는 문자를 제외한 문자셋.
    /// 인증키, 계약번호 난수 등에 사용한다.
    /// </summary>
    private const string AllowedChars = "23456789ABCDEFGHJKMNPRSTUVWXYZ";

    private const int StoreIdNumberMin = 1;
    private const int StoreIdNumberMax = 9999;

    public CodeGenerateService(IOptions<AuthPolicyOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 다음 매장 ID를 생성한다.
    /// 
    /// currentMaxStoreId가 없으면 설정값 StoreIdStartValue를 반환한다.
    /// currentMaxStoreId가 있으면 그 다음 값을 반환한다.
    /// 
    /// 예:
    /// currentMaxStoreId = PC1000 → PC1001
    /// currentMaxStoreId = PC9999 → PD0001
    /// </summary>
    public string CreateNextStoreId(string? currentMaxStoreId)
    {
        var startValue = NormalizeStoreId(_options.StoreIdStartValue);

        if (!IsValidStoreId(startValue))
        {
            throw new InvalidOperationException(
                $"매장 ID 시작값이 올바르지 않습니다. StoreIdStartValue={_options.StoreIdStartValue}");
        }

        if (string.IsNullOrWhiteSpace(currentMaxStoreId))
        {
            return startValue;
        }

        var current = NormalizeStoreId(currentMaxStoreId);

        if (!IsValidStoreId(current))
        {
            return startValue;
        }

        // 현재 DB의 최대 ID가 시작값보다 작으면 시작값부터 사용한다.
        if (CompareStoreId(current, startValue) < 0)
        {
            return startValue;
        }

        return IncrementStoreId(current);
    }

    /// <summary>
    /// 매장 ID를 1 증가시킨다.
    /// 
    /// 예:
    /// PC1000 → PC1001
    /// PC9999 → PD0001
    /// PZ9999 → QA0001
    /// </summary>
    public string IncrementStoreId(string storeId)
    {
        storeId = NormalizeStoreId(storeId);

        if (!IsValidStoreId(storeId))
        {
            throw new ArgumentException("매장 ID 형식이 올바르지 않습니다.", nameof(storeId));
        }

        var first = storeId[0];
        var second = storeId[1];
        var number = int.Parse(storeId.Substring(2, 4));

        if (number < StoreIdNumberMax)
        {
            return $"{first}{second}{number + 1:D4}";
        }

        number = StoreIdNumberMin;

        if (second < 'Z')
        {
            second++;
            return $"{first}{second}{number:D4}";
        }

        if (first < 'Z')
        {
            first++;
            second = 'A';
            return $"{first}{second}{number:D4}";
        }

        throw new InvalidOperationException("생성 가능한 매장 ID 범위를 초과했습니다.");
    }

    /// <summary>
    /// 계약번호를 생성한다.
    /// 
    /// 계약은 파트너사 기준으로 관리되므로
    /// 계약번호 역시 파트너사 코드를 기준값으로 사용한다.
    /// 
    /// 구성:
    /// CT + 계약유형 + 날짜 + 파트너코드 + 난수
    /// 
    /// 예:
    /// CTT26051500008A8K2
    /// CTP26051500008M7R9
    /// CTS26051500008W3X4
    /// </summary>
    public string CreateContractNo(
        ContractType contractType,
        int partnerCode)
    {
        var typeCode = contractType switch
        {
            ContractType.Trial => "T",
            ContractType.Purchase => "P",
            ContractType.Subscription => "S",
            _ => "X"
        };

        var datePart = DateTime.Now.ToString("yyMMdd");
        var randomPart = CreateRandomText(4);

        return $"CT{typeCode}{datePart}{partnerCode:D5}{randomPart}";
    }

    /// <summary>
    /// 라이선스 로그 코드를 생성한다.
    /// 
    /// licenselog.lig_code가 VARCHAR(20)이므로 20자 이하로 생성한다.
    /// </summary>
    public string CreateLicenseLogCode()
    {
        var timePart = DateTime.UtcNow.ToString("yyMMddHHmmssfff");
        var randomPart = RandomNumberGenerator.GetInt32(100, 999).ToString();

        return $"L{timePart}{randomPart}";
    }

    /// <summary>
    /// 설정 버전 문자열을 생성한다.
    /// </summary>
    public string CreateConfigVersion()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }

    /// <summary>
    /// 매장 ID 형식을 정규화한다.
    /// </summary>
    private static string NormalizeStoreId(string storeId)
    {
        return storeId.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// 매장 ID 형식을 검증한다.
    /// 
    /// 기준:
    /// - 전체 6자리
    /// - 앞 2자리 영문 A~Z
    /// - 뒤 4자리 숫자
    /// - 숫자 0000은 사용하지 않음
    /// </summary>
    private static bool IsValidStoreId(string storeId)
    {
        if (storeId.Length != 6)
        {
            return false;
        }

        if (storeId[0] < 'A' || storeId[0] > 'Z')
        {
            return false;
        }

        if (storeId[1] < 'A' || storeId[1] > 'Z')
        {
            return false;
        }

        if (!int.TryParse(storeId.Substring(2, 4), out var number))
        {
            return false;
        }

        return number >= StoreIdNumberMin && number <= StoreIdNumberMax;
    }

    /// <summary>
    /// 매장 ID를 비교한다.
    /// 
    /// 영문 2자리 + 숫자 4자리를 하나의 증가값처럼 비교한다.
    /// </summary>
    private static int CompareStoreId(string left, string right)
    {
        var leftValue = ConvertStoreIdToOrderValue(left);
        var rightValue = ConvertStoreIdToOrderValue(right);

        return leftValue.CompareTo(rightValue);
    }

    /// <summary>
    /// 매장 ID를 비교 가능한 숫자 값으로 변환한다.
    /// 
    /// AA0001 = 가장 작은 값
    /// ZZ9999 = 가장 큰 값
    /// </summary>
    private static long ConvertStoreIdToOrderValue(string storeId)
    {
        var first = storeId[0] - 'A';
        var second = storeId[1] - 'A';
        var number = int.Parse(storeId.Substring(2, 4));

        var prefixIndex = first * 26 + second;

        return prefixIndex * StoreIdNumberMax + (number - StoreIdNumberMin);
    }

    /// <summary>
    /// 지정된 길이의 난수 문자열을 생성한다.
    /// </summary>
    private static string CreateRandomText(int length)
    {
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(AllowedChars.Length);
            result.Append(AllowedChars[index]);
        }

        return result.ToString();
    }


}