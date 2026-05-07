# Fonepay Unity

Fonepay payment integration for Unity. Request QR codes, await payment confirmation over websocket, process tax refunds — all from a single async-friendly client.

## Requirements

- Unity 6000.4+
- [UniTask](https://github.com/Cysharp/UniTask) (for sample only — runtime uses `System.Threading.Tasks`)

## Setup

1. Install the package via Package Manager (Git URL or local).
2. Open **Tools > Fonepay > Settings** to create the `FonepayConfig` asset and enter your merchant credentials. Credentials are kept out of source control and baked at build time.
3. (Optional) **Window > Package Manager > Fonepay Unity > Samples > Import** to drop the example MonoBehaviour into your project.

## Usage

### 1. Request a QR

```csharp
using Darkmatter.Fonepay;

var fonepay = new FonepayClient();

QrResult qr = await fonepay.PurchaseAsync(new QrRequest
{
    amount = 100f,
    remarks1 = "order #1234"
}, ct);

// qr.qrCode is a Texture2D ready to render
// qr.thirdpartyQrWebSocketUrl — pass to AwaitPaymentAsync
```

### 2. Await payment

```csharp
QRPaymentStatus result = await fonepay.AwaitPaymentAsync(
    qr.thirdpartyQrWebSocketUrl,
    onQrVerified: verified => Debug.Log($"QR scanned: {verified}"),
    ct: ct);

switch (result.Outcome)
{
    case PaymentOutcome.Complete:        // success
    case PaymentOutcome.CancelledByUser: // user dismissed in app
    case PaymentOutcome.Failed:          // server rejected
}
```

`AwaitPaymentAsync` opens the websocket, fires `onQrVerified` when the QR is scanned, then resolves on the terminal payment frame and disconnects.

### 3. Cancellation & timeout

```csharp
var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
cts.CancelAfter(TimeSpan.FromMinutes(15)); // QR validity

try
{
    var payment = await fonepay.AwaitPaymentAsync(qr.thirdpartyQrWebSocketUrl, ct: cts.Token);
}
catch (OperationCanceledException)        { /* user cancelled or timed out */ }
catch (InvalidOperationException)         { /* server closed websocket early */ }
catch (FonepayError e)                    { /* API error — e.ErrorCode, e.Docs */ }
```

Calling `cts.Cancel()` from a button handler aborts the await and disconnects the socket.

### 4. Tax refund

```csharp
TaxRefundResponse refund = await fonepay.PostTaxRefundAsync(new TaxRefundRequest
{
    fonepayTraceId   = "...",
    merchantPRN      = "...",
    invoiceNumber    = "INV-001",
    invoiceDate      = DateTime.UtcNow,
    transactionAmount = 100f,
}, ct);
```

## Errors

`FonepayError` (extends `Exception`) carries `ErrorCode` and `Docs`. Throw paths:
- Missing/invalid `FonepayConfig` asset or credentials
- Non-2xx HTTP responses from Fonepay
- HMAC signing failures

## Samples

Import **Example Payment Flow** from the Package Manager. Wires a button → QR image → success/fail panels with cancel support.

## Tests

Editor tests live under `Tests/Editor` (NUnit). Run via **Window > General > Test Runner > EditMode**.
