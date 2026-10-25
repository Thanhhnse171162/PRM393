# UI/UX audit — 2026-10-25

64 route entries, 58 lớp màn hình, 26 nhóm module. Các dòng bên dưới là inventory và rà soát source; không chứng minh mọi tổ hợp dữ liệu đã được thao tác trên thiết bị.

## Đã cải thiện và kiểm chứng

- Admin center edit: sửa lỗi layout vô hạn, theme thật trong widget/router regression; emulator mở form và cuộn được.
- Admin center/court form: nút chọn ảnh từ thiết bị, tiến trình upload, báo lỗi, preview cache, URL tùy chọn; không mất dữ liệu khi upload lỗi/hủy.
- Center form có ErrorState/retry khi tải dữ liệu lỗi, dropdown timezone isExpanded.
- Danh sách/chi tiết Admin và Customer dùng SportImage với BoxFit.cover, loading/error/empty placeholder; ảnh upload relative URL được resolve theo host API.
- Navigation 4 link Admin và route checkout cũ đã sửa; SportChip gọi callback đúng.
- Nhãn trạng thái Admin và formatter phần trăm đúng dữ liệu backend.

## Phạm vi thủ công trên Android

Đã chạy APK, Admin dashboard → quản lý → cơ sở Q7 → chi tiết → chỉnh sửa → cuộn → picker → upload → preview; sân A1 → chi tiết → chỉnh sửa → picker → hủy vẫn giữ dữ liệu. Không bấm Lưu ảnh thử vào cơ sở thật. Những màn hình còn lại chủ yếu source review + tests liên quan. iOS/Windows picker chưa chạy trực tiếp.

## Rà soát từng màn hình

### AdminAccountPage
- Source: `FE/lib/features/admin/account/admin_account_page.dart`; route `/admin/account`.
- Dấu hiệu xử lý trong source: error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Auth/me, logout; AuthApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminBookingsPage
- Source: `FE/lib/features/admin/bookings/admin_bookings_page.dart`; route `/admin/bookings`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminBookings, AdminCancellationRequests / AdminBookingService, CancellationService; AdminCancellationApiTests; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminBookingDetailPage
- Source: `FE/lib/features/admin/bookings/admin_booking_detail_page.dart`; route `/admin/bookings/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminBookings, AdminCancellationRequests / AdminBookingService, CancellationService; AdminCancellationApiTests; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCancellationPoliciesPage
- Source: `FE/lib/features/admin/cancellation_policies/admin_cancellation_policies_page.dart`; route `/admin/cancellation-policies`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminCancellationPolicies / AdminCancellationPolicyService; AdminBusinessConfigApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCancellationRequestsPage
- Source: `FE/lib/features/admin/bookings/admin_cancellation_requests_page.dart`; route `/admin/cancellation-requests`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminBookings, AdminCancellationRequests / AdminBookingService, CancellationService; AdminCancellationApiTests; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCentersPage
- Source: `FE/lib/features/admin/branches/admin_centers_page.dart`; route `/admin/centers`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, images, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminSportCenters, AdminOperatingHours / AdminSportCenterService; AdminMasterDataApiTests; AdminImageUrlTests; admin_image_forms_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCenterDetailPage
- Source: `FE/lib/features/admin/branches/admin_center_detail_page.dart`; route `/admin/centers/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, images, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminSportCenters, AdminOperatingHours / AdminSportCenterService; AdminMasterDataApiTests; AdminImageUrlTests; admin_image_forms_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCenterFormPage
- Source: `FE/lib/features/admin/branches/admin_center_form_page.dart`; route `/admin/centers/:id/edit`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminSportCenters, AdminOperatingHours / AdminSportCenterService; AdminMasterDataApiTests; AdminImageUrlTests; admin_image_forms_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminLocationPickerPage
- Source: `FE/lib/features/admin/branches/admin_location_picker_page.dart`; route `/admin/centers/location-picker`.
- Dấu hiệu xử lý trong source: loading, error_retry, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminSportCenters, AdminOperatingHours / AdminSportCenterService; AdminMasterDataApiTests; AdminImageUrlTests; admin_image_forms_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCourtsPage
- Source: `FE/lib/features/admin/courts/admin_courts_page.dart`; route `/admin/courts`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, images, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminCourts / AdminCourtService; AdminMasterDataApiTests; AdminImageUrlTests; admin_images_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCourtDetailPage
- Source: `FE/lib/features/admin/courts/admin_court_detail_page.dart`; route `/admin/courts/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, images, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminCourts / AdminCourtService; AdminMasterDataApiTests; AdminImageUrlTests; admin_images_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminCourtFormPage
- Source: `FE/lib/features/admin/courts/admin_court_form_page.dart`; route `/admin/courts/:id/edit`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminCourts / AdminCourtService; AdminMasterDataApiTests; AdminImageUrlTests; admin_images_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminDashboardPage
- Source: `FE/lib/features/admin/dashboard/admin_dashboard_page.dart`; route `/admin/home`.
- Dấu hiệu xử lý trong source: loading, error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminDashboard / AdminDashboardService; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminManagementHubPage
- Source: `FE/lib/features/admin/management/admin_management_hub_page.dart`; route `/admin/management`.
- Dấu hiệu xử lý trong source: scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Điều hướng nội bộ; Kiểm tra route tĩnh.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminOperatingHoursPage
- Source: `FE/lib/features/admin/operating_hours/admin_operating_hours_page.dart`; route `/admin/operating-hours`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminOperatingHours / AdminOperatingHourService; AdminBusinessConfigApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminOperatingExceptionsPage
- Source: `FE/lib/features/admin/operating_hours/admin_operating_exceptions_page.dart`; route `/admin/operating-hours/exceptions`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminOperatingHours / AdminOperatingHourService; AdminBusinessConfigApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminPriceRulesPage
- Source: `FE/lib/features/admin/pricing/admin_price_rules_page.dart`; route `/admin/price-rules`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminPriceRules / AdminPriceRuleService; AdminBusinessConfigApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminReportsHubPage
- Source: `FE/lib/features/admin/reports/admin_reports_hub_page.dart`; route `/admin/reports`.
- Dấu hiệu xử lý trong source: scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminReports / AdminReportService; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminBookingReportPage
- Source: `FE/lib/features/admin/reports/admin_booking_report_page.dart`; route `/admin/reports/bookings-report`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminReports / AdminReportService; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminOccupancyReportPage
- Source: `FE/lib/features/admin/reports/admin_occupancy_report_page.dart`; route `/admin/reports/occupancy`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminReports / AdminReportService; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminRevenueReportPage
- Source: `FE/lib/features/admin/reports/admin_revenue_report_page.dart`; route `/admin/reports/revenue`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminReports / AdminReportService; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminSummaryReportPage
- Source: `FE/lib/features/admin/reports/admin_summary_report_page.dart`; route `/admin/reports/summary`.
- Dấu hiệu xử lý trong source: loading, error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminReports / AdminReportService; AdminBookingsAndReportsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminSettingsPage
- Source: `FE/lib/features/admin/settings/admin_settings_page.dart`; route `/admin/settings`.
- Dấu hiệu xử lý trong source: loading, error_retry, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminSystemSettings / AdminSystemSettingService; AdminBusinessConfigApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminSportsPage
- Source: `FE/lib/features/admin/sports/admin_sports_page.dart`; route `/admin/sports`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, images, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminSports / AdminSportService; AdminMasterDataApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminStaffPage
- Source: `FE/lib/features/admin/staff_management/admin_staff_page.dart`; route `/admin/staff`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminStaff / AdminStaffService; AdminStaffApiTests; admin_staff_create_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminStaffDetailPage
- Source: `FE/lib/features/admin/staff_management/admin_staff_detail_page.dart`; route `/admin/staff/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminStaff / AdminStaffService; AdminStaffApiTests; admin_staff_create_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AdminStaffCreatePage
- Source: `FE/lib/features/admin/staff_management/admin_staff_create_page.dart`; route `/admin/staff/create`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AdminStaff / AdminStaffService; AdminStaffApiTests; admin_staff_create_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AccountPage
- Source: `FE/lib/features/account/account_page.dart`; route `/customer/account`.
- Dấu hiệu xử lý trong source: loading, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Auth/me, logout / AuthService; AuthApiTests; kiểm tra nguồn.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### BookingHistoryPage
- Source: `FE/lib/features/booking/booking_history_page.dart`; route `/customer/bookings`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Bookings, CancellationRequests / BookingQueryService, CancellationService; BookingWorkflowApiTests; AdminCancellationE2ETests; payment_state_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### BookingDetailPage
- Source: `FE/lib/features/booking/booking_detail_page.dart`; route `/customer/bookings/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Bookings, CancellationRequests / BookingQueryService, CancellationService; BookingWorkflowApiTests; AdminCancellationE2ETests; payment_state_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### CancellationPage
- Source: `FE/lib/features/booking/cancellation_page.dart`; route `/customer/bookings/:id/cancellation`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Bookings, CancellationRequests / BookingQueryService, CancellationService; BookingWorkflowApiTests; AdminCancellationE2ETests; payment_state_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### CheckoutPage
- Source: `FE/lib/features/payments/checkout_page.dart`; route `/customer/bookings/:id/checkout`.
- Dấu hiệu xử lý trong source: loading, error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: BookingPayments / DepositPaymentService, PaymentReconciliationService; RealPaymentApiTests; VnPayQueryTests; payment_state_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### PaymentPage
- Source: `FE/lib/features/payments/payment_page.dart`; route `/customer/bookings/:id/payment`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: BookingPayments / DepositPaymentService, PaymentReconciliationService; RealPaymentApiTests; VnPayQueryTests; payment_state_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### BookingQrPage
- Source: `FE/lib/features/booking/booking_qr_page.dart`; route `/customer/bookings/:id/qr`.
- Dấu hiệu xử lý trong source: loading, error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Bookings, CancellationRequests / BookingQueryService, CancellationService; BookingWorkflowApiTests; AdminCancellationE2ETests; payment_state_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### ReviewPage
- Source: `FE/lib/features/reviews/review_pages.dart`; route `/customer/bookings/:id/review`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Reviews, SportCenters/reviews / ReviewService; ReviewsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### BookingSuccessPage
- Source: `FE/lib/features/booking/booking_success_page.dart`; route `/customer/bookings/:id/success`.
- Dấu hiệu xử lý trong source: loading, error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Bookings, CancellationRequests / BookingQueryService, CancellationService; BookingWorkflowApiTests; AdminCancellationE2ETests; payment_state_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### CenterDetailPage
- Source: `FE/lib/features/centers/center_detail_page.dart`; route `/customer/centers/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, images, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: SportCenters, Sports / SportCenterService; MasterDataApiTests; admin_images_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### CenterReviewsPage
- Source: `FE/lib/features/reviews/review_pages.dart`; route `/customer/centers/:id/reviews`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Reviews, SportCenters/reviews / ReviewService; ReviewsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### CourtDetailPage
- Source: `FE/lib/features/courts/court_detail_page.dart`; route `/customer/courts/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, images, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Courts / CourtService; MasterDataApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### AvailabilityPage
- Source: `FE/lib/features/availability/availability_page.dart`; route `/customer/courts/:id/availability`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Availability, Bookings/hold / AvailabilityService, BookingHoldService; AvailabilityApiTests; BookingHoldApiTests; SQL tests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### ExplorePage
- Source: `FE/lib/features/centers/explore_page.dart`; route `/customer/explore`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: SportCenters, Sports / SportCenterService; MasterDataApiTests; admin_images_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### CustomerHomePage
- Source: `FE/lib/features/home/customer_home_page.dart`; route `/customer/home`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: SportCenters, Sports / SportCenterService; customer_home_test; customer_home_page_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### NotificationsPage
- Source: `FE/lib/features/notifications/notifications_page.dart`; route `/customer/notifications`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Notifications / NotificationService; NotificationsApiTests.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### LocationPage
- Source: `FE/lib/features/location/presentation/pages/location_page.dart`; route `/location`.
- Dấu hiệu xử lý trong source: loading, error_retry, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: Nominatim / geolocator / local storage; admin_center_form_test; kiểm tra nguồn.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### LoginPage
- Source: `FE/lib/features/auth/presentation/pages/login_page.dart`; route `/login`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AuthController / AuthService; AuthApiTests; auth_provider_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### RegisterPage
- Source: `FE/lib/features/auth/presentation/pages/register_page.dart`; route `/register`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AuthController / AuthService; AuthApiTests; auth_provider_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### SplashPage
- Source: `FE/lib/features/auth/presentation/pages/splash_page.dart`; route `/splash`.
- Dấu hiệu xử lý trong source: error_retry, images; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: AuthController / AuthService; AuthApiTests; auth_provider_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffAccountPage
- Source: `FE/lib/features/staff/staff_home_schedule.dart`; route `/staff/account`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffSchedulePage
- Source: `FE/lib/features/staff/staff_home_schedule.dart`; route `/staff/bookings`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffBookingDetailPage
- Source: `FE/lib/features/staff/staff_booking_pages.dart`; route `/staff/bookings/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffCameraPage
- Source: `FE/lib/features/staff/staff_scanner.dart`; route `/staff/camera`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffScannerPage
- Source: `FE/lib/features/staff/staff_scanner.dart`; route `/staff/checkin`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffCourtsPage
- Source: `FE/lib/features/staff/staff_court_pages.dart`; route `/staff/courts`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffCourtDetailPage
- Source: `FE/lib/features/staff/staff_court_pages.dart`; route `/staff/courts/:id`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffCreateBlockPage
- Source: `FE/lib/features/staff/staff_court_pages.dart`; route `/staff/courts/:id/block`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffHomePage
- Source: `FE/lib/features/staff/staff_home_schedule.dart`; route `/staff/home`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffVerifyPage
- Source: `FE/lib/features/staff/staff_booking_pages.dart`; route `/staff/verify`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, scroll; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

### StaffWalkInPage
- Source: `FE/lib/features/staff/staff_walk_in.dart`; route `/staff/walk-in`.
- Dấu hiệu xử lý trong source: loading, error_retry, empty, forms; cần xem widget dùng chung nếu không có dấu hiệu trực tiếp.
- API/test liên quan: StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess; StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test.
- Validation: chưa xác nhận thủ công mọi field/keyboard/lỗi server. Navigation: đối chiếu route inventory; các lỗi đã biết ghi ở ISSUES_AND_FIXES.
- Loading/empty/error và ảnh: phát hiện source ở trên không thay thế test mạng lỗi/dữ liệu trống. Responsive: cần tiếp tục kiểm tra font lớn, màn hình nhỏ và landscape; không tuyên bố đã cover toàn bộ.

