import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/core/storage/local_storage_service.dart';
import 'package:courtgo_mobile/features/home/discovery_provider.dart';
import 'package:courtgo_mobile/shared/models/customer_models.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../test_support.dart';

void main() {
  test('Discovery sends real backend filters, parses nullable price and never injects sample venues', () async {
    SharedPreferences.setMockInitialValues({});
    final requests = <RequestOptions>[];
    final dio = Dio(BaseOptions(baseUrl: 'http://test'));
    dio.httpClientAdapter = TestAdapter((r) async {
      requests.add(r);
      if (r.path == '/api/sports') {
        return (
          200,
          [
            {'id': 'sport-id', 'name': 'Cầu lông'},
          ],
        );
      }
      return (
        200,
        [
          {
            'id': 'center-id',
            'name': 'CourtGo Q7',
            'addressLine': 'Đường A',
            'district': 'Quận 7',
            'city': 'HCM',
            'sports': ['Cầu lông'],
            'priceFrom': null,
          },
        ],
      );
    });
    final state = DiscoveryProvider(
      CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
      await LocalStorageService.create(),
    );
    state.sportId = 'sport-id';
    state.area = 'Quận 7';
    await state.load();
    expect(requests.last.queryParameters['sportId'], 'sport-id');
    expect(requests.last.queryParameters['district'], 'Quận 7');
    expect(state.centers.single.name, 'CourtGo Q7');
    expect(state.centers.single.priceFrom, isNull);
    state.dispose();
  });
  test('Court and booking parsers follow authoritative prices and historical snapshots', () {
    final court = Court.fromJson({
      'id': 'c',
      'sportCenterId': 's',
      'sportCenterName': 'New name',
      'sportId': 'x',
      'sportName': 'Tennis',
      'code': 'A',
      'name': 'A',
      'status': 'Active',
      'basePricePerHour': 125000,
    });
    expect(court.price, 125000);
    final booking = Booking.fromJson({
      'bookingId': 'b',
      'bookingCode': 'CG',
      'bookingStatus': 'Confirmed',
      'paymentStatus': 'DepositPaid',
      'center': {'name': 'Historical center'},
      'court': {'id': 'c', 'name': 'Historical court'},
      'sport': {'name': 'Tennis'},
      'startAt': '2026-10-20T10:00:00+07:00',
      'endAt': '2026-10-20T11:00:00+07:00',
      'durationMinutes': 60,
      'payment': {
        'totalAmount': 125000,
        'depositAmount': 37500,
        'paidAmount': 37500,
        'remainingAmount': 87500,
      },
      'hasQr': true,
    });
    expect(booking.center, 'Historical center');
    expect(booking.court, 'Historical court');
    expect(booking.remaining, 87500);
  });
}
