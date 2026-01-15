namespace BE.Application.DTOs;

public class SeatStatusDto
{
    public int SeatId { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public string SeatType { get; set; } = string.Empty; // Standard, VIP, Couple
    public string Status { get; set; } = string.Empty; // Available, Held, Sold
    public decimal Price { get; set; }
    public string? HeldByUser { get; set; }
}
