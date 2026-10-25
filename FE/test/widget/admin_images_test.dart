import 'package:cached_network_image/cached_network_image.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/admin/branches/admin_centers_page.dart';
import 'package:courtgo_mobile/features/admin/branches/admin_center_detail_page.dart';
import 'package:courtgo_mobile/features/admin/courts/admin_courts_page.dart';
import 'package:courtgo_mobile/features/admin/courts/admin_court_detail_page.dart';
import 'package:courtgo_mobile/shared/widgets/customer_widgets.dart';
import 'package:courtgo_mobile/shared/widgets/image_url_preview.dart';

import '../test_support.dart';

void main() {
  test(
    'URL validation allows HTTP(S), rejects malformed and other schemes',
    () {
      for (final url in [
        '',
        'abc',
        'https://',
        'file:///photo.png',
        'https://bad host/image',
      ]) {
        expect(isSportImageUrl(url), isFalse);
      }
      expect(isSportImageUrl(' https://example.com/photo?v=2 '), isTrue);
    },
  );

  testWidgets(
    'preview debounces typing, observes loaded values and preserves form text',
    (tester) async {
      final controller = TextEditingController(text: 'saved-invalid-url');
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Column(
              children: [
                TextField(controller: controller),
                ImageUrlPreview(controller: controller),
              ],
            ),
          ),
        ),
      );
      expect(find.textContaining('URL ảnh không hợp lệ'), findsOneWidget);
      controller.text = 'first-edit';
      await tester.pump(const Duration(milliseconds: 200));
      controller.text = 'last-edit';
      await tester.pump(const Duration(milliseconds: 399));
      expect(
        tester.widget<SportImage>(find.byType(SportImage)).url,
        'saved-invalid-url',
      );
      await tester.pump(const Duration(milliseconds: 1));
      expect(
        tester.widget<SportImage>(find.byType(SportImage)).url,
        'last-edit',
      );
      expect(controller.text, 'last-edit');
      controller.clear();
      await tester.pump(const Duration(milliseconds: 400));
      expect(find.text('Chưa có ảnh cover'), findsOneWidget);
      controller.text = 'pending';
      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(seconds: 1));
      controller.dispose();
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'cached image is keyed by trimmed URL and has loading/error feedback',
    (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: SportImage(
              url: ' https://images.example/photo-v1.png ',
              showMessages: true,
            ),
          ),
        ),
      );
      final image = tester.widget<CachedNetworkImage>(
        find.byType(CachedNetworkImage),
      );
      expect(image.imageUrl, 'https://images.example/photo-v1.png');
      expect(image.fit, BoxFit.cover);
      expect(image.key, const ValueKey('https://images.example/photo-v1.png'));
      final context = tester.element(find.byType(SportImage));
      final loading = image.placeholder!(context, image.imageUrl);
      final error = image.errorWidget!(
        context,
        image.imageUrl,
        Exception('offline'),
      );
      await tester.pumpWidget(MaterialApp(home: Scaffold(body: loading)));
      expect(find.text('Đang tải ảnh...'), findsOneWidget);
      await tester.pumpWidget(MaterialApp(home: Scaffold(body: error)));
      expect(find.textContaining('Không thể tải ảnh'), findsOneWidget);
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: SportImage(url: 'https://images.example/photo-v2.png'),
          ),
        ),
      );
      expect(
        tester.widget<CachedNetworkImage>(find.byType(CachedNetworkImage)).key,
        const ValueKey('https://images.example/photo-v2.png'),
      );
      await tester.pumpWidget(const SizedBox());
    },
  );

  for (final entry in <String, Widget>{
    'center list': const AdminCentersPage(),
    'center detail': const AdminCenterDetailPage(centerId: 'center'),
    'court list': const AdminCourtsPage(),
    'court detail': const AdminCourtDetailPage(courtId: 'court'),
  }.entries) {
    testWidgets(
      '${entry.key} renders API cover and refreshes URL at phone width',
      (tester) async {
        tester.view.physicalSize = const Size(360, 800);
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.resetPhysicalSize);
        addTearDown(tester.view.resetDevicePixelRatio);
        var url = 'invalid-original'; // No external HTTP in screen fixtures.
        final dio = Dio()
          ..httpClientAdapter = TestAdapter((r) async {
            final data = <String, dynamic>{
              'centerId': 'center',
              'courtId': 'court',
              'sportCenterId': 'center',
              'sportCenterName': 'CourtGo',
              'sportId': 'sport',
              'sportName': 'Tennis',
              'name': 'CourtGo',
              'code': 'C1',
              'coverImageUrl': url,
              'status': 'Active',
              'basePricePerHour': 150000,
            };
            if (r.path.endsWith('/center') || r.path.endsWith('/court')) {
              return (200, data);
            }
            if (r.path.endsWith('/sports')) return (200, {'items': []});
            return (
              200,
              {
                'items': [data],
                'totalCount': 1,
              },
            );
          });
        final api = AdminApi(ApiClient(storage: MemoryTokens(), dio: dio));
        await tester.pumpWidget(
          ChangeNotifierProvider.value(
            value: api,
            child: MaterialApp(home: entry.value),
          ),
        );
        await tester.pumpAndSettle();
        expect(
          tester.widget<SportImage>(find.byType(SportImage).first).url,
          url,
        );

        url = 'invalid-updated';
        if (entry.key.contains('detail')) {
          await tester.tap(find.byIcon(Icons.refresh));
        } else {
          await tester.drag(find.byType(ListView).first, const Offset(0, 400));
        }
        await tester.pumpAndSettle();
        expect(
          tester.widget<SportImage>(find.byType(SportImage).first).url,
          url,
        );

        await tester.pumpWidget(const SizedBox());
        api.dispose();
      },
    );
  }
}
