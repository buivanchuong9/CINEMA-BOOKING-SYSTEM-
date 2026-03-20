namespace BE.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; } // ùng để lưu ID duy nhất của request hiện tại, giúp log và debug request dễ dàng hơn

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId); // trả về true nếu RequestId không rỗng
}