import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/theme/app_colors.dart';
import '../shared/admin_widgets.dart';

class AdminReportsHubPage extends StatelessWidget {
  const AdminReportsHubPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Báo cáo & Thống kê',
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
              'Chọn loại báo cáo phân tích',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.textSecondary,
                letterSpacing: 0.5,
              ),
            ),
          ),
          AdminActionCard(
            title: 'Báo cáo Tổng quan',
            description:
                'Chỉ số tổng hợp kinh doanh, số lượt đặt hoàn thành, hủy, vắng mặt và doanh thu ròng',
            icon: Icons.dashboard_customize_outlined,
            onTap: () => context.push('/admin/reports/summary'),
          ),
          const SizedBox(height: 12),
          AdminActionCard(
            title: 'Báo cáo Doanh thu',
            description:
                'Biểu đồ doanh thu thực tế, tiền hoàn lại và doanh thu ròng theo ngày hoặc tháng',
            icon: Icons.monetization_on_outlined,
            onTap: () => context.push('/admin/reports/revenue'),
          ),
          const SizedBox(height: 12),
          AdminActionCard(
            title: 'Báo cáo Đặt sân',
            description:
                'Phân bổ đơn đặt theo trạng thái, môn thể thao, cơ sở và xu hướng lịch đặt theo ngày',
            icon: Icons.event_note_outlined,
            onTap: () => context.push('/admin/reports/bookings-report'),
          ),
          const SizedBox(height: 12),
          AdminActionCard(
            title: 'Báo cáo Công suất sử dụng',
            description:
                'Tỷ lệ lấp đầy sân, số phút khai thác so với tổng công suất theo từng sân và từng ngày',
            icon: Icons.pie_chart_outline,
            onTap: () => context.push('/admin/reports/occupancy'),
          ),
        ],
      ),
    );
  }
}
