using System;
using UnityEngine;

namespace Darkmatter.Fonepay
{
    [Serializable]
    public struct WebsocketMessage<T>
    {
        public string merchantId;
        public string deviceId;
        public string transactionStatus;
        public T Status => JsonUtility.FromJson<T>(transactionStatus);
    }
}