using System.ComponentModel.DataAnnotations;

namespace BE.Application.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;
}
