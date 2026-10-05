import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/routing/route_names.dart';
import '../../../../core/storage/local_storage_service.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';

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
    final storage = context.read<LocalStorageService>();
    await Future<void>.delayed(const Duration(milliseconds: 800));
    if (!mounted) return;
    // First launch: ask for search area. Otherwise go to guest browsing.
    context.go(storage.selectedArea == null
        ? RouteNames.location
        : RouteNames.customerHome);
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
              Text('CourtGo',
                  style: AppTextStyles.h1.copyWith(color: AppColors.white)),
              const SizedBox(height: 4),
              Text('Sport Court Booking',
                  style: AppTextStyles.body.copyWith(color: AppColors.mint)),
            ],
          ),
        ),
      ),
    );
  }
}
