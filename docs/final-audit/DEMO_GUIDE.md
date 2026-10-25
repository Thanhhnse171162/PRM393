# Demo guide — Windows / CourtGo

## 1. SQL Server
Dùng SQL Express đã cấu hình cho CourtGoDb. Kiểm tra `Get-Service 'MSSQL$SQLEXPRESS'`; nếu cần, khởi động bằng tài khoản có quyền. Không reset/seed/migration hoặc sửa payment thủ công trên dữ liệu demo đang dùng. Repo đã có 3 migrations, cần đối chiếu lịch sử trước khi triển khai môi trường mới.

## 2. ASP.NET Core Backend
Workspace có dấu apostrophe cuối tên. Cổng 5080 phải trống hoặc đúng API mới đang chạy; không chạy API thứ hai.

```powershell
Set-Location -LiteralPath "D:\PRM393_Project'"
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:Seed__Enabled='false'
$env:Payments__CallbackOnlyPort='5081'
dotnet run --project BE/src/CourtGo.Api --no-launch-profile --urls 'http://0.0.0.0:5080;http://127.0.0.1:5081'
```

Health: http://localhost:5080/health. API Release mới đã được chạy seed=false trong kiểm thử. Không in secrets/token/password ra terminal chia sẻ.

Ảnh upload nằm tại `BE/src/CourtGo.Api/App_Data/images` khi chạy content root dự án; giữ thư mục và backup khi deploy. JPEG/PNG ≤5 MiB; Flutter resolve `/api/images/...` theo API_BASE_URL.

## 3. Android Studio Emulator
Device Manager → Pixel_7, hoặc `D:\DAndroidSdk\emulator\emulator.exe` với AVD đã có. `D:\DAndroidSdk\platform-tools\adb.exe devices` phải thấy emulator-5554. Không chạy hai emulator cùng cổng.

## 4. Flutter
Flutter: `D:\develop\flutter\bin\flutter.bat`; Java: `D:\develop\androi\jbr`; Android SDK: `D:\DAndroidSdk`.

```powershell
Set-Location -LiteralPath "D:\PRM393_Project'\FE"
& D:\develop\flutter\bin\flutter.bat run --dart-define=API_BASE_URL=http://10.0.2.2:5080
```

APK debug mới đã cài. Điện thoại thật cần API_BASE_URL là địa chỉ LAN máy API, cùng mạng và firewall 5080; build lại, không dùng 10.0.2.2 cho điện thoại thật. Nếu Gradle vướng apostrophe, dùng drive subst còn trống (P: đang trỏ workspace trong phiên kiểm tra); không ghi đè drive đã dùng.

## 5. Cloudflare + VNPAY Sandbox
Chỉ cần tunnel khi demo callback provider. Listener 5081 chỉ cho GET health, VNPAY Return/IPN; không publish toàn API qua tunnel callback.

```powershell
cloudflared tunnel --url http://127.0.0.1:5081
```

Dùng HTTPS hostname do tunnel cấp hoặc named tunnel ổn định. Cập nhật ReturnUrl/IpnUrl trong cấu hình local bí mật và đăng ký IPN ở merchant portal. Quick tunnel có thể đổi hostname; URL cũ NXDOMAIN không chứng minh payment thất bại. Không dùng callback-only 5081 làm API_BASE_URL vì auth/ảnh bị chặn có chủ đích.

Trước demo payment: HTTPS health truy cập được, merchant Sandbox/signing secret đúng, callback route đúng, IPN đăng ký đúng, backend truy cập provider được. Chưa xác minh portal trong audit. Không tạo giao dịch chỉ để thử deep link. Browser Return không tự xác nhận DB đã Confirmed.

## 6. Ba luồng demo đề xuất
1. **Admin ảnh:** Cơ sở → chi tiết → Chỉnh sửa → Chọn ảnh từ thiết bị → chọn JPEG/PNG → đợi preview → Lưu → danh sách/chi tiết. Làm tương tự Sân. Customer ở phiên khác mở lại/refresh để xem ảnh. Muốn ảnh từ máy vào emulator: kéo file vào emulator hoặc `adb push <ảnh-của-bạn> /sdcard/Pictures/`, chọn qua Files/Photos.
2. **Customer → Staff:** sau khi callback sẵn sàng và bạn chủ động cho phép giao dịch Sandbox: login → availability → hold → Deposit Pending → Succeeded → Confirmed → QR; Staff đúng cơ sở → Verify → remaining payment → FullyPaid → Check-in → CheckedIn. Không bỏ qua thanh toán đầy đủ mặc định.
3. **Xử lý lỗi:** xem Expired/RefundPending sẵn có và không QR; dùng API/widget tests cho duplicate/wrong amount/late payment. Thử URL ảnh sai và hủy picker để thấy form giữ dữ liệu. Không tạo giao dịch hay hoàn tiền chỉ để demo lỗi.

## 7. Troubleshooting và checklist trước demo
- Form trắng: cài APK mới, không chỉ mở lại APK cũ. Bản sửa minimum width nút đã kiểm tra trên emulator.
- Ảnh lỗi: API 5080 đang chạy, file tồn tại App_Data/images, API_BASE_URL đúng thiết bị; localhost trên điện thoại không trỏ Windows.
- Upload xong vẫn cần bấm Lưu; file sai/oversize báo lỗi, form vẫn giữ dữ liệu. Upload chỉ dành Admin.
- 401: đăng nhập lại nếu refresh hết hạn. Không sao chép token thủ công.
- NXDOMAIN sau VNPAY: phục hồi HTTPS callback; không sửa DB thành paid, giữ RefundPending khi payment trễ.
- Chuẩn bị tài khoản Customer/Staff/Admin riêng, staff assignment đúng cơ sở, giờ hoạt động/ngày ngoại lệ/giá đúng, backup DB + ảnh, thử ảnh của bạn trước buổi demo.
- Refund thật và cửa sổ check-in sớm/muộn chưa hoàn thiện; 33 lint info và kiểm thử ngoài thiết bị còn trong backlog. Không tuyên bố production-ready.
