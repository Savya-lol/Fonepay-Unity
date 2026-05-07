using System;

namespace Darkmatter.Fonepay
{
    [Serializable]
    public struct QRVerificationStatus
    {
        public bool success;
        public string message;
        public bool qrVerified;
    }
}