namespace BE.Core.Enums;

/// <summary>
/// Trạng thái phim
/// </summary>
public enum MovieStatus
{
    /// <summary>
    /// Phim đang chiếu
    /// </summary>
    NowShowing = 0,
    
    /// <summary>
    /// Phim sắp chiếu
    /// </summary>
    ComingSoon = 1,
    
    /// <summary>
    /// Phim đã ngừng chiếu
    /// </summary>
    Ended = 2
}
