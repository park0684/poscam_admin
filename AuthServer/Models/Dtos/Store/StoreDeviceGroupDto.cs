namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 상세 화면의 장비 그룹 DTO.
/// 
/// PC캠과 캠뷰어를 분리해서 표시한다.
/// </summary>
public class StoreDeviceGroupDto
{
    public List<StoreDeviceDto> Pccams { get; set; } = new();

    public List<StoreDeviceDto> Viewers { get; set; } = new();
}