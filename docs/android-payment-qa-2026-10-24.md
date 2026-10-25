# CourtGo Android — kiểm thử kết quả VNPAY

Phạm vi: APK Flutter Android, deep link `courtgo://payment-result`, màn hình kết quả và lỗi giữ trạng thái của booking trước. Không tạo booking/checkout VNPAY, không gửi refund, không sửa database thủ công, không commit/push.

## Dữ liệu thật và dữ liệu cách ly

Booking `8e6c9d5f-a03f-427b-90cc-d0480467f87c` / `CG-20261024-7EP9NL` có cọc VNPAY `15713716` đã được QueryDR xác minh. Trạng thái đúng: Deposit Succeeded, Booking Expired, Payment RefundPending. Không đổi booking này thành Confirmed chỉ để kiểm thử giao diện. DB kiểm tra lại vẫn có đúng một successful deposit, một pending refund và **0 successful refund**.

Booking `daa66ba6-9bea-419b-848a-95218aa2aaa7` cùng khách đã có sẵn, Expired/Unpaid, được dùng để kiểm tra chuyển deep link giữa các booking. Tài khoản hiện tại không có booking Confirmed phù hợp, nên nhánh đó được kiểm tra bằng API fixture cách ly trên Android. Fixture không gọi HTTP thật và không lưu token vào secure storage.

## Lỗi phát hiện và sửa thêm khi kiểm thử Android

- Mở deep link với BookingId khác khi app còn chạy có thể giữ state màn hình cũ do GoRouter tái dùng cùng PaymentPage. Thêm `ValueKey` theo BookingId tại `lib/core/routing/app_router.dart`.
- Navigation được xếp vào post-frame callback có thể chờ khi Flutter đang idle. Thêm `ensureVisualUpdate()` trong `lib/app.dart`.
- Response cũ đến sau khi trang bị dispose không được tiếp tục chạy đối soát. `payment_page.dart` kiểm tra `mounted` ngay sau GET trạng thái.

Regression `test/widget/payment_route_test.dart` chạy bằng **AppRouter thật**, chuyển RefundPending → Expired → RefundPending và xác minh GET đúng từng booking. `integration_test/payment_return_states_test.dart` và `test_driver/payment_driver.dart` chạy cùng các widget sản phẩm trên Android, chụp ảnh có prefix `fixture-` để không nhầm với giao dịch thật.

Lượt chạy Android fixture đầu thất bại vì assertion nằm trong callback Dio bất đồng bộ xung đột với Flutter test guard khi native frame đang được pump. Đã chuyển assertion về thân test sau khi callback kết thúc và chờ API response, thay vì chỉ chờ hết frame; không sửa nghiệp vụ để làm test qua. Các lỗi transport fixture tiếp tục được ghi nhận và assertion phải rỗng, không bị bỏ qua.

## Kết quả Android UI integration

**7/7 kịch bản pass** trên Pixel_7 / emulator-5554. Log runner hiển thị `+8: All tests passed` vì tính thêm `tearDownAll`; có 7 bài kiểm tra nghiệp vụ:

1. AppRouter thật chuyển RefundPending → Expired → RefundPending đúng booking, đọc đúng từng endpoint.
2. Deposit Succeeded + Booking Confirmed → màn hình “Sân đã sẵn sàng!”, đã trả90.000đ, còn210.000đ, có nút QR.
3. Deposit Succeeded + Booking Expired/RefundPending → cảnh báo đối soát hoàn tiền, không hiển thị QR hay xác nhận sân.
4. Booking Expired → “Lượt giữ sân đã hết hạn”.
5. Payment Succeeded nhưng booking còn PendingPayment → vẫn chờ xác nhận, không tự chuyển success.
6. Failed → thông báo thanh toán chưa thành công.
7. Cancelled → thông báo đã hủy thanh toán.

Đây là kiểm tra UI Android bằng fixture cách ly, không phải bảy giao dịch VNPAY. Ảnh fixture-confirmed ghi rõ `UI-FIXTURE-CONFIRMED`. Trạng thái của giao dịch thật15713716 được kiểm tra riêng bằng backend và APK chính.

## Bằng chứng và giới hạn

Log trong `.local-build/vnpay-android-states.log`, `.local-build/vnpay-apk-build-final.log`, `.local-build/vnpay-flutter-tests.log`, `.local-build/vnpay-targeted-analyze.log`. Ảnh dữ liệu thật trong `.local-build/courtgo-payment-*.png`; ảnh fixture được xuất vào `FE/build/payment-qa/fixture-*.png`.

Kết quả cuối của APK/deep link sẽ được cập nhật sau lượt kiểm tra cuối. IPN merchant registration và một vòng checkout Sandbox mới vẫn **Chưa xác minh**; kiểm thử Android này không thay thế bằng chứng VNPAY đã gửi IPN thật. Host Quick Tunnel hiện hành được ghi ở `vnpay-incident-2026-10-24.md`; hostname có thể mất khi process dừng.
