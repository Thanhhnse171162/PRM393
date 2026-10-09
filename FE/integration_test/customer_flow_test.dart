import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';
import 'package:qr_flutter/qr_flutter.dart';

import 'package:courtgo_mobile/app.dart';
import 'package:courtgo_mobile/core/storage/local_storage_service.dart';
import 'package:courtgo_mobile/core/api/customer_api.dart';
import 'package:courtgo_mobile/core/storage/secure_storage_service.dart';
import 'package:courtgo_mobile/core/utils/customer_format.dart';
import 'package:courtgo_mobile/features/availability/availability_provider.dart';
import 'package:courtgo_mobile/shared/widgets/customer_widgets.dart';

// Uses the running Development API, real secure storage and real Android UI.
// Creates a distinct customer and booking on each run. Never run against production.
void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  final screenKey = GlobalKey();
  Future<void> launch(WidgetTester t) async {
    await t.pumpWidget(
      RepaintBoundary(
        key: screenKey,
        child: CourtGoApp(
          secureStorage: SecureStorageService(),
          localStorage: await LocalStorageService.create(),
        ),
      ),
    );
  }

  Future<void> waitFor(WidgetTester t, Finder finder) async {
    for (var i = 0; i < 100; i++) {
      await t.pump(const Duration(milliseconds: 200));
      if (finder.evaluate().isNotEmpty) return;
    }
    expect(finder, findsWidgets);
  }

  Future<void> tap(WidgetTester t, Finder finder) async {
    await waitFor(t, finder);
    await t.ensureVisible(finder.first);
    await t.pump(const Duration(milliseconds: 250));
    await t.tap(finder.first);
    await t.pump(const Duration(milliseconds: 350));
  }

  Future<void> reveal(WidgetTester t, Finder finder) async {
    await t.pump(const Duration(milliseconds: 350));
    for (var i = 0; i < 16 && finder.evaluate().isEmpty; i++) {
      final scroll = find.byWidgetPredicate(
        (w) => w is Scrollable && w.axisDirection == AxisDirection.down,
      );
      if (scroll.evaluate().isNotEmpty) {
        await t.drag(scroll.last, const Offset(0, -350));
      }
      await t.pump(const Duration(milliseconds: 300));
    }
    await waitFor(t, finder);
    await t.ensureVisible(finder.first);
  }

  Future<void> shot(WidgetTester t, String name) async {
    await t.pump(const Duration(milliseconds: 500));
    final boundary =
        screenKey.currentContext!.findRenderObject()! as RenderRepaintBoundary;
    final frame = await boundary.toImage(pixelRatio: 2);
    final bytes = await frame.toByteData(format: ui.ImageByteFormat.png);
    frame.dispose();
    binding.reportData ??= {};
    final screenshots = binding.reportData!.putIfAbsent(
      'screenshots',
      () => <dynamic>[],
    ) as List;
    screenshots.add({
      'screenshotName': name,
      'bytes': bytes!.buffer.asUint8List().toList(),
    });
  }

  testWidgets(
    'Customer real API: register through QR, cancellation and notifications',
    (t) async {
      await SecureStorageService().clear();
      await launch(t);
      await waitFor(t, find.byKey(const ValueKey('login-identity')));
      await shot(t, '01-login');
      await tap(t, find.text('Chưa có tài khoản? Đăng ký'));
      await waitFor(t, find.byType(TextFormField));
      final phone =
          '09${DateTime.now().millisecondsSinceEpoch.toString().substring(5)}';
      const password = 'CustomerQa2026!';
      final fields = find.byType(TextFormField);
      await t.enterText(fields.at(0), 'CourtGo UI Test');
      await t.enterText(fields.at(1), phone);
      await t.enterText(fields.at(2), 'ui.$phone@example.test');
      await t.enterText(fields.at(3), password);
      await t.enterText(fields.at(4), password);
      FocusManager.instance.primaryFocus?.unfocus();
      await tap(t, find.widgetWithText(PrimaryButton, 'Tạo tài khoản'));
      await waitFor(t, find.text('Chào mừng đến CourtGo!'));
      await tap(t, find.text('Đăng nhập'));
      await t.enterText(find.byKey(const ValueKey('login-identity')), phone);
      await t.enterText(find.byKey(const ValueKey('login-password')), password);
      FocusManager.instance.primaryFocus?.unfocus();
      await tap(t, find.widgetWithText(PrimaryButton, 'Đăng nhập'));
      await waitFor(t, find.text('Đặt sân nhanh'));
      await waitFor(t, find.byType(SportChip));
      await shot(t, '02-home');
      await reveal(t, find.byType(SportChip));
      // Select a sport exposed by the actual API, never a hardcoded sport ID.
      if (find.byType(SportChip).evaluate().length > 1) {
        await tap(t, find.byType(SportChip).at(1));
      }
      await reveal(t, find.byType(CenterCard));
      await tap(t, find.byType(CenterCard).first);
      await waitFor(t, find.text('Chi nhánh CourtGo'));
      await reveal(t, find.byType(CourtCard));
      await tap(t, find.byType(CourtCard).first);
      await tap(t, find.widgetWithText(PrimaryButton, 'Chọn ngày & giờ'));
      final date = DateTime.now().add(const Duration(days: 2));
      await tap(t, find.byKey(ValueKey('date-${CustomerFormat.day(date)}')));
      await waitFor(
        t,
        find.byWidgetPredicate(
          (w) =>
              w.key is ValueKey<String> &&
              (w.key! as ValueKey<String>).value.startsWith('slot-'),
        ),
      );
      final state = t
          .element(find.byType(CustomScrollView).last)
          .read<AvailabilityProvider>();
      for (var i = 0; i < 100 && state.loading; i++) {
        await t.pump(const Duration(milliseconds: 200));
      }
      final slots = state.data!.slots;
      final first = slots.indexWhere(
        (s) => s.available && slots.any((n) => n.available && n.start == s.end),
      );
      expect(first, greaterThanOrEqualTo(0));
      await reveal(t, find.byKey(ValueKey('slot-${slots[first].start}')));
      await tap(t, find.byKey(ValueKey('slot-${slots[first].start}')));
      await tap(t, find.byKey(ValueKey('slot-${slots[first + 1].start}')));
      expect(state.selected.length, 2);
      await shot(t, '03-availability');
      await tap(t, find.widgetWithText(PrimaryButton, 'Tiếp tục'));
      await waitFor(t, find.textContaining('Thanh toán cọc '));
      await shot(t, '04-checkout');
      await tap(t, find.textContaining('Thanh toán cọc '));
      await waitFor(t, find.text('Đang chờ xác nhận cọc'));
      await shot(t, '05-payment-pending');
      await tap(t, find.text('Giả lập cọc thành công'));
      await waitFor(t, find.text('Xem mã QR'));
      await shot(t, '06-confirmed');
      await tap(t, find.text('Xem lịch đặt'));
      await waitFor(t, find.byType(BookingCard));
      await tap(t, find.byType(BookingCard).first);
      await tap(t, find.text('Xem mã QR check-in'));
      await waitFor(t, find.byType(QrImageView));
      // QR is asserted on screen but deliberately excluded from screenshot artifacts.
      await t.pageBack();
      await t.pump(const Duration(milliseconds: 350));
      await reveal(t, find.text('Yêu cầu hủy / Xem tiến trình'));
      await tap(t, find.text('Yêu cầu hủy / Xem tiến trình'));
      await waitFor(t, find.byType(TextFormField));
      await t.enterText(
        find.byType(TextFormField),
        'Yêu cầu hủy từ kiểm thử giao diện CourtGo',
      );
      FocusManager.instance.primaryFocus?.unfocus();
      await tap(t, find.text('Gửi yêu cầu hủy'));
      await waitFor(t, find.text('Mức hoàn tính theo chính sách'));
      final api = t.element(find.byType(Scaffold).last).read<CustomerApi>();
      final bookings = await api.bookings('upcoming', 1);
      expect(bookings.items.single.status, 'Confirmed');
      await shot(t, '07-cancellation-pending');
      await t.pageBack();
      await t.pump(const Duration(milliseconds: 350));
      await t.pageBack();
      await t.pump(const Duration(milliseconds: 350));
      await tap(t, find.text('Thông báo').last);
      await waitFor(t, find.byTooltip('Đánh dấu tất cả đã đọc'));
      await tap(t, find.byTooltip('Đánh dấu tất cả đã đọc'));
      for (var i = 0; i < 30 && await api.unread() != 0; i++) {
        await t.pump(const Duration(milliseconds: 200));
      }
      expect(await api.unread(), 0);
      await shot(t, '08-notifications');
      expect(t.takeException(), isNull);
    },
    timeout: const Timeout(Duration(minutes: 8)),
  );

  testWidgets('Completed fixture: history, review and duplicate prevention', (
    t,
  ) async {
    const phone = String.fromEnvironment('E2E_PHONE');
    const password = String.fromEnvironment('E2E_PASSWORD');
    const bookingId = String.fromEnvironment('E2E_COMPLETED_BOOKING');
    expect(
      phone,
      isNotEmpty,
      reason: 'Prepare a dedicated local Completed fixture first.',
    );
    await SecureStorageService().clear();
    await launch(t);
    await waitFor(t, find.byKey(const ValueKey('login-identity')));
    await t.enterText(find.byKey(const ValueKey('login-identity')), phone);
    await t.enterText(find.byKey(const ValueKey('login-password')), password);
    FocusManager.instance.primaryFocus?.unfocus();
    await tap(t, find.widgetWithText(PrimaryButton, 'Đăng nhập'));
    await waitFor(t, find.text('Đặt sân nhanh'));
    await tap(t, find.text('Lịch đặt'));
    await tap(t, find.text('Hoàn thành'));
    final completedCard = find.byWidgetPredicate(
      (w) => w is BookingCard && w.booking.id == bookingId,
    );
    await reveal(t, completedCard);
    await tap(t, completedCard);
    await reveal(t, find.text('Đánh giá trải nghiệm'));
    await tap(t, find.text('Đánh giá trải nghiệm'));
    await waitFor(t, find.byType(TextField));
    await tap(t, find.byTooltip('4 sao'));
    await t.enterText(
      find.byType(TextField),
      'Kiểm thử giao diện Customer trên Android.',
    );
    FocusManager.instance.primaryFocus?.unfocus();
    await shot(t, '09-review');
    await tap(t, find.text('Gửi đánh giá'));
    await waitFor(t, find.text('Cảm ơn đánh giá của bạn'));
    await t.pageBack();
    await waitFor(t, find.text('Chi tiết lịch đặt'));
    await reveal(t, find.text('Bạn đã gửi đánh giá'));
    final api = t.element(find.byType(Scaffold).last).read<CustomerApi>();
    expect((await api.booking(bookingId)).completed, isTrue);
    await shot(t, '10-reviewed-detail');
    expect(t.takeException(), isNull);
  }, timeout: const Timeout(Duration(minutes: 4)));
}
