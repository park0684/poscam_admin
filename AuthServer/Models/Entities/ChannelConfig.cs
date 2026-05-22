namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// POS 번호와 NVR 채널 매핑 Entity.
/// DB 테이블: ch_config
/// 
/// 캠뷰어에서 POS 화면과 CCTV 영상을 함께 보여주기 위해 사용한다.
/// </summary>
public class ChannelConfig
{
    /// <summary>
    /// 매장 코드.
    /// DB 컬럼: chn_store
    /// </summary>
    public int ChnStore { get; set; }

    /// <summary>
    /// POS 번호.
    /// 예: 1번 계산대, 2번 계산대
    /// DB 컬럼: chn_pos
    /// </summary>
    public int ChnPos { get; set; }

    /// <summary>
    /// NVR 채널 번호.
    /// DB 컬럼명이 chn_ch 또는 chn_channel인지
    /// 실제 스키마 기준으로 Repository 작성 시 확정한다.
    /// </summary>
    public int ChnCh { get; set; }

    /// <summary>
    /// 화면 위치.
    /// 예: 0=좌측, 1=우측
    /// DB 컬럼: chn_screen
    /// </summary>
    public int ChnScreen { get; set; }

    /// <summary>
    /// 채널 설정 등록일.
    /// DB 컬럼: chn_rdate
    /// </summary>
    public DateTime? ChnRDate { get; set; }

    /// <summary>
    /// 채널 설정 수정일.
    /// DB 컬럼: chn_udate
    /// </summary>
    public DateTime? ChnUDate { get; set; }
}