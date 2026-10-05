# Business Rules

## BR-1 Structure
CourtGo is ONE business with several branches (sport centers). A branch has many courts; each court belongs to one sport.

## BR-2 Fixed 1-hour slots
Time is divided into fixed 1-hour slots aligned to the hour: 17:00–18:00, 18:00–19:00, 19:00–20:00, ...

A customer may select multiple **consecutive** slots. Example: 17–18 + 18–19 creates **ONE** Booking 17:00–19:00 (duration 2 h) – never two bookings.

Selected slots must:
1. belong to the **same court**,
2. be **consecutive** (no gaps, no duplicates, hour-aligned),
3. be **available**.

Implemented so far: `SlotRules.AreConsecutive` (Domain). Availability check comes with the booking engine.

## BR-3 Concurrency (no double booking)
- Selecting a slot in Flutter does **not** reserve it.
- When the customer taps **Continue**, the backend atomically re-checks every slot (single transaction, DB-level protection).
  - All available → create booking `PendingPayment` with a **temporary hold** (`HoldExpiresAt`).
  - Any unavailable → **HTTP 409 Conflict**; Flutter refreshes availability.
- Unpaid bookings whose hold expires become `Expired` and release slots.

## BR-4 Separate statuses
`BookingStatus` (PendingPayment, Confirmed, CheckedIn, InProgress, Completed, Cancelled, Expired, NoShow) and `PaymentStatus` (Unpaid, DepositPaid, FullyPaid, RefundPending, Refunded, Failed) are independent.

Example: `Confirmed` + `DepositPaid` = booking valid, customer still owes the remainder.

## BR-5 Payment
Example: total 240,000 VND; deposit 72,000 VND (default 30%); remaining 168,000 VND.
- Customer pays the deposit at checkout.
- Staff collects the remainder at the branch.
- Money movements are stored as separate `Payment` rows (Deposit / Remaining / Refund).
Implemented: `PaymentRules` (Domain).

## BR-6 Staff
- Staff has no public registration; Admin creates accounts.
- Staff belongs to ONE branch (`StaffAssignment`) and may operate all sports/courts of that branch.
- Staff may only access data of the assigned branch.

## BR-7 Admin
Manages branches, courts, sports, staff, prices, opening hours, cancellations/refunds, reports and configuration. Not a QR operator by default.

## BR-8 Court status
`Active`, `TemporarilyBlocked`, `Maintenance`, `Inactive`. Only `Active` courts accept bookings.

## BR-9 Authentication
Guests browse freely; creating a booking requires login (JWT). Passwords are stored only as hashes.
