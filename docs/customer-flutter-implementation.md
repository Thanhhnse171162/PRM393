# CUSTOMER FLUTTER IMPLEMENTATION REPORT

## Scope and sources

Customer Flutter frontend in `FE/`. The visual reference is the Customer portion
of `stitch_courtgo_sport_court_booking (1).zip`, extracted under
`docs/design/stitch/`. HTML was used where an exported PNG was invalid. CourtGo
is one brand with multiple branches. Existing backend DTOs, controllers and
Swagger determine business behavior; responses remain DTO + ProblemDetails.

Backend draft changes predate this frontend task and were preserved. Emulator
verification uses the committed backend at `c586698` in a detached local
worktree, connected to the existing local SQL Express database. No schema
migration or production deployment is part of this work.

## Screens and routes

- Startup `/splash`: secure token restore, session validation, offline retry.
- Login `/login`, registration `/register`: actual backend fields and validation,
  optional email, no OTP/social/reset-password workflow invented.
- Five persistent Customer tabs: `/customer/home`, `/customer/explore`,
  `/customer/bookings`, `/customer/notifications`, `/customer/account`.
- Area selection `/location`: search areas, recent choices and permission-based
  current location. This is a search filter, not a customer's home address.
- `/customer/centers/:id` and `/customer/centers/:id/reviews`.
- `/customer/courts/:id` and `/customer/courts/:id/availability`.
- `/customer/bookings/:id` and suffixes `/checkout`, `/payment`, `/success`,
  `/qr`, `/cancellation`, `/review`.

Deep screens hide bottom navigation. Stateful indexed tab navigation and list
storage keys preserve tab state. Staff/Admin placeholders were not expanded.

## Design system and reusable widgets

Central palette, spacing, typography, radii, durations and shadows live in
`core/theme`. Emerald primary, navy text, white/neutral surfaces, mint accents,
rounded 12–16 px cards and 50 px primary actions follow the supplied visual
direction. Availability selections animate at 180 ms. Cached images have
neutral placeholders and error fallbacks. Decorative brand artwork from the
Stitch login export is bundled locally, so Login/Home headers work offline.

Reusable components: CourtGoAppBar, BrandMark, PrimaryButton, SecondaryButton,
AppTextField, SearchField, StatusChip, SportChip, CenterCard, CourtCard,
BookingCard, PriceText, SectionHeader, LoadingSkeleton, NoticeState,
RequestView, BottomSheetContainer, ActionDock and InfoLine. RoleShell hosts the
existing Material NavigationBar. Shared ErrorState/EmptyState remain available.

## API services and state

`CustomerApi` wraps `ApiClient`/Dio. Typed models parse center/court details,
availability, booking snapshots, paged results and notifications.
`DiscoveryProvider` owns sport/area/search/quick-booking state, 350 ms debounce,
recent areas and optional distance sorting. `AvailabilityProvider` controls
consecutive selection and hold concurrency. `PagedController` handles history,
reviews and notifications, including stale-response protection. Notification
badge and auth have dedicated providers. Screen-local state owns forms and
payment polling; polling stops while backgrounded.

Actual endpoints used:

- Auth: POST `/api/auth/register`, `/login`, `/refresh`, `/logout`; GET `/me`.
- Discovery: GET `/api/sports`, `/api/sport-centers`, `/api/sport-centers/{id}`,
  `/api/courts/{id}`, `/api/courts/{id}/availability?date=yyyy-MM-dd`.
- Booking: POST `/api/bookings/hold`; GET `/api/bookings?statusGroup=...`,
  `/api/bookings/{id}`, `/api/bookings/{id}/qr`.
- Payment: POST `/api/bookings/{id}/payments/deposit`,
  GET `/api/bookings/{id}/payment-status`, Development-only
  POST `/api/dev/payments/{paymentId}/simulate`.
- Cancellation: GET/POST `/api/bookings/{id}/cancellation-requests`.
- Notifications: GET `/api/notifications`, `/api/notifications/unread-count`;
  PATCH `/api/notifications/{id}/read`, `/api/notifications/read-all`.
- Reviews: POST `/api/bookings/{id}/reviews`;
  GET `/api/sport-centers/{id}/reviews`.

The center list is an unpaged backend array; FE does not invent paging or
availability filtering for that endpoint. Booking/notification/review lists
use the backend's pagination.

## Auth behavior

Tokens are stored in flutter_secure_storage. Concurrent protected 401 responses
share one token refresh; successful rotation persists both tokens, and the
original request is retried once. A failed refresh clears authenticated state.
Logout revokes the refresh token through the real API and clears local tokens.
Offline startup retains credentials and offers retry. Guest browsing is public
only; booking and notification routes require authentication.

## Booking and payment

Successful booking/payment/review mutations invalidate retained booking lists
and notification state, so returning to an existing tab reflects new activity.
Availability, prices and slot statuses come from the server. Only consecutive
60-minute slots on the same court can be selected. A hold conflict refreshes
availability and clears stale selection. Checkout uses hold snapshot amounts
and expiry; authoritative totals always come from the backend.

MoMo/VNPay deposit creation remains Pending until status polling reports a
succeeded Deposit transaction and the updated booking state. Failed/cancelled
attempts show retry. Expired holds require selecting another slot. Development
simulation is explicitly labeled and restricted to debug + dev configuration +
Development gateway. **No real money is transferred.** Remaining payment is
explained as a later Staff action.

History and detail use historical booking snapshots. QR is fetched separately
from the authorized endpoint and rendered only on that screen. Tokens are not
displayed as text or logged. QR_NOT_AVAILABLE produces a friendly retry state.

## Cancellation, notifications and reviews

Cancellation submits the actual reason and displays the server-calculated
refund amount returned after submission. Pending requests do not mark a booking
Cancelled. UI does not promise a fixed cutoff or refund percentage.

Notifications support paged loading, unread counts, one/all read actions and
internal Booking references. Arbitrary backend deep-link strings are not
executed.

Reviews require a backend Completed booking, 1–5 stars, optional comment up to
1,000 characters and a guarded submit action. Successful submission is remembered
per user/booking locally; the backend also rejects duplicate reviews. Booking
detail reflects known review state.

## Loading, empty, error and offline states

RequestView and paged controllers provide skeletons, empty content, friendly
ProblemDetails mapping and retry actions. 401, 403, 404, 409, 429 and server/network
failures do not expose backend stack traces. Forms validate before submission,
disable duplicate actions while pending, and preserve input after recoverable
failures. Payment confirmation is never inferred from a button tap.

## Stitch corrections and backend limitations

- Removed unrelated marketplace brands, fabricated ratings/prices, promotions,
  amenities, OTP, social sign-in, profile editing and reset-password actions
  unsupported by the current API.
- Removed unsupported payment methods and pay-at-counter deposit promises.
- Removed hardcoded cancellation cutoffs and 100% refund promises.
- Current booking detail has no review-state field or review lookup by booking:
  review state from another device cannot be known before a duplicate response.
- Cancellation has no preview/policy-description endpoint: the authoritative
  refund estimate becomes available after a request is submitted.
- Detail has no complete status-history payload or hold expiry; the UI presents
  known booking state and uses expiry when the hold/payment response includes it.
- No branch availability filter is exposed. Quick booking carries criteria to
  discovery; final availability is checked on the selected court.
- Some existing local seed strings are mojibake (court/address). The frontend
  preserves actual API values instead of replacing them with invented names.
- Production payment integration remains a backend dependency. Debug simulation
  does not constitute a live MoMo/VNPay integration.

## Verification

Recorded emulator verification on 2026-10-08 with Flutter 3.47.4 / Dart 3.13.3:

- Android emulator: **PASS, two Customer scenarios** on Pixel_7 / emulator-5554.
  The runner also counts its tearDownAll hook, giving a final counter of three.
- Scenario one: registration/login, Home/sport/branch/court/date, consecutive
  slots, hold, Pending deposit, Development success, Confirmed, history/detail/QR,
  cancellation request and notification read-all. API assertions confirm that a
  pending cancellation leaves the booking Confirmed and unread count reaches zero.
- Scenario two: Completed history, four-star review submission, return to detail
  and visible “Bạn đã gửi đánh giá” state. A dedicated local QA booking was made
  through hold/deposit APIs; only that owned booking's status was then set to
  Completed for the UI fixture. This does **not** verify Staff collection/check-in
  or time-based completion.
- Ten Flutter-rendered screenshots were captured. Core booking screens and review
  state were visually inspected. QR itself was asserted but excluded from saved
  screenshots to avoid persisting its token.
- Android debug APK build: **PASS**. The normal application build targets
  `lib/main.dart`, separate from the integration-test harness.

The emulator results above precede subsequent icon/splash branding changes; they
are historical evidence, not a new emulator run on 2026-10-09.

Rechecked current working tree on 2026-10-09 after the branding changes:

- `flutter analyze`: **PASS**, no issues (88.2 seconds).
- `flutter test test`: **PASS, 31 tests** (exit code 0).
- `git diff --check -- FE docs .gitignore`: **PASS**.
- The APK exists at `FE/build/app/outputs/flutter-apk/app-debug.apk`; no current
  Dart, Android source or asset file was newer than it at this check.

Remaining limits: the backend dependencies listed above, no live-money payment
gateway, and no iOS/release deployment verification. Local QA customers, bookings
and reviews remain in the development database under clearly marked test names.

Reproduction commands and fixture inputs are documented in `FE/README.md`.
Generated screenshots live in `FE/build/customer-qa/` and omit the QR payload.
