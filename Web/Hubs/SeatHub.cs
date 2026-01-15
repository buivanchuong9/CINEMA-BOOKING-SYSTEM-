using Microsoft.AspNetCore.SignalR;

namespace BE.Web.Hubs;

/// <summary>
/// SignalR Hub cho Real-time Seat Updates
/// QUAN TRỌNG: Hub này broadcast seat status changes đến tất cả clients
/// </summary>
public class SeatHub : Hub
{
    private readonly ILogger<SeatHub> _logger;

    public SeatHub(ILogger<SeatHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client join vào group của showtime cụ thể
    /// Để nhận real-time updates cho showtime đó
    /// </summary>
    public async Task JoinShowtimeGroup(int showtimeId)
    {
        var groupName = GetShowtimeGroupName(showtimeId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} joined showtime {showtimeId} group");
    }

    /// <summary>
    /// Client leave group khi rời khỏi trang seat selection
    /// </summary>
    public async Task LeaveShowtimeGroup(int showtimeId)
    {
        var groupName = GetShowtimeGroupName(showtimeId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} left showtime {showtimeId} group");
    }

    /// <summary>
    /// Notify tất cả clients trong showtime group về ghế đã được chọn
    /// </summary>
    public async Task NotifySeatSelected(int showtimeId, int seatId, string userId)
    {
        var groupName = GetShowtimeGroupName(showtimeId);
        await Clients.OthersInGroup(groupName).SendAsync("SeatSelected", new
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
    public async Task NotifySeatReleased(int showtimeId, int seatId)
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
