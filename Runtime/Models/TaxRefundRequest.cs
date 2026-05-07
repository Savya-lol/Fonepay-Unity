using System;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// User-facing tax refund request. Credentials and HMAC dataValidation are
    /// injected by FonepayApiClient at send time.
    /// </summary>
    [Serializable]
    public struct TaxRefundRequest
    {
        public string fonepayTraceId;
        public string merchantPRN;
        public string invoiceNumber;
        public DateTime invoiceDate;
        public float transactionAmount;
    }

    [Serializable]
    internal struct TaxRefundRequestPayload
    {
        public string fonepayTraceId;
        public string merchantPRN;
        public string invoiceNumber;
        public string invoiceDate;
        public float transactionAmount;
        public string merchantCode;
        public string dataValidation;
        public string username;
        public string password;
    }
}
