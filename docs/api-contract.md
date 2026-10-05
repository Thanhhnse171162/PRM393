# API Contract

Base URL (dev): `http://localhost:5080` · Swagger: `/swagger` · JSON · Auth: `Authorization: Bearer <jwt>`.

> Status: only `GET /api/<module>/ping` placeholders exist. Everything else is **planned** – agree changes here BEFORE implementing.

## Route map

| Route | Access | Purpose |
|-------|--------|---------|
| `/api/auth` | public | register (customer), login |
| `/api/sports` | public | list sports |
| `/api/sport-centers` | public | list/filter centers, details |
| `/api/courts` | public | courts of a center |
| `/api/availability` | public | slots for court/date |
| `/api/bookings` | authenticated | create, history, detail, cancel |
| `/api/payments` | authenticated | deposit, remaining, refund |
| `/api/check-in` | Staff | QR check-in |
| `/api/notifications` | authenticated | list, mark read |
| `/api/staff` | Staff | branch operations |
| `/api/admin` | Admin | management |
| `/api/reports` | Admin | reports |

## Planned examples

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
