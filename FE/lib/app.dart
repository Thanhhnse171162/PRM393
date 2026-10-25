import 'dart:async';

import 'package:flutter/material.dart';
import 'package:app_links/app_links.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import 'core/constants/app_constants.dart';
import 'core/network/api_client.dart';
import 'core/api/customer_api.dart';
import 'features/home/discovery_provider.dart';
import 'features/notifications/notification_provider.dart';
import 'core/routing/app_router.dart';
import 'core/storage/local_storage_service.dart';
import 'core/storage/secure_storage_service.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/auth/data/services/auth_service.dart';
import 'features/auth/presentation/providers/auth_provider.dart';
import 'features/staff/staff_api.dart';
import 'features/admin/admin_api.dart';
import 'features/payments/payment_links.dart';
import 'shared/models/user_model.dart';

class CourtGoApp extends StatefulWidget {
  const CourtGoApp({
    super.key,
    required this.secureStorage,
    required this.localStorage,
  });

  final SecureStorageService secureStorage;
  final LocalStorageService localStorage;

  @override
  State<CourtGoApp> createState() => _CourtGoAppState();
}

class _CourtGoAppState extends State<CourtGoApp> {
  late final ApiClient _apiClient;
  late final AuthService _authService;
  late final AuthRepository _authRepository;
  late final AuthProvider _auth;
  late final GoRouter _router;
  StreamSubscription<Uri>? _links;
  String? _pendingPayment;

  @override
  void initState() {
    super.initState();
    _apiClient = ApiClient(storage: widget.secureStorage);
    _authService = AuthService(_apiClient);
    _authRepository = AuthRepository(
      service: _authService,
      storage: widget.secureStorage,
    );
    _auth = AuthProvider(_authRepository);
    _apiClient.onUnauthorized = _auth.handleUnauthorized;
    _router = AppRouter.create(_auth);
    _auth.addListener(_resumePayment);
    _router.routerDelegate.addListener(_resumePayment);
    _links = AppLinks().uriLinkStream.listen((uri) {
      _pendingPayment = paymentBookingFromLink(uri);
      _resumePayment();
    }, onError: (Object _) {});
  }

  void _resumePayment() {
    if (_pendingPayment == null ||
        !_auth.isAuthenticated ||
        _router.routeInformationProvider.value.uri.path == '/splash') {
      return;
    }
    if (_auth.role != UserRole.customer) {
      _pendingPayment = null;
      return;
    }
    final id = _pendingPayment!;
    _pendingPayment = null;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted && _auth.role == UserRole.customer) {
        _router.go('/customer/bookings/$id/payment');
      }
    });
    // A warm deep link can arrive while Flutter is idle, with no frame scheduled.
    WidgetsBinding.instance.ensureVisualUpdate();
  }

  @override
  void dispose() {
    _links?.cancel();
    _auth.removeListener(_resumePayment);
    _router.routerDelegate.removeListener(_resumePayment);
    _router.dispose();
    _auth.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthProvider>.value(value: _auth),
        Provider<ApiClient>.value(value: _apiClient),
        ChangeNotifierProvider<CustomerApi>(
          create: (_) => CustomerApi(_apiClient),
        ),
        ChangeNotifierProvider<AdminApi>(
          create: (c) => AdminApi(c.read<ApiClient>()),
        ),
        ChangeNotifierProxyProvider<AuthProvider, StaffApi>(
          create: (c) => StaffApi(c.read<CustomerApi>()),
          update: (_, auth, staff) => staff!..setUser(auth.user?.id),
        ),
        ChangeNotifierProvider<DiscoveryProvider>(
          create: (c) =>
              DiscoveryProvider(c.read<CustomerApi>(), widget.localStorage),
        ),
        ChangeNotifierProxyProvider<AuthProvider, NotificationProvider>(
          create: (c) => NotificationProvider(c.read<CustomerApi>()),
          update: (_, auth, notifications) =>
              notifications!..setUser(auth.user?.id),
        ),
        Provider<AuthService>.value(value: _authService),
        Provider<AuthRepository>.value(value: _authRepository),
        Provider<LocalStorageService>.value(value: widget.localStorage),
        Provider<SecureStorageService>.value(value: widget.secureStorage),
      ],
      child: MaterialApp.router(
        title: AppConstants.appName,
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light,
        routerConfig: _router,
      ),
    );
  }
}
