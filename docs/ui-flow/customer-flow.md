# Customer Flow

Bottom navigation: **Trang chủ · Khám phá · Lịch đặt · Thông báo · Tài khoản**

```
Splash → Location (first launch) → /customer/home   (guest allowed)
Home / Explore → filter by sport → Center detail → Courts
   → choose date → choose 1..n consecutive 1-hour slots
   → [Continue]  (login required → /login)
        backend re-checks slots:  OK → hold   |  409 → refresh availability
   → Checkout (pay deposit) → Booking confirmed + QR
Lịch đặt: upcoming / past bookings, QR, cancel (per policy)
Thông báo: booking & payment notifications
Tài khoản: profile, logout
```

States every screen must handle: loading, error, empty, success, disabled button.
