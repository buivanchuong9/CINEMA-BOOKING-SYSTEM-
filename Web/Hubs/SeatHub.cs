using Microsoft.AspNetCore.SignalR;

namespace BE.Web.Hubs;

/// <summary>
/// SignalR Hub cho Real-time Seat Updates
/// QUAN TRỌNG: Hub này broadcast seat status changes đến tất cả clients
/// </summary>
public class SeatHub : Hub
{
    private readonly ILogger<SeatHub> _logger; // để ghi log khi có lỗi hoặc thông tin cần theo dõi 

    public SeatHub(ILogger<SeatHub> logger) // DI
    {
        _logger = logger; // gán giá trị cho _logger
    }

    /// <summary>
    /// Client join vào group của showtime cụ thể
    /// Để nhận real-time updates cho showtime đó
    /// </summary>
    public async Task JoinShowtimeGroup(int showtimeId) // nhận id của showtime từ client
    {
        var groupName = GetShowtimeGroupName(showtimeId); 
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName); // thêm client vào group để nhận real-time updates 
        _logger.LogInformation($"Client {Context.ConnectionId} joined showtime {showtimeId} group");
    }

    /// <summary>
    /// Client leave group khi rời khỏi trang seat selection
    /// </summary>
    public async Task LeaveShowtimeGroup(int showtimeId) // rời khỏi group của showtime
    {
        var groupName = GetShowtimeGroupName(showtimeId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName); // xóa client khỏi group 
        _logger.LogInformation($"Client {Context.ConnectionId} left showtime {showtimeId} group"); // log thông tin
    }

    /// <summary>
    /// Notify tất cả clients trong showtime group về ghế đã được chọn
    /// </summary>
    public async Task NotifySeatSelected(int showtimeId, int seatId, string userId)  // thông báo cho các client khác trong group về ghế đã được chọn
    {
        var groupName = GetShowtimeGroupName(showtimeId);
        await Clients.OthersInGroup(groupName).SendAsync("SeatSelected", new // gửi thông báo cho các client khác trong group
        {
            showtimeId,
            seatId,
            userId,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogInformation($"Notified seat {seatId} selected for showtime {showtimeId}");
    }

    /// <summary>
    /// Notify khi ghế được release (hết timeout hoặc user bỏ chọn)
    /// </summary>
    public async Task NotifySeatReleased(int showtimeId, int seatId) // thông báo khi ghế được release (hết timeout hoặc user bỏ chọn)
    {
        var groupName = GetShowtimeGroupName(showtimeId);
        await Clients.Group(groupName).SendAsync("SeatReleased", new
        {
            showtimeId,
            seatId,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogInformation($"Notified seat {seatId} released for showtime {showtimeId}");
    }

    /// <summary>
    /// Notify khi booking hoàn tất (ghế đã sold)
    /// </summary>
    public async Task NotifySeatSold(int showtimeId, List<int> seatIds)
    {
        var groupName = GetShowtimeGroupName(showtimeId);
        await Clients.Group(groupName).SendAsync("SeatsSold", new
        {
            showtimeId,
            seatIds,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogInformation($"Notified {seatIds.Count} seats sold for showtime {showtimeId}");
    }

    /// <summary>
    /// Gửi thông báo lỗi cho client cụ thể
    /// </summary>
    public async Task SendErrorToClient(string connectionId, string errorMessage)
    {
        await Clients.Client(connectionId).SendAsync("Error", new
        {
            message = errorMessage,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Override: Xử lý khi client disconnect
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client {Context.ConnectionId} disconnected");
        await base.OnDisconnectedAsync(exception);
    }

    #region Helper Methods

    private string GetShowtimeGroupName(int showtimeId)
    {
        return $"Showtime_{showtimeId}";
    }

    #endregion
}
