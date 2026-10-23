import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';

import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/admin/branches/admin_center_form_page.dart';
import 'package:courtgo_mobile/features/admin/branches/admin_location_picker_page.dart';
import 'package:courtgo_mobile/features/admin/shared/admin_models.dart';
import '../test_support.dart';

void main() {
  late AdminApi adminApi;

  setUp(() {
    final dio = Dio()
      ..httpClientAdapter = TestAdapter((r) async {
        return (200, <String, dynamic>{});
      });
    adminApi = AdminApi(ApiClient(storage: MemoryTokens(), dio: dio));
  });

  Widget createSubject({
    String? centerId,
    AdminSportCenterDetail? initialData,
  }) {
    return ChangeNotifierProvider<AdminApi>.value(
      value: adminApi,
      child: MaterialApp(
        home: AdminCenterFormPage(
          centerId: centerId,
          initialData: initialData,
        ),
      ),
    );
  }

  group('AdminCenterFormPage Redesign Tests', () {
    testWidgets('renders all required form sections and fields in Create Mode',
        (tester) async {
      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      // Section A: Header
      expect(find.text('Thêm cơ sở thể thao'), findsOneWidget);
      expect(
        find.text('Tạo thông tin cơ sở thể thao để quản lý trên hệ thống'),
        findsOneWidget,
      );

      // Section B: Thông tin chung
      expect(find.text('Thông tin chung'), findsOneWidget);
      expect(find.text('Tên cơ sở thể thao *'), findsOneWidget);
      expect(find.text('Số điện thoại liên hệ'), findsOneWidget);
      expect(find.text('Múi giờ vận hành *'), findsOneWidget);

      // Section C: Địa chỉ & Vị trí
      expect(find.text('Địa chỉ & Vị trí'), findsOneWidget);
      expect(find.text('Địa chỉ (Số nhà, tên đường) *'), findsOneWidget);
      expect(find.text('Phường / Xã'), findsOneWidget);
      expect(find.text('Quận / Huyện *'), findsOneWidget);
      expect(find.text('Tỉnh / Thành phố *'), findsOneWidget);
      expect(find.text('Vị trí trên bản đồ *'), findsOneWidget);
      expect(find.text('Chọn vị trí trên bản đồ'), findsOneWidget);

      // Section D: Hình ảnh đại diện
      expect(find.text('Hình ảnh đại diện'), findsOneWidget);
      expect(find.text('Đường dẫn ảnh cover (URL)'), findsOneWidget);

      // Section E: Submit Button
      expect(find.text('Tạo cơ sở thể thao'), findsOneWidget);

      // Verify editable latitude and longitude text fields are REMOVED
      expect(find.text('Vĩ độ (Latitude)'), findsNothing);
      expect(find.text('Kinh độ (Longitude)'), findsNothing);
    });

    testWidgets('shows validation errors when submitting empty required fields',
        (tester) async {
      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      // Tap submit button without filling required fields
      final submitButton = find.text('Tạo cơ sở thể thao');
      await tester.ensureVisible(submitButton);
      await tester.tap(submitButton);
      await tester.pumpAndSettle();

      // Check validation messages
      expect(find.text('Vui lòng nhập tên cơ sở thể thao'), findsOneWidget);
      expect(find.text('Vui lòng nhập số nhà và tên đường'), findsOneWidget);
      expect(find.text('Vui lòng chọn vị trí cơ sở trên bản đồ.'), findsWidgets);
    });

    testWidgets('displays pinned coordinates and Edit mode properly',
        (tester) async {
      final detail = AdminSportCenterDetail(
        centerId: 'center-123',
        name: 'CourtGo Sala Complex',
        addressLine: 'Số 10 Mai Chí Thọ',
        ward: 'An Lợi Đông',
        district: 'Thành phố Thủ Đức',
        city: 'TP. Hồ Chí Minh',
        latitude: 10.771234,
        longitude: 106.721234,
        timeZoneId: 'Asia/Ho_Chi_Minh',
        phoneNumber: '0901234567',
        coverImageUrl: 'https://example.com/cover.jpg',
        status: 'Active',
        totalCourts: 6,
        activeCourts: 6,
        assignedActiveStaffCount: 2,
        createdAt: '2026-01-01T00:00:00Z',
      );

      await tester.pumpWidget(createSubject(
        centerId: 'center-123',
        initialData: detail,
      ));
      await tester.pumpAndSettle();

      // Edit Mode headers & button
      expect(find.text('Chỉnh sửa cơ sở thể thao'), findsOneWidget);
      expect(find.text('Lưu thay đổi'), findsOneWidget);

      // Pinned status & coordinates
      expect(find.text('Đã ghim vị trí'), findsOneWidget);
      expect(find.textContaining('10.771234'), findsOneWidget);
      expect(find.textContaining('106.721234'), findsOneWidget);
      expect(find.text('Đổi vị trí'), findsOneWidget);
    });
  });

  group('AdminLocationPickerPage Tests', () {
    testWidgets('renders search bar, map, and confirmation panel',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: AdminLocationPickerPage(
            initialLocation: LatLng(10.7769, 106.7009),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Chọn vị trí cơ sở'), findsOneWidget);
      expect(find.text('Tìm địa chỉ hoặc địa điểm...'), findsOneWidget);
      expect(find.text('Vị trí đã chọn'), findsOneWidget);
      expect(find.textContaining('10.776900'), findsOneWidget);
      expect(find.textContaining('106.700900'), findsOneWidget);
      expect(find.text('Xác nhận vị trí'), findsOneWidget);
      expect(find.byIcon(Icons.my_location), findsOneWidget);
    });

    test('NominatimPlace parses JSON correctly', () {
      final json = {
        'display_name': 'Khu Đô Thị Sala, Quận 2, TP. Hồ Chí Minh',
        'lat': '10.7712',
        'lon': '106.7212',
      };

      final place = NominatimPlace.fromJson(json);
      expect(place.displayName, 'Khu Đô Thị Sala, Quận 2, TP. Hồ Chí Minh');
      expect(place.latitude, 10.7712);
      expect(place.longitude, 106.7212);
    });
  });
}
