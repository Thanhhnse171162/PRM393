import 'package:flutter/foundation.dart';

import '../network/api_client.dart';
import '../network/network_result.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/models/sport_model.dart';

class CustomerApi extends ChangeNotifier {
  CustomerApi(this.client);
  final ApiClient client;
  Future<T> _value<T>(Future<NetworkResult<T>> result) async =>
      (await result).when(success: (v) => v, failure: (e) => throw e);
  Future<Json> get(String path, {Json? query}) =>
      _value(client.get<Json>(path, query: query));
  Future<Json> post(String path, Json body) async {
    final result = await _value(client.post<Json>(path, body: body));
    // Invalidate retained history/badges only after a successful mutation.
    notifyListeners();
    return result;
  }

  Future<Json> patch(String path) => _value(client.patch<Json>(path));

  Future<List<SportModel>> sports() async =>
      jsonList(await _value(client.get<dynamic>('/api/sports')))
          .map(SportModel.fromJson)
          .toList();
  Future<List<CenterInfo>> centers({
    String? sportId,
    String? district,
    String? city,
    String? search,
  }) async => jsonList(
    await _value(
      client.get<dynamic>(
        '/api/sport-centers',
        query: {
          'sportId': ?sportId,
          if (district != null && district.isNotEmpty) 'district': district,
          if (city != null && city.isNotEmpty) 'city': city,
          if (search != null && search.isNotEmpty) 'search': search,
        },
      ),
    ),
  ).map(CenterInfo.fromJson).toList();
  Future<CenterInfo> center(String id) async =>
      CenterInfo.fromJson(await get('/api/sport-centers/$id'));
  Future<Court> court(String id) async =>
      Court.fromJson(await get('/api/courts/$id'));
  Future<Availability> availability(String courtId, String date) async =>
      Availability.fromJson(
        await get('/api/courts/$courtId/availability', query: {'date': date}),
      );
  Future<Booking> hold(String courtId, List<Slot> slots) async =>
      Booking.fromJson(
        await post('/api/bookings/hold', {
          'courtId': courtId,
          'slotStartAts': slots.map((s) => s.start).toList(),
        }),
      );
  Future<Booking> booking(String id) async =>
      Booking.fromJson(await get('/api/bookings/$id'));
  Future<PageResult<Booking>> bookings(String group, int page) async =>
      PageResult.fromJson(
        await get(
          '/api/bookings',
          query: {'statusGroup': group, 'pageNumber': page, 'pageSize': 15},
        ),
        Booking.fromJson,
      );
  Future<Json> startDeposit(String id, String method) =>
      post('/api/bookings/$id/payments/deposit', {'paymentMethod': method});
  Future<Json> paymentStatus(String id) =>
      get('/api/bookings/$id/payment-status');
  Future<Json> reconcilePayment(String id) =>
      post('/api/bookings/$id/payments/reconcile', {});
  Future<Json> simulate(String paymentId, String result) =>
      post('/api/dev/payments/$paymentId/simulate', {'result': result});
  Future<Json> qr(String id) => get('/api/bookings/$id/qr');
  Future<Json> cancellationRequests(String id) => get(
    '/api/bookings/$id/cancellation-requests',
    query: {'pageNumber': 1, 'pageSize': 100},
  );
  Future<Json> cancel(String id, String reason) =>
      post('/api/bookings/$id/cancellation-requests', {'reason': reason});
  Future<PageResult<CustomerNotification>> notifications(int page) async =>
      PageResult.fromJson(
        await get(
          '/api/notifications',
          query: {'pageNumber': page, 'pageSize': 20},
        ),
        CustomerNotification.fromJson,
      );
  Future<int> unread() async =>
      (await get('/api/notifications/unread-count'))['unreadCount'] as int;
  Future<void> readNotification(String id) async {
    await patch('/api/notifications/$id/read');
  }

  Future<void> readAll() async {
    await patch('/api/notifications/read-all');
  }

  Future<Json> reviews(String centerId, int page) => get(
    '/api/sport-centers/$centerId/reviews',
    query: {'pageNumber': page, 'pageSize': 15},
  );
  Future<Json> review(String bookingId, int rating, String comment) =>
      post('/api/bookings/$bookingId/reviews', {
        'rating': rating,
        'comment': comment.trim().isEmpty ? null : comment.trim(),
      });
}
