# Fonepay Unity

See [README](../README.md) for setup, API reference, and examples.

## Architecture

| Layer | Type | Purpose |
|---|---|---|
| Public | `FonepayClient` | Façade. Auto-loads config + secrets. |
| API | `FonepayApiClient` | Signed REST calls (QR, status, tax refund). |
| Websocket | `FonepayWebsocketClient` | Receive-loop for QR verification + payment frames. |
| Crypto | `HmacSha512Signer` | HMAC-SHA512 dataValidation. |
| Models | `QrRequest`/`QrResult`/`QRPaymentStatus`/... | Serializable DTOs. |

## Flow

```
PurchaseAsync(req)        ── POST QR ──▶  QrResult { qrCode, thirdpartyQrWebSocketUrl }
AwaitPaymentAsync(url)    ── WS    ──▶  QRPaymentStatus { Outcome }
                                           ├─ qrVerified frame → onQrVerified callback
                                           └─ paymentSuccess frame → resolve
```

## Outcome rules

| `success` | `paymentSuccess` | `Outcome` |
|---|---|---|
| true  | true  | Complete |
| true  | false | CancelledByUser |
| false | *     | Failed |

## Termination

- Payment frame received → resolve normally.
- `CancellationToken` cancelled → `OperationCanceledException`, socket disconnects.
- Server closes socket before payment → `InvalidOperationException`.
