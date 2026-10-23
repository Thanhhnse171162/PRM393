import 'package:courtgo_mobile/features/staff/staff_api.dart';
import 'package:courtgo_mobile/features/staff/staff_scanner.dart';
import 'package:courtgo_mobile/features/staff/staff_walk_in.dart';
import 'package:courtgo_mobile/features/payments/payment_links.dart';
import 'package:courtgo_mobile/shared/models/customer_models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test(
    'Payment links accept only the payment host and one valid booking ID',
    () {
      const id = '11111111-2222-3333-4444-555555555555';
      expect(
        paymentBookingFromLink(
          Uri.parse('courtgo://payment-result?bookingId=$id&status=success'),
        ),
        id,
      );
      for (final url in [
        'https://payment-result?bookingId=$id',
        'courtgo://evil?bookingId=$id',
        'courtgo://payment-result?bookingId=$id&bookingId=$id',
        'courtgo://payment-result?bookingId=../../admin',
      ]) {
        expect(paymentBookingFromLink(Uri.parse(url)), isNull);
      }
      expect(
        trustedCheckoutUrl(Uri.parse('https://pay.vnpay.vn/vpcpay.html')),
        isTrue,
      );
      expect(
        trustedCheckoutUrl(Uri.parse('https://momo.vn.evil.test/pay')),
        isFalse,
      );
      expect(
        trustedCheckoutUrl(Uri.parse('http://test-payment.momo.vn/pay')),
        isFalse,
      );
    },
  );
  test(
    'Check-in requires a verified confirmed booking with no outstanding money',
    () {
      final booking = <String, dynamic>{
        'bookingStatus': 'Confirmed',
        'alreadyCheckedIn': false,
        'payment': {'remainingAmount': 100000},
      };
      expect(canCheckIn(booking), isFalse);
      booking['payment'] = {'remainingAmount': 0};
      expect(canCheckIn(booking), isTrue);
      booking['alreadyCheckedIn'] = true;
      expect(canCheckIn(booking), isFalse);
      booking['alreadyCheckedIn'] = false;
      booking['bookingStatus'] = 'Expired';
      expect(canCheckIn(booking), isFalse);
    },
  );
  test('NoShow is not available before the booking end', () {
    final b = <String, dynamic>{
      'bookingStatus': 'Confirmed',
      'endAt': '2030-01-01T10:00:00+07:00',
    };
    expect(canMarkNoShow(b, DateTime.parse('2030-01-01T02:59:59Z')), isFalse);
    expect(canMarkNoShow(b, DateTime.parse('2030-01-01T03:00:00Z')), isTrue);
    b['bookingStatus'] = 'CheckedIn';
    expect(canMarkNoShow(b, DateTime.parse('2030-01-01T04:00:00Z')), isFalse);
  });
  test('Walk-in rejects gaps, unavailable slots and non-hour durations', () {
    Slot slot(
      int hour, {
      String status = 'Available',
      int duration = 1,
    }) => Slot.fromJson({
      'startAt': '2030-01-01T${hour.toString().padLeft(2, '0')}:00:00+07:00',
      'endAt':
          '2030-01-01T${(hour + duration).toString().padLeft(2, '0')}:00:00+07:00',
      'price': 100000,
      'status': status,
    });
    expect(consecutiveStaffSlots([slot(10), slot(9)]), isTrue);
    expect(consecutiveStaffSlots([slot(9), slot(11)]), isFalse);
    expect(consecutiveStaffSlots([slot(9, status: 'Held')]), isFalse);
    expect(consecutiveStaffSlots([slot(9, duration: 2)]), isFalse);
    expect(validStaffQr('CG-250925-A1'), isFalse);
    expect(validStaffQr('a' * 48), isTrue);
  });
}
