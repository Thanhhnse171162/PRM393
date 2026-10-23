import 'dart:io';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:courtgo_mobile/app.dart';
import 'package:courtgo_mobile/core/storage/local_storage_service.dart';
import 'package:courtgo_mobile/core/storage/secure_storage_service.dart';
import 'package:courtgo_mobile/features/staff/staff_widgets.dart';
import 'package:courtgo_mobile/shared/widgets/customer_widgets.dart';

// Run only against the separate CourtGoStaffPaymentQa database, using locally generated fixture parameters.
void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  final screen = GlobalKey();
  Future<void> waitFor(WidgetTester t, Finder finder) async {
    for (var i = 0; i < 120; i++) {
      await t.pump(const Duration(milliseconds: 200));
      if (finder.evaluate().isNotEmpty) return;
    }
    expect(finder, findsWidgets);
  }

  Future<void> tap(WidgetTester t, Finder finder) async {
    await waitFor(t, finder);
    await t.ensureVisible(finder.first);
    await t.pump(const Duration(milliseconds: 250));
    await waitFor(t, finder.hitTestable());
    await t.tap(finder.hitTestable().first);
    await t.pump(const Duration(milliseconds: 400));
  }

  Future<void> reveal(WidgetTester t, Finder finder) async {
    await t.pump(const Duration(milliseconds: 500));
    for (var i = 0; i < 20 && finder.evaluate().isEmpty; i++) {
      final scroll = find.byWidgetPredicate(
        (w) => w is Scrollable && w.axisDirection == AxisDirection.down,
      );
      if (scroll.evaluate().isNotEmpty)
        await t.drag(scroll.last, const Offset(0, -300));
      await t.pump(const Duration(milliseconds: 300));
    }
    await waitFor(t, finder);
    await t.ensureVisible(finder.first);
  }

  Future<void> shot(WidgetTester t, String name) async {
    await t.pump(const Duration(milliseconds: 500));
    final frame =
        await (screen.currentContext!.findRenderObject()!
                as RenderRepaintBoundary)
            .toImage(pixelRatio: 2);
    final bytes = await frame.toByteData(format: ui.ImageByteFormat.png);
    frame.dispose();
    binding.reportData ??= {};
    final images = binding.reportData!.putIfAbsent(
      'screenshots',
      () => <dynamic>[],
    ) as List;
    images.add({
      'screenshotName': name,
      'bytes': bytes!.buffer.asUint8List().toList(),
    });
  }

  Future<void> pickDate(WidgetTester t) async {
    await waitFor(t, find.byType(CalendarDatePicker));
    t
        .widget<CalendarDatePicker>(find.byType(CalendarDatePicker))
        .onDateChanged(
          DateTime.parse(const String.fromEnvironment('STAFF_NEXT_DATE')),
        );
    await t.pump();
    await tap(t, find.text('OK'));
  }

  Future<void> pickTime(WidgetTester t, String hour) async {
    await waitFor(t, find.byType(TimePickerDialog));
    await tap(t, find.byIcon(Icons.keyboard_outlined));
    final inputs = find.byType(TextFormField);
    await t.enterText(inputs.at(0), hour);
    await t.enterText(inputs.at(1), '00');
    FocusManager.instance.primaryFocus?.unfocus();
    await tap(t, find.text('OK'));
  }

  testWidgets(
    'Staff real API: QR decode, collect, check-in, block, walk-in and NoShow',
    (t) async {
      const email = String.fromEnvironment('STAFF_EMAIL'),
          password = String.fromEnvironment('STAFF_PASSWORD');
      expect(email, isNotEmpty);
      await SecureStorageService().clear();
      await t.pumpWidget(
        RepaintBoundary(
          key: screen,
          child: CourtGoApp(
            secureStorage: SecureStorageService(),
            localStorage: await LocalStorageService.create(),
          ),
        ),
      );
      await waitFor(t, find.byKey(const ValueKey('login-identity')));
      await t.enterText(find.byKey(const ValueKey('login-identity')), email);
      await t.enterText(find.byKey(const ValueKey('login-password')), password);
      FocusManager.instance.primaryFocus?.unfocus();
      await tap(t, find.widgetWithText(PrimaryButton, 'Đăng nhập'));
      await waitFor(t, find.text('Lượt đặt tiếp theo'));
      await shot(t, '01-staff-home');
      await tap(t, find.text('Lịch đặt').last);
      await waitFor(t, find.byType(StaffBookingCard));
      await shot(t, '02-staff-schedule');
      await tap(
        t,
        find.text(const String.fromEnvironment('STAFF_BOOKING_CODE')),
      );
      await waitFor(t, find.text('Chi tiết lịch đặt'));
      await shot(t, '03-staff-detail');
      await reveal(t, find.text('Quét QR để check-in'));
      await tap(t, find.text('Quét QR để check-in'));
      await tap(t, find.text('Mở camera quét QR'));
      await waitFor(t, find.byType(MobileScanner));
      // Native Android ML Kit decodes a generated image containing the real API QR token.
      // Optical camera acquisition still needs a physical-device check; no decoded token is fabricated.
      const token = String.fromEnvironment('STAFF_QR');
      expect(token, isNotEmpty);
      final painter = QrPainter(
        data: token,
        version: QrVersions.auto,
        gapless: true,
      );
      final recorder = ui.PictureRecorder();
      final canvas = Canvas(recorder)..drawColor(Colors.white, BlendMode.src);
      canvas.translate(40, 40);
      painter.paint(canvas, const Size(720, 720));
      final picture = recorder.endRecording();
      final qrImage = await picture.toImage(800, 800);
      final bytes = await qrImage.toByteData(format: ui.ImageByteFormat.png);
      qrImage.dispose(); picture.dispose();
      final file = File('${Directory.systemTemp.path}/courtgo-staff-qa.png');
      await file.writeAsBytes(bytes!.buffer.asUint8List());
      final scanner = t.widget<MobileScanner>(find.byType(MobileScanner));
      final capture = await scanner.controller!.analyzeImage(file.path);
      expect(capture, isNotNull);
      expect(capture!.barcodes.any((b) => b.rawValue == token), isTrue);
      scanner.onDetect!(capture);
      await file.delete();
      await waitFor(t, find.text('QR hợp lệ'));
      await shot(t, '04-staff-verified');
      expect(find.text('Xác nhận check-in'), findsNothing);
      await reveal(t, find.text('Thu tiền còn lại'));
      await tap(t, find.text('Thu tiền còn lại'));
      await waitFor(t, find.textContaining('Đã nhận 70.000'));
      await shot(t, '05-staff-remaining');
      await tap(t, find.textContaining('Đã nhận 70.000'));
      await waitFor(t, find.text('Xác nhận check-in'));
      await reveal(t, find.text('Xác nhận check-in'));
      await tap(t, find.text('Xác nhận check-in'));
      await tap(t, find.text('Xác nhận'));
      await waitFor(t, find.text('Chi tiết lịch đặt'));
      await waitFor(t, find.text('Đã check-in').first);
      await shot(t, '06-staff-checked-in');
      var router = GoRouter.of(t.element(find.byType(Scaffold).last));
      router.go('/staff/courts');
      await waitFor(t, find.text('Sân A1'));
      await tap(t, find.text('Sân A1'));
      await waitFor(t, find.text('Vận hành sân'));
      await shot(t, '07-staff-court');
      await reveal(t, find.text('Tạo khóa sân / bảo trì'));
      await tap(t, find.text('Tạo khóa sân / bảo trì'));
      await tap(t, find.text('Chọn thời gian bắt đầu'));
      await pickDate(t);
      await pickTime(t, '18');
      await tap(t, find.text('Chọn thời gian kết thúc'));
      await pickDate(t);
      await pickTime(t, '19');
      await t.enterText(find.byType(TextFormField).last, 'Bảo trì kiểm thử QA');
      FocusManager.instance.primaryFocus?.unfocus();
      await shot(t, '08-staff-block-form');
      await reveal(t, find.text('Xác nhận khóa sân'));
      await tap(t, find.text('Xác nhận khóa sân'));
      await waitFor(t, find.text('Vận hành sân'));
      await reveal(t, find.text('Bảo trì kiểm thử QA'));
      await shot(t, '09-staff-block-created');
      await tap(t, find.text('Gỡ khóa').first);
      await tap(t, find.text('Xác nhận'));
      await waitFor(t, find.text('Không có lịch khóa sân.'));
      router = GoRouter.of(t.element(find.byType(Scaffold).last));
      router.go('/staff/walk-in');
      await waitFor(t, find.text('Tên khách'));
      await t.enterText(find.byType(TextFormField).at(0), 'Khách tại quầy QA');
      await t.enterText(find.byType(TextFormField).at(1), '0901234567');
      FocusManager.instance.primaryFocus?.unfocus();
      await tap(t, find.byType(DropdownButtonFormField<String>).last);
      await tap(t, find.text('Sân A1').last);
      await tap(t, find.textContaining('Ngày:'));
      await pickDate(t);
      await waitFor(t, find.byType(FilterChip));
      final available = find.byWidgetPredicate(
        (w) => w is FilterChip && w.onSelected != null,
      );
      await tap(t, available.first);
      await reveal(t, find.text('Đã thu đủ tiền · Tạo lịch'));
      await shot(t, '10-staff-walk-in');
      await tap(t, find.text('Đã thu đủ tiền · Tạo lịch'));
      await tap(t, find.text('Xác nhận'));
      await waitFor(t, find.text('Chi tiết lịch đặt'));
      await waitFor(t, find.textContaining('Khách tại quầy QA'));
      await shot(t, '11-staff-walk-in-created');
      router = GoRouter.of(t.element(find.byType(Scaffold).last));
      router.go(
        '/staff/bookings/${const String.fromEnvironment('STAFF_NOSHOW_ID')}',
      );
      await waitFor(t, find.text('Chi tiết lịch đặt'));
      await reveal(t, find.text('Đánh dấu khách vắng mặt'));
      await tap(t, find.text('Đánh dấu khách vắng mặt'));
      await tap(t, find.text('Xác nhận'));
      await waitFor(t, find.text('Không đến'));
      await shot(t, '12-staff-no-show');
      expect(t.takeException(), isNull);
    },
  );
}
