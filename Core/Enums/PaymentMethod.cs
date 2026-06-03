namespace BE.Core.Enums;

/// <summary>
/// Phương thức thanh toán
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Thanh toán tiền mặt tại quầy
    /// </summary>
    Cash = 0,
    
    /// <summary>
    /// Thanh toán qua VNPAY (deprecated)
    /// </summary>
    VNPAY = 1,
    
    /// <summary>
    /// Thanh toán qua MoMo
    /// </summary>
    MoMo = 2,
    
    /// <summary>
    /// Thanh toán qua ZaloPay
    /// </summary>
    ZaloPay = 3,

    /// <summary>
    /// Thanh toán qua VietQR (chuyển khoản ngân hàng)
    /// </summary>
    VietQR = 4
}
