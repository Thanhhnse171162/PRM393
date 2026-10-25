"""Read-only source inventory; writes audit artifacts in this directory only."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
router = (ROOT / 'FE/lib/core/routing/app_router.dart').read_text(encoding='utf-8')
names = dict(re.findall(r"static const String (\w+)\s*=\s*'([^']+)'", (ROOT / 'FE/lib/core/routing/route_names.dart').read_text(encoding='utf-8')))
sources = {p: p.read_text(encoding='utf-8') for p in (ROOT / 'FE/lib').rglob('*.dart')}
classes = {}
for path, source in sources.items():
    for name in re.findall(r'class (\w+) extends (?:StatefulWidget|StatelessWidget)', source):
        classes[name] = path

routes = []
for match in re.finditer(r'GoRoute\(\s*path:\s*(RouteNames\.\w+|\x27[^\x27]+\x27),', router):
    raw = match.group(1)
    path = names.get(raw.split('.')[-1], raw.strip("'"))
    block = router[match.end():router.find('\n        ),', match.end())]
    screens = [n for n in re.findall(r'\b(\w+Page)\(', block) if n in classes]
    routes.append({'route': path, 'screen': screens[0] if screens else '(redirect)'})
for raw, screen in re.findall(r'\(RouteNames\.(\w+), const (\w+)\(\)\)', router):
    routes.append({'route': names[raw], 'screen': screen})

api_files = list((ROOT / 'BE/src/CourtGo.Api/Controllers').glob('*.cs'))
controllers = []
for file in api_files:
    s = file.read_text(encoding='utf-8')
    base = re.search(r'Route\("([^"]+)"\)', s)
    controllers.append({'file': str(file.relative_to(ROOT)), 'base': base[1] if base else '',
        'services': sorted(set(re.findall(r'\bI\w+Service\b', s))),
        'authorization': re.findall(r'Authorize(?:\(([^)]*)\))?', s),
        'endpoints': re.findall(r'Http(Get|Post|Put|Patch|Delete)(?:\("([^"]*)"\))?', s)})

# Audited module mapping (not a claim of device E2E coverage).
mapping = [
 ('auth', 'AuthController / AuthService', 'User, RefreshToken', 'AuthApiTests; auth_provider_test'),
 ('location', 'Nominatim / geolocator / local storage', 'Không ghi DB', 'admin_center_form_test; kiểm tra nguồn'),
 ('home', 'SportCenters, Sports / SportCenterService', 'SportCenter, Court, Sport', 'customer_home_test; customer_home_page_test'),
 ('centers', 'SportCenters, Sports / SportCenterService', 'SportCenter, Court, OperatingHour', 'MasterDataApiTests; admin_images_test'),
 ('courts', 'Courts / CourtService', 'Court, Sport', 'MasterDataApiTests'),
 ('availability', 'Availability, Bookings/hold / AvailabilityService, BookingHoldService', 'Court, Booking, BookingSlot, PriceRule', 'AvailabilityApiTests; BookingHoldApiTests; SQL tests'),
 ('booking', 'Bookings, CancellationRequests / BookingQueryService, CancellationService', 'Booking, CheckIn, CancellationRequest, Payment', 'BookingWorkflowApiTests; AdminCancellationE2ETests; payment_state_test'),
 ('payments', 'BookingPayments / DepositPaymentService, PaymentReconciliationService', 'Booking, BookingSlot, Payment', 'RealPaymentApiTests; VnPayQueryTests; payment_state_test'),
 ('notifications', 'Notifications / NotificationService', 'Notification', 'NotificationsApiTests'),
 ('reviews', 'Reviews, SportCenters/reviews / ReviewService', 'Review, Booking', 'ReviewsApiTests'),
 ('account', 'Auth/me, logout / AuthService', 'User, RefreshToken', 'AuthApiTests; kiểm tra nguồn'),
 ('staff', 'StaffOperations, StaffBookings, StaffCourts, WalkInBookings / StaffAccess', 'StaffAssignment, Booking, Payment, CheckIn, CourtBlock', 'StaffOperationsApiTests; BookingWorkflowApiTests; staff_verify_test'),
 ('admin', 'Admin controllers / Admin*Service', 'Theo module, xem API_INVENTORY.json', 'Admin*ApiTests; admin_*_test'),
]
admin_map = {
 'branches': ('AdminSportCenters, AdminOperatingHours / AdminSportCenterService', 'SportCenter, Court, OperatingHour', 'AdminMasterDataApiTests; AdminImageUrlTests; admin_image_forms_test'),
 'courts': ('AdminCourts / AdminCourtService', 'Court, Sport, SportCenter', 'AdminMasterDataApiTests; AdminImageUrlTests; admin_images_test'),
 'staff_management': ('AdminStaff / AdminStaffService', 'User, StaffAssignment', 'AdminStaffApiTests; admin_staff_create_test'),
 'sports': ('AdminSports / AdminSportService', 'Sport, Court', 'AdminMasterDataApiTests'),
 'operating_hours': ('AdminOperatingHours / AdminOperatingHourService', 'OperatingHour, OperatingHourException', 'AdminBusinessConfigApiTests'),
 'pricing': ('AdminPriceRules / AdminPriceRuleService', 'PriceRule, Court', 'AdminBusinessConfigApiTests'),
 'settings': ('AdminSystemSettings / AdminSystemSettingService', 'SystemSetting', 'AdminBusinessConfigApiTests'),
 'cancellation_policies': ('AdminCancellationPolicies / AdminCancellationPolicyService', 'CancellationPolicy', 'AdminBusinessConfigApiTests'),
 'bookings': ('AdminBookings, AdminCancellationRequests / AdminBookingService, CancellationService', 'Booking, CancellationRequest, Payment', 'AdminCancellationApiTests; AdminBookingsAndReportsApiTests'),
 'reports': ('AdminReports / AdminReportService', 'Booking, Payment, CheckIn', 'AdminBookingsAndReportsApiTests'),
 'dashboard': ('AdminDashboard / AdminDashboardService', 'Booking, Payment, Court', 'AdminBookingsAndReportsApiTests'),
 'account': ('Auth/me, logout', 'User, RefreshToken', 'AuthApiTests'),
 'management': ('Điều hướng nội bộ', 'Không ghi DB', 'Kiểm tra route tĩnh'),
}
for row in routes:
    path = classes.get(row['screen'])
    row['file'] = str(path.relative_to(ROOT)).replace('\\', '/') if path else ''
    row['role'] = 'Admin' if row['route'].startswith('/admin') else 'Staff' if row['route'].startswith('/staff') else 'Customer/Guest'
    source = sources.get(path, '')
    module = row['file'].split('/features/')[-1].split('/')[0] if path else 'redirect'
    row['module'] = module
    found = next((v[1:] for v in mapping if v[0] == module), ('Điều hướng', '—', 'Kiểm tra route'))
    if module == 'admin':
        sub = row['file'].split('/admin/')[-1].split('/')[0]
        row['module'] += '/' + sub
        found = admin_map.get(sub, found)
    row['api'], row['entities'], row['tests'] = found
    row['signals'] = {k: bool(re.search(v, source)) for k, v in {
        'loading': r'LoadingWidget|CircularProgressIndicator|RequestView|loading|_loading',
        'error_retry': r'ErrorState|RequestView|onRetry|SnackBar|error',
        'empty': r'EmptyState|isEmpty|empty',
        'forms': r'Form\(|TextFormField|TextField',
        'images': r'SportImage|ImageUrlPreview|CachedNetworkImage|Image\.',
        'scroll': r'ListView|ScrollView|Sliver',
    }.items()}

routes.sort(key=lambda r: r['route'])
(OUT / 'ROUTE_INVENTORY.json').write_text(json.dumps(routes, ensure_ascii=False, indent=2), encoding='utf-8')
(OUT / 'API_INVENTORY.json').write_text(json.dumps(controllers, ensure_ascii=False, indent=2), encoding='utf-8')
lines = ['# Feature inventory', '',
 'Sinh từ route và mã nguồn hiện tại bằng `python docs/final-audit/generate_inventory.py`.',
 '“Có code/API” chỉ xác nhận có triển khai; không đồng nghĩa đã kiểm thử thủ công hoặc hoàn tất E2E. Test trong bảng là suite liên quan, không phải bằng chứng mọi màn hình đã được cover.', '',
 '| Vai trò | Module | Màn hình / route | API / service / entity | Trạng thái triển khai | UI / kiểm thử | Vấn đề / ưu tiên |',
 '|---|---|---|---|---|---|---|']
for r in routes:
    status = 'Redirect tương thích' if not r['file'] else 'Có code/API; xem giới hạn trong BUSINESS_FLOW_AUDIT'
    note = 'Chưa kiểm thử thủ công toàn màn hình; P2 backlog'
    if 'payments/' in r['route']: note = 'AUD-04 redirect API cũ đã sửa; external VNPAY chưa E2E'
    if r['screen'] == 'AdminCourtDetailPage': note = 'AUD-03 link cấu hình giá đã sửa'
    if r['screen'] in ['ExplorePage','CustomerHomePage']: note = 'AUD-02 callback lọc môn đã sửa'
    lines.append(f"| {r['role']} | {r['module']} | {r['screen']} `{r['route']}` | {r['api']}; {r['entities']} | {status} | {r['tests']} | {note} |")
lines += ['', '## Phạm vi và chức năng chưa có', '',
 f"- {len(routes)} route entries; {len(set(r['screen'] for r in routes if r['file']))} lớp màn hình; {len(set(r['module'] for r in routes))} nhóm module theo đường dẫn; {len(controllers)} controller files.",
 '- Chưa có endpoint/UI quên mật khẩu, reset mật khẩu hoặc sửa hồ sơ Customer. Account hiện đọc thông tin và logout.',
 '- Upload ảnh JPEG/PNG từ gallery/file đã bổ sung cho Admin cơ sở/sân; lưu cục bộ tại API App_Data/images, không dùng Cloudinary. URL cũ vẫn hỗ trợ tùy chọn.',
 '- Hoàn tiền provider thật chưa triển khai; RefundPending/manual reconciliation không phải Refunded.',
 '- `VnPayPaymentPage`/`VnPayService` cũ còn trong source để tránh xóa ngoài phạm vi, không còn được router sử dụng.',
 '- Camera, quyền vị trí, tile map và VNPAY cần xác minh thiết bị/nhà cung cấp; xem TEST_REPORT.',
 '- Navigation `Navigator.push` như dialog/map có thể nằm trong file nhiều widget; grep bổ sung được dùng cùng inventory route.']
(OUT / 'FEATURE_INVENTORY.md').write_text('\n'.join(lines)+'\n', encoding='utf-8')
print(json.dumps({'route_entries': len(routes), 'screen_classes': len(set(r['screen'] for r in routes if r['file'])), 'modules': len(set(r['module'] for r in routes)), 'controller_files': len(controllers)}))
