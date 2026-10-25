import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/admin/branches/admin_center_form_page.dart';
import 'package:courtgo_mobile/features/admin/courts/admin_court_form_page.dart';
import 'package:courtgo_mobile/shared/widgets/customer_widgets.dart';

import '../test_support.dart';

void main() {
  for (final center in [true, false]) {
    testWidgets(
      '${center ? 'center' : 'court'} loads saved preview and preserves form when URL fails',
      (tester) async {
        Map<String, dynamic>? submitted;
        final data = <String, dynamic>{
          'centerId': 'center',
          'courtId': 'court',
          'sportCenterId': 'center',
          'sportCenterName': 'CourtGo',
          'sportId': 'sport',
          'sportName': 'Tennis',
          'name': 'CourtGo QA',
          'code': 'C1',
          'coverImageUrl': 'https://images.example/saved.png',
          'status': 'Active',
          'basePricePerHour': 150000,
          'addressLine': '123 Street',
          'city': 'City',
          'district': 'District',
          'latitude': 10.77,
          'longitude': 106.7,
          'timeZoneId': 'Asia/Ho_Chi_Minh',
        };
        final dio = Dio()
          ..httpClientAdapter = TestAdapter((r) async {
            if (r.method == 'PUT') {
              submitted = Map<String, dynamic>.from(r.data as Map);
              return (200, {...data, ...submitted!});
            }
            if (r.path.endsWith('/sports')) {
              return (
                200,
                {
                  'items': [
                    {'id': 'sport', 'name': 'Tennis'},
                  ],
                },
              );
            }
            if (r.path.endsWith('/sport-centers')) {
              return (
                200,
                {
                  'items': [data],
                },
              );
            }
            return (200, data);
          });
        final api = AdminApi(ApiClient(storage: MemoryTokens(), dio: dio));
        final navigator = GlobalKey<NavigatorState>();
        await tester.pumpWidget(
          ChangeNotifierProvider.value(
            value: api,
            child: MaterialApp(
              navigatorKey: navigator,
              home: const Scaffold(body: Text('Saved')),
            ),
          ),
        );
        navigator.currentState!.push(
          MaterialPageRoute<void>(
            builder: (_) => center
                ? const AdminCenterFormPage(centerId: 'center')
                : const AdminCourtFormPage(courtId: 'court'),
          ),
        );
        for (var i = 0; i < 5; i++) {
          await tester.pump(const Duration(milliseconds: 200));
        }
        final preview = find.byType(SportImage);
        expect(tester.widget<SportImage>(preview).url, data['coverImageUrl']);
        final advanced = find.text('Nhập URL ảnh (tùy chọn)');
        await tester.ensureVisible(advanced);
        await tester.tap(advanced);
        await tester.pump(const Duration(milliseconds: 400));
        final originalField = find.byWidgetPredicate(
          (w) =>
              w is TextFormField && w.controller?.text == data['coverImageUrl'],
        );
        final imageController = tester
            .widget<TextFormField>(originalField)
            .controller;
        final field = find.byWidgetPredicate(
          (w) => w is TextFormField && w.controller == imageController,
        );
        await tester.ensureVisible(field);
        await tester.enterText(field, 'bad-url');
        await tester.pump(const Duration(milliseconds: 400));
        expect(find.textContaining('URL ảnh không hợp lệ'), findsOneWidget);
        expect(find.text('CourtGo QA'), findsWidgets);
        // Fix the URL after an error; only the image value changes in the form.
        await tester.enterText(field, ' https://images.example/updated.png ');
        await tester.pump(const Duration(milliseconds: 400));
        expect(
          tester.widget<SportImage>(preview).url,
          'https://images.example/updated.png',
        );
        final save = find.text('Lưu thay đổi');
        await tester.ensureVisible(save);
        await tester.tap(save);
        for (var i = 0; i < 5; i++) {
          await tester.pump(const Duration(milliseconds: 200));
        }
        expect(
          submitted?['coverImageUrl'],
          'https://images.example/updated.png',
        );
        expect(submitted?['name'], 'CourtGo QA');
        expect(find.text('Saved'), findsOneWidget);
        await tester.pumpWidget(const SizedBox());
        api.dispose();
      },
    );
  }
}
