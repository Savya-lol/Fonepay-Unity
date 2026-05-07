using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Darkmatter.Fonepay.Samples
{
    public class SamplePayment : MonoBehaviour
    {
        [SerializeField] private Image qrImage;
        [SerializeField] private GameObject successObject;
        [SerializeField] private GameObject failedObject;
        [SerializeField] private Button payButton;

        private void Start()
        {
            payButton.onClick.AddListener(OnPayButtonClicked);
        }

        private void OnPayButtonClicked()
        {
            InitiatePayment().Forget();
        }

        private async UniTask InitiatePayment()
        {
            var fonepay = new FonepayClient();
            var request = new QrRequest
            {
                amount = 1,
                remarks1 = "mausham ko paisa"
            };
            var qr = await fonepay.PurchaseAsync(request, destroyCancellationToken);

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
                ct: destroyCancellationToken);

            var ok = payment.Outcome == PaymentOutcome.Complete;
            
            qrImage.gameObject.SetActive(false);
            successObject.SetActive(ok);
            failedObject.SetActive(!ok);
        }
    }
}