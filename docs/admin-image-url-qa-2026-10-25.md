# CourtGo — ảnh URL Admin và Customer

Ngày: 25/10/2026.

## Đã thực hiện

- Danh sách cơ sở và sân: thumbnail 64 × 64, bo góc 10, lấy `coverImageUrl` từ API.
- Chi tiết cơ sở và sân: banner cao 200, bo góc 16; giữ các thông tin và thao tác quản lý.
- Form cơ sở và sân: preview cao 160, theo dõi cả URL nhập tay và dữ liệu nạp bất đồng bộ khi sửa; debounce 400 ms; hủy timer khi rời màn hình.
- Dùng lại `SportImage` và `CachedNetworkImage` cho Admin và Customer: `BoxFit.cover`, kích thước cố định, loading, placeholder thể thao khi thiếu ảnh hoặc lỗi. Preview phân biệt URL sai và lỗi tải ảnh, không sửa dữ liệu form.
- Cache theo URL đã trim; đổi URL tạo image key mới. Không ép xóa cache toàn bộ. Nếu thay nội dung tại cùng URL, cache vẫn theo chính sách hiện có; nên dùng URL/version mới khi thay ảnh.
- Giữ cơ chế tải lại dữ liệu sau lưu/quay về màn hình trước. Bộ lọc danh sách cơ sở xuống dòng khi hẹp; tiêu đề section có giới hạn chiều rộng, tránh overflow.

## Lỗi Backend thực sự được sửa

Entity, request lưu/sửa, API chi tiết, API sân và API Customer đều có `CoverImageUrl`. Riêng DTO/projection danh sách cơ sở Admin thiếu trường này. Đã bổ sung trường nullable tương thích ngược, không đổi schema hoặc migration.

## File thuộc task

Frontend:

- `FE/lib/shared/widgets/customer_widgets.dart`
- `FE/lib/shared/widgets/image_url_preview.dart` (mới)
- `FE/lib/features/admin/shared/admin_models.dart`
- `FE/lib/features/admin/shared/admin_widgets.dart`
- `FE/lib/features/admin/branches/admin_centers_page.dart`
- `FE/lib/features/admin/branches/admin_center_detail_page.dart`
- `FE/lib/features/admin/branches/admin_center_form_page.dart`
- `FE/lib/features/admin/courts/admin_courts_page.dart`
- `FE/lib/features/admin/courts/admin_court_detail_page.dart`
- `FE/lib/features/admin/courts/admin_court_form_page.dart`

Backend:

- `BE/src/CourtGo.Application/SportCenters/AdminSportCenterDtos.cs`
- `BE/src/CourtGo.Infrastructure/Services/AdminSportCenterService.cs`

Kiểm thử:

- `FE/test/widget/admin_images_test.dart` (mới)
- `FE/test/widget/admin_image_forms_test.dart` (mới)
- `FE/test/widget/admin_center_form_test.dart`
- `BE/tests/CourtGo.IntegrationTests/AdminImageUrlTests.cs` (mới)

## Kết quả kiểm thử

- Toàn bộ Flutter suite: **77 passed**. Log: `.local-build/admin-images-flutter-tests.log`.
- Test ảnh tập trung sau chỉnh lint: **9 passed**. Log: `.local-build/admin-images-final-target-tests.log`.
- Backend `AdminImageUrlTests` + `AdminMasterDataApiTests`: **8 passed**, database test cô lập. Tạo cơ sở/sân qua API, sửa và xóa URL; đối chiếu Admin list/detail, Customer list/detail và sân trong chi tiết cơ sở. Log: `.local-build/admin-images-backend-tests.log`.
- Flutter kiểm tra URL sai/trống, debounce, loading/error presentation, key đổi theo URL; bốn màn hình Admin ở chiều rộng 360 px và refresh; hai form nạp ảnh đã lưu, giữ tên sau lỗi preview và gửi đúng URL mới khi lưu.
- `flutter analyze --no-pub`: **0 errors, 3 warnings, 35 infos**, đều là vấn đề có sẵn; không phát sinh lint mới. Có hai info deprecated `DropdownButtonFormField.value` trong form sân vốn đã có. Log: `.local-build/admin-images-analyze.log`.
- `git diff --check` đối với các file task: đạt. Workspace còn các thay đổi có sẵn ngoài task, được giữ nguyên.

## Giới hạn xác minh

Kiểm thử Flutter dùng fixture API và kiểm tra widget/trạng thái; chưa chạy thao tác tạo/sửa bằng tay trên Android với ảnh Internet thật trong task này. Kiểm thử Backend xác minh lưu và trả URL, không tải nội dung ảnh từ nhà cung cấp bên ngoài. Không khẳng định đã kiểm thử E2E trên thiết bị cho task ảnh.

Backend đang chạy cần khởi động bằng bản build mới để API danh sách cơ sở trả trường vừa thêm. Không thêm dịch vụ upload, không sửa nghiệp vụ booking/VNPAY/Staff, không tạo giao dịch thanh toán, không commit/push Git.
