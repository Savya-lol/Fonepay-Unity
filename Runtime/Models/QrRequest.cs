using System;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// User-facing QR request. Credentials (merchantCode/username/password) and the
    /// HMAC dataValidation are injected by FonepayApiClient at send time.
    /// </summary>
    [Serializable]
    public struct QrRequest
    {
        public float amount;
        public string remarks1;
        public string remarks2;
    }

    [Serializable]
    internal struct QrRequestPayload
    {
        public float amount;
        public string prn;
        public string remarks1;
        public string remarks2;
        public string merchantCode;
        public string dataValidation;
        public string username;
        public string password;
    }
}
