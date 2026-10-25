import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/pages/login_page.dart';
import '../../features/auth/presentation/pages/register_page.dart';
import '../../features/auth/presentation/pages/splash_page.dart';
import '../../features/auth/presentation/providers/auth_provider.dart';
import '../../features/home/customer_home_page.dart';
import '../../features/centers/explore_page.dart';
import '../../features/centers/center_detail_page.dart';
import '../../features/courts/court_detail_page.dart';
import '../../features/availability/availability_page.dart';
import '../../features/booking/booking_history_page.dart';
import '../../features/booking/booking_detail_page.dart';
import '../../features/booking/booking_qr_page.dart';
import '../../features/booking/booking_success_page.dart';
import '../../features/booking/cancellation_page.dart';
import '../../features/payments/checkout_page.dart';
import '../../features/payments/payment_page.dart';
import '../../features/notifications/notifications_page.dart';
import '../../features/reviews/review_pages.dart';
import '../../features/account/account_page.dart';
import '../../shared/models/customer_models.dart';
import '../../features/location/presentation/pages/location_page.dart';
import '../../shared/widgets/role_shell.dart';
import 'route_guard.dart';
import 'route_names.dart';
import '../../features/staff/staff_home_schedule.dart';
import '../../features/staff/staff_booking_pages.dart';
import '../../features/staff/staff_scanner.dart';
import '../../features/staff/staff_court_pages.dart';
import '../../features/staff/staff_walk_in.dart';
import '../../features/admin/dashboard/admin_dashboard_page.dart';
import '../../features/admin/management/admin_management_hub_page.dart';
import '../../features/admin/staff_management/admin_staff_page.dart';
import '../../features/admin/staff_management/admin_staff_detail_page.dart';
import '../../features/admin/staff_management/admin_staff_create_page.dart';
import '../../features/admin/branches/admin_centers_page.dart';
import '../../features/admin/branches/admin_center_detail_page.dart';
import '../../features/admin/branches/admin_center_form_page.dart';
import '../../features/admin/branches/admin_location_picker_page.dart';
import '../../features/admin/shared/admin_models.dart';

import 'package:latlong2/latlong.dart';

import '../../features/admin/sports/admin_sports_page.dart';
import '../../features/admin/courts/admin_courts_page.dart';
import '../../features/admin/courts/admin_court_detail_page.dart';
import '../../features/admin/courts/admin_court_form_page.dart';
import '../../features/admin/operating_hours/admin_operating_hours_page.dart';
import '../../features/admin/operating_hours/admin_operating_exceptions_page.dart';
import '../../features/admin/pricing/admin_price_rules_page.dart';
import '../../features/admin/settings/admin_settings_page.dart';
import '../../features/admin/cancellation_policies/admin_cancellation_policies_page.dart';
import '../../features/admin/bookings/admin_bookings_page.dart';
import '../../features/admin/bookings/admin_booking_detail_page.dart';
import '../../features/admin/bookings/admin_cancellation_requests_page.dart';
import '../../features/admin/reports/admin_reports_hub_page.dart';
import '../../features/admin/reports/admin_summary_report_page.dart';
import '../../features/admin/reports/admin_revenue_report_page.dart';
import '../../features/admin/reports/admin_booking_report_page.dart';
import '../../features/admin/reports/admin_occupancy_report_page.dart';
import '../../features/admin/account/admin_account_page.dart';

class AppRouter {
  const AppRouter._();

  static const _customerTabs = [
    RoleTab(
      label: 'Trang chủ',
      icon: Icons.home_outlined,
      selectedIcon: Icons.home,
    ),
    RoleTab(
      label: 'Khám phá',
      icon: Icons.explore_outlined,
      selectedIcon: Icons.explore,
    ),
    RoleTab(
      label: 'Lịch đặt',
      icon: Icons.calendar_month_outlined,
      selectedIcon: Icons.calendar_month,
    ),
    RoleTab(
      label: 'Thông báo',
      icon: Icons.notifications_outlined,
      selectedIcon: Icons.notifications,
    ),
    RoleTab(
      label: 'Tài khoản',
      icon: Icons.person_outline,
      selectedIcon: Icons.person,
    ),
  ];

  static const _staffTabs = [
    RoleTab(
      label: 'Trang chủ',
      icon: Icons.home_outlined,
      selectedIcon: Icons.home,
    ),
    RoleTab(
      label: 'Lịch đặt',
      icon: Icons.event_note_outlined,
      selectedIcon: Icons.event_note,
    ),
    RoleTab(
      label: 'Quét QR',
      icon: Icons.qr_code_scanner_outlined,
      selectedIcon: Icons.qr_code_scanner,
    ),
    RoleTab(
      label: 'Quản lý sân',
      icon: Icons.sports_tennis_outlined,
      selectedIcon: Icons.sports_tennis,
    ),
    RoleTab(
      label: 'Tài khoản',
      icon: Icons.person_outline,
      selectedIcon: Icons.person,
    ),
  ];

  static const _adminTabs = [
    RoleTab(
      label: 'Tổng quan',
      icon: Icons.dashboard_outlined,
      selectedIcon: Icons.dashboard,
    ),
    RoleTab(
      label: 'Quản lý',
      icon: Icons.settings_outlined,
      selectedIcon: Icons.settings,
    ),
    RoleTab(
      label: 'Lịch đặt',
      icon: Icons.event_note_outlined,
      selectedIcon: Icons.event_note,
    ),
    RoleTab(
      label: 'Báo cáo',
      icon: Icons.bar_chart_outlined,
      selectedIcon: Icons.bar_chart,
    ),
    RoleTab(
      label: 'Tài khoản',
      icon: Icons.person_outline,
      selectedIcon: Icons.person,
    ),
  ];

  static GoRouter create(AuthProvider auth) {
    return GoRouter(
      initialLocation: RouteNames.splash,
      refreshListenable: auth,
      redirect: (context, state) {
        final path = state.matchedLocation;
        if ((path.startsWith('/customer/bookings') ||
                path == RouteNames.customerNotifications) &&
            !auth.isAuthenticated) {
          final target = state.uri.toString();
          return '${RouteNames.login}?redirect=${Uri.encodeComponent(target)}';
        }
        return RouteGuard.redirect(location: path, role: auth.role);
      },
      routes: [
        GoRoute(path: RouteNames.splash, builder: (_, _) => const SplashPage()),
        GoRoute(path: RouteNames.login, builder: (_, _) => const LoginPage()),
        GoRoute(
          path: RouteNames.register,
          builder: (_, _) => const RegisterPage(),
        ),
        GoRoute(
          path: RouteNames.location,
          builder: (_, _) => const LocationPage(),
        ),
        GoRoute(
          path: RouteNames.customerPayment,
          // Preserve old bookmarks, but use the current deposit/status API flow.
          redirect: (_, state) =>
              '/customer/bookings/${state.pathParameters['bookingId']!}/checkout',
        ),
        _shell(_customerTabs, [
          (RouteNames.customerHome, const CustomerHomePage()),
          (RouteNames.customerExplore, const ExplorePage()),
          (RouteNames.customerBookings, const BookingHistoryPage()),
          (RouteNames.customerNotifications, const NotificationsPage()),
          (RouteNames.customerAccount, const AccountPage()),
        ]),
        GoRoute(
          path: '/customer/centers/:id',
          builder: (_, s) => CenterDetailPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/centers/:id/reviews',
          builder: (_, s) =>
              CenterReviewsPage(centerId: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/courts/:id',
          builder: (_, s) => CourtDetailPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/courts/:id/availability',
          builder: (_, s) => AvailabilityPage(courtId: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/bookings/:id',
          builder: (_, s) => BookingDetailPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/bookings/:id/checkout',
          builder: (_, s) => CheckoutPage(
            id: s.pathParameters['id']!,
            hold: s.extra is Booking ? s.extra as Booking : null,
          ),
        ),
        GoRoute(
          path: '/customer/bookings/:id/payment',
          builder: (_, s) => PaymentPage(
            key: ValueKey('payment-${s.pathParameters['id']}'),
            id: s.pathParameters['id']!,
            attempt: s.extra is Json ? s.extra as Json : null,
          ),
        ),
        GoRoute(
          path: '/customer/bookings/:id/success',
          builder: (_, s) => BookingSuccessPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/bookings/:id/qr',
          builder: (_, s) => BookingQrPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/bookings/:id/cancellation',
          builder: (_, s) => CancellationPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/customer/bookings/:id/review',
          builder: (_, s) => ReviewPage(bookingId: s.pathParameters['id']!),
        ),
        _shell(_staffTabs, [
          (RouteNames.staffHome, const StaffHomePage()),
          (RouteNames.staffBookings, const StaffSchedulePage()),
          (RouteNames.staffCheckIn, const StaffScannerPage()),
          (RouteNames.staffCourts, const StaffCourtsPage()),
          (RouteNames.staffAccount, const StaffAccountPage()),
        ]),
        GoRoute(
          path: '/staff/check-in',
          redirect: (_, _) => RouteNames.staffCheckIn,
        ),
        GoRoute(
          path: '/staff/camera',
          builder: (_, _) => const StaffCameraPage(),
        ),
        GoRoute(
          path: '/staff/verify',
          builder: (_, s) => s.extra is String
              ? StaffVerifyPage(token: s.extra as String)
              : const StaffScannerPage(),
        ),
        GoRoute(
          path: '/staff/bookings/:id',
          builder: (_, s) =>
              StaffBookingDetailPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/staff/walk-in',
          builder: (_, _) => const StaffWalkInPage(),
        ),
        GoRoute(
          path: '/staff/courts/:id',
          builder: (_, s) => StaffCourtDetailPage(id: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/staff/courts/:id/block',
          builder: (_, s) =>
              StaffCreateBlockPage(courtId: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/staff/notifications',
          builder: (_, _) => const NotificationsPage(),
        ),
        _shell(_adminTabs, [
          (RouteNames.adminHome, const AdminDashboardPage()),
          (RouteNames.adminManagement, const AdminManagementHubPage()),
          (RouteNames.adminBookings, const AdminBookingsPage()),
          (RouteNames.adminReports, const AdminReportsHubPage()),
          (RouteNames.adminAccount, const AdminAccountPage()),
        ]),
        // Admin Staff Routes
        GoRoute(
          path: RouteNames.adminStaff,
          builder: (_, _) => const AdminStaffPage(),
        ),
        GoRoute(
          path: RouteNames.adminStaffCreate,
          builder: (_, _) => const AdminStaffCreatePage(),
        ),
        GoRoute(
          path: '/admin/staff/:id',
          builder: (_, s) =>
              AdminStaffDetailPage(staffId: s.pathParameters['id']!),
        ),
        // Admin Centers Routes
        GoRoute(
          path: RouteNames.adminCenters,
          builder: (_, _) => const AdminCentersPage(),
        ),
        GoRoute(
          path: RouteNames.adminCenterCreate,
          builder: (_, _) => const AdminCenterFormPage(),
        ),
        GoRoute(
          path: '/admin/centers/:id',
          builder: (_, s) =>
              AdminCenterDetailPage(centerId: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/admin/centers/:id/edit',
          builder: (_, s) => AdminCenterFormPage(
            centerId: s.pathParameters['id']!,
            initialData: s.extra is AdminSportCenterDetail
                ? s.extra as AdminSportCenterDetail
                : null,
          ),
        ),
        GoRoute(
          path: RouteNames.adminCenterLocationPicker,
          builder: (_, s) => AdminLocationPickerPage(
            initialLocation: s.extra is LatLng ? s.extra as LatLng : null,
          ),
        ),
        // Admin Sports
        GoRoute(
          path: RouteNames.adminSports,
          builder: (_, _) => const AdminSportsPage(),
        ),
        // Admin Courts Routes
        GoRoute(
          path: RouteNames.adminCourts,
          builder: (_, _) => const AdminCourtsPage(),
        ),
        GoRoute(
          path: RouteNames.adminCourtCreate,
          builder: (_, _) => const AdminCourtFormPage(),
        ),
        GoRoute(
          path: '/admin/courts/:id',
          builder: (_, s) =>
              AdminCourtDetailPage(courtId: s.pathParameters['id']!),
        ),
        GoRoute(
          path: '/admin/courts/:id/edit',
          builder: (_, s) =>
              AdminCourtFormPage(courtId: s.pathParameters['id']!),
        ),
        // Admin Operating Hours & Exceptions
        GoRoute(
          path: RouteNames.adminOperatingHours,
          builder: (_, s) => AdminOperatingHoursPage(
            initialCenterId: s.extra is String
                ? s.extra as String
                : s.uri.queryParameters['centerId'],
          ),
        ),
        GoRoute(
          path: RouteNames.adminOperatingExceptions,
          builder: (_, s) => AdminOperatingExceptionsPage(
            initialCenterId: s.extra is String
                ? s.extra as String
                : s.uri.queryParameters['centerId'],
          ),
        ),
        // Admin Price Rules
        GoRoute(
          path: RouteNames.adminPriceRules,
          builder: (_, s) => AdminPriceRulesPage(
            initialCourtId: s.extra is String
                ? s.extra as String
                : s.uri.queryParameters['courtId'],
            courtName: s.uri.queryParameters['courtName'],
          ),
        ),
        // Admin Settings
        GoRoute(
          path: RouteNames.adminSettings,
          builder: (_, _) => const AdminSettingsPage(),
        ),
        // Admin Cancellation Policies
        GoRoute(
          path: RouteNames.adminCancellationPolicies,
          builder: (_, _) => const AdminCancellationPoliciesPage(),
        ),
        // Admin Bookings & Cancellations
        GoRoute(
          path: '/admin/bookings/:id',
          builder: (_, s) =>
              AdminBookingDetailPage(bookingId: s.pathParameters['id']!),
        ),
        GoRoute(
          path: RouteNames.adminCancellationRequests,
          builder: (_, _) => const AdminCancellationRequestsPage(),
        ),
        // Admin Reports
        GoRoute(
          path: RouteNames.adminReportsHub,
          builder: (_, _) => const AdminReportsHubPage(),
        ),
        GoRoute(
          path: RouteNames.adminSummaryReport,
          builder: (_, _) => const AdminSummaryReportPage(),
        ),
        GoRoute(
          path: RouteNames.adminRevenueReport,
          builder: (_, _) => const AdminRevenueReportPage(),
        ),
        GoRoute(
          path: RouteNames.adminBookingReport,
          builder: (_, _) => const AdminBookingReportPage(),
        ),
        GoRoute(
          path: RouteNames.adminOccupancyReport,
          builder: (_, _) => const AdminOccupancyReportPage(),
        ),
      ],
    );
  }

  /// Builds a bottom-nav shell with one branch per (path, page) record.
  static StatefulShellRoute _shell(
    List<RoleTab> tabs,
    List<(String, Widget)> pages,
  ) {
    return StatefulShellRoute.indexedStack(
      builder: (context, state, shell) =>
          RoleShell(navigationShell: shell, tabs: tabs),
      branches: [
        for (final (path, page) in pages)
          StatefulShellBranch(
            routes: [GoRoute(path: path, builder: (_, _) => page)],
          ),
      ],
    );
  }
}
