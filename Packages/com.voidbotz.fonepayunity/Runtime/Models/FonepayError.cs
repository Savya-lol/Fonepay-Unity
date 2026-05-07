using System;

namespace Darkmatter.Fonepay
{
    public sealed class FonepayError : Exception
    {
        public int ErrorCode { get; }
        public string Docs { get; }

        public FonepayError(int errorCode, string message, string docs)
            : base(message)
        {
            ErrorCode = errorCode;
            Docs = docs ?? throw new ArgumentNullException(nameof(docs));
        }

        public override string ToString()
        {
            return $"{base.ToString()} (ErrorCode: {ErrorCode}, Docs: {Docs})";
        }
    }
}