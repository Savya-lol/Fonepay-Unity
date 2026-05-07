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
        public Task<QrResponse> PostQRAsync(QrRequest req, CancellationToken ct = default)
            => _api.PostQRAsync(req, ct);

        /// <summary>
        /// Check if a QR code has been paid. Call after PostQRAsync() to check if the QR code has been paid. Returns "PAID" if successful, "UNPAID" if not yet paid, or an error message if the PRN is invalid or expired.
        /// </summary>
        /// <param name="prn"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<QrResponse> GetStatusAsync(string prn, CancellationToken ct = default)
            => _api.GetStatusAsync(prn, ct);

        /// <summary>
        /// Request a tax refund. Returns "REFUND_SUCCESS" if successful, or an error message if the PRN is invalid, expired, or not eligible for refund.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<TaxRefundResponse> PostTaxRefundAsync(TaxRefundRequest req, CancellationToken ct = default)
            => _api.PostTaxRefundAsync(req, ct);
    }
}