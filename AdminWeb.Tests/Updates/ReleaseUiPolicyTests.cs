using System.ComponentModel.DataAnnotations;
using poscam.AdminWeb.Models.Updates;
using poscam.AdminWeb.Services;
using Xunit;

namespace poscam.AdminWeb.Tests.Updates;

public class ReleaseUiPolicyTests
{
    [Fact]
    public void Draft는_수정삭제와_Artifact관리가_가능하고_Artifact가있을때만_게시할수있다()
    {
        Assert.True(ReleaseUiPolicy.CanEdit(ReleaseStatusCodes.Draft));
        Assert.True(ReleaseUiPolicy.CanDelete(ReleaseStatusCodes.Draft));
        Assert.False(ReleaseUiPolicy.CanPublish(ReleaseStatusCodes.Draft, 0));
        Assert.True(ReleaseUiPolicy.CanPublish(ReleaseStatusCodes.Draft, 1));
        Assert.False(ReleaseUiPolicy.CanDisable(ReleaseStatusCodes.Draft));
        Assert.True(ReleaseUiPolicy.CanManageArtifacts(ReleaseStatusCodes.Draft));
        Assert.False(ReleaseUiPolicy.IsReadOnly(ReleaseStatusCodes.Draft));
    }

    [Fact]
    public void Published는_읽기전용이며_Disable만_가능하다()
    {
        Assert.False(ReleaseUiPolicy.CanEdit(ReleaseStatusCodes.Published));
        Assert.False(ReleaseUiPolicy.CanDelete(ReleaseStatusCodes.Published));
        Assert.False(ReleaseUiPolicy.CanPublish(ReleaseStatusCodes.Published, 1));
        Assert.True(ReleaseUiPolicy.CanDisable(ReleaseStatusCodes.Published));
        Assert.False(ReleaseUiPolicy.CanManageArtifacts(ReleaseStatusCodes.Published));
        Assert.True(ReleaseUiPolicy.IsReadOnly(ReleaseStatusCodes.Published));
    }

    [Fact]
    public void Disabled는_기본정보는_읽기전용이지만_Artifact관리와_재게시가_가능하다()
    {
        Assert.False(ReleaseUiPolicy.CanEdit(ReleaseStatusCodes.Disabled));
        Assert.False(ReleaseUiPolicy.CanDelete(ReleaseStatusCodes.Disabled));
        Assert.False(ReleaseUiPolicy.CanPublish(ReleaseStatusCodes.Disabled, 0));
        Assert.True(ReleaseUiPolicy.CanPublish(ReleaseStatusCodes.Disabled, 1));
        Assert.False(ReleaseUiPolicy.CanDisable(ReleaseStatusCodes.Disabled));
        Assert.True(ReleaseUiPolicy.CanManageArtifacts(ReleaseStatusCodes.Disabled));
        Assert.True(ReleaseUiPolicy.IsReadOnly(ReleaseStatusCodes.Disabled));
    }

    [Fact]
    public void ReleaseEditModel_전체강제와_기준버전을_동시에허용하지않는다()
    {
        var model = CreateValidModel();
        model.IsMandatory = true;
        model.ForceUpdateBelowVersion = "1.0.0";

        var errors = Validate(model);

        Assert.Contains(
            errors,
            error => error.ErrorMessage?.Contains("동시에") == true);
    }

    [Fact]
    public void ReleaseEditModel_기준버전이_릴리스보다높으면_거부한다()
    {
        var model = CreateValidModel();
        model.Version = "1.2.3";
        model.ForceUpdateBelowVersion = "1.2.4";

        var errors = Validate(model);

        Assert.Contains(
            errors,
            error => error.ErrorMessage?.Contains("높을 수 없습니다") == true);
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4")]
    public void ReleaseEditModel_3자리와4자리버전을_허용한다(string version)
    {
        var model = CreateValidModel();
        model.Version = version;

        Assert.Empty(Validate(model));
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("1.2.3.4.5")]
    [InlineData("v1.2.3")]
    [InlineData("1.2.x")]
    public void ReleaseEditModel_잘못된버전형식을_거부한다(string version)
    {
        var model = CreateValidModel();
        model.Version = version;

        Assert.NotEmpty(Validate(model));
    }

    [Fact]
    public void ReleaseEditModel_요청변환시_선택문자열을_정규화한다()
    {
        var model = CreateValidModel();
        model.ProductCode = " PCCAM ";
        model.Version = " 1.2.3 ";
        model.Channel = " stable ";
        model.ReleaseNotes = "  note  ";
        model.InternalMemo = "   ";

        var request = model.ToCreateRequest();

        Assert.Equal("PCCAM", request.ProductCode);
        Assert.Equal("1.2.3", request.Version);
        Assert.Equal("stable", request.Channel);
        Assert.Equal("note", request.ReleaseNotes);
        Assert.Null(request.InternalMemo);
    }

    private static ReleaseEditModel CreateValidModel()
    {
        return new ReleaseEditModel
        {
            ProductCode = "PCCAM",
            Version = "1.2.3",
            Channel = ReleaseChannels.Stable,
            IsMandatory = false
        };
    }

    private static List<ValidationResult> Validate(ReleaseEditModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        return results;
    }
}
