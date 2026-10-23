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

  // Admin Shell Tabs
  static const String adminHome = '/admin/home';
  static const String adminManagement = '/admin/management';
  static const String adminBookings = '/admin/bookings';
  static const String adminReports = '/admin/reports';
  static const String adminAccount = '/admin/account';

  // Admin Management Modules
  static const String adminStaff = '/admin/staff';
  static const String adminStaffCreate = '/admin/staff/create';
  static const String adminStaffDetail = '/admin/staff/:id';
  static const String adminCenters = '/admin/centers';
  static const String adminCenterCreate = '/admin/centers/create';
  static const String adminCenterDetail = '/admin/centers/:id';
  static const String adminCenterEdit = '/admin/centers/:id/edit';
  static const String adminCenterLocationPicker =
      '/admin/centers/location-picker';
  static const String adminSports = '/admin/sports';
  static const String adminCourts = '/admin/courts';
  static const String adminCourtCreate = '/admin/courts/create';
  static const String adminCourtDetail = '/admin/courts/:id';
  static const String adminCourtEdit = '/admin/courts/:id/edit';
  static const String adminOperatingHours = '/admin/operating-hours';
  static const String adminOperatingExceptions =
      '/admin/operating-hours/exceptions';
  static const String adminPriceRules = '/admin/price-rules';
  static const String adminSettings = '/admin/settings';
  static const String adminCancellationPolicies =
      '/admin/cancellation-policies';

  // Admin Bookings & Cancellations
  static const String adminBookingDetail = '/admin/bookings/:id';
  static const String adminCancellationRequests =
      '/admin/cancellation-requests';

  // Admin Reports
  static const String adminReportsHub = '/admin/reports/hub';
  static const String adminSummaryReport = '/admin/reports/summary';
  static const String adminRevenueReport = '/admin/reports/revenue';
  static const String adminBookingReport = '/admin/reports/bookings-report';
  static const String adminOccupancyReport = '/admin/reports/occupancy';
}
