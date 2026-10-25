import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/theme/app_colors.dart';
import '../shared/admin_widgets.dart';

class AdminManagementHubPage extends StatelessWidget {
  const AdminManagementHubPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Trung tâm quản lý',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
      ),
      body: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        children: [
          const Padding(
            padding: EdgeInsets.only(bottom: 12),
            child: Text(
              'Cơ sở & Vận hành',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.textSecondary,
                letterSpacing: 0.5,
              ),
            ),
          ),
          AdminActionCard(
            title: 'Cơ sở thể thao',
            description:
                'Quản lý mạng lưới chi nhánh, địa chỉ, múi giờ và thông tin liên hệ',
            icon: Icons.location_city_outlined,
            onTap: () => context.push('/admin/centers'),
          ),
          const SizedBox(height: 10),
          AdminActionCard(
            title: 'Sân thể thao',
            description:
                'Quản lý danh sách sân, loại mặt sân, giá cơ sở và trạng thái bảo trì',
            icon: Icons.sports_tennis_outlined,
            onTap: () => context.push('/admin/courts'),
          ),
          const SizedBox(height: 10),
          AdminActionCard(
            title: 'Môn thể thao',
            description: 'Danh mục các môn thể thao, thứ tự hiển thị và trạng thái',
            icon: Icons.sports_basketball_outlined,
            onTap: () => context.push('/admin/sports'),
          ),
          const SizedBox(height: 10),
          AdminActionCard(
            title: 'Giờ hoạt động',
            description:
                'Cấu hình giờ mở/đóng cửa 7 ngày trong tuần và ngày lễ/ngoại lệ',
            icon: Icons.schedule_outlined,
            onTap: () => context.push('/admin/operating-hours'),
          ),
          const SizedBox(height: 24),
          const Padding(
            padding: EdgeInsets.only(bottom: 12),
            child: Text(
              'Nhân sự & Phân công',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.textSecondary,
                letterSpacing: 0.5,
              ),
            ),
          ),
          AdminActionCard(
            title: 'Nhân viên & Phân công cơ sở',
            description:
                'Tạo tài khoản nhân viên và phân công quản lý chi nhánh CourtGo',
            icon: Icons.badge_outlined,
            onTap: () => context.push('/admin/staff'),
          ),
          const SizedBox(height: 24),
          const Padding(
            padding: EdgeInsets.only(bottom: 12),
            child: Text(
              'Giá & Chính sách',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.textSecondary,
                letterSpacing: 0.5,
              ),
            ),
          ),
          AdminActionCard(
            title: 'Bảng giá linh hoạt',
            description:
                'Khung giá theo giờ vàng, ngày cuối tuần và giai đoạn áp dụng',
            icon: Icons.price_change_outlined,
            onTap: () => context.push('/admin/price-rules'),
          ),
          const SizedBox(height: 10),
          AdminActionCard(
            title: 'Chính sách hủy sân',
            description:
                'Phiên bản chính sách hoàn tiền bất biến và tỷ lệ hoàn theo giờ',
            icon: Icons.policy_outlined,
            onTap: () => context.push('/admin/cancellation-policies'),
          ),
          const SizedBox(height: 10),
          AdminActionCard(
            title: 'Cấu hình hệ thống',
            description:
                'Thời gian giữ chỗ, tỷ lệ đặt cọc mặc định và cho phép check-in còn nợ',
            icon: Icons.tune_outlined,
            onTap: () => context.push('/admin/settings'),
          ),
          const SizedBox(height: 32),
        ],
      ),
    );
  }
}
