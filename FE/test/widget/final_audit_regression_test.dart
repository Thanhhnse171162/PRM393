import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:courtgo_mobile/shared/widgets/customer_widgets.dart';
import 'package:courtgo_mobile/features/admin/shared/admin_widgets.dart';

void main() {
  test('Admin occupancy keeps percentage points returned by the API', () {
    expect(AdminFormat.percent(4.44), '4.4%');
    expect(AdminFormat.percent(100), '100.0%');
    expect(AdminFormat.percent(0), '0.0%');
  });
  testWidgets('sport filter invokes selection callback exactly once', (
    tester,
  ) async {
    var calls = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SportChip(
            label: 'Tennis',
            selected: false,
            onTap: () => calls++,
          ),
        ),
      ),
    );
    await tester.tap(find.text('Tennis'));
    expect(calls, 1);
  });

  test('Admin labels match actual booking and payment enums', () {
    expect(AdminFormat.bookingStatusName('PendingPayment'), 'Chờ thanh toán');
    expect(AdminFormat.bookingStatusName('Expired'), 'Hết thời gian giữ');
    expect(AdminFormat.paymentStatusName('Unpaid'), 'Chưa thanh toán');
    expect(AdminFormat.paymentStatusName('DepositPaid'), 'Đã cọc');
    expect(AdminFormat.paymentStatusName('RefundPending'), 'Chờ hoàn tiền');
    expect(AdminFormat.paymentStatusName('Refunded'), 'Đã hoàn tiền');
  });
}
