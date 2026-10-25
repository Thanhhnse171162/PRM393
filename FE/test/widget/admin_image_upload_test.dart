import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import 'package:courtgo_mobile/core/config/app_config.dart';
import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/core/theme/app_theme.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/admin/shared/admin_image_field.dart';
import 'package:courtgo_mobile/shared/widgets/customer_widgets.dart';

import '../test_support.dart';

void main() {
  test('uploaded image paths resolve against the device API host', () {
    const path = '/api/images/11111111111111111111111111111111.png';
    expect(resolveSportImageUrl(path), '${AppConfig.apiBaseUrl}$path');
    expect(isSportImageUrl(path), isTrue);
    expect(isSportImageUrl('/api/images/../../secret'), isFalse);
  });
  for (final status in [200, 503]) {
    testWidgets(
      'image picker upload $status preserves or replaces saved image safely',
      (tester) async {
        final controller = TextEditingController(text: 'old-image');
        final busy = <bool>[];
        var posts = 0;
        final dio = Dio()
          ..httpClientAdapter = TestAdapter((r) async {
            posts++;
            return (
              status,
              status == 200
                  ? {
                      'imageUrl':
                          '/api/images/11111111111111111111111111111111.png',
                    }
                  : {'code': 'INTERNAL_ERROR'},
            );
          });
        final api = AdminApi(ApiClient(storage: MemoryTokens(), dio: dio));
        await tester.pumpWidget(
          ChangeNotifierProvider.value(
            value: api,
            child: MaterialApp(
              theme: AppTheme.light,
              home: Scaffold(
                body: SingleChildScrollView(
                  child: AdminImageField(
                    controller: controller,
                    onBusyChanged: busy.add,
                    pickImage: () async => XFile.fromData(
                      Uint8List.fromList([1, 2, 3]),
                      name: 'photo.png',
                    ),
                  ),
                ),
              ),
            ),
          ),
        );
        await tester.tap(find.text('Chọn ảnh từ thiết bị'));
        for (var i = 0; i < 10; i++) {
          await tester.pump(const Duration(milliseconds: 100));
        }
        expect(posts, 1);
        expect(busy, [true, false]);
        expect(
          controller.text,
          status == 200
              ? '/api/images/11111111111111111111111111111111.png'
              : 'old-image',
        );
        expect(tester.takeException(), isNull);
        await tester.pumpWidget(const SizedBox());
        controller.dispose();
        api.dispose();
      },
    );
  }
  test('multipart upload is retryable after token refresh', () async {
    final storage = MemoryTokens()
      ..access = 'old'
      ..refresh = 'refresh';
    var attempts = 0;
    final dio = Dio(BaseOptions(baseUrl: 'http://fixture.invalid'))
      ..httpClientAdapter = TestAdapter((r) async {
        if (r.path == '/api/auth/refresh') {
          return (200, {'accessToken': 'new', 'refreshToken': 'new-refresh'});
        }
        attempts++;
        return attempts == 1
            ? (401, {})
            : (200, {'imageUrl': '/api/images/example.png'});
      });
    final api = AdminApi(ApiClient(storage: storage, dio: dio));
    final result = await api.post(
      '/api/admin/images',
      FormData.fromMap({
        'file': MultipartFile.fromBytes([1, 2, 3], filename: 'photo.png'),
      }),
    );
    expect(attempts, 2);
    expect(result['imageUrl'], '/api/images/example.png');
    api.dispose();
  });
}
