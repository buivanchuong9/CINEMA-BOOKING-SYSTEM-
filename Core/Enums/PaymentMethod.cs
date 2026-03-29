namespace BE.Core.Enums;

/// <summary>
/// Phương thức thanh toán
/// </summary>
public enum PaymentMethod // 0: Thanh toán tiền mặt tại quầy, 1: Thanh toán qua VNPAY, 2: Thanh toán qua MoMo, 3: Thanh toán qua ZaloPay
{
    /// <summary>
    /// Thanh toán tiền mặt tại quầy
    /// </summary>
    Cash = 0,
    
    /// <summary>
    /// Thanh toán qua VNPAY
    /// </summary>
    VNPAY = 1,
    
    /// <summary>
    /// Thanh toán qua MoMo
    /// </summary>
    MoMo = 2,
    
    /// <summary>
    /// Thanh toán qua ZaloPay
    /// </summary>
    ZaloPay = 3
}
