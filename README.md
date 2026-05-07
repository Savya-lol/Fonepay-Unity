# Fonepay Unity

Fonepay payment SDK for Unity. Generate Fonepay QR codes, await payment confirmation over websocket, and process tax refunds — all from a single async-friendly client.

This repo is a **Unity 6000.4.5f1** project that hosts the package source under `Packages/com.darkmattergameproduction.fonepay-unity/` and a sample scene under `Assets/`.

---

## Features

- `FonepayClient` façade — `PurchaseAsync`, `GetStatusAsync`, `AwaitPaymentAsync`, `PostTaxRefundAsync`.
- HMAC-SHA512 dataValidation on all signed payloads.
- Websocket payment listener with cancellation, timeout, and clean disconnect.
- QR rendered straight into a `Texture2D` ready to assign to `UnityEngine.UI.Image`.
- Editor-side credential management (Tools > Fonepay > Settings) — secrets baked at build time, never committed.
- NUnit edit-mode + play-mode tests.
- Importable sample under Package Manager > Samples.

---

## Requirements

| | |
|---|---|
| Unity | 6000.4.5f1+ |
| Test Framework | `com.unity.test-framework` 1.6.0+ |
| UniTask | for the sample only — runtime uses `System.Threading.Tasks` |

---

## Install

### Via Unity Package Manager (recommended)

**Window > Package Manager > + > Install package from git URL**

```
https://github.com/Savya-lol/Fonepay-Unity.git#0.2.0
```

Or edit `Packages/manifest.json` directly:
```json
"com.darkmattergameproduction.fonepay-unity": "https://github.com/Savya-lol/Fonepay-Unity.git#0.2.0"
```

Pin to any released tag (e.g. `#0.1.0`) or track the floating `upm` branch (`#upm`). The `upm` branch is the package subtree, auto-published from `main` via GitHub Actions.

### As a local clone of this repo

1. Clone:
   ```bash
   git clone https://github.com/Savya-lol/Fonepay-Unity.git "Fonepay Unity"
   ```
2. Open in Unity Hub (6000.4.5f1).

### Setup credentials

1. **Tools > Fonepay > Settings** — create the `FonepayConfig` asset under `Assets/Resources/FonepayConfig.asset`.
2. Enter merchant code, username, password, and HMAC secret. The window stores secrets outside source control; a build preprocessor injects them as `FonepayBakedSecrets` and removes the resource after build.

---

## Usage

### Request a QR

```csharp
using Darkmatter.Fonepay;

var fonepay = new FonepayClient();

QrResult qr = await fonepay.PurchaseAsync(new QrRequest
{
    amount   = 100f,
    remarks1 = "order #1234",
}, ct);

qrImage.sprite = Sprite.Create(
    qr.qrCode,
    new Rect(0, 0, qr.qrCode.width, qr.qrCode.height),
    new Vector2(0.5f, 0.5f));
```

### Await payment

```csharp
QRPaymentStatus result = await fonepay.AwaitPaymentAsync(
    qr.thirdpartyQrWebSocketUrl,
    onQrVerified: scanned => Debug.Log($"QR scanned: {scanned}"),
    ct: ct);

switch (result.Outcome)
{
    case PaymentOutcome.Complete:        break;  // success
    case PaymentOutcome.CancelledByUser: break;  // dismissed in app
    case PaymentOutcome.Failed:          break;  // server rejected
}
```

### Cancel / timeout

```csharp
var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
cts.CancelAfter(TimeSpan.FromMinutes(15)); // QR validity window

try
{
    var payment = await fonepay.AwaitPaymentAsync(qr.thirdpartyQrWebSocketUrl, ct: cts.Token);
}
catch (OperationCanceledException) { /* user cancel or timeout */ }
catch (InvalidOperationException)  { /* server closed websocket early */ }
catch (FonepayError e)             { /* API error: e.ErrorCode, e.Docs */ }
```

`cts.Cancel()` from a button handler aborts the await and disconnects the socket.

### Status poll

```csharp
QrResult status = await fonepay.GetStatusAsync(prn, ct);
// status.message: "PAID" | "UNPAID" | error
```

### Tax refund

```csharp
TaxRefundResponse refund = await fonepay.PostTaxRefundAsync(new TaxRefundRequest
{
    fonepayTraceId    = "...",
    merchantPRN       = "...",
    invoiceNumber     = "INV-001",
    invoiceDate       = DateTime.UtcNow,
    transactionAmount = 100f,
}, ct);
```

---

## API surface

| Type | Purpose |
|---|---|
| `FonepayClient` | Public façade, auto-loads config + secrets. |
| `QrRequest` / `QrResult` | QR request/response DTOs. |
| `QRPaymentStatus` | Terminal payment frame, exposes `Outcome`. |
| `QRVerificationStatus` | Intermediate "QR scanned" frame. |
| `PaymentOutcome` | `Complete` / `CancelledByUser` / `Failed`. |
| `TaxRefundRequest` / `TaxRefundResponse` | Refund DTOs. |
| `FonepayError` | API-level exception with `ErrorCode` + `Docs`. |

### `Outcome` rules

| `success` | `paymentSuccess` | `Outcome` |
|---|---|---|
| true  | true  | Complete |
| true  | false | CancelledByUser |
| false | *     | Failed |

### Termination of `AwaitPaymentAsync`

| Condition | Result |
|---|---|
| Payment frame received | resolves with `QRPaymentStatus` |
| `CancellationToken` cancelled | throws `OperationCanceledException` |
| Server closes socket before payment | throws `InvalidOperationException` |
| Network/HTTP error during connect | throws `WebSocketException` |

---

## Repo layout

```
Assets/                                Sample scene + test MonoBehaviour
Packages/
  com.darkmattergameproduction.fonepay-unity/
    Runtime/
      Core/        FonepayClient, FonepayConfig, FonepayConfigSO
      API/         FonepayApiClient (REST), FonepayWebsocketClient
      Crypto/      HmacSha512Signer
      Models/      DTOs (QrRequest, QrResult, …)
      QR/          FonepayQRGenerator
    Editor/        Settings window, build secrets injector
    Samples~/      Importable example
    Tests/
      Editor/      Edit-mode unit tests (NUnit)
      Runtime/     Play-mode tests
    Documentation/ Architecture notes
    README.md      Package-level docs
```

---

## Tests

**Window > General > Test Runner**

- **EditMode** — `WebsocketMessage<T>.Status` parsing, `PaymentOutcome` truth table, `FonepayError`.
- **PlayMode** — runtime parse sanity, missing-config throw, empty-URL throw, pre-cancelled token throw.

CLI (Unity 6 batchmode):
```bash
"<Unity>" -batchmode -nographics -projectPath . -runTests \
  -testPlatform EditMode -testResults Logs/edit-tests.xml -quit
```

---

## Security

- Credentials never live in this repo. `FonepayConfigSO` holds only public fields; password + HMAC secret are stored via `FonepaySecretsStore` (EditorPrefs) and baked into a temp `FonepayBakedSecrets` resource at build, then removed.
- All signed requests use HMAC-SHA512 — see `HmacSha512Signer`.
- `FonepayConfig.Invalidate()` clears the cached config (use after rotating credentials in Editor).

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `FonepayError: FonepayConfig asset missing` | No `Resources/FonepayConfig.asset` | Tools > Fonepay > Settings → Save |
| `FonepayError: Fonepay credentials missing` | Secrets not entered (Editor) or baked secrets stripped (build) | Re-open Settings window |
| `AwaitPaymentAsync` returns empty/default `QRPaymentStatus` | Old build pre-fix on `WebsocketMessage<T>.Status` | Update to current; status now reparses each access |
| `AwaitPaymentAsync` hangs forever | Server closed socket without payment frame | Update to current; now throws `InvalidOperationException` |
| `transactionDate` always default | `DateTime` not supported by `JsonUtility` | Fixed — field is `string` now |

---

## Contributing

1. Branch from `main`.
2. Run **Test Runner** (EditMode + PlayMode) before opening a PR.
3. Update `CHANGELOG.md` in the package.
4. Keep `FonepayConfig.asset` and any baked secrets out of commits.

---

## License

See `Packages/com.darkmattergameproduction.fonepay-unity/Third Party Notices.md` for third-party attributions (QRCoder, UniTask). Project license: TBD.
