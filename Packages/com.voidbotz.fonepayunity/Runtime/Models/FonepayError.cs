using System;

namespace Darkmatter.Fonepay
{
    public sealed class FonepayError : Exception
    {
        public int ErrorCode { get; }

        public FonepayError(int errorCode, string message,
            string docs)
        {
            ErrorCode = errorCode;
        }
    }
}