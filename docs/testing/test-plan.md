# Test Plan

## Scope
Automated tests for FE and BE plus manual scenario checks.

## Automated

| Area | Location | Tool | Current examples |
|------|----------|------|------------------|
| Flutter unit | `FE/test/unit` | flutter_test | Validators, CurrencyUtils, RouteGuard |
| Flutter widget | `FE/test/widget` | flutter_test | AppButton (tap / loading / disabled) |
| Backend unit | `BE/tests/CourtGo.UnitTests` | xUnit | SlotRules, PaymentRules, PasswordHasher |
| Backend integration | `BE/tests/CourtGo.IntegrationTests` | xUnit + WebApplicationFactory | public ping 200, protected 401 |

Run: `cd FE && flutter test` · `cd BE && dotnet test`. CI runs them on every PR.

## Manual scenarios (to grow with features)
1. Guest browses centers, is asked to log in at booking.
2. Select 17–18 + 18–19 → one booking 17:00–19:00.
3. Two users select the same slot; second gets 409 after Continue.
4. Deposit paid → Confirmed + DepositPaid; staff collects remainder → FullyPaid.
5. Staff cannot see other branches' bookings; Customer cannot open `/admin/*`.
6. Small phone (320 dp wide), keyboard open on forms, Android back button.

## Exit criteria
CI green, no failing tests, key flows verified on Android emulator.
