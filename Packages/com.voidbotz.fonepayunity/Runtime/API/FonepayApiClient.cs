using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// HTTP layer. Auto-injects merchantCode/username/password from FonepayConfigSO and
    /// computes the HMAC SHA-512 dataValidation before each request. Callers never set
    /// credentials on request structs.
    /// </summary>
    internal sealed class FonepayApiClient
    {
        private const string QrPath = "merchant/merchantDetailsForThirdParty/thirdPartyDynamicQrDownload";
        private const string StatusPath = "merchant/merchantDetailsForThirdParty/thirdPartyDynamicQrGetStatus";
        private const string TaxRefundPath = "merchant/merchantDetailsForThirdParty/taxRefund";

        private readonly FonepayConfigSO _config;
        private readonly HmacSha512Signer _signer;

        internal FonepayApiClient(FonepayConfigSO config, HmacSha512Signer signer)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        }

        // ── public API ────────────────────────────────────────────────────

        internal async Task<QrResult> PostQRAsync(QrRequest request, CancellationToken ct)
        {
            var amountStr = request.amount.ToString("0.00", CultureInfo.InvariantCulture);
            var payload = new QrRequestPayload
            {
                amount = request.amount,
                prn = request.prn,
                remarks1 = request.remarks1,
                remarks2 = request.remarks2,
                pm = request.pm,
                merchantCode = _config.MerchantCode,
                username = _config.Username,
                password = _config.GetPassword(),
                dataValidation = _signer.SignQrRequest(
                    amountStr, request.prn, _config.MerchantCode,
                    request.remarks1, request.remarks2),
            };
            var response = await SendPostAsync<QrResponse>(QrPath, payload, ct);
            return MapToQrResult(response);
        }

        internal async Task<QrResult> GetStatusAsync(string prn, CancellationToken ct)
        {
            var sig = _signer.SignStatusCheck(prn, _config.MerchantCode);
            var url = $"{_config.ResolveBaseUrl()}{StatusPath}" +
                      $"?prn={UnityWebRequest.EscapeURL(prn)}" +
                      $"&merchantCode={UnityWebRequest.EscapeURL(_config.MerchantCode)}" +
                      $"&dataValidation={UnityWebRequest.EscapeURL(sig)}" +
                      $"&username={UnityWebRequest.EscapeURL(_config.Username)}" +
                      $"&password={UnityWebRequest.EscapeURL(_config.GetPassword())}";
            var response = await SendAsync<QrResponse>(url, UnityWebRequest.kHttpVerbGET, null, ct);
            return MapToQrResult(response);
        }


        internal Task<TaxRefundResponse> PostTaxRefundAsync(TaxRefundRequest r, CancellationToken ct)
        {
            var invoiceDate = r.invoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var amountStr = r.transactionAmount.ToString("0.00", CultureInfo.InvariantCulture);
            var payload = new TaxRefundRequestPayload
            {
                fonepayTraceId = r.fonepayTraceId,
                merchantPRN = r.merchantPRN,
                invoiceNumber = r.invoiceNumber,
                invoiceDate = invoiceDate,
                transactionAmount = r.transactionAmount,
                merchantCode = _config.MerchantCode,
                username = _config.Username,
                password = _config.GetPassword(),
                dataValidation = _signer.SignTaxRefund(
                    r.fonepayTraceId, r.merchantPRN, r.invoiceNumber,
                    invoiceDate, amountStr, _config.MerchantCode),
            };
            return SendPostAsync<TaxRefundResponse>(TaxRefundPath, payload, ct);
        }

        // ── transport ─────────────────────────────────────────────────────

        private Task<T> SendPostAsync<T>(string relativePath, object body, CancellationToken ct)
        {
            var url = _config.ResolveBaseUrl() + relativePath;
            var json = JsonUtility.ToJson(body);
            return SendAsync<T>(url, UnityWebRequest.kHttpVerbPOST, json, ct);
        }

        private static Task<T> SendAsync<T>(string url, string method, string jsonBody, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var req = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };
            if (!string.IsNullOrEmpty(jsonBody))
            {
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.SetRequestHeader("Accept", "application/json");

            CancellationTokenRegistration reg = default;
            if (ct.CanBeCanceled)
                reg = ct.Register(() =>
                {
                    try
                    {
                        req.Abort();
                    }
                    catch
                    {
                        /* ignored */
                    }
                });

            var op = req.SendWebRequest();
            op.completed += _ =>
            {
                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(ct);
                        return;
                    }
#if UNITY_2020_2_OR_NEWER
                    var failed = req.result != UnityWebRequest.Result.Success;
#else
                    var failed = req.isHttpError || req.isNetworkError;
#endif
                    if (failed)
                    {
                        tcs.TrySetException(new FonepayError(
                            (int)req.responseCode,
                            $"HTTP {(int)req.responseCode} {req.error}: {req.downloadHandler?.text}",
                            url));
                        return;
                    }

                    var text = req.downloadHandler.text ?? string.Empty;
                    T result;
                    try
                    {
                        result = JsonUtility.FromJson<T>(text);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(new FonepayError(0,
                            $"JSON parse failed: {e.Message}. Body: {text}", url));
                        return;
                    }

                    tcs.TrySetResult(result);
                }
                finally
                {
                    reg.Dispose();
                    req.Dispose();
                }
            };
            return tcs.Task;
        }


        private QrResult MapToQrResult(QrResponse response)
        {
            return new QrResult
            {
                message = response.message,
                qrCode = string.IsNullOrEmpty(response.qrMessage)
                    ? null
                    : FonepayQRGenerator.GenerateTexture(response.qrMessage),
                status = response.status,
                statusCode = response.statusCode,
                success = response.success,
                thirdpartyQrWebSocketUrl = response.thirdpartyQrWebSocketUrl,
            };
        }
    }
}