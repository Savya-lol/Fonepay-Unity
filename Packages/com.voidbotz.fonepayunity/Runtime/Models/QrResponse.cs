using System;

namespace Darkmatter.Fonepay
{
    [Serializable]
    public struct QrResponse
    {
        public string message;
        public string qrMessage;
        public string status;
        public int statusCode;
        public bool success;
        public string thirdpartyQrWebSocketUrl;
    }
}