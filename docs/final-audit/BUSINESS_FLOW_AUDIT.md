# Business flow audit — 2026-10-25

Phạm vi: đọc source API/service/entity/route và chạy test hiện có; không coi unit/widget test là giao dịch VNPAY hoặc E2E thiết bị thật. Không thay đổi schema, secrets, trạng thái giao dịch hay thực hiện hoàn tiền.

## Auth và phân quyền
- Login/register/refresh/logout và phân vai Customer/Staff/Admin có API + Flutter. AuthApiTests kiểm tra các contract hiện có. Flutter giữ DTO trực tiếp + ProblemDetails theo lựa chọn của người dùng.
- StaffAccess kiểm tra phân công và tài khoản hoạt động. Upload ảnh mới kiểm tra cả role JWT và Admin đang hoạt động trong DB.
- Refresh token được serialize phía Flutter; bổ sung clone FormData khi retry upload sau 401. Không lưu token vào tài liệu/log audit.
- Chưa có quên/reset mật khẩu hoặc sửa hồ sơ Customer. Account là xem thông tin/logout.
- Chưa xác minh đầy đủ việc thu hồi JWT còn hạn ở mọi endpoint Admin sau khi tài khoản bị khóa; đây là hạng mục rà soát tiếp, không phải exploit đã chứng minh.

## Customer: tìm sân → availability → hold → cọc → QR
- Discovery/filter, chi tiết cơ sở, availability, giữ slot, checkout, lịch đặt, chi tiết và QR đã có code. Callback SportChip không gọi hành động đã sửa (AUD-02).
- Hold kiểm tra giờ mở cửa, block, trạng thái sân, giá và cạnh tranh slot. Sửa TimeOnly wrap 23:00→00:00 vượt giờ mở cửa (AUD-01). Không bổ sung nghiệp vụ sân mở qua đêm.
- SQL QA kiểm chứng tranh chấp và rollback. Unique filtered index UX_BookingSlots_ActiveCourtStart bảo vệ slot chiếm chỗ; kiểm tra migration/schema bằng read-only.
- Deposit Pending khác Succeeded; Booking Confirmed chỉ sau kết quả hợp lệ. QR không phát cho booking Expired/RefundPending.
- Route thanh toán cũ được chuyển sang checkout hiện hành, không còn gọi API /api/payments/vnpay/create không tồn tại (AUD-04).
- Notification/favorite/review/profile được đưa vào inventory; không coi tất cả màn hình đã chạy thủ công.

## Payment/VNPAY
- Browser Return chỉ điều hướng/hiển thị, không tự xác nhận tiền. IPN kiểm tra chữ ký, mã tham chiếu, số tiền; reconciliation QueryDR được kiểm soát chủ sở hữu và trạng thái.
- Test backend bao phủ callback lặp, số tiền sai, tham chiếu sai, thanh toán trễ và slot đã được đặt lại. Booking hết hạn nhận tiền trễ vẫn Expired/RefundPending, không hồi sinh slot hoặc sinh QR.
- User-secrets có merchant và signing secret; không sao chép giá trị. Cấu hình sử dụng adapter thật với payment URL Sandbox.
- Public callback tunnel hiện không truy cập được khi kiểm tra; chưa xác minh đăng ký IPN ở merchant portal. Không tạo giao dịch Sandbox mới để audit.
- Hoàn tiền provider thật chưa được triển khai tự động; RefundPending/manual reconciliation không phải Refunded. MoMo chưa có bằng chứng kết nối provider thật.

## Staff: Verify → remaining payment → FullyPaid → Check-in
- Verify và check-in là thao tác khác nhau. Luồng mặc định thu phần còn lại trước check-in; chỉ cho ngoại lệ nợ tiền nếu cấu hình + yêu cầu + lý do cho phép.
- StaffBookingService kiểm tra cơ sở, QR, trạng thái booking, tiền và chống thao tác lặp; SQL tests bao phủ concurrent collection/check-in và rollback.
- Chưa có cửa sổ thời gian check-in sớm/muộn. Cần thống nhất nghiệp vụ trước khi áp đặt; không tự thêm giới hạn để làm lệch test/luồng hiện tại.
- Camera scan/quyền camera trên thiết bị thật chưa kiểm thử lại trong phiên audit này.

## Admin/master data
- Cơ sở/sân/môn thể thao, nhân viên/phân công, giờ mở cửa/ngày ngoại lệ, giá, chính sách hủy, booking, cancellation request, cấu hình và báo cáo có controller/service/UI tương ứng (FEATURE_INVENTORY).
- 4 liên kết Admin sai route đã sửa; nhãn trạng thái thanh toán/booking bổ sung theo enum thực tế.
- Form chỉnh sửa cơ sở trắng do theme OutlinedButton có minimum width vô hạn trong Row; sửa width về 0, regression dùng production theme.
- Upload JPEG/PNG ≤5 MiB mới dùng /api/admin/images. Server đặt tên GUID, giới hạn kích thước/định dạng/dimensions và phục vụ /api/images/{name}. Không thay DB: coverImageUrl chứa relative URL; Flutter resolve theo API host.
- Test tích hợp tạo, sửa cơ sở/sân với ảnh upload và đọc lại qua API Customer thành công. Emulator kiểm tra form, picker, upload và preview; không thay ảnh mẫu vào cơ sở thật.
- Ảnh lưu trên disk API App_Data/images: phải backup/mount volume khi deploy. Chưa có tự dọn file upload rồi hủy form; không xóa tự động để tránh ảnh còn được tham chiếu.

## Điều kiện trước demo/production
- Demo local có thể kiểm tra Admin/image flow. Không công bố production-ready hoặc hoàn tất E2E VNPAY khi tunnel/IPN chưa xác minh.
- Cần cấu hình callback HTTPS ổn định, tài khoản demo riêng và xác nhận các chính sách check-in/refund còn thiếu. Xem DEMO_GUIDE và ISSUES_AND_FIXES.
