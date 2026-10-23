import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminCenterDetailPage extends StatefulWidget {
  const AdminCenterDetailPage({super.key, required this.centerId});

  final String centerId;

  @override
  State<AdminCenterDetailPage> createState() => _AdminCenterDetailPageState();
}

class _AdminCenterDetailPageState extends State<AdminCenterDetailPage> {
  Future<AdminSportCenterDetail>? _detailFuture;
  bool _mutating = false;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  void _loadDetail() {
    setState(() {
      _detailFuture = context.read<AdminApi>().getCenter(widget.centerId);
    });
  }

  Future<void> _toggleStatus(AdminSportCenterDetail detail) async {
    final nextStatus = detail.isActive ? 'Inactive' : 'Active';
    final confirmed = await adminConfirmDialog(
      context: context,
      title: detail.isActive ? 'Tạm ngừng cơ sở?' : 'Mở lại cơ sở?',
      message: detail.isActive
          ? 'Cơ sở "${detail.name}" sẽ tạm ngừng nhận các lượt đặt sân mới.'
          : 'Cơ sở "${detail.name}" sẽ hoạt động trở lại và có thể nhận đặt sân.',
      confirmLabel: detail.isActive ? 'Tạm ngừng' : 'Kích hoạt',
      confirmColor: detail.isActive ? AppColors.warning : AppColors.primary,
    );
    if (!confirmed || !mounted) return;

    setState(() => _mutating = true);
    try {
      await context.read<AdminApi>().updateCenterStatus(widget.centerId, nextStatus);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(detail.isActive
                ? 'Đã chuyển cơ sở sang trạng thái Tạm ngừng'
                : 'Đã kích hoạt cơ sở hoạt động trở lại'),
          ),
        );
        _loadDetail();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'Thao tác thất bại'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _mutating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Chi tiết cơ sở',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadDetail,
          ),
        ],
      ),
      body: FutureBuilder<AdminSportCenterDetail>(
        future: _detailFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const LoadingWidget();
          }
          if (snapshot.hasError) {
            return ErrorState(
              message: 'Không thể tải thông tin cơ sở',
              onRetry: _loadDetail,
            );
          }
          final detail = snapshot.data;
          if (detail == null) {
            return const Center(child: Text('Không tìm thấy cơ sở'));
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Primary Info Card
                AdminCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppColors.lightMint,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(
                              Icons.location_city,
                              color: AppColors.primary,
                              size: 28,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  detail.name,
                                  style: const TextStyle(
                                    fontSize: 18,
                                    fontWeight: FontWeight.w800,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                AdminStatusBadge(
                                  label: AdminFormat.centerStatusName(detail.status),
                                  statusType: detail.status,
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      const Divider(height: 1),
                      const SizedBox(height: 14),
                      _buildInfoRow(
                        Icons.place_outlined,
                        'Địa chỉ',
                        '${detail.addressLine}${detail.ward != null ? ', ${detail.ward}' : ''}, ${detail.district}, ${detail.city}',
                      ),
                      const SizedBox(height: 10),
                      _buildInfoRow(
                        Icons.phone_outlined,
                        'Điện thoại',
                        detail.phoneNumber?.isNotEmpty == true
                            ? detail.phoneNumber!
                            : 'Chưa cập nhật',
                      ),
                      const SizedBox(height: 10),
                      _buildInfoRow(
                        Icons.schedule_outlined,
                        'Múi giờ',
                        detail.timeZoneId,
                      ),
                      if (detail.latitude != null && detail.longitude != null) ...[
                        const SizedBox(height: 10),
                        _buildInfoRow(
                          Icons.map_outlined,
                          'Tọa độ',
                          '${detail.latitude!.toStringAsFixed(4)}, ${detail.longitude!.toStringAsFixed(4)}',
                        ),
                      ],
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              icon: const Icon(Icons.edit_outlined, size: 16),
                              label: const Text('Chỉnh sửa'),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: AppColors.primary,
                                side: const BorderSide(color: AppColors.primary),
                              ),
                              onPressed: _mutating
                                  ? null
                                  : () async {
                                      final updated = await context.push<bool>(
                                        '/admin/centers/${detail.centerId}/edit',
                                        extra: detail,
                                      );
                                      if (updated == true) _loadDetail();
                                    },
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: OutlinedButton.icon(
                              icon: Icon(
                                detail.isActive
                                    ? Icons.pause_circle_outline
                                    : Icons.play_circle_outline,
                                size: 16,
                              ),
                              label: Text(
                                detail.isActive ? 'Tạm ngừng' : 'Kích hoạt',
                              ),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: detail.isActive
                                    ? AppColors.warning
                                    : AppColors.primary,
                                side: BorderSide(
                                  color: detail.isActive
                                      ? AppColors.warning
                                      : AppColors.primary,
                                ),
                              ),
                              onPressed: _mutating ? null : () => _toggleStatus(detail),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // Stats Summary Grid
                Row(
                  children: [
                    Expanded(
                      child: AdminKpiCard(
                        title: 'Sân hoạt động',
                        value: '${detail.activeCourts}/${detail.totalCourts}',
                        icon: Icons.sports_tennis_outlined,
                        color: AppColors.primary,
                        subtitle: 'Tổng số sân tại cơ sở',
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: AdminKpiCard(
                        title: 'Nhân viên phụ trách',
                        value: '${detail.assignedActiveStaffCount}',
                        icon: Icons.badge_outlined,
                        color: const Color(0xFF2563EB),
                        subtitle: 'Đang làm việc tại cơ sở',
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 20),

                // Quick links for operational management
                const AdminSectionHeader(title: 'Vận hành tại cơ sở này'),
                AdminActionCard(
                  title: 'Giờ hoạt động & Ngày ngoại lệ',
                  description:
                      'Cấu hình mở/đóng cửa 7 ngày trong tuần và các ngày lễ ngoại lệ',
                  icon: Icons.schedule_outlined,
                  onTap: () => context.push(
                    '/admin/operating-hours?centerId=${detail.centerId}',
                  ),
                ),
                const SizedBox(height: 10),
                AdminActionCard(
                  title: 'Danh sách sân thể thao',
                  description:
                      'Xem và quản lý tất cả ${detail.totalCourts} sân thuộc cơ sở này',
                  icon: Icons.sports_tennis_outlined,
                  onTap: () => context.push(
                    '/admin/courts?centerId=${detail.centerId}',
                  ),
                ),
                const SizedBox(height: 24),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 16, color: AppColors.textSecondary),
        const SizedBox(width: 8),
        SizedBox(
          width: 90,
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
            ),
          ),
        ),
      ],
    );
  }
}
