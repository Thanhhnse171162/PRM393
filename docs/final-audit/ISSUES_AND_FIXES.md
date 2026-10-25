# Issues and fixes — 2026-10-25

Không phát hiện P0 đã được chứng minh trong phạm vi kiểm tra; đây không phải chứng nhận an toàn production. Các thay đổi có sẵn trong worktree được giữ nguyên. Không commit/push.

## AUD-01 — P1
- File/module: `BookingHoldService.cs; BookingHoldServiceTests.cs`.
- Nguyên nhân: TimeOnly.AddHours wrap qua 00:00 khiến slot ngoài giờ lọt kiểm tra.
- Xử lý/bằng chứng: Đã thêm chặn end<=start; test trước sửa thất bại, sau sửa đạt.
- Trạng thái: **Đã sửa**.

## AUD-02 — P2
- File/module: `FE/lib/shared/widgets/customer_widgets.dart; final_audit_regression_test.dart`.
- Nguyên nhân: SportChip callback trả function thay vì gọi function.
- Xử lý/bằng chứng: Gọi onTap(); test click callback đúng một lần.
- Trạng thái: **Đã sửa**.

## AUD-03 — P1
- File/module: `admin_court_detail_page.dart; admin_management_hub_page.dart; admin_operating_hours_page.dart; admin_reports_hub_page.dart`.
- Nguyên nhân: 4 đường dẫn Admin không khớp GoRouter.
- Xử lý/bằng chứng: Sửa route/giữ query filter; final_audit_navigation_test xác minh router thật.
- Trạng thái: **Đã sửa**.

## AUD-04 — P1
- File/module: `FE/lib/core/routing/app_router.dart; final_audit_navigation_test.dart`.
- Nguyên nhân: Route thanh toán legacy mở màn hình gọi endpoint không tồn tại.
- Xử lý/bằng chứng: Redirect checkout hiện tại; regression không POST tạo giao dịch.
- Trạng thái: **Đã sửa**.

## AUD-05 — P2
- File/module: `FE/lib/features/admin/shared/admin_widgets.dart`.
- Nguyên nhân: Nhãn Admin thiếu PendingPayment/Expired/Unpaid/DepositPaid/RefundPending.
- Xử lý/bằng chứng: Bổ sung nhãn/màu; test phân biệt RefundPending và Refunded.
- Trạng thái: **Đã sửa**.

## AUD-06 — P1
- File/module: `FE/lib/core/theme/app_theme.dart; admin_center_form_page.dart; admin_center_form_test.dart; admin_router_test.dart`.
- Nguyên nhân: minimumSize Size.fromHeight(48) ép nút Đổi vị trí trong Row có width vô hạn. Test cũ không dùng theme thật.
- Xử lý/bằng chứng: minimumSize Size(0,48); production-theme tests và emulator mở/sửa/cuộn bình thường.
- Trạng thái: **Đã sửa**.

## AUD-07 — P2
- File/module: `admin_image_field.dart; LocalImageStorage.cs; ImagesController.cs; auth_interceptor.dart; ImageUploadApiTests.cs; admin_image_upload_test.dart`.
- Nguyên nhân: Nhập URL thủ công khó dùng; chưa có upload ảnh từ thiết bị. Multipart retry dùng stream đã tiêu thụ.
- Xử lý/bằng chứng: Upload JPEG/PNG, preview, lỗi rõ ràng, khóa Lưu trong lúc upload, clone FormData khi refresh; test API và Android picker/preview.
- Trạng thái: **Đã triển khai theo yêu cầu mới**.

## AUD-08 — P2
- File/module: `FE/lib/features/admin/shared/admin_widgets.dart; final_audit_regression_test.dart`.
- Nguyên nhân: API trả occupancy theo 0–100, formatter lại nhân 100; emulator hiển thị 444% thay vì 4.44%.
- Xử lý/bằng chứng: Bỏ phép nhân lặp; test 0,4.44,100 percentage points.
- Trạng thái: **Đã sửa**.

## OPEN-01 — P1 trước demo payment
- File/module: `docs/real-payment-configuration.md; cấu hình runtime ngoài Git`.
- Nguyên nhân: Tunnel callback HTTPS hiện không truy cập được; chưa xác minh merchant IPN.
- Xử lý/bằng chứng: Cần tunnel ổn định và merchant portal; không tự tạo giao dịch.
- Trạng thái: **Còn phụ thuộc cấu hình ngoài**.

## OPEN-02 — P1 nghiệp vụ
- File/module: `BE/src/CourtGo.Infrastructure/Services/StaffBookingService.cs`.
- Nguyên nhân: Chưa có cửa sổ thời gian check-in.
- Xử lý/bằng chứng: Cần thống nhất sớm/muộn bao lâu rồi bổ sung regression; không tự áp chính sách.
- Trạng thái: **Chưa sửa**.

## OPEN-03 — P1 nếu yêu cầu refund tự động
- File/module: `BE/src/CourtGo.Infrastructure/Payments/RealPaymentGateway.cs`.
- Nguyên nhân: Refund thật chưa triển khai, chỉ manual pending.
- Xử lý/bằng chứng: Chưa được thực hiện hoàn tiền; phải giữ RefundPending và quy trình xử lý riêng.
- Trạng thái: **Giới hạn hiện tại**.

## OPEN-04 — P2
- File/module: `FE/lib/features/admin/bookings/admin_booking_detail_page.dart`.
- Nguyên nhân: Nút hủy có thể còn hiện cho Expired/NoShow dù server từ chối.
- Xử lý/bằng chứng: Đồng bộ điều kiện hiển thị với trạng thái backend trong lượt tiếp theo.
- Trạng thái: **Chưa sửa; server vẫn bảo vệ**.

## OPEN-05 — P2
- File/module: `Các list/form Admin; TEST_REPORT.md`.
- Nguyên nhân: Một số list giới hạn page đầu; field error chưa đồng đều; còn lint info.
- Xử lý/bằng chứng: Hoàn thiện pagination/inline validation, kiểm thử từng thiết bị nhỏ.
- Trạng thái: **Backlog**.

## OPEN-06 — P2 triển khai ảnh
- File/module: `BE/src/CourtGo.Api/App_Data/images`.
- Nguyên nhân: Local disk không tự được backup hoặc chia sẻ giữa nhiều instance; upload hủy form để lại file.
- Xử lý/bằng chứng: Mount persistent volume, backup; thiết kế GC chỉ sau đối chiếu mọi tham chiếu. Header validation không thay thế full decode/antivirus.
- Trạng thái: **Giới hạn được ghi nhận**.

## OPEN-07 — P2 chức năng
- File/module: `Auth/account modules`.
- Nguyên nhân: Chưa có forgot/reset password, sửa profile Customer.
- Xử lý/bằng chứng: Chỉ phát triển khi phạm vi nghiệp vụ được yêu cầu; không ghi là đã hoàn tất.
- Trạng thái: **Chưa triển khai**.

## File thay đổi cho luồng ảnh mới

- `.gitignore`
- `BE/src/CourtGo.Api/Program.cs`
- `BE/src/CourtGo.Api/Controllers/ImagesController.cs`
- `BE/src/CourtGo.Application/Interfaces/IImageStorage.cs`
- `BE/src/CourtGo.Infrastructure/Services/LocalImageStorage.cs`
- `BE/tests/CourtGo.IntegrationTests/ImageUploadApiTests.cs`
- `FE/ios/Runner/Info.plist`
- `FE/lib/core/theme/app_theme.dart`
- `FE/lib/core/network/auth_interceptor.dart`
- `FE/lib/features/admin/admin_api.dart`
- `FE/lib/features/admin/shared/admin_image_field.dart`
- `FE/lib/features/admin/branches/admin_center_form_page.dart`
- `FE/lib/features/admin/courts/admin_court_form_page.dart`
- `FE/lib/shared/widgets/customer_widgets.dart`
- `FE/test/widget/admin_center_form_test.dart`
- `FE/test/widget/admin_image_forms_test.dart`
- `FE/test/widget/admin_image_upload_test.dart`
- `FE/test/widget/admin_router_test.dart`

Ảnh URL/thumbnails/banner từ công việc trước vẫn được sử dụng; xem ../admin-image-url-qa-2026-10-25.md. README và các tài liệu final-audit được cập nhật. Không quy toàn bộ git diff có sẵn thành thay đổi của lần sửa này.
