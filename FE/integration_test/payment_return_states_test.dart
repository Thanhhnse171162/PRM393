import 'dart:ui' as ui;

import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/booking/booking_success_page.dart';
import 'package:courtgo_mobile/features/payments/payment_page.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

import '../test/test_support.dart';
import '../test/widget/payment_route_test.dart' as routing;

// Android rendering tests with isolated API fixtures. No real HTTP, credentials,
// DB mutation, checkout creation, provider call or refund is permitted here.
void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  routing.paymentRouteTests();
  const id = '11111111-1111-1111-1111-111111111111';
  final cases = [
    ('confirmed', 'Confirmed', 'DepositPaid', 'Succeeded', 'Sân đã sẵn sàng!'),
    (
      'refund-pending',
      'Expired',
      'RefundPending',
      'Succeeded',
      'Đang chờ đối soát hoàn tiền',
    ),
    ('expired', 'Expired', 'Unpaid', 'Failed', 'Lượt giữ sân đã hết hạn'),
    (
      'paid-unconfirmed',
      'PendingPayment',
      'Unpaid',
      'Succeeded',
      'Đang chờ xác nhận cọc',
    ),
    (
      'failed',
      'PendingPayment',
      'Unpaid',
      'Failed',
      'Thanh toán chưa thành công',
    ),
    ('cancelled', 'PendingPayment', 'Unpaid', 'Cancelled', 'Đã hủy thanh toán'),
  ];

  for (final scenario in cases) {
    testWidgets('Android payment fixture: ${scenario.$1}', (t) async {
      final requests = <String>[];
      final fixtureErrors = <Object>[];
      final dio = Dio(BaseOptions(baseUrl: 'http://fixture.invalid'))
        ..httpClientAdapter = TestAdapter((request) async {
          requests.add('${request.method} ${request.path}');
          if (request.path.endsWith('/payment-status')) {
            return (
              200,
              {
                'bookingId': id,
                'bookingStatus': scenario.$2,
                'paymentStatus': scenario.$3,
                'payments': [
                  {
                    'paymentId': 'fixture-payment',
                    'paymentKind': 'Deposit',
                    'paymentMethod': 'VNPay',
                    'transactionStatus': scenario.$4,
                    'amount': 90000,
                  },
                ],
              },
            );
          }
          return (
            200,
            {
              'bookingId': id,
              'bookingCode': 'UI-FIXTURE-CONFIRMED',
              'bookingStatus': scenario.$2,
              'paymentStatus': scenario.$3,
              'centerName': 'CourtGo UI fixture',
              'courtName': 'Sân thử giao diện',
              'sportName': 'Cầu lông',
              'startAt': '2026-10-25T17:00:00+07:00',
              'endAt': '2026-10-25T19:00:00+07:00',
              'durationMinutes': 120,
              'totalAmount': 300000,
              'depositAmount': 90000,
              'paidAmount': 90000,
              'remainingAmount': 210000,
              'hasQr': true,
              'slots': [],
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
      final router = GoRouter(
        initialLocation: '/customer/bookings/$id/payment',
        routes: [
          GoRoute(
            path: '/customer/bookings/:id/payment',
            builder: (_, _) => const PaymentPage(id: id),
          ),
          GoRoute(
            path: '/customer/bookings/:id/success',
            builder: (_, _) => const BookingSuccessPage(id: id),
          ),
        ],
      );
      final screenKey = GlobalKey();
      await t.pumpWidget(
        ChangeNotifierProvider<CustomerApi>(
          create: (_) =>
              CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
          child: RepaintBoundary(
            key: screenKey,
            child: MaterialApp.router(routerConfig: router),
          ),
        ),
      );
      for (
        var i = 0;
        i < 40 &&
            (requests.length < (scenario.$1 == 'confirmed' ? 2 : 1) ||
                find.text(scenario.$5).evaluate().isEmpty);
        i++
      ) {
        await t.pump(const Duration(milliseconds: 200));
      }
      await t.pump(const Duration(milliseconds: 200));
      expect(
        fixtureErrors,
        isEmpty,
        reason: 'Fixture transport must not fail: $fixtureErrors',
      );
      expect(find.text(scenario.$5), findsOneWidget);
      // Assertions run in the test zone, never in a live Android frame's async transport callback.
      expect(
        requests,
        scenario.$1 == 'confirmed'
            ? ['GET /api/bookings/$id/payment-status', 'GET /api/bookings/$id']
            : ['GET /api/bookings/$id/payment-status'],
      );
      if (scenario.$1 == 'confirmed') {
        expect(
          router.routeInformationProvider.value.uri.path,
          endsWith('/success'),
        );
        expect(find.textContaining('90.000'), findsOneWidget);
        expect(requests.length, 2);
      } else {
        expect(
          router.routeInformationProvider.value.uri.path,
          endsWith('/payment'),
        );
        expect(find.text('Xem mã QR'), findsNothing);
        expect(requests.length, 1);
      }
      await t.pump(const Duration(milliseconds: 300));
      final boundary =
          screenKey.currentContext!.findRenderObject()!
              as RenderRepaintBoundary;
      final frame = await boundary.toImage(pixelRatio: 1);
      final bytes = await frame.toByteData(format: ui.ImageByteFormat.png);
      frame.dispose();
      binding.reportData ??= {};
      final screenshots = binding.reportData!.putIfAbsent(
        'screenshots',
        () => <dynamic>[],
      ) as List;
      screenshots.add({
        'screenshotName': 'fixture-${scenario.$1}',
        'bytes': bytes!.buffer.asUint8List().toList(),
      });
      await t.pumpWidget(const SizedBox());
      router.dispose();
    });
  }
}
