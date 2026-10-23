import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/routing/route_names.dart';
import '../../../core/theme/app_colors.dart';
import '../../auth/presentation/providers/auth_provider.dart';
import '../shared/admin_widgets.dart';

class AdminAccountPage extends StatelessWidget {
  const AdminAccountPage({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.user;

    final displayName = user?.fullName.isNotEmpty == true
        ? user!.fullName
        : (user?.phoneNumber ?? 'Quản trị viên');
    final email = user?.email ?? 'admin@courtgo.vn';
    final phone = user?.phoneNumber ?? 'Chưa cập nhật';

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Tài khoản',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Profile Header Card
          AdminCard(
            child: Row(
              children: [
                CircleAvatar(
                  radius: 32,
                  backgroundColor: AppColors.primary.withOpacity(0.12),
                  child: Text(
                    displayName.isNotEmpty ? displayName[0].toUpperCase() : 'A',
                    style: const TextStyle(
                      fontSize: 26,
                      fontWeight: FontWeight.w900,
                      color: AppColors.primary,
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        displayName,
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        email,
                        style: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textSecondary,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withOpacity(0.1),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.shield_outlined,
                                size: 12, color: AppColors.primary),
                            SizedBox(width: 4),
                            Text(
                              'Quản trị viên (Admin)',
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                                color: AppColors.primary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),

          // User Information Card
          AdminCard(
            title: 'Thông tin tài khoản',
            child: Column(
              children: [
                _buildInfoRow('Họ và tên', displayName),
                const Divider(height: 1, color: AppColors.border),
                _buildInfoRow('Số điện thoại', phone),
                const Divider(height: 1, color: AppColors.border),
                _buildInfoRow('Email', email),
                const Divider(height: 1, color: AppColors.border),
                _buildInfoRow('Vai trò', 'Hệ thống Quản trị (Admin)'),
                if (user?.id != null) ...[
                  const Divider(height: 1, color: AppColors.border),
                  _buildInfoRow('Mã tài khoản', user!.id),
                ],
              ],
            ),
          ),
          const SizedBox(height: 16),

          // System Quick Links Card
          AdminCard(
            title: 'Lối tắt cấu hình',
            child: Column(
              children: [
                _buildActionRow(
                  context,
                  icon: Icons.tune,
                  title: 'Cấu hình hệ thống',
                  subtitle: 'Thời gian giữ sân, đặt cọc tối thiểu, check-in',
                  onTap: () => context.push(RouteNames.adminSettings),
                ),
                const Divider(height: 1, color: AppColors.border),
                _buildActionRow(
                  context,
                  icon: Icons.policy_outlined,
                  title: 'Chính sách hủy sân',
                  subtitle: 'Quản lý phiên bản chính sách hoàn tiền',
                  onTap: () => context.push(RouteNames.adminCancellationPolicies),
                ),
                const Divider(height: 1, color: AppColors.border),
                _buildActionRow(
                  context,
                  icon: Icons.bar_chart,
                  title: 'Trung tâm báo cáo',
                  subtitle: 'Doanh thu, đặt sân, công suất sử dụng',
                  onTap: () => context.push(RouteNames.adminReportsHub),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          // Logout Button
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.error,
                side: const BorderSide(color: AppColors.error),
                padding: const EdgeInsets.symmetric(vertical: 14),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
              ),
              icon: const Icon(Icons.logout, size: 20),
              label: const Text(
                'Đăng xuất',
                style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15),
              ),
              onPressed: () => _confirmLogout(context),
            ),
          ),
          const SizedBox(height: 20),

          // App Footer
          const Center(
            child: Column(
              children: [
                Text(
                  'CourtGo Admin Mobile',
                  style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'Phiên bản 1.0.0 (PRM393)',
                  style: TextStyle(
                    fontSize: 11,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],
      ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: AppColors.textPrimary,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildActionRow(
    BuildContext context, {
    required IconData icon,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 10),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: AppColors.primary.withOpacity(0.08),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, color: AppColors.primary, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 14,
                    ),
                  ),
                  Text(
                    subtitle,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
            const Icon(Icons.chevron_right, color: AppColors.textSecondary),
          ],
        ),
      ),
    );
  }

  void _confirmLogout(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text(
          'Xác nhận đăng xuất',
          style: TextStyle(fontWeight: FontWeight.w800),
        ),
        content: const Text('Bạn có chắc chắn muốn đăng xuất khỏi CourtGo Admin?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Hủy',
                style: TextStyle(color: AppColors.textSecondary)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
              foregroundColor: AppColors.white,
            ),
            onPressed: () async {
              Navigator.of(ctx).pop();
              await context.read<AuthProvider>().logout();
              if (context.mounted) {
                context.go(RouteNames.login);
              }
            },
            child: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
  }
}
