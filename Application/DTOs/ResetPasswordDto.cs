using System.ComponentModel.DataAnnotations;

namespace BE.Application.DTOs;

public class ResetPasswordDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
