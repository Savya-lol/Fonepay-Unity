using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// Public facade. Auto-loads config + secrets via <see cref="FonepayConfig.Load"/>.
    /// Caller never touches credentials.
    /// </summary>
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

        /// <summary>
        /// Request a new QR code. The returned URL is valid for 15 minutes. Call GetStatusAsync() to check if the QR code has been paid.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<QrResult> PurchaseAsync(QrRequest req, CancellationToken ct = default)
            => _api.PostQRAsync(req, ct);

        /// <summary>
        /// Check if a QR code has been paid. Call after PostQRAsync() to check if the QR code has been paid. Returns "PAID" if successful, "UNPAID" if not yet paid, or an error message if the PRN is invalid or expired.
        /// </summary>
        /// <param name="prn"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<QrResult> GetStatusAsync(string prn, CancellationToken ct = default)
            => _api.GetStatusAsync(prn, ct);

        /// <summary>
        /// Request a tax refund. Returns "REFUND_SUCCESS" if successful, or an error message if the PRN is invalid, expired, or not eligible for refund.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<TaxRefundResponse> PostTaxRefundAsync(TaxRefundRequest req, CancellationToken ct = default)
            => _api.PostTaxRefundAsync(req, ct);

        /// <summary>
        /// Connects to the QR websocket and awaits the terminal payment frame, then auto-disconnects.
        /// Pass <see cref="QrResult.thirdpartyQrWebSocketUrl"/> from <see cref="PurchaseAsync"/>.
        /// </summary>
        public async Task<QRPaymentStatus> AwaitPaymentAsync(
            string websocketUrl,
            Action<bool> onQrVerified = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(websocketUrl))
                throw new ArgumentException("Websocket URL required", nameof(websocketUrl));

            using var ws = new FonepayWebsocketClient();
            var tcs = new TaskCompletionSource<QRPaymentStatus>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (onQrVerified != null)
                ws.OnQrVerified += onQrVerified;

            ws.OnPaymentReceived += msg => tcs.TrySetResult(msg.Status);

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