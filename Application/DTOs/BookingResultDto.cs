namespace BE.Application.DTOs;

public class BookingSeatResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<int> SelectedSeatIds { get; set; } = new();
    public DateTime HoldExpiryTime { get; set; }
}

public class CreateBookingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public decimal TotalAmount { get; set; }
}
