# Cấu hình MoMo Test và VNPAY Sandbox

Ngày 24/10/2026 đã xác minh merchant VNPAY Sandbox qua QueryDR có chữ ký và đối soát giao dịch 15713716. IPN đăng ký tại merchant và E2E thanh toán mới vẫn cần xác minh. Không dùng tiền thật. `ProviderMode=Production` chọn adapter nhà cung cấp; endpoint vẫn là sandbox theo cấu hình. Tên mode không đồng nghĩa đã sẵn sàng vận hành tiền thật. Xem `vnpay-incident-2026-10-24.md` để biết bằng chứng và trạng thái hiện tại.

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

Nếu đặt sau reverse proxy, cấu hình địa chỉ client đáng tin cậy ở hạ tầng ASP.NET trước khi tạo VNPAY URL. Adapter hiện có xử lý header proxy; không coi header do client tự gửi là bằng chứng thanh toán. Đồng bộ thời gian máy chủ; VNPAY dùng GMT+7 và thời hạn giữ sân.

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

Flutter mở URL HTTPS do backend trả về bằng trình duyệt ngoài. Return URL kiểm tra chữ ký và payment trong DB rồi hiện trang HTML có nút mở `courtgo://payment-result?bookingId=<id>`; thao tác nhấn giúp Chrome cho phép mở custom scheme. Return không cập nhật tiền. App phục hồi phiên đăng nhập và gọi payment-status; pending VNPAY được đối soát khi quay lại app, mở màn hình kết quả hoặc bấm kiểm tra. Tham số status từ link không có quyền xác nhận tiền. Có thể vào Lịch đặt → Chi tiết → Kiểm tra kết quả thanh toán nếu đã đóng Chrome. App polling trạng thái tối đa khoảng 2 phút; truy vấn merchant có cooldown riêng 5 phút.

## 5. Checklist sandbox cần thực hiện khi có merchant

Với từng provider, tạo hold mới bằng Customer, mở URL thanh toán, thanh toán thử, kiểm tra IPN trên HTTPS, xác nhận đúng một Deposit Succeeded, Booking Confirmed, các slot Reserved và một QR. Mở lại return URL phải không làm thay đổi trạng thái. Thử hủy/thất bại, callback trùng, đóng app trước callback và mở lại, hết hold trước IPN, sai số tiền và chữ ký. Cuối cùng Staff verify → thu tiền còn lại → FullyPaid → check-in → CheckedIn.

Lưu bằng chứng mã giao dịch đã che thông tin nhạy cảm, thời gian, kết quả database/API và ảnh màn hình. Chỉ chuyển endpoint/credentials production sau khi sandbox hoàn tất và merchant xác nhận quyền sản phẩm.

## 6. Đối soát và hoàn tiền

Callback thanh toán thành công sau khi hold kết thúc được ghi nhận là tiền đã nhận và tạo một Refund Pending liên kết giao dịch gốc. Booking không được hồi sinh, QR không được phát và slot không được chiếm lại. Admin phê duyệt hủy cũng chỉ tạo refund pending.

Chưa có quyền merchant refund: adapter giữ yêu cầu ở `ManualReconciliation`; không gửi lệnh hoàn tiền tự động và không báo Succeeded. Cần đối soát qua merchant portal và bổ sung xác nhận refund đã được nhà cung cấp kiểm chứng trước khi tự động hóa hoàn tiền. Không dùng simulator để đánh dấu hoàn tiền thật.

VNPAY có `POST /api/bookings/{bookingId}/payments/reconcile` (JWT Customer, chỉ booking của chính user). Server lấy reference/amount/create date từ payment đã lưu, kiểm tra HMAC-SHA512 QueryDR, merchant, loại giao dịch, reference, VND và số tiền trước khi gọi chung `DepositPaymentService`. Không tạo payment mới. Cooldown 5 phút cho mỗi payment trong một process; sau restart/nhiều instance VNPAY vẫn có thể trả 94. Phản hồi chưa ký, pending, mã lạ hoặc lỗi mạng không được dùng để xác nhận cọc. Không có worker QueryDR định kỳ; IPN vẫn là đường đồng bộ chính.

Operator có thể đọc giao dịch Sandbox bằng công cụ local, không cần tạo JWT hay mở endpoint Admin mới:

```powershell
dotnet run --project BE/tools/CourtGo.PaymentReconcile -- BE/src/CourtGo.Api <PaymentId>
```

Mặc định chỉ đọc. Chỉ thêm `--apply <expected-provider-transaction-number>` khi đã xác minh đúng giao dịch và có quyền đối soát; công cụ kiểm tra lại chữ ký và receipt rồi gọi nghiệp vụ hiện có. Không áp dụng schema migration. Không tự chạy lại liên tục: VNPAY giới hạn QueryDR cùng giao dịch trong 5 phút.

## 7. Tunnel riêng cho callback VNPAY khi demo local

Chạy API với `ASPNETCORE_ENVIRONMENT=Development`, `Payments__CallbackOnlyPort=5081` và hai listener `http://0.0.0.0:5080;http://127.0.0.1:5081`. Flutter emulator vẫn gọi `http://10.0.2.2:5080`. Chỉ trỏ Cloudflare tới `http://127.0.0.1:5081`: cổng này chỉ cho GET `/health`, `/api/payments/vnpay/ipn`, `/api/payments/vnpay/return`; mọi route khác trả 404. Kiểm tra giới hạn trước khi mở tunnel. MoMo cần thiết kế allowlist riêng nếu sử dụng cổng này.

Sau khi HTTPS `/health` trả `200 Healthy`, cập nhật đúng `Payments:VnPay:ReturnUrl` và `Payments:VnPay:IpnUrl` trong User Secrets rồi restart đúng API. Merchant IPN phải được cập nhật riêng trong Portal/hỗ trợ VNPAY. Giữ API và cloudflared chạy trong suốt demo. URL checkout đã phát hành giữ snapshot URL cũ; không dùng lại URL của lần thử cũ sau khi đổi tunnel.

Quick Tunnel tạo hostname tạm, phụ thuộc vòng đời process; không có cam kết uptime. Nên dùng Named Tunnel với hostname ổn định cho demo dài hạn. Chưa tạo tài khoản, mua domain, thay DNS hay triển khai Named Tunnel trong task này. HashSecret từng chia sẻ trong chat cần được đổi cho lần sử dụng dài hạn; không tự đổi khi chẩn đoán.

Nếu tạo phiên MoMo bị timeout hoặc provider báo trùng order/request, giữ nguyên PaymentId và pending attempt. Không đổi khóa idempotency để thử thu lần khác. IPN vẫn có thể xử lý intent đã lưu; trường hợp không lấy lại được checkout URL cần đối soát merchant. Chưa có worker truy vấn merchant định kỳ.

Tài liệu chính thức: [MoMo create](https://developers.momo.vn/v3/vi/docs/payment/api/wallet/onetime/), [MoMo IPN](https://developers.momo.vn/v3/vi/docs/payment/api/result-handling/notification/), [MoMo result codes](https://developers.momo.vn/v3/vi/docs/payment/api/result-handling/resultcode/), [VNPAY PAY/IPN](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html), [VNPAY QueryDR](https://sandbox.vnpayment.vn/apis/docs/truy-van-hoan-tien/querydr%26refund.html), [VNPAY code 94 / giới hạn 5 phút](https://sandbox.vnpayment.vn/apis/docs/bang-ma-loi/), [Cloudflare Quick Tunnel](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/).
