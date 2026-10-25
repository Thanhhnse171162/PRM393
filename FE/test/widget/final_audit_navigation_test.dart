import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/core/routing/app_router.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/admin/pricing/admin_price_rules_page.dart';
import 'package:courtgo_mobile/features/admin/settings/admin_settings_page.dart';
import 'package:courtgo_mobile/features/admin/reports/admin_booking_report_page.dart';
import 'package:courtgo_mobile/features/admin/operating_hours/admin_operating_exceptions_page.dart';
import 'package:courtgo_mobile/features/payments/checkout_page.dart';
import 'package:courtgo_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:courtgo_mobile/features/auth/data/services/auth_service.dart';
import 'package:courtgo_mobile/features/auth/presentation/providers/auth_provider.dart';
import 'package:courtgo_mobile/shared/models/user_model.dart';

import '../test_support.dart';

class _Auth extends AuthProvider {
  _Auth(this.testRole)
    : super(
        AuthRepository(
          service: AuthService(ApiClient(storage: MemoryTokens())),
          storage: MemoryTokens(),
        ),
      );
  final UserRole testRole;
  @override
  UserRole get role => testRole;
  @override
  bool get isAuthenticated => true;
}

void main() {
  final cases = [
    ('/admin/management', 'Cấu hình hệ thống', AdminSettingsPage),
    ('/admin/reports/hub', 'Báo cáo Đặt sân', AdminBookingReportPage),
    ('/admin/courts/court', 'Cấu hình quy tắc giá', AdminPriceRulesPage),
    ('/admin/operating-hours', 'Ngày ngoại lệ', AdminOperatingExceptionsPage),
  ];
  for (final scenario in cases) {
    testWidgets('real router: ${scenario.$2} opens the configured screen', (
      tester,
    ) async {
      final dio = Dio()
        ..httpClientAdapter = TestAdapter((r) async {
          if (r.path == '/api/admin/courts/court') {
            return (
              200,
              {
                'courtId': 'court',
                'sportCenterId': 'center',
                'name': 'Court & Tennis',
                'status': 'Active',
              },
            );
          }
          if (r.path == '/api/admin/sport-centers') {
            return (
              200,
              {
                'items': [
                  {'centerId': 'center', 'name': 'Center'},
                ],
              },
            );
          }
          if (r.path.endsWith('/operating-hours')) return (200, {'days': []});
          // Destination API unavailable: navigation must still show its error UI.
          return (503, {'code': 'UNAVAILABLE'});
        });
      final auth = _Auth(UserRole.admin);
      final router = AppRouter.create(auth)..go(scenario.$1);
      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthProvider>.value(value: auth),
            ChangeNotifierProvider(
              create: (_) =>
                  AdminApi(ApiClient(storage: MemoryTokens(), dio: dio)),
            ),
          ],
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await tester.pumpAndSettle();
      final link = find.text(scenario.$2);
      if (link.evaluate().isEmpty) {
        await tester.scrollUntilVisible(
          link,
          300,
          scrollable: find.byType(Scrollable).first,
        );
      }
      await tester.ensureVisible(link);
      await tester.tap(link);
      await tester.pumpAndSettle();
      expect(find.byType(scenario.$3), findsOneWidget);
      if (scenario.$3 == AdminPriceRulesPage) {
        final page = tester.widget<AdminPriceRulesPage>(
          find.byType(AdminPriceRulesPage),
        );
        expect(page.initialCourtId, 'court');
        expect(page.courtName, 'Court & Tennis');
      }
      if (scenario.$3 == AdminOperatingExceptionsPage) {
        expect(
          tester
              .widget<AdminOperatingExceptionsPage>(
                find.byType(AdminOperatingExceptionsPage),
              )
              .initialCenterId,
          'center',
        );
      }
      await tester.pumpWidget(const SizedBox());
      router.dispose();
      auth.dispose();
    });
  }

  testWidgets(
    'legacy payment bookmark redirects without creating a transaction',
    (tester) async {
      final requests = <String>[];
      final dio = Dio()
        ..httpClientAdapter = TestAdapter((r) async {
          requests.add('${r.method} ${r.path}');
          return (404, {'code': 'BOOKING_NOT_FOUND'});
        });
      final auth = _Auth(UserRole.customer);
      final router = AppRouter.create(auth)
        ..go('/customer/payments/old-booking');
      await tester.pumpWidget(
        ChangeNotifierProvider(
          create: (_) =>
              CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.byType(CheckoutPage), findsOneWidget);
      expect(requests, ['GET /api/bookings/old-booking']);
      await tester.pumpWidget(const SizedBox());
      router.dispose();
      auth.dispose();
    },
  );
}
