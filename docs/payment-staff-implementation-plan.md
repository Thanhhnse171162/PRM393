# Staff and real payment implementation

User authorization: implement both supplied prompts; merchant accounts and public HTTPS callbacks are not available. Keep DTO + ProblemDetails contracts. Never present a local simulator as a real charge.

## Existing architecture and required changes

`IPaymentGateway` creates attempts; `DepositPaymentService.ApplyVerifiedResultAsync` owns booking confirmation, reserved slots, QR, status history and notifications inside `BookingCommandExecutor`'s serializable booking lock. Retain that finalization boundary.

`ProviderTransactionId` currently means the merchant attempt reference, including `dev-*`. Retain it for compatibility. Add nullable receipt ID, checkout URL and gateway name to Payment with an additive migration. A unique provider receipt prevents one real charge from finalizing two attempts. Persist the pending intent before external HTTP; stable PaymentId is the provider idempotency key. Cache the returned checkout URL. A late successful charge must create a pending refund without confirming the booking or occupying slots.

Create payment settings, signature helpers, MoMo/VNPAY adapters, central resolver, callback verifier and public IPN/return controller. Modify Program registration, Payment mapping, deposit service and development controller discovery. Refund approval remains pending until confirmed; unavailable merchant refund permission must not report success. Return URLs only navigate; verified server callbacks own payment state.

Resolve the pre-existing duplicate CancellationDecisionRequest declaration and preserve the original admin query/detail/decision contract while supporting the draft refund workflow.

Create Staff API/models and operational pages under FE/lib/features/staff; replace only Staff route placeholders. Use existing teal/navy tokens and shared auth. Add real camera scanning, remaining-payment confirmation, backend-validated check-in, walk-in, court blocks, NoShow, shared notifications and assigned-center account. Update Customer payment URL/deep-link handling and authoritative status polling.

## Verification

Baseline build currently fails on duplicate cancellation DTO. After repair: .NET build and complete tests, provider signature/HTTP/callback/idempotency/late-charge tests, Flutter analysis and tests, Android operational flow where environment permits. Provider sandbox E2E cannot be claimed until merchant credentials and HTTPS callbacks are configured. Final reports must distinguish automated tests, device checks, and this external blocker.
