namespace BE.Application.DTOs;

public class CreateBookingDto
{
    public int ShowtimeId { get; set; }
    public List<int> SeatIds { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public int? VoucherId { get; set; }
    public List<FoodItemDto>? Foods { get; set; }
    public string? Notes { get; set; }
}

public class FoodItemDto
{
    public int FoodId { get; set; }
    public int Quantity { get; set; }
}
