namespace BE.Application.DTOs;

public class SeatStatusDto
{
    public int SeatId { get; set; }
    public string Row { get; set; } = string.Empty; // hàng ghế
    public int Number { get; set; }
    public string SeatType { get; set; } = string.Empty; // thường, VIP, đôi
    public string Status { get; set; } = string.Empty; // trống, đã bán, đang có người giữ
    public decimal Price { get; set; } // giá
    public string? HeldByUser { get; set; } // người giữ
}
