using System.ComponentModel.DataAnnotations;

namespace poscam.AdminWeb.Models.Updates;

public static class ReleaseStatusCodes
{
    public const int Draft = 0;
    public const int Published = 1;
    public const int Disabled = 9;
}

public static class ReleaseChannels
{
    public const string Stable = "stable";
    public const string Beta = "beta";
    public const string Internal = "internal";

    public static readonly string[] All =
    {
        Stable,
        Beta,
        Internal
    };
}

public sealed class ActiveProductResponse
{
    public string ProductCode { get; set; } = "";

    public string ProductName { get; set; } = "";

    public string? ProductDescription { get; set; }
}

public sealed class ReleaseListRequest
{
    public string? ProductCode { get; set; }

    public string? Channel { get; set; }

    public int? Status { get; set; }

    public string? Keyword { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed class CreateReleaseRequest
{
    public string? ProductCode { get; set; }

    public string? Version { get; set; }

    public string? Channel { get; set; }

    public bool IsMandatory { get; set; }

    public string? ForceUpdateBelowVersion { get; set; }

    public string? ReleaseNotes { get; set; }

    public string? InternalMemo { get; set; }
}

public sealed class UpdateReleaseRequest
{
    public string? ProductCode { get; set; }

    public string? Version { get; set; }

    public string? Channel { get; set; }

    public bool IsMandatory { get; set; }

    public string? ForceUpdateBelowVersion { get; set; }

    public string? ReleaseNotes { get; set; }

    public string? InternalMemo { get; set; }
}

public sealed class ReleaseListItemResponse
{
    public long ReleaseCode { get; set; }

    public string ProductCode { get; set; } = "";

    public string Version { get; set; } = "";

    public string Channel { get; set; } = "";

    public bool IsMandatory { get; set; }

    public string? ForceUpdateBelowVersion { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = "";

    public DateTime? PublishedAt { get; set; }

    public int? CreatedByUserCode { get; set; }

    public string? CreatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public sealed class ReleaseArtifactSummaryResponse
{
    public long ArtifactCode { get; set; }

    public string PublicId { get; set; } = "";

    public string Os { get; set; } = "";

    public string Architecture { get; set; } = "";

    public string PackageType { get; set; } = "";

    public string FileName { get; set; } = "";

    public long FileSize { get; set; }

    public string Sha256 { get; set; } = "";

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class ReleaseDetailResponse
{
    public long ReleaseCode { get; set; }

    public string ProductCode { get; set; } = "";

    public string Version { get; set; } = "";

    public string Channel { get; set; } = "";

    public bool IsMandatory { get; set; }

    public string? ForceUpdateBelowVersion { get; set; }

    public string? ReleaseNotes { get; set; }

    public string? InternalMemo { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = "";

    public DateTime? PublishedAt { get; set; }

    public int? CreatedByUserCode { get; set; }

    public string? CreatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<ReleaseArtifactSummaryResponse> Artifacts { get; set; } = new();
}

public sealed class DeleteReleaseResponse
{
    public long ReleaseCode { get; set; }
}

public sealed class ReleaseLifecycleResponse
{
    public long ReleaseCode { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = "";

    public DateTime? PublishedAt { get; set; }
}

/// <summary>
/// 릴리스 생성·Draft 수정 화면에서 사용하는 입력 모델.
/// </summary>
public sealed class ReleaseEditModel : IValidatableObject
{
    [Required(ErrorMessage = "제품을 선택해 주세요.")]
    public string ProductCode { get; set; } = "";

    [Required(ErrorMessage = "릴리스 버전을 입력해 주세요.")]
    [RegularExpression(
        @"^\d+\.\d+\.\d+(?:\.\d+)?$",
        ErrorMessage = "버전은 1.2.3 또는 1.2.3.4 형식이어야 합니다.")]
    public string Version { get; set; } = "";

    [Required(ErrorMessage = "업데이트 채널을 선택해 주세요.")]
    public string Channel { get; set; } = ReleaseChannels.Stable;

    public bool IsMandatory { get; set; }

    [RegularExpression(
        @"^$|^\d+\.\d+\.\d+(?:\.\d+)?$",
        ErrorMessage = "기준 버전은 1.2.3 또는 1.2.3.4 형식이어야 합니다.")]
    public string? ForceUpdateBelowVersion { get; set; }

    public string? ReleaseNotes { get; set; }

    public string? InternalMemo { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var threshold = NormalizeOptional(ForceUpdateBelowVersion);

        if (IsMandatory && threshold is not null)
        {
            yield return new ValidationResult(
                "전체 강제 업데이트와 기준 버전 미만 강제를 동시에 설정할 수 없습니다.",
                new[]
                {
                    nameof(IsMandatory),
                    nameof(ForceUpdateBelowVersion)
                });
        }

        if (threshold is null
            || !TryParseComparableVersion(Version, out var releaseVersion)
            || !TryParseComparableVersion(threshold, out var thresholdVersion))
        {
            yield break;
        }

        if (thresholdVersion > releaseVersion)
        {
            yield return new ValidationResult(
                "강제 업데이트 기준 버전은 릴리스 버전보다 높을 수 없습니다.",
                new[] { nameof(ForceUpdateBelowVersion) });
        }
    }

    public CreateReleaseRequest ToCreateRequest()
    {
        return new CreateReleaseRequest
        {
            ProductCode = ProductCode.Trim(),
            Version = Version.Trim(),
            Channel = Channel.Trim(),
            IsMandatory = IsMandatory,
            ForceUpdateBelowVersion = NormalizeOptional(
                ForceUpdateBelowVersion),
            ReleaseNotes = NormalizeOptional(ReleaseNotes),
            InternalMemo = NormalizeOptional(InternalMemo)
        };
    }

    public UpdateReleaseRequest ToUpdateRequest()
    {
        return new UpdateReleaseRequest
        {
            ProductCode = ProductCode.Trim(),
            Version = Version.Trim(),
            Channel = Channel.Trim(),
            IsMandatory = IsMandatory,
            ForceUpdateBelowVersion = NormalizeOptional(
                ForceUpdateBelowVersion),
            ReleaseNotes = NormalizeOptional(ReleaseNotes),
            InternalMemo = NormalizeOptional(InternalMemo)
        };
    }

    public static ReleaseEditModel FromDetail(ReleaseDetailResponse detail)
    {
        return new ReleaseEditModel
        {
            ProductCode = detail.ProductCode,
            Version = detail.Version,
            Channel = detail.Channel,
            IsMandatory = detail.IsMandatory,
            ForceUpdateBelowVersion = detail.ForceUpdateBelowVersion,
            ReleaseNotes = detail.ReleaseNotes,
            InternalMemo = detail.InternalMemo
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool TryParseComparableVersion(
        string? value,
        out Version version)
    {
        version = new Version();

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split('.');
        if (parts.Length is not (3 or 4)
            || parts.Any(part =>
                !int.TryParse(part, out var number) || number < 0))
        {
            return false;
        }

        var numbers = parts.Select(int.Parse).ToArray();
        version = numbers.Length == 3
            ? new Version(numbers[0], numbers[1], numbers[2], 0)
            : new Version(numbers[0], numbers[1], numbers[2], numbers[3]);

        return true;
    }
}
