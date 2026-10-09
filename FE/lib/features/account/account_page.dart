import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_colors.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../auth/presentation/providers/auth_provider.dart';

class AccountPage extends StatefulWidget {
  const AccountPage({super.key});
  @override
  State<AccountPage> createState() => _AccountPageState();
}

class _AccountPageState extends State<AccountPage> {
  bool busy = false;
  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.user;
    return Scaffold(
      appBar: CourtGoAppBar(title: 'Tài khoản'),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 16),
          const Center(child: BrandMark(size: 72)),
          const SizedBox(height: 20),
          Text(
            user?.fullName ?? 'Chào mừng đến CourtGo',
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 23, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          const Text(
            'Cùng bạn sẵn sàng ra sân',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textSecondary),
          ),
          const SizedBox(height: 28),
          if (user != null) ...[
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    InfoLine('Họ tên', user.fullName),
                    InfoLine(
                      'Số điện thoại',
                      user.phoneNumber ?? 'Chưa cung cấp',
                    ),
                    InfoLine(
                      'Email',
                      user.email.isEmpty ? 'Chưa cung cấp' : user.email,
                    ),
                    const InfoLine('Vai trò', 'Khách hàng'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            PrimaryButton(
              label: 'Đăng xuất',
              loading: busy,
              onPressed: () async {
                setState(() => busy = true);
                await auth.logout();
                if (context.mounted) context.go('/login');
              },
            ),
          ] else
            PrimaryButton(
              label: 'Đăng nhập',
              onPressed: () => context.go('/login'),
            ),
        ],
      ),
    );
  }
}
