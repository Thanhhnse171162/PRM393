import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:courtgo_mobile/core/network/api_client.dart';
import 'package:courtgo_mobile/features/admin/admin_api.dart';
import 'package:courtgo_mobile/features/admin/staff_management/admin_staff_create_page.dart';
import '../test_support.dart';

void main() {
  late AdminApi adminApi;
  late Future<(int, Object?)> Function(RequestOptions) respond;

  setUp(() {
    respond = (r) async {
      if (r.path == '/api/admin/sport-centers' && r.method == 'GET') {
        return (
          200,
          {
            'items': [
              {
                'centerId': 'center-1',
                'name': 'Sân Cầu Lông Quận 1',
                'district': 'Quận 1',
                'status': 'Active',
              },
              {
                'centerId': 'center-2',
                'name': 'Sân Bóng Đá Quận 7',
                'district': 'Quận 7',
                'status': 'Active',
              },
            ],
            'pageNumber': 1,
            'pageSize': 100,
            'totalCount': 2,
            'totalPages': 1,
          }
        );
      }
      return (200, <String, dynamic>{});
    };

    final dio = Dio()..httpClientAdapter = TestAdapter((r) => respond(r));
    adminApi = AdminApi(ApiClient(storage: MemoryTokens(), dio: dio));
  });

  Future<void> pumpSubject(
    WidgetTester tester, {
    String? initialCenterId,
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
    await tester.pumpWidget(
      ChangeNotifierProvider<AdminApi>.value(
        value: adminApi,
        child: MaterialApp(
          home: AdminStaffCreatePage(initialCenterId: initialCenterId),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('AdminStaffCreatePage Validation & UX Tests', () {
    testWidgets('Renders all fields, labels, and password guidelines',
        (tester) async {
      await pumpSubject(tester);

      expect(find.text('Thêm nhân viên mới'), findsOneWidget);
      expect(find.text('Họ và tên *'), findsOneWidget);
      expect(find.text('Số điện thoại đăng nhập *'), findsOneWidget);
      expect(find.text('Email (tùy chọn)'), findsOneWidget);
      expect(find.text('Mật khẩu khởi tạo *'), findsOneWidget);
      expect(find.text('Cơ sở trực thuộc *'), findsOneWidget);
      expect(find.text('Tạo nhân viên'), findsOneWidget);

      // Password requirement checklist box
      expect(find.text('Yêu cầu bảo mật mật khẩu:'), findsOneWidget);
      expect(find.text('Tối thiểu 8 ký tự'), findsOneWidget);
      expect(find.text('Chữ hoa (A-Z)'), findsOneWidget);
      expect(find.text('Chữ thường (a-z)'), findsOneWidget);
      expect(find.text('Chữ số (0-9)'), findsOneWidget);
    });

    testWidgets('Shows inline errors for missing required fields on submit',
        (tester) async {
      await pumpSubject(tester);

      // Submit with empty inputs
      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();

      expect(find.text('Vui lòng nhập họ và tên'), findsOneWidget);
      expect(find.text('Vui lòng nhập số điện thoại'), findsOneWidget);
      expect(find.text('Vui lòng nhập mật khẩu'), findsOneWidget);
      expect(
        find.text('Vui lòng kiểm tra lại các trường được đánh dấu đỏ.'),
        findsOneWidget,
      );
    });

    testWidgets('Validates invalid phone format and email format',
        (tester) async {
      await pumpSubject(tester);

      // Enter invalid phone
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Số điện thoại đăng nhập *'),
        '123',
      );
      // Enter invalid email
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Email (tùy chọn)'),
        'not-an-email',
      );

      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();

      expect(
        find.text('Số điện thoại không hợp lệ (9-11 chữ số, ví dụ 0901234567)'),
        findsOneWidget,
      );
      expect(
        find.text('Email không đúng định dạng (ví dụ: staff@courtgo.vn)'),
        findsOneWidget,
      );
    });

    testWidgets('Validates password rules (short, no upper, no lower, no digit)',
        (tester) async {
      await pumpSubject(tester);

      final passField =
          find.widgetWithText(TextFormField, 'Mật khẩu khởi tạo *');

      // Too short
      await tester.enterText(passField, 'Ab1');
      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();
      expect(find.text('Mật khẩu phải từ 8 đến 128 ký tự'), findsOneWidget);

      // No upper
      await tester.enterText(passField, 'abcdefg1');
      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();
      expect(find.text('Mật khẩu phải chứa ít nhất 1 chữ hoa (A-Z)'),
          findsOneWidget);

      // No lower
      await tester.enterText(passField, 'ABCDEFG1');
      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();
      expect(find.text('Mật khẩu phải chứa ít nhất 1 chữ thường (a-z)'),
          findsOneWidget);

      // No digit
      await tester.enterText(passField, 'Abcdefgh');
      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();
      expect(
          find.text('Mật khẩu phải chứa ít nhất 1 chữ số (0-9)'), findsOneWidget);
    });

    testWidgets('Maps Backend HTTP 409 PHONE_ALREADY_EXISTS to phone field error',
        (tester) async {
      respond = (r) async {
        if (r.path == '/api/admin/sport-centers' && r.method == 'GET') {
          return (
            200,
            {
              'items': [
                {
                  'centerId': 'center-1',
                  'name': 'Sân Cầu Lông Quận 1',
                  'district': 'Quận 1',
                  'status': 'Active',
                }
              ],
              'pageNumber': 1,
              'pageSize': 100,
              'totalCount': 1,
              'totalPages': 1,
            }
          );
        }
        if (r.path == '/api/admin/staff' && r.method == 'POST') {
          return (
            409,
            {
              'status': 409,
              'code': 'PHONE_ALREADY_EXISTS',
              'title': 'Conflict',
              'detail': 'Phone number is already registered.',
            }
          );
        }
        return (200, <String, dynamic>{});
      };

      await pumpSubject(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Họ và tên *'),
        'Nguyen Van Staff',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Số điện thoại đăng nhập *'),
        '0901111111',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Mật khẩu khởi tạo *'),
        'Password123!',
      );

      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();

      // Field error should be shown in red under phone field
      expect(find.text('Số điện thoại này đã được đăng ký.'), findsWidgets);

      // Form data must NOT be cleared on failure
      expect(find.text('Nguyen Van Staff'), findsOneWidget);
      expect(find.text('0901111111'), findsOneWidget);
    });

    testWidgets('Maps Backend HTTP 409 EMAIL_ALREADY_EXISTS to email field error',
        (tester) async {
      respond = (r) async {
        if (r.path == '/api/admin/sport-centers' && r.method == 'GET') {
          return (
            200,
            {
              'items': [
                {
                  'centerId': 'center-1',
                  'name': 'Sân Cầu Lông Quận 1',
                  'district': 'Quận 1',
                  'status': 'Active',
                }
              ],
              'pageNumber': 1,
              'pageSize': 100,
              'totalCount': 1,
              'totalPages': 1,
            }
          );
        }
        if (r.path == '/api/admin/staff' && r.method == 'POST') {
          return (
            409,
            {
              'status': 409,
              'code': 'EMAIL_ALREADY_EXISTS',
              'title': 'Conflict',
              'detail': 'Email is already registered.',
            }
          );
        }
        return (200, <String, dynamic>{});
      };

      await pumpSubject(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Họ và tên *'),
        'Nguyen Van Staff',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Số điện thoại đăng nhập *'),
        '0908888888',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Email (tùy chọn)'),
        'dup@courtgo.vn',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Mật khẩu khởi tạo *'),
        'Password123!',
      );

      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();

      // Field error should be shown under email field
      expect(find.text('Email này đã được đăng ký.'), findsWidgets);

      // Data preserved
      expect(find.text('dup@courtgo.vn'), findsOneWidget);
    });

    testWidgets('Handles network timeout / server error safely without crashing',
        (tester) async {
      respond = (r) async {
        if (r.path == '/api/admin/sport-centers' && r.method == 'GET') {
          return (
            200,
            {
              'items': [
                {
                  'centerId': 'center-1',
                  'name': 'Sân Cầu Lông Quận 1',
                  'district': 'Quận 1',
                  'status': 'Active',
                }
              ],
              'pageNumber': 1,
              'pageSize': 100,
              'totalCount': 1,
              'totalPages': 1,
            }
          );
        }
        if (r.path == '/api/admin/staff' && r.method == 'POST') {
          return (
            500,
            {
              'status': 500,
              'code': 'INTERNAL_ERROR',
              'title': 'Server error',
              'detail': 'Npgsql.NpgsqlException: Connection refused',
            }
          );
        }
        return (200, <String, dynamic>{});
      };

      await pumpSubject(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Họ và tên *'),
        'Nguyen Van Staff',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Số điện thoại đăng nhập *'),
        '0908888888',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Mật khẩu khởi tạo *'),
        'Password123!',
      );

      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pumpAndSettle();

      // Must NOT leak NpgsqlException or internal error
      expect(find.textContaining('Npgsql'), findsNothing);
      expect(find.textContaining('Connection refused'), findsNothing);
      expect(find.text('Đã có lỗi xảy ra trên hệ thống. Vui lòng thử lại sau.'),
          findsOneWidget);

      // Form data is retained
      expect(find.text('Nguyen Van Staff'), findsOneWidget);
    });

    testWidgets('Creates staff successfully and pops screen', (tester) async {
      respond = (r) async {
        if (r.path == '/api/admin/sport-centers' && r.method == 'GET') {
          return (
            200,
            {
              'items': [
                {
                  'centerId': 'center-1',
                  'name': 'Sân Cầu Lông Quận 1',
                  'district': 'Quận 1',
                  'status': 'Active',
                }
              ],
              'pageNumber': 1,
              'pageSize': 100,
              'totalCount': 1,
              'totalPages': 1,
            }
          );
        }
        if (r.path == '/api/admin/staff' && r.method == 'POST') {
          return (
            200,
            {
              'staffId': 'staff-123',
              'fullName': 'Nguyen Van Staff',
              'phoneNumber': '0908888888',
              'email': 'staff@courtgo.vn',
              'isActive': true,
              'assignedCenter': {
                'centerId': 'center-1',
                'centerName': 'Sân Cầu Lông Quận 1',
              },
              'assignmentHistory': [],
              'createdAt': DateTime.now().toIso8601String(),
            }
          );
        }
        return (200, <String, dynamic>{});
      };

      await pumpSubject(tester, initialCenterId: 'center-1');

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Họ và tên *'),
        'Nguyen Van Staff',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Số điện thoại đăng nhập *'),
        '0908888888',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Email (tùy chọn)'),
        'staff@courtgo.vn',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Mật khẩu khởi tạo *'),
        'Password123!',
      );

      await tester.tap(find.text('Tạo nhân viên'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.text('Tạo nhân viên thành công!'), findsOneWidget);
    });
  });
}
