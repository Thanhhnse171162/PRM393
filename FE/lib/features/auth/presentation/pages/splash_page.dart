import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/routing/route_guard.dart';
import '../../../../core/routing/route_names.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../providers/auth_provider.dart';

class SplashPage extends StatefulWidget {
  const SplashPage({super.key});

  @override
  State<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends State<SplashPage> {
  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    final auth = context.read<AuthProvider>();

    // Brief splash delay for branding
    await Future<void>.delayed(const Duration(milliseconds: 600));
    if (!mounted) return;

    await auth.restoreSession();
    if (!mounted) return;

    if (auth.isAuthenticated) {
      context.go(RouteGuard.homeFor(auth.role));
    } else {
      context.go(RouteNames.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.primary,
      body: SafeArea(
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.sports_tennis, size: 72, color: AppColors.white),
              const SizedBox(height: 12),
              Text(
                'CourtGo',
                style: AppTextStyles.h1.copyWith(color: AppColors.white),
              ),
              const SizedBox(height: 4),
              Text(
                'Sport Court Booking',
                style: AppTextStyles.body.copyWith(color: AppColors.mint),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
