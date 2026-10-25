import 'dart:async';

import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/booking/booking_qr_page.dart';
import 'package:courtgo_mobile/features/payments/payment_page.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../test_support.dart';

void main() {
  testWidgets(
    'resume during status request queues one reconciliation without creating payment',
    (t) async {
      final first = Completer<void>();
      var reads = 0, queries = 0;
      final pending = <String, dynamic>{
        'bookingStatus': 'Expired',
        'paymentStatus': 'Unpaid',
        'payments': [
          {
            'paymentId': 'payment',
            'paymentKind': 'Deposit',
            'paymentMethod': 'VNPay',
            'transactionStatus': 'Pending',
          },
        ],
      };
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter((r) async {
          if (r.method == 'POST') {
            queries++;
            expect(r.path.endsWith('/reconcile'), isTrue);
          }
          if (r.method == 'GET' && reads++ == 0) await first.future;
          return (200, pending);
        });
      await t.pumpWidget(
        ChangeNotifierProvider<CustomerApi>(
          create: (_) =>
              CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
          child: const MaterialApp(
            home: PaymentPage(id: 'booking', attempt: {'paymentId': 'payment'}),
          ),
        ),
      );
      await t.pump();
      t.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
      t.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
      first.complete();
      await t.pumpAndSettle();
      expect(reads, 2);
      expect(queries, 1);
    },
  );

  testWidgets(
    'expired hold recovers verified charge without another checkout',
    (t) async {
      final requests = <String>[];
      final pending = <String, dynamic>{
        'bookingStatus': 'Expired',
        'paymentStatus': 'Unpaid',
        'payments': [
          {
            'paymentId': 'payment',
            'paymentKind': 'Deposit',
            'paymentMethod': 'VNPay',
            'transactionStatus': 'Pending',
          },
        ],
      };
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter((r) async {
          requests.add('${r.method} ${r.path}');
          return (
            200,
            r.method == 'POST'
                ? {
                    ...pending,
                    'paymentStatus': 'RefundPending',
                    'payments': [
                      {
                        'paymentId': 'payment',
                        'paymentKind': 'Deposit',
                        'paymentMethod': 'VNPay',
                        'transactionStatus': 'Succeeded',
                      },
                    ],
                  }
                : pending,
          );
        });
      await t.pumpWidget(
        ChangeNotifierProvider<CustomerApi>(
          create: (_) =>
              CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
          child: const MaterialApp(home: PaymentPage(id: 'booking')),
        ),
      );
      await t.pumpAndSettle();
      expect(find.text('Đang chờ đối soát hoàn tiền'), findsOneWidget);
      expect(find.text('Mở trang thanh toán'), findsNothing);
      expect(find.text('Thử thanh toán lại'), findsNothing);
      expect(requests, [
        'GET /api/bookings/booking/payment-status',
        'POST /api/bookings/booking/payments/reconcile',
      ]);
    },
  );

  testWidgets(
    'query network failure preserves pending state and allows manual recovery',
    (t) async {
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter(
          (r) async => r.method == 'POST'
              ? (503, {'code': 'PAYMENT_QUERY_UNAVAILABLE'})
              : (
                  200,
                  {
                    'bookingStatus': 'Expired',
                    'paymentStatus': 'Unpaid',
                    'payments': [
                      {
                        'paymentKind': 'Deposit',
                        'paymentMethod': 'VNPay',
                        'transactionStatus': 'Pending',
                      },
                    ],
                  },
                ),
        );
      await t.pumpWidget(
        ChangeNotifierProvider<CustomerApi>(
          create: (_) =>
              CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
          child: const MaterialApp(home: PaymentPage(id: 'booking')),
        ),
      );
      await t.pumpAndSettle();
      expect(find.textContaining('không cần thanh toán thêm'), findsOneWidget);
      expect(find.text('Kiểm tra trạng thái'), findsOneWidget);
      expect(find.text('Lượt giữ sân đã hết hạn'), findsOneWidget);
    },
  );

  Widget host(Widget page, int status, Map<String, dynamic> response) {
    final dio = Dio(BaseOptions(baseUrl: 'http://test'))
      ..httpClientAdapter = TestAdapter((_) async => (status, response));
    return ChangeNotifierProvider<CustomerApi>(
      create: (_) => CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
      child: MaterialApp(home: page),
    );
  }

  for (final state in ['Pending', 'Failed', 'Cancelled']) {
    testWidgets('$state deposit never becomes a confirmed booking locally', (
      t,
    ) async {
      await t.pumpWidget(
        host(
          const PaymentPage(
            id: 'booking',
            attempt: {
              'paymentId': 'payment',
              'amount': 90000,
              'gateway': 'Development',
            },
          ),
          200,
          {
            'bookingStatus': 'PendingPayment',
            'paymentStatus': 'Unpaid',
            'payments': [
              {
                'paymentId': 'payment',
                'paymentKind': 'Deposit',
                'transactionStatus': state,
              },
            ],
          },
        ),
      );
      await t.pump(const Duration(milliseconds: 300));
      expect(find.byType(PaymentPage), findsOneWidget);
      expect(find.text('Xem mã QR'), findsNothing);
      if (state == 'Pending') {
        expect(find.text('Đang chờ xác nhận cọc'), findsOneWidget);
        expect(find.textContaining('Không chuyển tiền thật'), findsOneWidget);
      } else {
        expect(find.text('Thử thanh toán lại'), findsOneWidget);
        expect(find.text('Giả lập cọc thành công'), findsNothing);
      }
      await t.pumpWidget(const SizedBox());
    });
  }

  testWidgets('QR_NOT_AVAILABLE shows retry without rendering any token', (
    t,
  ) async {
    await t.pumpWidget(
      host(const BookingQrPage(id: 'booking'), 409, {
        'code': 'QR_NOT_AVAILABLE',
        'detail': 'private-token-do-not-display',
      }),
    );
    await t.pumpAndSettle();
    expect(find.byType(QrImageView), findsNothing);
    expect(find.textContaining('Mã QR chưa khả dụng'), findsOneWidget);
    expect(find.textContaining('private-token'), findsNothing);
  });
}
