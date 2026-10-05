import '../../shared/models/user_model.dart';
import 'route_names.dart';

/// Pure role-based redirect logic (easy to unit test).
class RouteGuard {
  const RouteGuard._();

  /// Home route for a given role. `null` role means guest.
  static String homeFor(UserRole? role) {
    switch (role) {
      case UserRole.staff:
        return RouteNames.staffHome;
      case UserRole.admin:
        return RouteNames.adminHome;
      case UserRole.customer:
      case null:
        return RouteNames.customerHome;
    }
  }

  /// Returns a redirect path, or `null` if [location] is allowed.
  static String? redirect({
    required String location,
    required UserRole? role,
  }) {
    if (location.startsWith('/staff') && role != UserRole.staff) {
      return role == null ? RouteNames.login : homeFor(role);
    }
    if (location.startsWith('/admin') && role != UserRole.admin) {
      return role == null ? RouteNames.login : homeFor(role);
    }
    // Customer area: guests and customers only.
    if (location.startsWith('/customer') &&
        (role == UserRole.staff || role == UserRole.admin)) {
      return homeFor(role);
    }
    return null;
  }
}
