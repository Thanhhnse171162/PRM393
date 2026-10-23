# Cấu hình MoMo Test và VNPAY Sandbox

Hiện chưa có tài khoản merchant hoặc HTTPS callback được xác minh. Không dùng tiền thật. `ProviderMode=Production` chọn adapter nhà cung cấp; endpoint vẫn là sandbox theo cấu hình mẫu. Tên mode không đồng nghĩa đã sẵn sàng vận hành tiền thật.

## 1. Database

Sao lưu database triển khai trước khi áp dụng migration. Migration `20261009044240_AddProviderPaymentMetadata` thêm ba cột nullable vào Payments và unique index cho mã biên nhận; không xóa dữ liệu. `ProviderTransactionId` giữ nghĩa mã order do CourtGo tạo; `ProviderReceiptId` lưu mã nhà cung cấp kèm tên gateway; `CheckoutUrl` phục hồi phiên thanh toán; `GatewayName` phân biệt nguồn.

Từ thư mục repository:

```powershell
dotnet ef database update --project BE/src/CourtGo.Infrastructure --startup-project BE/src/CourtGo.Api
```

Kiểm tra connection string trước khi chạy. Trong quá trình kiểm thử, chỉ database riêng `CourtGoStaffPaymentQa20261009` đã được tạo/cập nhật; không tự áp dụng migration vào database ứng dụng của bạn.

## 2. Merchant và HTTPS

Đăng ký MoMo Test và VNPAY Sandbox. Nhận thông tin merchant trực tiếp từ nhà cung cấp. Cấu hình API có hostname HTTPS công khai với chứng chỉ hợp lệ; callback phải truy cập được từ Internet, không yêu cầu JWT, không trả trang đăng nhập và không bị proxy đổi tham số.

Đăng ký các URL:

- MoMo IPN: `https://<api-host>/api/payments/momo/ipn` (POST JSON).
- MoMo redirect: `https://<api-host>/api/payments/momo/return` (GET).
- VNPAY IPN: `https://<api-host>/api/payments/vnpay/ipn` (GET, đăng ký trên merchant portal; không phải tham số gửi trong payment URL).
- VNPAY return: `https://<api-host>/api/payments/vnpay/return` (GET).

Nếu đặt sau reverse proxy, cấu hình địa chỉ client đáng tin cậy ở hạ tầng ASP.NET trước khi tạo VNPAY URL. Code lấy `RemoteIpAddress`, không tin `X-Forwarded-For` do client tự gửi. Không mở trust-all forwarded headers. Đồng bộ thời gian máy chủ; VNPAY dùng GMT+7 và thời hạn giữ sân.

## 3. Biến môi trường

File `BE/src/CourtGo.Api/appsettings.example.json` chỉ minh họa, không được tự load. Thiết lập qua secret manager của hosting hoặc dotnet user-secrets khi phát triển. Không commit giá trị thật và không gửi khóa vào chat.

Các tên biến môi trường tương ứng:

```text
Payments__ProviderMode=Production
Payments__MoMo__PartnerCode=<merchant>
Payments__MoMo__AccessKey=<secret storage>
Payments__MoMo__SecretKey=<secret storage>
Payments__MoMo__ApiBaseUrl=https://test-payment.momo.vn
Payments__MoMo__RedirectUrl=https://<api-host>/api/payments/momo/return
Payments__MoMo__IpnUrl=https://<api-host>/api/payments/momo/ipn
Payments__VnPay__TmnCode=<merchant>
Payments__VnPay__HashSecret=<secret storage>
Payments__VnPay__PaymentUrl=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
Payments__VnPay__ReturnUrl=https://<api-host>/api/payments/vnpay/return
Payments__VnPay__IpnUrl=https://<api-host>/api/payments/vnpay/ipn
Payments__VnPay__Version=2.1.0
Payments__VnPay__Locale=vn
```

Chỉ phương thức được sử dụng cần đủ cấu hình. Thiếu cấu hình trả `PAYMENT_GATEWAY_NOT_CONFIGURED`, không tạo giao dịch giả. Simulator chỉ có khi cả ASPNETCORE_ENVIRONMENT=Development và Payments:ProviderMode=Development. Staging/Production không đăng ký route simulator. Nếu chuyển mode trên database đã có giao dịch pending từ mode cũ, cần xử lý các lượt thử nghiệm cũ trước; không tái sử dụng giao dịch Development để thu tiền thật.

Kiểm tra `appsettings.Local.json` (nếu có): ứng dụng hiện load file này sau các nguồn cấu hình mặc định; loại bỏ giá trị trùng có thể che biến môi trường. Không mang file local chứa secret vào bản triển khai.

## 4. Flutter

```text
flutter run --dart-define=API_BASE_URL=https://<api-host> --dart-define=APP_ENV=prod
```

Flutter mở URL HTTPS do backend trả về bằng trình duyệt ngoài. Return URL kiểm tra chữ ký rồi điều hướng cố định tới `courtgo://payment-result?bookingId=<id>`. Android/iOS đăng ký scheme này; app phục hồi phiên đăng nhập và gọi payment-status. Tham số status từ link không có quyền xác nhận tiền. App tự kiểm tra khoảng 2 phút, có nút kiểm tra lại, phục hồi sau khi quay về từ trình duyệt và phân biệt tiền đến muộn cần hoàn trả.

## 5. Checklist sandbox cần thực hiện khi có merchant

Với từng provider, tạo hold mới bằng Customer, mở URL thanh toán, thanh toán thử, kiểm tra IPN trên HTTPS, xác nhận đúng một Deposit Succeeded, Booking Confirmed, các slot Reserved và một QR. Mở lại return URL phải không làm thay đổi trạng thái. Thử hủy/thất bại, callback trùng, đóng app trước callback và mở lại, hết hold trước IPN, sai số tiền và chữ ký. Cuối cùng Staff verify → thu tiền còn lại → FullyPaid → check-in → CheckedIn.

Lưu bằng chứng mã giao dịch đã che thông tin nhạy cảm, thời gian, kết quả database/API và ảnh màn hình. Chỉ chuyển endpoint/credentials production sau khi sandbox hoàn tất và merchant xác nhận quyền sản phẩm.

## 6. Đối soát và hoàn tiền

Callback thanh toán thành công sau khi hold kết thúc được ghi nhận là tiền đã nhận và tạo một Refund Pending liên kết giao dịch gốc. Booking không được hồi sinh, QR không được phát và slot không được chiếm lại. Admin phê duyệt hủy cũng chỉ tạo refund pending.

Chưa có quyền merchant refund: adapter giữ yêu cầu ở `ManualReconciliation`; không gửi lệnh hoàn tiền tự động và không báo Succeeded. Cần đối soát qua merchant portal và bổ sung xác nhận refund đã được nhà cung cấp kiểm chứng trước khi tự động hóa hoàn tiền. Không dùng simulator để đánh dấu hoàn tiền thật.

Nếu tạo phiên MoMo bị timeout hoặc provider báo trùng order/request, giữ nguyên PaymentId và pending attempt. Không đổi khóa idempotency để thử thu lần khác. IPN vẫn có thể xử lý intent đã lưu; trường hợp không lấy lại được checkout URL cần đối soát merchant. Chưa có worker truy vấn merchant định kỳ.

Tài liệu chính thức dùng khi triển khai: [MoMo create](https://developers.momo.vn/v3/vi/docs/payment/api/wallet/onetime/), [MoMo IPN](https://developers.momo.vn/v3/vi/docs/payment/api/result-handling/notification/), [MoMo result codes](https://developers.momo.vn/v3/vi/docs/payment/api/result-handling/resultcode/), [VNPAY PAY/IPN](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html).
