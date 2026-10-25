import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/core/routing/app_router.dart';
import 'package:courtgo_mobile/core/storage/local_storage_service.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:courtgo_mobile/core/theme/app_theme.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:courtgo_mobile/features/auth/data/services/auth_service.dart';
import 'package:courtgo_mobile/features/auth/presentation/providers/auth_provider.dart';
import 'package:courtgo_mobile/features/staff/staff_api.dart';
import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/features/home/discovery_provider.dart';
import 'package:courtgo_mobile/features/notifications/notification_provider.dart';
import 'package:courtgo_mobile/shared/models/user_model.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../test_support.dart';

class _AdminAuth extends AuthProvider {
  _AdminAuth(super.repository);
  @override
  UserRole get role => UserRole.admin;
  @override
  bool get isAuthenticated => true;
}

void main() {
  testWidgets('Full GoRouter navigation to admin center edit page', (tester) async {
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

    final dio = Dio()
      ..httpClientAdapter = TestAdapter((r) async {
        return (200, q7Json);
      });
    final client = ApiClient(storage: MemoryTokens(), dio: dio);
    final authService = AuthService(client);
    final authRepo = AuthRepository(service: authService, storage: MemoryTokens());
    final auth = _AdminAuth(authRepo);

    final router = AppRouter.create(auth);
    final customerApi = CustomerApi(client);
    SharedPreferences.setMockInitialValues({});
    final local = await LocalStorageService.create();

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthProvider>.value(value: auth),
          Provider<ApiClient>.value(value: client),
          ChangeNotifierProvider<CustomerApi>.value(value: customerApi),
          ChangeNotifierProvider<AdminApi>(create: (_) => AdminApi(client)),
          ChangeNotifierProxyProvider<AuthProvider, StaffApi>(
            create: (_) => StaffApi(customerApi),
            update: (_, a, s) => s!..setUser(a.user?.id),
          ),
          ChangeNotifierProvider<DiscoveryProvider>(
            create: (_) => DiscoveryProvider(customerApi, local),
          ),
          ChangeNotifierProxyProvider<AuthProvider, NotificationProvider>(
            create: (_) => NotificationProvider(customerApi),
            update: (_, a, n) => n!..setUser(a.user?.id),
          ),
        ],
        child: MaterialApp.router(
          theme: AppTheme.light,
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();

    // Now navigate to center detail, then click Edit
    router.go('/admin/centers/11111111-1111-1111-1111-111111111111');
    await tester.pumpAndSettle();

    expect(find.text('Chỉnh sửa'), findsOneWidget);
    await tester.tap(find.text('Chỉnh sửa'));
    await tester.pumpAndSettle();

    final err = tester.takeException();
    expect(err, isNull);

    expect(find.text('Chỉnh sửa cơ sở thể thao'), findsOneWidget);
    expect(find.text('Thông tin chung'), findsOneWidget);
    expect(find.text('Lưu thay đổi'), findsOneWidget);
  });
}
