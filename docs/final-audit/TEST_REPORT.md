# Test report — 2026-10-25

## Kết quả cuối
- Backend Release build: thành công, 0 warning / 0 error.
- Backend **399/399 passed**: 229 integration + 170 unit, **0 skipped**. SQL workflow chạy trên database QA CourtGoStaffPaymentQa20261009 có sẵn, không migration hoặc cập nhật thủ công DB thật.
- Flutter **98/98 passed**. Flutter analyze: **0 error, 0 warning, 33 info**; vẫn trả non-zero do lint info, không gọi là analyze sạch.
- APK debug build thành công; adb install -r thành công, app mở được và giữ phiên Admin.
- APK: `D:\PRM393_Project'\FE\build\app\outputs\flutter-apk\app-debug.apk`, 238441536 bytes, timestamp bản cuối 2026-10-25 23:06:16 theo đồng hồ máy.

## Android Emulator: kết quả trực tiếp
- Pixel_7 / emulator-5554, package com.prm393.courtgo_mobile.
- Dashboard bản cuối hiển thị occupancy 4.4% thay vì 444.0%.
- Admin → Cơ sở → Q7 → chi tiết → Chỉnh sửa: đầy đủ form và dữ liệu, cuộn được, Đổi vị trí render, không trắng.
- Photo picker mở; chọn ảnh QA chụp trong lần kiểm tra → upload thật → file trên API → GET 200 image/png → preview render.
- Không bấm Lưu ảnh QA lên cơ sở thật. File upload thử chưa được gắn bản ghi; không tự dọn file server khi chưa có chính sách retention.
- Sân A1 → chi tiết → Chỉnh sửa: form mở, picker mở; hủy picker vẫn giữ tên sân và nút chọn ảnh.
- Deep link courtgo://payment-result không booking ID: Android chuyển intent vào MainActivity thành công. Đây là kiểm tra dispatch, không phải paid-state E2E dưới phiên Customer.
- Không tạo giao dịch VNPAY, không hoàn tiền, không sửa DB thủ công.

## Regression/contract được kiểm tra
- ImageUploadApiTests: chưa login 401; Customer/Staff 403; SVG/oversize 400; PNG upload/read bytes; safe filename; tạo/sửa center/court và đọc lại qua API Customer. DB/storage fixture riêng.
- AdminImageUrlTests: coverImageUrl danh sách/chi tiết.
- admin_image_upload_test: success/failure giữ/thay ảnh, resolve URL theo host API, chặn traversal, multipart retry sau refresh.
- admin_image_forms_test: preview saved và URL lỗi không mất form; admin_center_form_test/admin_router_test dùng production theme.
- final_audit_navigation_test: bốn link Admin và redirect checkout cũ, không POST tạo giao dịch.
- final_audit_regression_test: SportChip callback, nhãn trạng thái, percentage points.
- BookingHoldServiceTests: midnight rollover ngoài giờ, test trước sửa fail rồi đạt.

## Failures đã sửa trong quá trình test
- BoxConstraints forces an infinite width: sửa minimum width OutlinedButton.
- ExpansionTile trong DecoratedBox thiếu Material: thêm Material riêng.
- Router test fixture gọi User/setUserForTesting/MemoryLocalStorage không tồn tại: thay auth test subclass + SharedPreferences mock, giữ kiểm tra theme thật.
- Các lỗi trên không còn trong 98 tests cuối; không xóa test để làm xanh.

## Lệnh thực thi
```powershell
$env:DOTNET_PROCESSOR_COUNT='2'
$env:COURTGO_TEST_SQLSERVER='Server=.\SQLEXPRESS;Database=CourtGoStaffPaymentQa20261009;Trusted_Connection=True;TrustServerCertificate=True'
dotnet build BE/CourtGo.sln -c Release --no-restore -m:1 -p:UseAppHost=false
dotnet test BE/CourtGo.sln -c Release --no-build --no-restore -m:1
# Trong FE qua P: subst workspace để tránh apostrophe trong đường dẫn Gradle
flutter test --reporter expanded
flutter analyze
flutter build apk --debug --no-pub --target=lib/main.dart --dart-define=API_BASE_URL=http://10.0.2.2:5080
adb install -r FE/build/app/outputs/flutter-apk/app-debug.apk
adb shell am start -n com.prm393.courtgo_mobile/.MainActivity
adb shell am start -W -a android.intent.action.VIEW -d courtgo://payment-result com.prm393.courtgo_mobile
```

## Bằng chứng local
`.local-build/` (git-ignored): image-upload-backend-build.log, image-upload-backend-tests.log, image-upload-flutter-tests.log, image-upload-analyze.log, image-upload-apk-build.log, image-upload-focused.log, center-edit-theme-before.log, courtgo-center-edit-fixed.png, courtgo-upload-preview.png, courtgo-court-edit-fixed.png.

## Chưa kiểm chứng và giới hạn
- Không chạy thủ công toàn bộ 58 màn hình; inventory/source review khác manual E2E.
- Chưa lưu ảnh thử vào bản ghi thật rồi mở Customer UI thật. Save/read được kiểm chứng bằng integration tests, render bằng widget tests; không thay ảnh người dùng bằng fixture.
- Chưa chạy picker iOS/Windows/điện thoại vật lý; đã thêm iOS photo-library usage description, chưa build iOS.
- Tunnel callback không truy cập được khi audit; merchant IPN chưa xác minh. Không giao dịch Sandbox mới/refund thật. MoMo, camera scan và các provider khác chưa có E2E mới.
- Local image storage cần persistent volume/backup và retention khi triển khai; header validation không thay full decode/antivirus.
