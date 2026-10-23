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

class AdminCourtDetailPage extends StatefulWidget {
  const AdminCourtDetailPage({super.key, required this.courtId});

  final String courtId;

  @override
  State<AdminCourtDetailPage> createState() => _AdminCourtDetailPageState();
}

class _AdminCourtDetailPageState extends State<AdminCourtDetailPage> {
  Future<AdminCourtDetail>? _detailFuture;
  bool _mutating = false;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  void _loadDetail() {
    setState(() {
      _detailFuture = context.read<AdminApi>().getCourt(widget.courtId);
    });
  }

  void _showChangeStatusSheet(AdminCourtDetail detail) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text(
                  'Cập nhật trạng thái sân',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                ),
              ),
              ListTile(
                leading: const Icon(Icons.check_circle, color: Color(0xFF16A34A)),
                title: const Text('Hoạt động (Active)'),
                subtitle: const Text('Sân sẵn sàng nhận lịch đặt của khách'),
                trailing: detail.status == 'Active'
                    ? const Icon(Icons.check, color: AppColors.primary)
                    : null,
                onTap: () {
                  Navigator.pop(ctx);
                  _updateStatus('Active');
                },
              ),
              ListTile(
                leading: const Icon(Icons.build, color: AppColors.warning),
                title: const Text('Bảo trì (Maintenance)'),
                subtitle: const Text('Tạm khóa sân để bảo dưỡng, sửa chữa'),
                trailing: detail.status == 'Maintenance'
                    ? const Icon(Icons.check, color: AppColors.primary)
                    : null,
                onTap: () {
                  Navigator.pop(ctx);
                  _updateStatus('Maintenance');
                },
              ),
              ListTile(
                leading: const Icon(Icons.cancel, color: AppColors.error),
                title: const Text('Tạm ngừng (Inactive)'),
                subtitle: const Text('Không cho phép đặt sân trong thời gian dài'),
                trailing: detail.status == 'Inactive'
                    ? const Icon(Icons.check, color: AppColors.primary)
                    : null,
                onTap: () {
                  Navigator.pop(ctx);
                  _updateStatus('Inactive');
                },
              ),
              const SizedBox(height: 12),
            ],
          ),
        );
      },
    );
  }

  Future<void> _updateStatus(String nextStatus) async {
    setState(() => _mutating = true);
    try {
      await context.read<AdminApi>().updateCourtStatus(widget.courtId, nextStatus);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
                'Đã cập nhật trạng thái sân thành ${AdminFormat.courtStatusName(nextStatus)}'),
          ),
        );
        _loadDetail();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'Cập nhật thất bại'),
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
          'Chi tiết sân thể thao',
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
      body: FutureBuilder<AdminCourtDetail>(
        future: _detailFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const LoadingWidget();
          }
          if (snapshot.hasError) {
            return ErrorState(
              message: 'Không thể tải thông tin sân',
              onRetry: _loadDetail,
            );
          }
          final detail = snapshot.data;
          if (detail == null) {
            return const Center(child: Text('Không tìm thấy sân'));
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
                            child: const Icon(Icons.sports_tennis,
                                color: AppColors.primary, size: 28),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceBetween,
                                  children: [
                                    Expanded(
                                      child: Text(
                                        detail.name,
                                        style: const TextStyle(
                                          fontSize: 18,
                                          fontWeight: FontWeight.w800,
                                          color: AppColors.textPrimary,
                                        ),
                                      ),
                                    ),
                                    AdminStatusBadge(
                                      label: AdminFormat.courtStatusName(
                                          detail.status),
                                      statusType: detail.status,
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  'Mã sân: ${detail.code} · Môn: ${detail.sportName}',
                                  style: const TextStyle(
                                    fontSize: 13,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      const Divider(height: 1),
                      const SizedBox(height: 14),
                      _buildInfoRow(Icons.storefront_outlined, 'Cơ sở',
                          detail.sportCenterName),
                      const SizedBox(height: 10),
                      _buildInfoRow(
                          Icons.layers_outlined,
                          'Mặt sân',
                          detail.surfaceType?.isNotEmpty == true
                              ? detail.surfaceType!
                              : 'Tiêu chuẩn'),
                      const SizedBox(height: 10),
                      _buildInfoRow(
                        Icons.payments_outlined,
                        'Giá cơ sở',
                        '${AdminFormat.currency(detail.basePricePerHour)} / giờ',
                      ),
                      if (detail.description?.isNotEmpty == true) ...[
                        const SizedBox(height: 10),
                        _buildInfoRow(Icons.notes_outlined, 'Mô tả',
                            detail.description!),
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
                                        '/admin/courts/${detail.courtId}/edit',
                                        extra: detail,
                                      );
                                      if (updated == true) _loadDetail();
                                    },
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: OutlinedButton.icon(
                              icon: const Icon(Icons.tune, size: 16),
                              label: const Text('Đổi trạng thái'),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: AppColors.textPrimary,
                                side: const BorderSide(color: AppColors.border),
                              ),
                              onPressed: _mutating
                                  ? null
                                  : () => _showChangeStatusSheet(detail),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // KPI: Active bookings on this court
                AdminKpiCard(
                  title: 'Đơn đặt đang hoạt động',
                  value: '${detail.activeBookingCount} lượt',
                  icon: Icons.calendar_month_outlined,
                  color: AppColors.primary,
                  subtitle: 'Các lịch đặt chưa hoàn thành hoặc chưa hủy',
                ),
                const SizedBox(height: 20),

                // Link to Price Rules
                const AdminSectionHeader(title: 'Bảng giá sân'),
                AdminActionCard(
                  title: 'Cấu hình quy tắc giá',
                  description:
                      'Thiết lập giá theo thứ, khung giờ vàng hoặc giai đoạn áp dụng cho sân này',
                  icon: Icons.price_change_outlined,
                  onTap: () => context.push(
                    '/admin/courts/${detail.courtId}/price-rules?courtName=${Uri.encodeComponent(detail.name)}',
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
          width: 80,
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
