# Customer deposit payment through Staff check-in

Implemented against the existing CourtGoDb schema. No migrations, schema/index changes, EnsureCreated, EnsureDeleted, or database update commands are required.

## Scope and payment environment

The complete application flow is implemented: Login → Availability → Hold → Deposit Pending → Deposit Succeeded → Confirmed → History/Detail/QR → Staff Verify → Remaining payment → FullyPaid → Check-in → CheckedIn.

The provider is explicitly **Development-only**. MoMo and VNPay are simulated labels; no money is transferred and no real provider credentials are used. Production/Staging have no simulation route or simulation service. Deposit processing remains unavailable outside Development until a real provider adapter is installed.

The integration test `Login_Hold_DepositPending_DepositSucceeded_History_Qr_Collection_CheckIn` calls every step over HTTP. It no longer assigns Confirmed, DepositPaid or QrToken in a fixture. Only reference data/accounts/Staff assignment are seeded. The test also reads DB states and verifies Availability changes from Held to Booked. Separate SQL Server tests verify actual relational transactions and locking.

## Deposit APIs

Customer JWT and booking ownership are required. Unknown and another customer's booking both return 404 BOOKING_NOT_FOUND. The simulator returns 404 PAYMENT_NOT_FOUND for an unknown or another customer's payment.

POST /api/bookings/{bookingId}/payments/deposit

    { "paymentMethod": "MoMo" }

Only MoMo and VNPay are simulated. Cash, BankTransfer, Other and numeric method strings are rejected for this endpoint. The response includes `gateway: "Development"`, `paymentUrl: null`, and a provider reference prefixed with `dev-`. This is separate from Staff's manual Cash/BankTransfer collection.

The amount comes only from Booking.DepositAmount. Unknown request fields such as amount, totalAmount, depositAmount, depositPercent, customerId and bookingStatus are rejected. Slot pricing and deposit percentage are not recalculated.

After expired-hold cleanup, the command locks and reloads the booking and slots. It requires PendingPayment/Unpaid, a strictly future booking expiry, and at least one slot with Held/IsOccupying and a strictly future expiry. A non-positive deposit is rejected as PAYMENT_AMOUNT_MISMATCH because the existing Payments schema requires Amount > 0; a zero-deposit business policy is not invented.

One Deposit/Pending payment is created, with PaidByUserId from JWT and PaidAt/ConfirmedByUserId unset. Booking, slots and QR remain unchanged. Repeated starts return the existing pending attempt (including its original method), even if the user selects the other supported method. A successful deposit rejects another start with PAYMENT_ALREADY_COMPLETED. Failed/cancelled attempts can be retried only while the hold remains valid.

POST /api/dev/payments/{paymentId}/simulate

    { "result": "success" }

Allowed results are success, failed and cancelled. This route is removed from controller discovery outside Development and also checks the environment in the action. Within Development it requires Customer authentication and payment ownership.

IPaymentGateway creates idempotent provider attempts using PaymentId. IDevelopmentPaymentGateway verifies simulation references/results. The simulator then calls the reusable IDepositPaymentService.ApplyVerifiedResultAsync workflow; it does not implement booking confirmation in the controller. VerifiedPaymentResult is an internal application contract and is never accepted from an HTTP request body.

On success, the transaction reloads payment, booking and every slot, verifies amount/reference/state/expiry, then commits Payment Succeeded, Booking Confirmed/DepositPaid, cleared hold expiries, and Reserved/IsOccupying slots. Slot IDs and UnitPrice remain unchanged. IQrTokenGenerator uses RandomNumberGenerator to create a 256-bit token, stored as 64 hexadecimal characters under the existing unique index. No QR image is stored. Confirmation does not return the token; use the existing Customer QR endpoint.

The first success creates one PendingPayment → Confirmed history (reason: Deposit payment succeeded) and one Booking notification (Đặt sân thành công). Repeating the same terminal result returns current state without rotating QR or duplicating rows. A conflicting terminal result returns PAYMENT_ALREADY_FINALIZED.

Failed/cancelled results change only the payment attempt. Slots stay Held, booking remains PendingPayment/Unpaid while valid, and QR remains null. Cancellation of a payment attempt does not cancel the booking. A late success returns PAYMENT_AFTER_HOLD_EXPIRED and never marks the Development payment succeeded or resurrects released slots.

GET /api/bookings/{bookingId}/payment-status

Returns booking/payment states, total/deposit/paid/remaining amounts and a compact transaction list. It does not expose ProviderTransactionId, QR, internal refund links or secrets. PaymentRules.SuccessfulCharge is the shared SQL-translatable predicate for Customer, Staff and deposit totals: only successful Deposit/Remaining charges count; remaining is max(0, total - successful charges).

## Customer API

All three endpoints require a Customer JWT. The customer ID comes exclusively from JWT claims.

- GET /api/bookings?statusGroup=upcoming&pageNumber=1&pageSize=20
- GET /api/bookings/{bookingId}
- GET /api/bookings/{bookingId}/qr

Omitting statusGroup defaults to upcoming. Groups are case-insensitive:

- upcoming: Confirmed, CheckedIn, InProgress, plus PendingPayment only when HoldExpiresAt is non-null and strictly later than the current time.
- completed: Completed.
- cancelled: Cancelled, Expired, NoShow.

An expired PendingPayment row is excluded from upcoming immediately. Read endpoints do not mutate it into Expired; the existing expired-hold cleanup remains responsible for that transition.

Page number starts at 1. Page size defaults to 20, range 1–100. Invalid values, offset overflow and invalid groups return 400. Upcoming sorts StartAt ascending; completed/cancelled sort descending. Id is a deterministic tie-breaker. COUNT and Skip/Take execute in SQL; only the requested page is materialized.

List/detail use historical name/phone snapshots and successful Deposit/Remaining charges. Refund, failed and pending transactions do not contribute to paidAmount. remainingAmount = max(0, totalAmount - paidAmount).

List/detail never serialize QrToken. The dedicated QR endpoint returns raw qrValue only for Confirmed with a non-empty stored token, with Cache-Control: no-store. Other states return 409 QR_NOT_AVAILABLE. Unknown or another customer's booking returns the same 404 BOOKING_NOT_FOUND.

## Staff API

All endpoints require a Staff JWT; Customer and Admin are denied.

POST /api/staff/check-ins/verify

    { "qrToken": "<value from customer QR>" }

Read-only verification resolves the active StaffAssignment, checks the booking's court center, matches the stored token ordinally (including case), and accepts Confirmed or returns alreadyCheckedIn for CheckedIn. It returns payment totals and requiresRemainingPayment without modifying rows.

POST /api/staff/bookings/{bookingId}/payments/remaining

    { "paymentMethod": "Cash" }

Cash and BankTransfer are supported manual methods. Unknown properties such as amount are rejected. The server recalculates successful charges under the booking lock; zero remaining returns 409 PAYMENT_ALREADY_FULLY_PAID. Collection is allowed for Confirmed, CheckedIn or InProgress (including collection after an authorized outstanding-payment check-in). PendingPayment and terminal states are rejected.

The new payment is Remaining/Succeeded, with PaidByUserId from the booking, ConfirmedByUserId from JWT, and no invented provider transaction ID. Payment and recalculated FullyPaid status commit atomically. BookingStatus remains unchanged.

POST /api/staff/bookings/{bookingId}/check-ins

    {
      "qrToken": "<value from customer QR>",
      "allowOutstandingPayment": false,
      "overrideReason": null
    }

The command reloads the booking and rechecks assignment, center, token, status and balance. Confirmed transitions only to CheckedIn. A repeated request for CheckedIn returns the existing check-in after authorization and QR revalidation.

If money remains, SystemSettings.Id=1 must exist and AllowOutstandingCheckIn must be true, the request must explicitly set allowOutstandingPayment=true, and overrideReason must be nonblank (maximum 500 characters). Otherwise the command rejects the request. No time-window restriction has been invented.

The transaction creates one CheckIn, one Confirmed→CheckedIn history entry and one database notification for an account customer. A fully paid check-in stores OutstandingPaymentOverride=false and OverrideReason=null. An override preserves the outstanding payment status. The stored QrToken remains for history, but the Customer QR endpoint becomes unavailable.

## Transactions and concurrency

BookingCommandExecutor starts a Serializable relational transaction and obtains a SQL Server UPDLOCK/HOLDLOCK on the booking before reading mutable business state. This protects commands across API instances. A bounded in-process semaphore array also serializes the existing InMemory test provider. It is not a substitute for the database lock.

The existing unique CheckIns.BookingId, Payments.ProviderTransactionId and Bookings.QrToken constraints are preserved. Duplicate collection returns 409; duplicate check-in returns the original successful response with no extra history/notification. Unique payment-reference conflicts return a safe PAYMENT_STATE_CONFLICT without SQL details. Exceptions roll back relational changes and clear tracked failed changes. API requests should use their normal scoped DbContext.

ExpiredBookingHoldService now uses TimeProvider and the same booking-first command lock, then rechecks the candidate and reloads slots inside the transaction. It preserves the expiration history previously written by the stored procedure. The application no longer calls the legacy procedure; the procedure/schema itself is unchanged. If confirmation wins, expiry cannot release the reservation. If expiry wins, confirmation rejects the callback. Cleanup writes one PendingPayment → Expired history and releases held slots atomically.

SQL Server tests explicitly verify an independent connection's lock, server-side pagination, concurrent starts/callbacks, duplicate provider references, both expiry/confirmation race outcomes, and injected failures after SQL writes but before commit. InMemory tests alone do not prove transaction behavior. SQL test classes share one xUnit collection to prevent fixture teardown from racing another class's global cleanup; concurrent requests within the race tests remain concurrent.

## Responses and errors

Successful responses remain plain DTOs, matching the current API. Failures retain the existing RFC 7807 ProblemDetails plus code. No new response wrapper is introduced.

Booking/Staff error codes remain unchanged. Deposit errors include BOOKING_NOT_PENDING_PAYMENT, BOOKING_HOLD_EXPIRED, PAYMENT_NOT_FOUND, PAYMENT_ALREADY_COMPLETED, PAYMENT_ALREADY_FINALIZED, PAYMENT_AFTER_HOLD_EXPIRED, PAYMENT_VERIFICATION_FAILED, PAYMENT_AMOUNT_MISMATCH and PAYMENT_STATE_CONFLICT. Existing ownership and unsupported-method errors are reused.

## Verification

Executed successfully:

- dotnet restore BE/CourtGo.sln
- dotnet build BE/CourtGo.sln --no-restore
- dotnet test BE/CourtGo.sln --no-restore, with COURTGO_TEST_SQLSERVER set for the eight real SQL Server tests
- Final verification on 2026-10-08: 131 unit + 121 integration = 252 passed, 0 failed, 0 skipped; build 0 errors, 0 warnings. All 194 previous test cases are retained, with the old E2E fixture boundary replaced by API calls.
- Swagger JSON contract checks for all nine workflow paths, HTTP verbs, camelCase query names and no client amount field.

For repeatable SQL Server tests, set COURTGO_TEST_SQLSERVER to a connection string for an existing CourtGo schema. Tests create isolated random-ID fixtures and delete only those IDs in foreign-key order. They never create or alter schema. With the environment variable absent, only the eight SQL Server tests are skipped. Use a development/test database.

## Swagger manual procedure

Automated Swagger JSON and HTTP tests passed. An interactive Swagger UI session has not been performed.

1. Run the API in Development with its normal local configuration and open /swagger.
2. Log in as Customer and authorize with that access token.
3. GET /api/courts/{courtId}/availability?date=yyyy-MM-dd and choose consecutive available slots.
4. POST /api/bookings/hold with courtId and slotStartAts. Verify PendingPayment/Unpaid, Held slots and no QR.
5. POST /api/bookings/{bookingId}/payments/deposit with paymentMethod MoMo or VNPay. Verify Deposit/Pending and the amount from the hold.
6. GET /api/bookings/{bookingId}/payment-status. Paid amount is still zero; the QR endpoint still rejects access.
7. POST /api/dev/payments/{paymentId}/simulate with result success. Verify Succeeded, Confirmed/DepositPaid, Reserved slots and cleared expiries. No real money is transferred.
8. Availability now shows Booked. GET /api/bookings and /api/bookings/{bookingId}; verify the upcoming booking, historical snapshots and remaining balance.
9. GET /api/bookings/{bookingId}/qr; obtain qrValue.
10. Log in as Staff assigned to that booking's center; replace the Swagger token.
11. POST /api/staff/check-ins/verify with qrToken in the body. Verify remainingAmount and unchanged booking state.
12. POST /api/staff/bookings/{bookingId}/payments/remaining with Cash or BankTransfer, after actual payment receipt. Expect FullyPaid; booking remains Confirmed.
13. POST /api/staff/bookings/{bookingId}/check-ins with the token. Expect CheckedIn.
14. Repeat check-in; expect the original result without duplicate records. Switch back to Customer; QR now returns 409 QR_NOT_AVAILABLE.
15. Verify Customer ownership, Staff center isolation, failure/cancellation retry, expired-hold rejection and Production's missing simulation route.

## Remaining production payment work

Implement the chosen real provider's IPaymentGateway adapter, credentials and payment URL contract. A production callback must verify provider signatures, reference, amount/currency and payment identity before constructing VerifiedPaymentResult and invoking the shared confirmation workflow. Never expose a public endpoint that directly accepts that trusted contract.

Before enabling real money, add provider timeout/retry and reconciliation handling. An external provider can receive money after hold expiry or before the local transaction commits; keep the reservation rejected and reconcile/refund through an explicit policy instead of reviving slots. The Development adapter has no external side effects; a production adapter must persist/recover attempts with provider idempotency keys and avoid relying on rollback of a database transaction to undo a remote charge.

The application flow is complete with a Development provider. Production payment, interactive Swagger UI verification, cancellation/refund, Admin, reviews, walk-in, Staff schedules, push and automatic InProgress/Completed transitions remain outside this implementation.

## Deposit-task file inventory

Created:

- BE/src/CourtGo.Api/Controllers/BookingPaymentsController.cs
- BE/src/CourtGo.Api/Controllers/DevelopmentPaymentsController.cs
- BE/src/CourtGo.Application/Bookings/DepositPaymentDtos.cs
- BE/src/CourtGo.Application/Interfaces/IDepositPaymentService.cs
- BE/src/CourtGo.Application/Interfaces/IPaymentGateway.cs
- BE/src/CourtGo.Infrastructure/Services/BookingPaymentQueries.cs
- BE/src/CourtGo.Infrastructure/Services/DepositPaymentService.cs
- BE/src/CourtGo.Infrastructure/Services/DevelopmentPaymentGateway.cs
- BE/src/CourtGo.Infrastructure/Services/DevelopmentPaymentSimulationService.cs
- BE/src/CourtGo.Infrastructure/Services/QrTokenGenerator.cs
- BE/tests/CourtGo.UnitTests/DepositPaymentServiceTests.cs
- BE/tests/CourtGo.IntegrationTests/DepositPaymentApiTests.cs
- BE/tests/CourtGo.IntegrationTests/SqlServerDepositPaymentTests.cs

Modified (relative to the completed Customer/Staff task, whose files may still be untracked in Git):

- BE/src/CourtGo.Api/Program.cs
- BE/src/CourtGo.Application/Common/Exceptions/AppException.cs
- BE/src/CourtGo.Domain/Rules/PaymentRules.cs
- BE/src/CourtGo.Infrastructure/DependencyInjection.cs
- BE/src/CourtGo.Infrastructure/Services/BookingCommandExecutor.cs
- BE/src/CourtGo.Infrastructure/Services/BookingQueryService.cs
- BE/src/CourtGo.Infrastructure/Services/StaffBookingService.cs
- BE/src/CourtGo.Infrastructure/Services/ExpiredBookingHoldService.cs
- BE/tests/CourtGo.IntegrationTests/BookingWorkflowApiTests.cs
- BE/tests/CourtGo.IntegrationTests/SqlServerBookingWorkflowTests.cs
- docs/booking-workflow.md
