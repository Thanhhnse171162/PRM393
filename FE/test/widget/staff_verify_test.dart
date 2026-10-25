import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/staff/staff_api.dart';
import 'package:courtgo_mobile/features/staff/staff_booking_pages.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../test_support.dart';

void main() {
  testWidgets(
    'Verify, remaining payment and check-in are separate server operations',
    (t) async {
      var remaining = 70000;
      final requests = <RequestOptions>[];
      Map<String, dynamic> payment() => {
        'totalAmount': 100000,
        'paidAmount': 100000 - remaining,
        'remainingAmount': remaining,
      };
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter((r) async {
          requests.add(r);
          if (r.path.endsWith('/dashboard')) {
            return (200, {'centerId': 'center', 'date': '2030-01-01'});
          }
          if (r.path == '/api/sport-centers/center') {
            return (
              200,
              {
                'id': 'center',
                'name': 'Trung tâm QA',
                'timeZoneId': 'Asia/Ho_Chi_Minh',
              },
            );
          }
          if (r.path.endsWith('/verify')) {
            return (
              200,
              {
                'bookingId': 'booking',
                'bookingCode': 'QA-001',
                'bookingStatus': 'Confirmed',
                'alreadyCheckedIn': false,
                'customer': {
                  'fullName': 'Khách QA',
                  'phoneNumber': '0900000000',
                },
                'court': {'name': 'Sân A'},
                'sport': {'name': 'Cầu lông'},
                'startAt': '2030-01-01T10:00:00+07:00',
                'endAt': '2030-01-01T11:00:00+07:00',
                'payment': payment(),
              },
            );
          }
          if (r.path == '/api/staff/bookings/booking') {
            return (
              200,
              {
                'booking': {'bookingStatus': 'Confirmed'},
                'payment': payment(),
              },
            );
          }
          if (r.path.endsWith('/payments/remaining')) {
            remaining = 0;
            return (200, {'paymentStatus': 'FullyPaid'});
          }
          if (r.path.endsWith('/check-ins')) {
            return (200, {'bookingStatus': 'CheckedIn'});
          }
          return (404, {});
        });
      final api = StaffApi(
        CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
      );
      final router = GoRouter(
        initialLocation: '/staff/verify',
        routes: [
          GoRoute(
            path: '/staff/verify',
            builder: (_, _) => StaffVerifyPage(token: 'a' * 48),
          ),
          GoRoute(
            path: '/staff/bookings/booking',
            builder: (_, _) => const Scaffold(body: Text('CheckedIn verified')),
          ),
        ],
      );
      await t.pumpWidget(
        ChangeNotifierProvider.value(
          value: api,
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await t.pumpAndSettle();
      expect(find.text('QR hợp lệ'), findsOneWidget);
      expect(requests.where((r) => r.path.endsWith('/check-ins')), isEmpty);
      expect(find.text('Xác nhận check-in'), findsNothing);
      await t.ensureVisible(find.text('Thu tiền còn lại'));
      await t.tap(find.text('Thu tiền còn lại'));
      await t.pumpAndSettle();
      await t.tap(find.textContaining('Đã nhận 70.000'));
      await t.pumpAndSettle();
      final collected = requests.singleWhere(
        (r) => r.path.endsWith('/payments/remaining'),
      );
      expect(collected.data, {'paymentMethod': 'Cash'});
      expect(requests.where((r) => r.path.endsWith('/check-ins')), isEmpty);
      await t.ensureVisible(find.text('Xác nhận check-in'));
      await t.tap(find.text('Xác nhận check-in'));
      await t.pumpAndSettle();
      await t.tap(find.text('Xác nhận'));
      await t.pumpAndSettle();
      expect(requests.where((r) => r.path.endsWith('/check-ins')).length, 1);
      expect(
        requests.where((r) => r.path.endsWith('/verify')).length,
        greaterThanOrEqualTo(3),
      );
      expect(find.text('CheckedIn verified'), findsOneWidget);
      await t.pumpWidget(const SizedBox());
      router.dispose();
      api.dispose();
    },
  );
}
