# VNPAY Sandbox — đối soát giao dịch 15713716

Báo cáo ngày 24/10/2026. Các giờ dưới đây là UTC+07:00 (giờ giao dịch VNPAY), không phải thời gian giả lập. Không tạo thêm booking hoặc payment checkout để điều tra. Không chạy migration, SQL UPDATE thủ công, commit hoặc push.

## A. Giao dịch gốc 90.000 VND

- VNPAY TransactionNo: **15713716**, ngân hàng **NCB**.
- CourtGo PaymentId: **285f1312-bbdb-4309-ac48-1054c8822dbb**.
- Merchant reference: **285f1312bbdb4309ac481054c8822dbb**.
- BookingId: **8e6c9d5f-a03f-427b-90cc-d0480467f87c**.
- BookingCode: **CG-20261024-7EP9NL**.
- Provider/method: VNPay; `Payments:ProviderMode=Production` chọn adapter thật, endpoint là **Sandbox**, không phải tiền production.
- Payment transaction trước: `Pending`, receipt NULL, PaidAt NULL; sau: **Succeeded**, receipt **VNPay:15713716**.
- Booking trước: `PendingPayment / Unpaid`; sau: **Expired / RefundPending**.
- TotalAmount **300.000 VND**, DepositAmount **90.000 VND**. PaidAmount **0 → 90.000**, RemainingAmount theo ledger **300.000 → 210.000 VND**. Con số còn lại không có nghĩa khách phải trả tiếp cho booking đã Expired; booking này không đủ điều kiện thu tiền/check-in.
- Payment CreatedAt: **12:40:50.5686505**. Original `vnp_CreateDate`: **20261024124050**.
- VNPAY PayDate: **20261024124120**, tức **12:41:20**; thanh toán ở provider diễn ra trước hạn giữ sân.
- HoldExpiresAt trước: **12:45:44.5625180**; sau NULL vì booking đã hết hạn và được giải phóng.
- Payment không có cột UpdatedAt. PaidAt sau đối soát: **13:31:51.5996090**, là thời điểm CourtGo ghi nhận qua nghiệp vụ, không thay cho thời điểm provider đã thu tiền.
- Booking UpdatedAt trước NULL; sau **13:31:51.6415062**.

QueryDR trả `vnp_ResponseCode=00`, `vnp_TransactionStatus=00`, `vnp_TransactionType=01`, `vnp_Amount=9000000` (VND × 100). Đã xác minh HMAC-SHA512, merchant, reference, số tiền và currency VND của checkout gốc. Dữ liệu liên kết bằng merchant reference và receipt; không dựa riêng vào số tiền/thời gian.

Công cụ .NET Sandbox `BE/tools/CourtGo.PaymentReconcile` đã query lại độc lập, xác minh receipt 15713716 rồi gọi `DepositPaymentService.ApplyVerifiedResultAsync`. Không giả IPN hay sửa DB trực tiếp. Hai slot gốc trở thành `Released / IsOccupying=false`; không chiếm lại sân và không phát QR. Trước khi xử lý, truy vấn DB không thấy slot đang chiếm của booking khác trùng hai slot gốc; quy tắc hiện có vẫn không phục hồi hold đã quá hạn.

Nghiệp vụ hiện có đã tạo đúng một Refund Pending, Id **6f6d7a14-f321-4168-bc40-81a1274acb92**, liên kết payment gốc, gateway `ManualReconciliation`, PaidAt NULL. **Chưa gửi lệnh hoàn tiền**. Tiền đã được ghi nhận chính xác; phần hoàn tiền/xử lý thủ công vẫn còn chờ, booking không được Confirmed.

IPN thật của giao dịch gốc: **Chưa xác minh**. Không có log callback có chữ ký gốc đủ bằng chứng để kết luận VNPAY đã giao IPN hoặc chữ ký đã pass tại thời điểm thanh toán. Return gốc: ảnh lỗi DNS và việc xác minh DNS cũ cho thấy trình duyệt không tới được host đó. Không dùng HTTP 200/ack97 của probe chưa ký làm bằng chứng nhận IPN thật.

## B. Nguyên nhân

1. **Cấu hình/tunnel**: ReturnUrl và IpnUrl còn trỏ tới `wine-miscellaneous-galleries-models.trycloudflare.com`. DNS trả `DNS_ERROR_RCODE_NAME_ERROR`; lúc điều tra không có cloudflared chạy. Provider xử lý tiền độc lập nên có thể báo thành công trong khi browser return không tới backend.
2. **Dữ liệu chưa đồng bộ**: DB còn Pending/Unpaid và chưa có receipt. Chưa có bằng chứng IPN gốc; không thể kết luận riêng do sai registration hay delivery thất bại. Booking quá hạn theo đồng hồ nhưng còn PendingPayment trong DB do cleanup hold được chạy khi thao tác booking/availability; worker lifecycle hiện tại chỉ xử lý CheckedIn/InProgress.
3. **Lỗi thiếu chức năng khôi phục**: FE chỉ GET payment-status; BE chưa có QueryDR. Khi mất IPN, việc bấm kiểm tra không thể tìm thấy tiền đã thu ở provider. Đã bổ sung đối soát an toàn.
4. **Trải nghiệm return**: 302 sang custom scheme có thể bị Chrome chặn; đã dùng trang kết quả với thao tác nhấn mở CourtGo và hướng dẫn khôi phục trong Lịch đặt. Không biến return thành bằng chứng thu tiền.
5. **Tương thích QueryDR**: HTTP client không có User-Agent đã gặp HTTP 403 thực tế. Request hiện có `CourtGo/1.0`, Content-Length, timeout và endpoint cố định. Gọi lặp trong thời gian giới hạn gặp response chưa ký code94; không cập nhật trạng thái từ response đó. Cooldown theo payment là 5 phút, theo tài liệu VNPAY.
6. **Deep link khi app đang chạy**: kiểm thử Android phát hiện màn hình có thể giữ state booking trước khi link đổi BookingId. `app_router.dart` thêm key theo BookingId để khởi tạo lại PaymentPage; `app.dart` yêu cầu một frame cho callback điều hướng khi Flutter đang idle. PaymentPage bỏ qua response của trang đã dispose. Regression dùng AppRouter thật đã qua trên Flutter widget tests; kết quả Android cuối ghi trong báo cáo kiểm thử bổ sung.

## C. Thay đổi code và cấu hình

- Application: `IPaymentReconciliationService` bổ sung use case đối soát, giữ DTO trực tiếp + ProblemDetails.
- Infrastructure: `VnPayTransactionQuery` triển khai request/response signature riêng của QueryDR, strict matching, số tiền/currency gốc, các trường tùy chọn được ghép đúng vị trí; `PaymentReconciliationGate` giới hạn truy vấn; `PaymentReconciliationService` xác minh owner và tái dùng finalizer hiện có. Không sửa quy tắc khôi phục sân hoặc refund.
- `BookingPaymentsController`: Customer POST `/api/bookings/{bookingId}/payments/reconcile`, không nhận amount/reference từ client; giới hạn request và kiểm tra ownership.
- `PaymentNotificationVerifier`: từ chối currency không phải VND nếu callback có gửi currency; mã transaction không rõ không bị mặc định coi là failure. Success vẫn cần cả response00 và transaction00.
- `PaymentCallbacksController`: log kết quả đã lọc theo PaymentId/status/receipt; HTML return dùng bookingId lấy từ DB, cố định scheme, `no-store`, `no-referrer`, CSP, không phản chiếu query/message provider. Không gọi finalizer từ return.
- `Program.cs`: đăng ký QueryDR/DI/rate limiter, không ghi full callback query bằng framework request-start log. `PaymentCallbackPortMiddleware` giới hạn listener công khai riêng.
- `BE/tools/CourtGo.PaymentReconcile`: công cụ operator Sandbox, mặc định chỉ đọc; apply cần receipt mong đợi và chữ ký hợp lệ. Dùng cùng finalizer, không mở endpoint quản trị mới.
- FE `customer_api.dart`, `payment_page.dart`: truy vấn đối soát khi return/resume/manual, không tạo payment mới khi kiểm tra; không tiêu tốn QueryDR ngay lúc mở checkout; giữ trạng thái DB nếu mạng lỗi; xếp hàng kiểm tra khi resume trùng request đang chạy. UI phân biệt failed/cancelled/expired/refund-pending, không coi URL success=true là thành công.
- FE `booking_detail_page.dart`: đường khôi phục “Kiểm tra kết quả thanh toán” cho cả booking đã quá hạn.
- FE `app.dart`, `core/routing/app_router.dart`: khắc phục warm deep link và state dùng lại sai booking. `test/widget/payment_route_test.dart`, `integration_test/payment_return_states_test.dart`, `test_driver/payment_driver.dart` bổ sung kiểm thử router thật và UI trên Android với API fixture cách ly.
- Tests: `VnPayQueryTests`, `RealPaymentApiTests`, `PaymentCallbackPortTests`, `payment_state_test.dart`.
- Cập nhật `real-payment-configuration.md` về QueryDR, công cụ operator, giới hạn5 phút và cách chạy tunnel callback.

Runtime: ASP.NET Core **Development**, kết nối **CourtGoDb** kiểm tra thành công. API ứng dụng ở 5080; listener **127.0.0.1:5081** chỉ cho GET `/health`, `/api/payments/vnpay/ipn`, `/api/payments/vnpay/return`. Tunnel trỏ tới5081; Swagger và API booking qua tunnel trả404. Sau lần khởi động lại gần nhất:

- Public hostname: **https://also-processing-side-desperate.trycloudflare.com**.
- Return: **https://also-processing-side-desperate.trycloudflare.com/api/payments/vnpay/return**.
- IPN: **https://also-processing-side-desperate.trycloudflare.com/api/payments/vnpay/ipn**.
- HTTPS `/health`: **200 Healthy** tại thời điểm kiểm tra.
- Hai URL đã lưu trong .NET User Secrets, backend đã restart để nạp. TmnCode/HashSecret/PaymentUrl/ProviderMode/Version/Locale được giữ nguyên, không xuất ra báo cáo.
- Registration IPN tại VNPAY: **Chưa xác minh**. Đổi IpnUrl trong backend không tự thay registration merchant.
- Signed Return thật qua hostname mới: **Chưa xác minh**. Return hợp lệ đã có regression; chưa tạo giao dịch Sandbox mới để kiểm tra browser round trip.

Quick Tunnel là địa chỉ tạm, có thể đổi/mất khi process dừng. Cần Named Tunnel/hostname ổn định cho demo dài hạn; chưa tạo tài khoản, DNS hoặc hạ tầng ngoài. Automatic approval từng chặn tunnel tới toàn bộ 5080; đã thay bằng listener 5081 có allowlist, kiểm thử và kiểm tra 404 từ Internet trước khi mở. Không còn block do approval ở cấu hình hạn chế này.

## D. Kiểm thử

- Backend Release build qua junction không chứa dấu nháy: pass, 0 warning/error tại lần build kiểm tra.
- Backend toàn bộ: **385 pass, 0 fail, 0 skip** = 216 integration +169 unit. Đã bật 9 SQL Server tests trên **CourtGoStaffPaymentQa20261009**, không dùng CourtGoDb để seed test hay xóa giao dịch.
- Regression gồm signed IPN, sai signature/amount, unknown reference, duplicate, out-of-order failure sau success, return trước/sau IPN, mất cả IPN/return được QueryDR khôi phục, ownership, query signature, expiry/rebooking, concurrent query/IPN, callback listener isolation.
- QueryDR transport bổ sung assertion Content-Length/User-Agent: **14 pass** sau khi hoàn thiện chẩn đoán thực tế.
- Flutter tests mới nhất: **60 pass**, gồm resume trùng request, late-payment reconciliation và lỗi mạng.
- Flutter analyze toàn bộ: **38 issues sẵn có**, chủ yếu Admin và test Staff (3 warning +35 info); không sửa module ngoài phạm vi. Analyze riêng các file thanh toán đã sửa: **No issues found** tại lần kiểm tra trước regression resume; sẽ kiểm tra lại khi chốt.
- Build/test Windows đã xử lý lỗi đường dẫn dấu nháy/cache P: bằng junction hoặc subst tạm; mapping được gỡ sau command. Lần build APK đầu thiếu RAM; build sau giảm heap/worker và chạy khi emulator tắt.
- Android APK/deep-link runtime: đang kiểm tra, sẽ cập nhật bằng chứng cuối.
- Sandbox E2E giao dịch mới: **Chưa xác minh**, chưa phát sinh giao dịch mới. QueryDR thật + DB của giao dịch 15713716 đã xác minh; không thay thế bằng chứng của toàn bộ checkout → browser return → app cho một booking mới.

## E. Bước còn lại cho demo

1. Giữ API và cloudflared chạy; kiểm tra `/health` và dùng hostname hiện hành. Nếu tunnel đổi, đổi cả 2 URL User Secrets, restart API và cập nhật merchant IPN riêng. Không dùng hostname `general-golf-educated-ordinary` của lần chạy trước.
2. Trong Merchant Portal/qua hỗ trợ VNPAY, cập nhật IPN bằng URL chính xác ở mục C, xác nhận registration. Không gửi HashSecret qua chat.
3. Mở app bằng tài khoản customer sở hữu booking gốc → Lịch đặt → Chi tiết → Kiểm tra kết quả thanh toán. Kết quả đúng của booking gốc là Expired/RefundPending, không phải Confirmed/QR. Xử lý yêu cầu hoàn cọc đã tạo theo quy trình hiện có; không thu thêm 210.000đ cho booking đã hết hạn.
4. Sau khi IPN registration được xác nhận, customer thực hiện **một** booking/checkout Sandbox mới bằng thẻ test chính thức. Hoàn tất trong thời hạn giữ sân; từ trang return nhấn “Mở CourtGo”. Không dùng lại URL giao dịch 15713716.
5. Ghi bằng chứng receipt, log IPN/QueryDR hợp lệ, đúng một Deposit Succeeded, Booking Confirmed, slot Reserved, QR và app hiển thị trạng thái DB. Sau đó Staff verify → thu phần còn lại → FullyPaid → Check-in. Nếu IPN chưa tới, kiểm tra trạng thái để QueryDR; nếu provider báo 94, chờ 5 phút, không trả tiền thêm.
6. Với demo dài hạn dùng hostname ổn định; đổi HashSecret từng bị chia sẻ theo quy trình merchant sau khi hoàn tất chẩn đoán, không tự đổi trong task này.

Nguồn: [VNPAY QueryDR](https://sandbox.vnpayment.vn/apis/docs/truy-van-hoan-tien/querydr%26refund.html), [VNPAY mã lỗi/giới hạn query94](https://sandbox.vnpayment.vn/apis/docs/bang-ma-loi/), [VNPAY PAY/IPN](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html), [Cloudflare Quick Tunnel](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/). Bằng chứng local đã lọc nằm trong `.local-build`; không lưu request ký, HashSecret, JWT hay toàn bộ checkout URL trong báo cáo.

