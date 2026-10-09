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
import '../../shared/widgets/placeholder_page.dart';
import '../../shared/widgets/role_shell.dart';
import 'route_guard.dart';
import 'route_names.dart';
import '../../features/staff/staff_home_schedule.dart';
import '../../features/staff/staff_booking_pages.dart';
import '../../features/staff/staff_scanner.dart';
import '../../features/staff/staff_court_pages.dart';
import '../../features/staff/staff_walk_in.dart';

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
          return RouteNames.login;
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
          (
            RouteNames.adminHome,
            const PlaceholderPage(
              title: 'Tổng quan',
              icon: Icons.dashboard_outlined,
            ),
          ),
          (
            RouteNames.adminManagement,
            const PlaceholderPage(
              title: 'Quản lý',
              icon: Icons.settings_outlined,
            ),
          ),
          (
            RouteNames.adminBookings,
            const PlaceholderPage(
              title: 'Lịch đặt',
              icon: Icons.event_note_outlined,
            ),
          ),
          (
            RouteNames.adminReports,
            const PlaceholderPage(
              title: 'Báo cáo',
              icon: Icons.bar_chart_outlined,
            ),
          ),
          (
            RouteNames.adminAccount,
            const PlaceholderPage(
              title: 'Tài khoản',
              icon: Icons.person_outline,
              showSignOut: true,
            ),
          ),
        ]),
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
