using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Darkmatter.Fonepay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Darkmatter.FonepayUnity.Tests
{
    public class WebsocketMessagePlayModeTests
    {
        [Test]
        public void Status_ParsesNestedJsonInPlayer()
        {
            const string raw =
                "{\"transactionStatus\":\"{\\\"paymentSuccess\\\":true,\\\"success\\\":true,\\\"amount\\\":7}\"}";

            var envelope = JsonUtility.FromJson<WebsocketMessage<QRPaymentStatus>>(raw);
            var status = envelope.Status;

            Assert.IsTrue(status.paymentSuccess);
            Assert.AreEqual(7f, status.amount);
            Assert.AreEqual(PaymentOutcome.Complete, status.Outcome);
        }

        [Test]
        public void Status_ReparsesEachAccess()
        {
            const string raw =
                "{\"transactionStatus\":\"{\\\"success\\\":true,\\\"paymentSuccess\\\":true}\"}";

            var envelope = JsonUtility.FromJson<WebsocketMessage<QRPaymentStatus>>(raw);
            var a = envelope.Status;
            var b = envelope.Status;

            Assert.AreEqual(a.Outcome, b.Outcome);
            Assert.AreEqual(PaymentOutcome.Complete, a.Outcome);
        }
    }

    public class FonepayClientPlayModeTests
    {
        [SetUp]
        public void Reset() => FonepayConfig.Invalidate();

        [Test]
        public void Ctor_WithoutConfigAsset_Throws()
        {
            // No FonepayConfig resource in test context → Load() throws FonepayError.
            Assert.Throws<FonepayError>(() => new FonepayClient());
        }

        [UnityTest]
        public IEnumerator AwaitPaymentAsync_EmptyUrl_Throws() => RunAsync(async () =>
        {
            var client = TryBuildClient();
            if (client == null) Assert.Pass("Skipped: no FonepayConfig in test context.");

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.AwaitPaymentAsync(string.Empty));
        });

        [UnityTest]
        public IEnumerator AwaitPaymentAsync_PreCancelled_Throws() => RunAsync(async () =>
        {
            var client = TryBuildClient();
            if (client == null) Assert.Pass("Skipped: no FonepayConfig in test context.");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await client.AwaitPaymentAsync("ws://127.0.0.1:1/", ct: cts.Token));
        });

        private static FonepayClient TryBuildClient()
        {
            try { return new FonepayClient(); }
            catch (FonepayError) { return null; }
        }

        private static IEnumerator RunAsync(Func<Task> body)
        {
            var t = body();
            while (!t.IsCompleted) yield return null;
            if (t.IsFaulted) throw t.Exception;
        }
    }
}
