/// Central place for route paths so they are never typed as raw strings.
class RouteNames {
  const RouteNames._();

  static const String splash = '/splash';
  static const String login = '/login';
  static const String register = '/register';
  static const String location = '/location';

  // Customer
  static const String customerHome = '/customer/home';
  static const String customerExplore = '/customer/explore';
  static const String customerBookings = '/customer/bookings';
  static const String customerNotifications = '/customer/notifications';
  static const String customerAccount = '/customer/account';

  // Staff
  static const String staffHome = '/staff/home';
  static const String staffBookings = '/staff/bookings';
  static const String staffCheckIn = '/staff/checkin';
  static const String staffCourts = '/staff/courts';
  static const String staffAccount = '/staff/account';

  // Admin
  static const String adminHome = '/admin/home';
  static const String adminManagement = '/admin/management';
  static const String adminBookings = '/admin/bookings';
  static const String adminReports = '/admin/reports';
  static const String adminAccount = '/admin/account';
}
