import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/admin/branches/admin_center_form_page.dart';
import 'package:courtgo_mobile/features/admin/shared/admin_models.dart';

import '../test_support.dart';

void main() {
  late AdminApi adminApi;

  final q7Json = {
    'centerId': '11111111-1111-1111-1111-111111111111',
    'name': 'CourtGo Arena Q7',
    'addressLine': '32 Huỳnh Tấn Phát',
    'ward': 'Tân Thuận Đông',
    'district': 'Quận 7',
    'city': 'Hồ Chí Minh',
    'latitude': 10.819840,
    'longitude': 106.831944,
    'timeZoneId': 'Asia/Ho_Chi_Minh',
    'phoneNumber': null,
    'coverImageUrl': null,
    'status': 'Active',
    'totalCourts': 3,
    'activeCourts': 3,
    'assignedActiveStaffCount': 3,
    'createdAt': '2026-10-06T06:34:56.4603286+00:00',
    'updatedAt': '2026-10-25T14:24:29.5459497+00:00',
  };

  final thuDucJson = {
    'centerId': '81498ffd-8279-4f2f-9898-5af6ccac7199',
    'name': 'CourtGo Thu Duc',
    'addressLine': '10 Vo Van Ngan',
    'ward': 'Linh Chieu',
    'district': 'Thu Duc',
    'city': 'Ho Chi Minh',
    'latitude': null,
    'longitude': null,
    'timeZoneId': 'Asia/Ho_Chi_Minh',
    'phoneNumber': null,
    'coverImageUrl': null,
    'status': 'Active',
    'totalCourts': 1,
    'activeCourts': 1,
    'assignedActiveStaffCount': 0,
    'createdAt': '2026-10-25T14:07:01.7124481+00:00',
    'updatedAt': null,
  };

  setUp(() {
    final dio = Dio()
      ..httpClientAdapter = TestAdapter((r) async {
        if (r.path.contains('11111111-1111-1111-1111-111111111111')) {
          return (200, q7Json);
        }
        return (200, thuDucJson);
      });
    adminApi = AdminApi(ApiClient(storage: MemoryTokens(), dio: dio));
  });

  Widget createSubject({String? centerId, AdminSportCenterDetail? initialData}) {
    return ChangeNotifierProvider<AdminApi>.value(
      value: adminApi,
      child: MaterialApp(
        home: AdminCenterFormPage(centerId: centerId, initialData: initialData),
      ),
    );
  }

  testWidgets('Test Edit Q7 with initialData', (tester) async {
    final detail = AdminSportCenterDetail.fromJson(q7Json);
    await tester.pumpWidget(createSubject(centerId: detail.centerId, initialData: detail));
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.text('Chỉnh sửa cơ sở thể thao'), findsOneWidget);
    expect(find.text('Lưu thay đổi'), findsOneWidget);
  });

  testWidgets('Test Edit Q7 without initialData (calls _fetchInitial)', (tester) async {
    await tester.pumpWidget(createSubject(centerId: '11111111-1111-1111-1111-111111111111'));
    await tester.pumpAndSettle();
    expect(find.text('Chỉnh sửa cơ sở thể thao'), findsOneWidget);
    expect(find.text('Lưu thay đổi'), findsOneWidget);
  });

  testWidgets('Test Edit Thu Duc (null coordinates) with initialData', (tester) async {
    final detail = AdminSportCenterDetail.fromJson(thuDucJson);
    await tester.pumpWidget(createSubject(centerId: detail.centerId, initialData: detail));
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.text('Chỉnh sửa cơ sở thể thao'), findsOneWidget);
    expect(find.text('Chưa chọn vị trí trên bản đồ'), findsOneWidget);
  });

  testWidgets('Test Edit Thu Duc without initialData', (tester) async {
    await tester.pumpWidget(createSubject(centerId: '81498ffd-8279-4f2f-9898-5af6ccac7199'));
    await tester.pumpAndSettle();
    expect(find.text('Chỉnh sửa cơ sở thể thao'), findsOneWidget);
    expect(find.text('Chưa chọn vị trí trên bản đồ'), findsOneWidget);
  });
}
