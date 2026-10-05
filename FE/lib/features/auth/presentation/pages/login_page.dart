import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/routing/route_guard.dart';
import '../../../../core/routing/route_names.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../core/utils/extensions.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../shared/models/user_model.dart';
import '../providers/auth_provider.dart';

/// Placeholder login screen. Real authentication is added in feature/api-auth.
/// The "dev" buttons only simulate roles so navigation can be developed.
class LoginPage extends StatelessWidget {
  const LoginPage({super.key});

  void _devSignIn(BuildContext context, UserRole role) {
    context.read<AuthProvider>().signInAs(role);
    context.go(RouteGuard.homeFor(role));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đăng nhập')),
      body: SafeArea(
        child: ListView(
          padding: AppSpacing.page,
          children: [
            const SizedBox(height: AppSpacing.lg),
            Text('Chào mừng đến CourtGo', style: AppTextStyles.h1),
            const SizedBox(height: AppSpacing.sm),
            Text('Login form will be implemented in feature/api-auth.',
                style: AppTextStyles.caption),
            const SizedBox(height: AppSpacing.lg),
            AppButton(
              label: 'Tiếp tục với tư cách khách',
              isOutlined: true,
              onPressed: () => context.go(RouteNames.customerHome),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppButton(
              label: 'Tạo tài khoản',
              isOutlined: true,
              onPressed: () => context.push(RouteNames.register),
            ),
            const SizedBox(height: AppSpacing.lg),
            Text('DEV ONLY – simulate role',
                style: context.textTheme.bodySmall),
            const SizedBox(height: AppSpacing.sm),
            for (final role in UserRole.values) ...[
              AppButton(
                label: 'Demo ${role.name.capitalized}',
                onPressed: () => _devSignIn(context, role),
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ],
        ),
      ),
    );
  }
}
