# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-05-07

### Added
- `FonepayClient` façade: `PurchaseAsync`, `GetStatusAsync`, `AwaitPaymentAsync`, `PostTaxRefundAsync`.
- HMAC-SHA512 signing of all signed payloads.
- Editor tooling for credential management (Tools > Fonepay > Settings).
- Example sample under Package Manager > Samples.
- Edit-mode tests covering `WebsocketMessage<T>.Status` parsing and `PaymentOutcome` rules.

### Fixed
- `WebsocketMessage<T>.Status` returned default values due to invalid `??=` cache on generic struct. Now reparses on each access.
- `AwaitPaymentAsync` hung indefinitely when the server closed the websocket before a payment frame; now throws `InvalidOperationException`.
- `QRPaymentStatus.transactionDate` changed from `DateTime` to `string` (JsonUtility cannot deserialize `DateTime`).
