using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Darkmatter.Fonepay.Samples
{
    /// <summary>
    /// Minimal end-to-end sample: request QR, render to Image, await payment,
    /// support cancellation via a cancel button.
    /// </summary>
    public class SamplePayment : MonoBehaviour
    {
        [SerializeField] private Image qrImage;
        [SerializeField] private GameObject successObject;
        [SerializeField] private GameObject failedObject;
        [SerializeField] private Button payButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private float amount = 1f;

        private CancellationTokenSource _payCts;

        private void Start()
        {
            payButton.onClick.AddListener(() => InitiatePayment().Forget());
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => _payCts?.Cancel());
        }

        private async UniTask InitiatePayment()
        {
            var fonepay = new FonepayClient();
            _payCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            try
            {
                var qr = await fonepay.PurchaseAsync(
                    new QrRequest { amount = amount, remarks1 = "sample" },
                    _payCts.Token);

                if (qr.qrCode != null)
                {
                    qrImage.sprite = Sprite.Create(
                        qr.qrCode,
                        new Rect(0, 0, qr.qrCode.width, qr.qrCode.height),
                        new Vector2(0.5f, 0.5f));
                    qrImage.gameObject.SetActive(true);
                }

                var payment = await fonepay.AwaitPaymentAsync(
                    qr.thirdpartyQrWebSocketUrl,
                    onQrVerified: v => Debug.Log($"Fonepay QR verified: {v}"),
                    ct: _payCts.Token);

                Debug.Log($"Payment frame: {JsonUtility.ToJson(payment)}");
                ShowResult(payment.Outcome == PaymentOutcome.Complete);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Payment cancelled.");
                ShowResult(false);
            }
            catch (FonepayError e)
            {
                Debug.LogError($"Fonepay API error {e.ErrorCode}: {e.Message}");
                ShowResult(false);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogWarning($"Websocket closed early: {e.Message}");
                ShowResult(false);
            }
            finally
            {
                _payCts.Dispose();
                _payCts = null;
            }
        }

        private void ShowResult(bool ok)
        {
            qrImage.gameObject.SetActive(false);
            if (successObject != null) successObject.SetActive(ok);
            if (failedObject != null) failedObject.SetActive(!ok);
        }
    }
}
