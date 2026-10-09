# CourtGo Customer

Flutter Material 3, Provider, Dio and go_router. Customer screens use the real
CourtGo API. Existing Staff/Admin route placeholders are outside this task.

```sh
flutter pub get
flutter run --dart-define=APP_ENV=dev --dart-define=API_BASE_URL=http://10.0.2.2:5080
flutter analyze
flutter test test
```

Run the API in Development on port 5080. `10.0.2.2` reaches the Windows host from
the Android emulator. HTTP is enabled only by the debug Android manifest; use
HTTPS for release. Location permission is requested only on “Vị trí hiện tại”.

MoMo/VNPay currently use the backend **Development simulation**. Debug payment
controls explicitly state that no real money is transferred. Controls require
all three conditions: debug build, `APP_ENV=dev`, and API `gateway=Development`.

The Android integration suite creates real QA customers/bookings in the local
development database. Do not point it at production. Its second scenario needs
a dedicated Customer with a Completed booking and no existing review. Supply
an ignored JSON file containing `APP_ENV`, `API_BASE_URL`, `E2E_PHONE`,
`E2E_PASSWORD`, `E2E_COMPLETED_BOOKING`:

```sh
flutter drive --driver=test_driver/customer_driver.dart --target=integration_test/customer_flow_test.dart -d emulator-5554 --dart-define-from-file=../.local-build/customer-e2e.json
```

Screenshots are written to `build/customer-qa/`; QR tokens are deliberately
excluded. The completed fixture tests Customer history/review behavior, not
Staff check-in or automatic completion.

On Windows, this checkout's apostrophe can break Flutter's generated test
imports. Run commands through the existing junction
`C:\Users\Admin\.codex\visualizations\2026\10\07\01a116eb-e47f-7513-b2dd-4e8c81beb698\courtgo-fe`
when needed. Gradle cache
and temporary output can be redirected to `.local-build/` on drive D.

See [Customer implementation report](../docs/customer-flutter-implementation.md)
for routes, API contracts, design decisions and verification results.
