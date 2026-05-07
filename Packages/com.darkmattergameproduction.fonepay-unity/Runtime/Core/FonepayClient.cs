using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darkmatter.Fonepay
{
    public sealed class FonepayClient
    {
        private readonly FonepayApiClient _api;

        public FonepayClient() : this(FonepayConfig.Load())
        {
        }

        public FonepayClient(FonepayConfigSO config)
        {
            var signer = new HmacSha512Signer(config.GetSecretKey());
            _api = new FonepayApiClient(config, signer);
        }

        public FonepayAsync<QrResult> PurchaseAsync(QrRequest req, CancellationToken ct = default)
            => FonepayAsyncBridge.Wrap(_api.PostQRAsync(req, ct), ct);

        public FonepayAsync<QrResult> GetStatusAsync(string prn, CancellationToken ct = default)
            => FonepayAsyncBridge.Wrap(_api.GetStatusAsync(prn, ct), ct);

        public FonepayAsync<TaxRefundResponse> PostTaxRefundAsync(TaxRefundRequest req, CancellationToken ct = default)
            => FonepayAsyncBridge.Wrap(_api.PostTaxRefundAsync(req, ct), ct);

        public FonepayAsync<QRPaymentStatus> AwaitPaymentAsync(
            string websocketUrl,
            Action<bool> onQrVerified = null,
            CancellationToken ct = default)
            => FonepayAsyncBridge.Wrap(AwaitPaymentInternalAsync(websocketUrl, onQrVerified, ct), ct);

        private async Task<QRPaymentStatus> AwaitPaymentInternalAsync(
            string websocketUrl,
            Action<bool> onQrVerified,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(websocketUrl))
                throw new ArgumentException("Websocket URL required", nameof(websocketUrl));

            using var ws = new FonepayWebsocketClient();
            var tcs = new TaskCompletionSource<QRPaymentStatus>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (onQrVerified != null)
                ws.OnQrVerified += onQrVerified;

            ws.OnPaymentReceived += msg => tcs.TrySetResult(msg.Status);
            ws.OnClosed += err =>
            {
                if (err != null)
                    tcs.TrySetException(err);
                else
                    tcs.TrySetException(new InvalidOperationException(
                        "Fonepay websocket closed before payment frame received."));
            };

            using var ctReg = ct.Register(() => tcs.TrySetCanceled(ct));

            try
            {
                await ws.ConnectAsync(websocketUrl, ct);
                return await tcs.Task;
            }
            finally
            {
                await ws.DisconnectAsync();
            }
        }
    }
}