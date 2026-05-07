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
        private T _status;
        public T Status => _status ??= JsonUtility.FromJson<T>(transactionStatus);
    }
}