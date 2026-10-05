import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/routing/route_names.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/empty_state.dart';
import '../../features/auth/presentation/providers/auth_provider.dart';

/// Compile-safe placeholder used by every tab until real screens exist.
class PlaceholderPage extends StatelessWidget {
  const PlaceholderPage({
    super.key,
    required this.title,
    this.icon = Icons.construction_outlined,
    this.showSignOut = false,
  });

  final String title;
  final IconData icon;
  final bool showSignOut;

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: EmptyState(
                icon: icon,
                title: title,
                message: 'Coming soon',
              ),
            ),
            if (showSignOut)
              Padding(
                padding: const EdgeInsets.all(16),
                child: AppButton(
                  label: auth.isAuthenticated ? 'Đăng xuất' : 'Đăng nhập',
                  onPressed: () async {
                    if (auth.isAuthenticated) {
                      await auth.logout();
                    }
                    if (context.mounted) {
                      context.go(RouteNames.login);
                    }
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}
