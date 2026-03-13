namespace BE.Application.DTOs;

public class BookingSeatResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty; // validate khi có người đặt trước
    public List<int> SelectedSeatIds { get; set; } = new(); // danh sách ghế nếu rỗng khởi tạo
    public DateTime HoldExpiryTime { get; set; } 
}

public class CreateBookingResult // in vé
{
    public bool Success { get; set; } // check thanh toán
    public string Message { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public decimal TotalAmount { get; set; }
}
