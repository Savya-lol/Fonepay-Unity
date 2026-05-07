using System;

namespace Darkmatter.Fonepay
{
    [Serializable]
    public struct QRPaymentStatus
    {
        public string remarks1;
        public string remarks2;
        public DateTime transactionDate;
        public string productNumber;
        public float amount;
        public string message;
        public bool success;
        public string commissionType;
        public float commissionAmount;
        public float totalCalculatedAmount;
        public bool paymentSuccess;
        
        public PaymentOutcome Outcome => (success, paymentSuccess) switch
        {
            (true, true) => PaymentOutcome.Complete,
            (true, false) => PaymentOutcome.CancelledByUser,
            (false, _) => PaymentOutcome.Failed,
        };
    }
}