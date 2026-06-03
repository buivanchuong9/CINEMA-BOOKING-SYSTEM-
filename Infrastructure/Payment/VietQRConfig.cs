namespace BE.Infrastructure.Payment;

public class VietQRConfig
{
    public string BankId { get; set; } = "VPB";
    public string AccountNo { get; set; } = "0964578206";
    public string AccountName { get; set; } = "DAO VAN DUONG";
    public string Template { get; set; } = "compact2";
    public string Username { get; set; } = "vietqr_user";
    public string Password { get; set; } = "vietqr_password_123";
}
