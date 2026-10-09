import 'package:flutter/foundation.dart';
import 'package:timezone/data/latest.dart' as tz_data;
import 'package:timezone/timezone.dart' as tz;

import '../../core/api/customer_api.dart';
import '../../core/network/api_exception.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';

class StaffApi extends ChangeNotifier {
  StaffApi(this.api) {
    tz_data.initializeTimeZones();
  }
  final CustomerApi api;
  CenterInfo? center;
  String? today, _userId;
  int revision = 0;
  Future<void>? _initializing;
  void setUser(String? id) {
    if (_userId == id) return;
    _userId = id;
    center = null;
    today = null;
    _initializing = null;
    revision++;
  }

  Future<void> ensureCenter() => _initializing ??= _loadCenter();
  Future<void> _loadCenter() async {
    try {
      final owner = _userId;
      final dashboard = await api.get('/api/staff/dashboard');
      final assigned = await api.center(dashboard['centerId']);
      if (_userId != owner) return;
      center = assigned;
      today = dashboard['date'];
    } catch (_) {
      _initializing = null;
      rethrow;
    }
  }

  tz.Location get zone => tz.getLocation(switch (center?.timeZoneId) {
    'SE Asia Standard Time' => 'Asia/Ho_Chi_Minh',
    'UTC' => 'UTC',
    final String name => name,
    _ => throw StateError('Chưa tải múi giờ trung tâm'),
  });
  DateTime get now => tz.TZDateTime.now(zone);
  String local(String iso) =>
      tz.TZDateTime.from(DateTime.parse(iso), zone).toIso8601String();
  String interval(Json item) =>
      '${CustomerFormat.date(local(item['startAt']))} · ${CustomerFormat.interval(local(item['startAt']), local(item['endAt']))}';
  String instant(DateTime date, int hour, int minute) => tz.TZDateTime(
    zone,
    date.year,
    date.month,
    date.day,
    hour,
    minute,
  ).toUtc().toIso8601String();
  Future<Json> get(String path, {Json? query}) =>
      api.get('/api/staff/$path', query: query);
  Future<Json> command(String path, [Json body = const {}]) async {
    final result = await api.post('/api/staff/$path', body);
    changed();
    return result;
  }

  Future<void> removeBlock(String court, String block) async {
    (await api.client.delete<dynamic>('/api/staff/courts/$court/blocks/$block'))
        .when(success: (_) {}, failure: (e) => throw e);
    changed();
  }

  void changed() {
    revision++;
    notifyListeners();
  }

  Future<Json> verify(String token) =>
      api.post('/api/staff/check-ins/verify', {'qrToken': token});
  Future<PageResult<Json>> schedule(
    int page, {
    String? date,
    String? status,
    String? sport,
    String? search,
  }) async => PageResult.fromJson(
    await get(
      'bookings',
      query: {
        'pageNumber': page,
        'pageSize': 20,
        'date': ?date,
        'status': ?status,
        'sportId': ?sport,
        if (search?.trim().isNotEmpty ?? false) 'search': search!.trim(),
      },
    ),
    (j) => j,
  );
  Future<List<Json>> allPages(String path, {Json query = const {}}) async {
    final result = <Json>[];
    for (var page = 1; ; page++) {
      final data = await get(
        path,
        query: {...query, 'pageNumber': page, 'pageSize': 100},
      );
      result.addAll(jsonList(data['items']));
      if (page >= (data['totalPages'] as int)) return result;
    }
  }
}

String staffError(Object error) => error is ApiException
    ? error.message
    : 'Chưa thể hoàn tất. Kiểm tra kết nối rồi thử lại.';
bool canCollect(Json booking, Json payment) =>
    [
      'Confirmed',
      'CheckedIn',
      'InProgress',
    ].contains(booking['bookingStatus']) &&
    amount(payment['remainingAmount']) > 0;
bool canMarkNoShow(Json booking, DateTime now) =>
    booking['bookingStatus'] == 'Confirmed' &&
    !DateTime.parse(booking['endAt']).isAfter(now);
bool canCheckIn(Json verified) =>
    verified['bookingStatus'] == 'Confirmed' &&
    verified['alreadyCheckedIn'] != true &&
    amount((verified['payment'] as Map)['remainingAmount']) == 0;
