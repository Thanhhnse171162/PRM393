import 'package:courtgo_mobile/core/routing/route_guard.dart';
import 'package:courtgo_mobile/core/routing/route_names.dart';
import 'package:courtgo_mobile/core/utils/currency_utils.dart';
import 'package:courtgo_mobile/core/utils/validators.dart';
import 'package:courtgo_mobile/shared/models/user_model.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Validators', () {
    test('email accepts valid and rejects invalid', () {
      expect(Validators.email('a@b.com'), isNull);
      expect(Validators.email('abc'), isNotNull);
      expect(Validators.email(''), isNotNull);
    });

    test('phone accepts Vietnamese formats', () {
      expect(Validators.phone('0901234567'), isNull);
      expect(Validators.phone('+84901234567'), isNull);
      expect(Validators.phone('12345'), isNotNull);
    });

    test('password requires minimum length', () {
      expect(Validators.password('1234567'), isNotNull);
      expect(Validators.password('12345678'), isNull);
    });
  });

  group('CurrencyUtils', () {
    test('formats VND with dot separators', () {
      expect(CurrencyUtils.formatVnd(240000), '240.000\u0111');
      expect(CurrencyUtils.formatVnd(72000), '72.000\u0111');
    });
  });

  group('RouteGuard', () {
    test('guest cannot open staff or admin areas', () {
      expect(RouteGuard.redirect(location: RouteNames.staffHome, role: null),
          RouteNames.login);
      expect(RouteGuard.redirect(location: RouteNames.adminHome, role: null),
          RouteNames.login);
    });

    test('staff is kept out of customer and admin areas', () {
      expect(
          RouteGuard.redirect(
              location: RouteNames.adminHome, role: UserRole.staff),
          RouteNames.staffHome);
      expect(
          RouteGuard.redirect(
              location: RouteNames.customerHome, role: UserRole.staff),
          RouteNames.staffHome);
    });

    test('guest may browse customer area', () {
      expect(RouteGuard.redirect(location: RouteNames.customerHome, role: null),
          isNull);
    });
  });
}
