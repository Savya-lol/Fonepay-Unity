using System;
using UnityEngine;

namespace Darkmatter.Fonepay
{
    [Serializable]
    public struct QrResult
    {
        public string message;
        public Texture2D qrCode;
        public string status;
        public int statusCode;
        public bool success;
        public string thirdpartyQrWebSocketUrl;
    }


    [Serializable]
    internal struct QrResponse
    {
        public string message;
        public string qrMessage;
        public string status;
        public int statusCode;
        public bool success;
        public string thirdpartyQrWebSocketUrl;
    }
}