import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/pages/login_page.dart';
import '../../features/auth/presentation/pages/register_page.dart';
import '../../features/auth/presentation/pages/splash_page.dart';
import '../../features/auth/presentation/providers/auth_provider.dart';
import '../../features/customer/home/presentation/pages/customer_home_page.dart';
import '../../features/location/presentation/pages/location_page.dart';
import '../../shared/widgets/placeholder_page.dart';
import '../../shared/widgets/role_shell.dart';
import 'route_guard.dart';
import 'route_names.dart';

class AppRouter {
  const AppRouter._();

  static const _customerTabs = [
    RoleTab(label: 'Trang chủ', icon: Icons.home_outlined, selectedIcon: Icons.home),
    RoleTab(label: 'Khám phá', icon: Icons.explore_outlined, selectedIcon: Icons.explore),
    RoleTab(label: 'Lịch đặt', icon: Icons.calendar_month_outlined, selectedIcon: Icons.calendar_month),
    RoleTab(label: 'Thông báo', icon: Icons.notifications_outlined, selectedIcon: Icons.notifications),
    RoleTab(label: 'Tài khoản', icon: Icons.person_outline, selectedIcon: Icons.person),
  ];

  static const _staffTabs = [
    RoleTab(label: 'Trang chủ', icon: Icons.home_outlined, selectedIcon: Icons.home),
    RoleTab(label: 'Lịch đặt', icon: Icons.event_note_outlined, selectedIcon: Icons.event_note),
    RoleTab(label: 'Quét QR', icon: Icons.qr_code_scanner_outlined, selectedIcon: Icons.qr_code_scanner),
    RoleTab(label: 'Quản lý sân', icon: Icons.sports_tennis_outlined, selectedIcon: Icons.sports_tennis),
    RoleTab(label: 'Tài khoản', icon: Icons.person_outline, selectedIcon: Icons.person),
  ];

  static const _adminTabs = [
    RoleTab(label: 'Tổng quan', icon: Icons.dashboard_outlined, selectedIcon: Icons.dashboard),
    RoleTab(label: 'Quản lý', icon: Icons.settings_outlined, selectedIcon: Icons.settings),
    RoleTab(label: 'Lịch đặt', icon: Icons.event_note_outlined, selectedIcon: Icons.event_note),
    RoleTab(label: 'Báo cáo', icon: Icons.bar_chart_outlined, selectedIcon: Icons.bar_chart),
    RoleTab(label: 'Tài khoản', icon: Icons.person_outline, selectedIcon: Icons.person),
  ];

  static GoRouter create(AuthProvider auth) {
    return GoRouter(
      initialLocation: RouteNames.splash,
      refreshListenable: auth,
      redirect: (context, state) => RouteGuard.redirect(
        location: state.matchedLocation,
        role: auth.role,
      ),
      routes: [
        GoRoute(path: RouteNames.splash, builder: (_, _) => const SplashPage()),
        GoRoute(path: RouteNames.login, builder: (_, _) => const LoginPage()),
        GoRoute(path: RouteNames.register, builder: (_, _) => const RegisterPage()),
        GoRoute(path: RouteNames.location, builder: (_, _) => const LocationPage()),
        _shell(_customerTabs, [
          (RouteNames.customerHome, const CustomerHomePage()),
          (RouteNames.customerExplore, const PlaceholderPage(title: 'Khám phá', icon: Icons.explore_outlined)),
          (RouteNames.customerBookings, const PlaceholderPage(title: 'Lịch đặt', icon: Icons.calendar_month_outlined)),
          (RouteNames.customerNotifications, const PlaceholderPage(title: 'Thông báo', icon: Icons.notifications_outlined)),
          (RouteNames.customerAccount, const PlaceholderPage(title: 'Tài khoản', icon: Icons.person_outline, showSignOut: true)),
        ]),
        _shell(_staffTabs, [
          (RouteNames.staffHome, const PlaceholderPage(title: 'Trang chủ', icon: Icons.home_outlined)),
          (RouteNames.staffBookings, const PlaceholderPage(title: 'Lịch đặt', icon: Icons.event_note_outlined)),
          (RouteNames.staffCheckIn, const PlaceholderPage(title: 'Quét QR', icon: Icons.qr_code_scanner_outlined)),
          (RouteNames.staffCourts, const PlaceholderPage(title: 'Quản lý sân', icon: Icons.sports_tennis_outlined)),
          (RouteNames.staffAccount, const PlaceholderPage(title: 'Tài khoản', icon: Icons.person_outline, showSignOut: true)),
        ]),
        _shell(_adminTabs, [
          (RouteNames.adminHome, const PlaceholderPage(title: 'Tổng quan', icon: Icons.dashboard_outlined)),
          (RouteNames.adminManagement, const PlaceholderPage(title: 'Quản lý', icon: Icons.settings_outlined)),
          (RouteNames.adminBookings, const PlaceholderPage(title: 'Lịch đặt', icon: Icons.event_note_outlined)),
          (RouteNames.adminReports, const PlaceholderPage(title: 'Báo cáo', icon: Icons.bar_chart_outlined)),
          (RouteNames.adminAccount, const PlaceholderPage(title: 'Tài khoản', icon: Icons.person_outline, showSignOut: true)),
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
