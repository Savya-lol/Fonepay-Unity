using Darkmatter.Fonepay;
using NUnit.Framework;
using UnityEngine;

namespace Darkmatter.FonepayUnity.Editor.Tests
{
    public class WebsocketMessageTests
    {
        [Test]
        public void Status_ParsesNestedJsonString()
        {
            const string raw =
                "{\"merchantId\":\"M1\",\"deviceId\":\"D1\"," +
                "\"transactionStatus\":\"{\\\"paymentSuccess\\\":true,\\\"success\\\":true,\\\"amount\\\":42.5}\"}";

            var envelope = JsonUtility.FromJson<WebsocketMessage<QRPaymentStatus>>(raw);
            var status = envelope.Status;

            Assert.IsTrue(status.success);
            Assert.IsTrue(status.paymentSuccess);
            Assert.AreEqual(42.5f, status.amount);
            Assert.AreEqual(PaymentOutcome.Complete, status.Outcome);
        }

        [Test]
        public void Status_QrVerifiedFrame()
        {
            const string raw =
                "{\"transactionStatus\":\"{\\\"success\\\":true,\\\"qrVerified\\\":true,\\\"message\\\":\\\"ok\\\"}\"}";

            var envelope = JsonUtility.FromJson<WebsocketMessage<QRVerificationStatus>>(raw);
            var v = envelope.Status;

            Assert.IsTrue(v.qrVerified);
            Assert.AreEqual("ok", v.message);
        }
    }

    public class PaymentOutcomeTests
    {
        [Test]
        public void SuccessTrue_PaymentTrue_Complete()
        {
            var s = new QRPaymentStatus { success = true, paymentSuccess = true };
            Assert.AreEqual(PaymentOutcome.Complete, s.Outcome);
        }

        [Test]
        public void SuccessTrue_PaymentFalse_CancelledByUser()
        {
            var s = new QRPaymentStatus { success = true, paymentSuccess = false };
            Assert.AreEqual(PaymentOutcome.CancelledByUser, s.Outcome);
        }

        [Test]
        public void SuccessFalse_Failed()
        {
            var s = new QRPaymentStatus { success = false, paymentSuccess = true };
            Assert.AreEqual(PaymentOutcome.Failed, s.Outcome);

            s = new QRPaymentStatus { success = false, paymentSuccess = false };
            Assert.AreEqual(PaymentOutcome.Failed, s.Outcome);
        }
    }

    public class FonepayErrorTests
    {
        [Test]
        public void Carries_CodeAndDocs()
        {
            var err = new FonepayError(42, "boom", "docs/url");
            Assert.AreEqual(42, err.ErrorCode);
            Assert.AreEqual("docs/url", err.Docs);
            Assert.AreEqual("boom", err.Message);
            StringAssert.Contains("ErrorCode: 42", err.ToString());
        }
    }
}
