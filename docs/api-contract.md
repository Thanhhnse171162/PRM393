# API Contract

Base URL (dev): `http://localhost:5080` · Swagger: `/swagger` · JSON · Auth: `Authorization: Bearer <jwt>`.

> Status: authentication and VNPay deposit endpoints are implemented; most other modules still expose placeholders.

## Route map

| Route | Access | Purpose |
|-------|--------|---------|
| `/api/auth` | public | register (customer), login |
| `/api/sports` | public | list sports |
| `/api/sport-centers` | public | list/filter centers, details |
| `/api/courts` | public | courts of a center |
| `/api/availability` | public | slots for court/date |
| `/api/bookings` | authenticated | create, history, detail, cancel |
| `/api/payments` | mixed | VNPay deposit creation, status, return and IPN |
| `/api/check-in` | Staff | QR check-in |
| `/api/notifications` | authenticated | list, mark read |
| `/api/staff` | Staff | branch operations |
| `/api/admin` | Admin | management |
| `/api/reports` | Admin | reports |

## Planned examples

## Implemented VNPay deposit flow

- `POST /api/payments/vnpay/create` (Customer JWT): `{ "bookingId": "guid" }` → `{ paymentId, bookingId, amount, paymentUrl }`. Only the booking owner can start payment for a pending, unexpired booking. The amount comes from the booking, never the client. Repeated calls reuse an active attempt.
- `GET /api/payments/{paymentId}` (Customer JWT): returns the owner's payment and booking status. Flutter uses this after returning from the browser; a redirect query string alone is not proof of payment.
- `GET /api/payments/vnpay/return` (public browser redirect): verifies the VNPay HMAC and amount, applies the result once, then redirects to `VnPay:FrontendReturnUrl` if configured. The frontend must fetch the status above.
- `GET /api/payments/vnpay/ipn` (public VNPay notification): verifies the same fields and responds with VNPay `RspCode`. Configure this URL in the VNPay merchant dashboard with a publicly reachable API address.

A successful deposit sets the payment to `DepositPaid` and booking to `Confirmed`. Failed attempts can be retried. A success arriving after the booking hold has expired sets `RefundPending` for staff reconciliation. This implementation does not issue automatic refunds.

The current booking API and Flutter booking creation UI are still placeholders. Once they create a pending booking, navigate Flutter to `/customer/payments/{bookingId}`.

`POST /api/auth/login`
```json
{ "email": "a@b.com", "password": "..." }
```
→ `200 { "accessToken": "...", "userId": "...", "fullName": "...", "role": "Customer" }`

`GET /api/availability?courtId={guid}&date=2026-10-06` → list of hourly slots with `startTime`, `endTime`, `price`, `isAvailable`.

`POST /api/bookings`
```json
{ "courtId": "guid", "slotStartTimes": ["2026-10-06T17:00:00", "2026-10-06T18:00:00"] }
```
→ `201` booking in `PendingPayment` with `holdExpiresAt`, or `409` if any slot is taken.

## Error format (RFC 7807)
```json
{ "status": 409, "title": "Conflict", "detail": "Slot 18:00 is no longer available.", "instance": "/api/bookings" }
```
| Code | Meaning |
|------|---------|
| 400 | Validation error (`errors` map) |
| 401 | Missing/invalid token |
| 403 | Role/branch not allowed |
| 404 | Not found |
| 409 | Conflict – slot already booked |
| 500 | Server error |
