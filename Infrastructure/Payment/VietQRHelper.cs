using System;

namespace BE.Infrastructure.Payment;

public class VietQRHelper
{
    private readonly VietQRConfig _config;

    public VietQRHelper(VietQRConfig config)
    {
        _config = config;
    }

    public string GenerateQRUrl(decimal amount, string bookingId)
    {
        // Format description as "DATVE" followed by bookingId, e.g. "DATVE123"
        string addInfo = $"DATVE{bookingId}";
        
        string accountNameEncoded = Uri.EscapeDataString(_config.AccountName);
        string addInfoEncoded = Uri.EscapeDataString(addInfo);
        
        // Dynamic QR URL using vietqr.io API
        // Format: https://img.vietqr.io/image/<BANK_ID>-<ACCOUNT_NO>-<TEMPLATE>.png?amount=<AMOUNT>&addInfo=<DESCRIPTION>&accountName=<ACCOUNT_NAME>
        return $"https://img.vietqr.io/image/{_config.BankId}-{_config.AccountNo}-{_config.Template}.png?amount={Convert.ToInt64(amount)}&addInfo={addInfoEncoded}&accountName={accountNameEncoded}";
    }

    public VietQRConfig Config => _config;
}
