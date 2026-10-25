import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/core/routing/app_router.dart';
import 'package:courtgo_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:courtgo_mobile/features/auth/data/services/auth_service.dart';
import 'package:courtgo_mobile/features/auth/presentation/providers/auth_provider.dart';
import 'package:courtgo_mobile/shared/models/user_model.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import '../test_support.dart';

class _CustomerAuth extends AuthProvider {
  _CustomerAuth()
    : super(
        AuthRepository(
          service: AuthService(ApiClient(storage: MemoryTokens())),
          storage: MemoryTokens(),
        ),
      );
  @override
  bool get isAuthenticated => true;
  @override
  UserRole get role => UserRole.customer;
}

void main() => paymentRouteTests();

// Also executed on Android by the isolated payment integration suite.
void paymentRouteTests() {
  Future<void> waitForText(WidgetTester tester, String text) async {
    for (var i = 0; i < 80 && find.text(text).evaluate().isEmpty; i++) {
      await tester.pump(const Duration(milliseconds: 100));
    }
    expect(find.text(text), findsOneWidget);
  }

  testWidgets(
    'real AppRouter switches payment result when warm link changes booking',
    (t) async {
      final reads = <String>[];
      final methods = <String>[];
      final fixtureErrors = <Object>[];
      final dio = Dio(BaseOptions(baseUrl: 'http://fixture.invalid'))
        ..httpClientAdapter = TestAdapter((request) async {
          methods.add(request.method);
          reads.add(request.path);
          final first = request.path.contains('/first/');
          return (
            200,
            {
              'bookingStatus': 'Expired',
              'paymentStatus': first ? 'RefundPending' : 'Unpaid',
              'payments': [
                {
                  'paymentKind': 'Deposit',
                  'paymentMethod': 'VNPay',
                  'transactionStatus': first ? 'Succeeded' : 'Failed',
                },
              ],
            },
          );
        });
      dio.interceptors.add(
        InterceptorsWrapper(
          onError: (error, handler) {
            fixtureErrors.add(error.error ?? error.type);
            handler.next(error);
          },
        ),
      );
      final auth = _CustomerAuth();
      final router = AppRouter.create(auth)
        ..go('/customer/bookings/first/payment');
      await t.pumpWidget(
        ChangeNotifierProvider<CustomerApi>(
          create: (_) =>
              CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await t.pumpAndSettle();
      await waitForText(t, 'Đang chờ đối soát hoàn tiền');
      expect(
        fixtureErrors,
        isEmpty,
        reason: 'Fixture transport must not fail: $fixtureErrors',
      );
      expect(find.text('Đang chờ đối soát hoàn tiền'), findsOneWidget);
      router.go('/customer/bookings/second/payment');
      await t.pumpAndSettle();
      await waitForText(t, 'Lượt giữ sân đã hết hạn');
      expect(find.text('Lượt giữ sân đã hết hạn'), findsOneWidget);
      expect(find.text('Đang chờ đối soát hoàn tiền'), findsNothing);
      expect(reads, [
        '/api/bookings/first/payment-status',
        '/api/bookings/second/payment-status',
      ]);
      expect(methods, everyElement('GET'));
      router.go('/customer/bookings/first/payment');
      await t.pumpAndSettle();
      await waitForText(t, 'Đang chờ đối soát hoàn tiền');
      expect(find.text('Đang chờ đối soát hoàn tiền'), findsOneWidget);
      await t.pumpWidget(const SizedBox());
      router.dispose();
      auth.dispose();
    },
  );
}
