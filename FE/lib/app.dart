import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import 'core/constants/app_constants.dart';
import 'core/network/api_client.dart';
import 'core/routing/app_router.dart';
import 'core/storage/local_storage_service.dart';
import 'core/storage/secure_storage_service.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/auth/data/services/auth_service.dart';
import 'features/auth/presentation/providers/auth_provider.dart';

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
  }

  @override
  void dispose() {
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
