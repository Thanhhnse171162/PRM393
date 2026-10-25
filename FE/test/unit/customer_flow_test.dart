import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/core/network/network_result.dart';
import 'package:courtgo_mobile/features/availability/availability_provider.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import '../test_support.dart';

Map<String, dynamic> availabilityJson() => {
  'court': {'id': 'court', 'name': 'A1', 'centerName': 'CourtGo'},
  'date': '2026-10-20',
  'isClosed': false,
  'slots': List.generate(
    4,
    (i) => {
      'startAt': '2026-10-20T${10 + i}:00:00+07:00',
      'endAt': '2026-10-20T${11 + i}:00:00+07:00',
      'price': 100000 + i * 10000,
      'status': i == 3 ? 'Booked' : 'Available',
    },
  ),
};
void main() {
  test(
    'Slot selection rejects gaps, disabled slots and interior removal',
    () async {
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter(
          (_) async => (200, availabilityJson()),
        );
      final state = AvailabilityProvider(
        CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
        'court',
        DateTime(2026, 10, 20),
      );
      await state.load();
      final s = state.data!.slots;
      expect(state.toggle(s[0]), isNull);
      expect(state.toggle(s[2]), isNotNull);
      state.toggle(s[1]);
      state.toggle(s[2]);
      expect(state.estimatedTotal, 330000);
      expect(state.toggle(s[1]), isNotNull);
      state.toggle(s[3]);
      expect(state.selected.length, 3);
      state.toggle(s[2]);
      expect(state.selected.length, 2);
      state.dispose();
    },
  );
  test(
    'Hold conflict reloads backend availability and clears stale selection',
    () async {
      var reads = 0, posts = 0;
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter((r) async {
          if (r.method == 'POST') {
            posts++;
            return (409, {'code': 'BOOKING_SLOT_CONFLICT'});
          }
          reads++;
          return (200, availabilityJson());
        });
      final state = AvailabilityProvider(
        CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
        'court',
        DateTime(2026, 10, 20),
      );
      await state.load();
      state.toggle(state.data!.slots.first);
      final results = await Future.wait([state.hold(), state.hold()]);
      expect(results.every((b) => b == null), isTrue);
      expect(posts, 1);
      expect(reads, 2);
      expect(state.selected, isEmpty);
      expect(state.error, contains('Vui lòng chọn lại'));
      state.dispose();
    },
  );
  test(
    'Concurrent 401 responses use one refresh and rotate both tokens',
    () async {
      final store = MemoryTokens()
        ..access = 'old-access'
        ..refresh = 'old-refresh';
      var refreshes = 0;
      final dio = Dio(BaseOptions(baseUrl: 'http://test'));
      dio.httpClientAdapter = TestAdapter((r) async {
        if (r.path == '/api/auth/refresh') {
          refreshes++;
          await Future<void>.delayed(const Duration(milliseconds: 20));
          expect(r.data, {'refreshToken': 'old-refresh'});
          return (
            200,
            {'accessToken': 'new-access', 'refreshToken': 'new-refresh'},
          );
        }
        return r.headers['Authorization'] == 'Bearer new-access'
            ? (200, {'ok': true})
            : (401, {'code': 'UNAUTHORIZED'});
      });
      final client = ApiClient(storage: store, dio: dio);
      final results = await Future.wait(
        List.generate(4, (_) => client.get<Map<String, dynamic>>('/protected')),
      );
      expect(results.every((r) => r is NetworkSuccess), isTrue);
      expect(refreshes, 1);
      expect(store.access, 'new-access');
      expect(store.refresh, 'new-refresh');
    },
  );
  test(
    'ProblemDetails never exposes stack traces and maps rate limits',
    () async {
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter(
          (_) async => (429, {'detail': 'SECRET stack trace'}),
        );
      final result = await ApiClient(
        storage: MemoryTokens(),
        dio: dio,
      ).get<dynamic>('/test');
      expect((result as NetworkFailure).error.message, contains('quá nhanh'));
      expect(result.error.message, isNot(contains('SECRET')));
    },
  );
  test(
    'revalidateSelection preserves selections when slots remain available on server',
    () async {
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter((_) async => (200, availabilityJson()));
      final state = AvailabilityProvider(
        CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
        'court',
        DateTime(2026, 10, 20),
      );
      await state.load();
      state.toggle(state.data!.slots[0]);
      state.toggle(state.data!.slots[1]);
      expect(state.selected.length, 2);

      final ok = await state.revalidateSelection();
      expect(ok, isTrue);
      expect(state.selected.length, 2);
      expect(state.error, isNull);
      state.dispose();
    },
  );
  test(
    'revalidateSelection clears selection, updates slots and sets error when slot becomes unavailable',
    () async {
      var callCount = 0;
      final dio = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = TestAdapter((_) async {
          callCount++;
          if (callCount == 1) {
            return (200, availabilityJson());
          }
          // On second call, slot 0 is booked!
          final modified = availabilityJson();
          (modified['slots'] as List)[0]['status'] = 'Booked';
          return (200, modified);
        });
      final state = AvailabilityProvider(
        CustomerApi(ApiClient(storage: MemoryTokens(), dio: dio)),
        'court',
        DateTime(2026, 10, 20),
      );
      await state.load();
      state.toggle(state.data!.slots[0]);
      expect(state.selected.length, 1);

      final ok = await state.revalidateSelection();
      expect(ok, isFalse);
      expect(state.selected, isEmpty);
      expect(state.error, contains('không còn khả dụng'));
      expect(state.data!.slots[0].available, isFalse);
      state.dispose();
    },
  );
}
