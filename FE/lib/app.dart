import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import 'core/constants/app_constants.dart';
import 'core/network/api_client.dart';
import 'core/routing/app_router.dart';
import 'core/storage/local_storage_service.dart';
import 'core/storage/secure_storage_service.dart';
import 'core/theme/app_theme.dart';
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
  late final AuthProvider _auth = AuthProvider();
  late final ApiClient _apiClient = ApiClient(storage: widget.secureStorage);
  late final GoRouter _router = AppRouter.create(_auth);

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
