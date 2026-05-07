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
            if (_payCts != null) return;

            payButton.interactable = false;
            var fonepay = new FonepayClient();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _payCts = cts;

            try
            {
                var qr = await fonepay.PurchaseAsync(
                    new QrRequest { amount = amount, remarks1 = "sample" },
                    cts.Token);

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
                    ct: cts.Token);

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
                cts.Dispose();
                if (_payCts == cts) _payCts = null;
                if (this != null && payButton != null) payButton.interactable = true;
            }
        }

        private void ShowResult(bool ok)
        {
            if (qrImage != null)
            {
                qrImage.sprite = null;
                qrImage.gameObject.SetActive(false);
            }
            if (successObject != null) successObject.SetActive(ok);
            if (failedObject != null) failedObject.SetActive(!ok);
        }
    }
}
