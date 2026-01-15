using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BE.Infrastructure.Payment;

public class VNPayHelper
{
    private readonly VNPayConfig _config;

    public VNPayHelper(VNPayConfig config)
    {
        _config = config;
    }

    public string CreatePaymentUrl(string orderId, decimal amount, string orderInfo, string ipAddress)
    {
        var vnpay = new VNPayLibrary();
        
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", _config.TmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", ipAddress);
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", _config.ReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", orderId);

        string paymentUrl = vnpay.CreateRequestUrl(_config.BaseUrl, _config.HashSecret);
        return paymentUrl;
    }

    public VNPayResponse ProcessReturn(IDictionary<string, string> queryParams)
    {
        var vnpay = new VNPayLibrary();
        
        foreach (var param in queryParams)
        {
            if (!string.IsNullOrEmpty(param.Key) && param.Key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(param.Key, param.Value);
            }
        }

        long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
        long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
        string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        string vnp_SecureHash = queryParams["vnp_SecureHash"];
        
        bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config.HashSecret);

        return new VNPayResponse
        {
            Success = checkSignature && vnp_ResponseCode == "00",
            OrderId = orderId,
            TransactionId = vnpayTranId,
            ResponseCode = vnp_ResponseCode,
            SecureHashValid = checkSignature
        };
    }
}

public class VNPayResponse
{
    public bool Success { get; set; }
    public long OrderId { get; set; }
    public long TransactionId { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public bool SecureHashValid { get; set; }
}

public class VNPayLibrary
{
    private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VNPayCompare());
    private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VNPayCompare());

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _requestData.Add(key, value);
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _responseData.Add(key, value);
        }
    }

    public string GetResponseData(string key)
    {
        return _responseData.TryGetValue(key, out string? value) ? value : string.Empty;
    }

    public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
    {
        StringBuilder data = new StringBuilder();
        
        foreach (KeyValuePair<string, string> kv in _requestData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        string queryString = data.ToString();
        
        if (queryString.Length > 0)
        {
            queryString = queryString.Remove(queryString.Length - 1, 1);
        }

        string signData = queryString;
        string vnpSecureHash = HmacSHA512(vnp_HashSecret, signData);
        
        return baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnpSecureHash;
    }

    public bool ValidateSignature(string inputHash, string secretKey)
    {
        StringBuilder rawData = new StringBuilder();
        
        foreach (KeyValuePair<string, string> kv in _responseData)
        {
            if (!string.IsNullOrEmpty(kv.Value) && kv.Key != "vnp_SecureHash")
            {
                rawData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        if (rawData.Length > 0)
        {
            rawData.Remove(rawData.Length - 1, 1);
        }

        string myChecksum = HmacSHA512(secretKey, rawData.ToString());
        
        return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }

        return hash.ToString();
    }
}

public class VNPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}
