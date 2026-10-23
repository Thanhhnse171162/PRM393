import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminDashboardPage extends StatefulWidget {
  const AdminDashboardPage({super.key});

  @override
  State<AdminDashboardPage> createState() => _AdminDashboardPageState();
}

class _AdminDashboardPageState extends State<AdminDashboardPage> {
  DateTime _selectedDate = DateTime.now();
  String? _selectedCenterId;
  List<AdminSportCenterItem> _centers = [];
  bool _loadingCenters = false;

  Future<AdminDashboardData>? _dashboardFuture;

  @override
  void initState() {
    super.initState();
    _loadCenters();
    _loadDashboard();
  }

  void _loadCenters() async {
    setState(() => _loadingCenters = true);
    try {
      final api = context.read<AdminApi>();
      final res = await api.getCenters(pageSize: 100);
      if (mounted) {
        setState(() {
          _centers = res.items;
          _loadingCenters = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _loadingCenters = false);
    }
  }

  void _loadDashboard() {
    final api = context.read<AdminApi>();
    final dateStr =
        '${_selectedDate.year}-${_selectedDate.month.toString().padLeft(2, '0')}-${_selectedDate.day.toString().padLeft(2, '0')}';
    setState(() {
      _dashboardFuture = api.getDashboard(
        date: dateStr,
        centerId: _selectedCenterId,
      );
    });
  }

  Future<void> _selectDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime(2024),
      lastDate: DateTime(2028),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.primary,
              onPrimary: AppColors.white,
              onSurface: AppColors.textPrimary,
            ),
          ),
          child: child!,
        );
      },
    );
    if (picked != null && picked != _selectedDate) {
      setState(() => _selectedDate = picked);
      _loadDashboard();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Tổng quan hệ thống',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Làm mới',
            onPressed: _loadDashboard,
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => _loadDashboard(),
        color: AppColors.primary,
        child: FutureBuilder<AdminDashboardData>(
          future: _dashboardFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const LoadingWidget();
            }
            if (snapshot.hasError) {
              return ErrorState(
                message: 'Không thể tải dữ liệu tổng quan. Vui lòng thử lại.',
                onRetry: _loadDashboard,
              );
            }
            final data = snapshot.data;
            if (data == null) {
              return const Center(child: Text('Chưa có dữ liệu'));
            }

            return SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Filter header: Date & Center
                  _buildFilterBar(),
                  const SizedBox(height: 16),

                  // Alert banner if there are pending cancellation requests
                  if (data.pendingCancellationRequests > 0) ...[
                    AdminCard(
                      color: const Color(0xFFFEF2F2),
                      borderColor: const Color(0xFFFCA5A5),
                      onTap: () => context.push('/admin/cancellation-requests'),
                      child: Row(
                        children: [
                          const Icon(Icons.warning_amber_rounded,
                              color: AppColors.error, size: 28),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Có ${data.pendingCancellationRequests} yêu cầu hủy sân chờ duyệt',
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 13,
                                    color: AppColors.error,
                                  ),
                                ),
                                const Text(
                                  'Nhấn để xem xét và xử lý hoàn tiền',
                                  style: TextStyle(
                                    fontSize: 11,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const Icon(Icons.arrow_forward_ios,
                              size: 14, color: AppColors.error),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // Primary KPI Grid
                  Row(
                    children: [
                      Expanded(
                        child: AdminKpiCard(
                          title: 'Doanh thu ngày',
                          value: AdminFormat.currency(data.todayRevenue),
                          icon: Icons.payments_outlined,
                          color: AppColors.primary,
                          subtitle: data.refundAmount > 0
                              ? 'Đã hoàn: ${AdminFormat.currency(data.refundAmount)}'
                              : 'Doanh thu ròng hôm nay',
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: AdminKpiCard(
                          title: 'Tỷ lệ lấp đầy sân',
                          value: AdminFormat.percent(data.occupancyRate),
                          icon: Icons.pie_chart_outline,
                          color: AppColors.warning,
                          subtitle: 'Bao gồm giờ đã giữ chỗ',
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),

                  Row(
                    children: [
                      Expanded(
                        child: AdminKpiCard(
                          title: 'Lượt đặt ngày',
                          value: '${data.totalBookings}',
                          icon: Icons.calendar_today_outlined,
                          color: const Color(0xFF2563EB),
                          subtitle: '${data.upcomingBookings} lượt sắp diễn ra',
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: AdminKpiCard(
                          title: 'Quy mô hoạt động',
                          value: '${data.activeCourts} sân',
                          icon: Icons.sports_tennis_outlined,
                          color: const Color(0xFF0D9488),
                          subtitle: '${data.activeCenters} cơ sở đang mở',
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),

                  // Booking Status Breakdown Card
                  AdminSectionHeader(
                    title: 'Phân bổ trạng thái đặt sân',
                    actionLabel: 'Xem tất cả',
                    onAction: () => context.go('/admin/bookings'),
                  ),
                  AdminCard(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _buildStatusRow(
                          'Đã xác nhận',
                          data.confirmedBookings,
                          data.totalBookings,
                          const Color(0xFF16A34A),
                        ),
                        const Divider(height: 16),
                        _buildStatusRow(
                          'Đang chơi & Đã check-in',
                          data.checkedInBookings + data.inProgressBookings,
                          data.totalBookings,
                          const Color(0xFF2563EB),
                        ),
                        const Divider(height: 16),
                        _buildStatusRow(
                          'Hoàn thành',
                          data.completedBookings,
                          data.totalBookings,
                          const Color(0xFF059669),
                        ),
                        const Divider(height: 16),
                        _buildStatusRow(
                          'Chờ thanh toán',
                          data.pendingBookings,
                          data.totalBookings,
                          const Color(0xFFD97706),
                        ),
                        const Divider(height: 16),
                        _buildStatusRow(
                          'Đã hủy',
                          data.cancelledBookings,
                          data.totalBookings,
                          const Color(0xFFDC2626),
                        ),
                        const Divider(height: 16),
                        _buildStatusRow(
                          'Vắng mặt (No-show)',
                          data.noShowBookings,
                          data.totalBookings,
                          const Color(0xFFEA580C),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Quick Navigation shortcuts
                  const AdminSectionHeader(title: 'Lối tắt quản lý'),
                  Row(
                    children: [
                      Expanded(
                        child: _buildShortcutButton(
                          icon: Icons.badge_outlined,
                          label: 'Nhân viên',
                          onTap: () => context.push('/admin/staff'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: _buildShortcutButton(
                          icon: Icons.location_city_outlined,
                          label: 'Cơ sở',
                          onTap: () => context.push('/admin/centers'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: _buildShortcutButton(
                          icon: Icons.sports_tennis_outlined,
                          label: 'Sân',
                          onTap: () => context.push('/admin/courts'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: _buildShortcutButton(
                          icon: Icons.price_change_outlined,
                          label: 'Bảng giá',
                          onTap: () => context.push('/admin/price-rules'),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _buildFilterBar() {
    final dateDisplay =
        '${_selectedDate.day.toString().padLeft(2, '0')}/${_selectedDate.month.toString().padLeft(2, '0')}/${_selectedDate.year}';
    final centerName = _selectedCenterId == null
        ? 'Tất cả cơ sở'
        : _centers
            .firstWhere(
              (c) => c.centerId == _selectedCenterId,
              orElse: () => _centers.first,
            )
            .name;

    return AdminCard(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      child: Row(
        children: [
          Expanded(
            child: InkWell(
              onTap: _selectDate,
              borderRadius: BorderRadius.circular(8),
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 4),
                child: Row(
                  children: [
                    const Icon(Icons.calendar_today_outlined,
                        size: 16, color: AppColors.primary),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        dateDisplay,
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
          Container(
            height: 24,
            width: 1,
            color: AppColors.border,
            margin: const EdgeInsets.symmetric(horizontal: 8),
          ),
          Expanded(
            child: InkWell(
              onTap: _showCenterFilterDialog,
              borderRadius: BorderRadius.circular(8),
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 4),
                child: Row(
                  children: [
                    const Icon(Icons.storefront_outlined,
                        size: 16, color: AppColors.primary),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        centerName,
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    const Icon(Icons.arrow_drop_down,
                        size: 18, color: AppColors.textSecondary),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _showCenterFilterDialog() {
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
                  'Chọn cơ sở thể thao',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                ),
              ),
              ListTile(
                title: const Text('Tất cả cơ sở',
                    style: TextStyle(fontWeight: FontWeight.w600)),
                trailing: _selectedCenterId == null
                    ? const Icon(Icons.check, color: AppColors.primary)
                    : null,
                onTap: () {
                  Navigator.pop(ctx);
                  setState(() => _selectedCenterId = null);
                  _loadDashboard();
                },
              ),
              const Divider(height: 1),
              Expanded(
                child: ListView.separated(
                  itemCount: _centers.length,
                  separatorBuilder: (context, index) => const Divider(height: 1),
                  itemBuilder: (_, index) {
                    final c = _centers[index];
                    final isSel = c.centerId == _selectedCenterId;
                    return ListTile(
                      title: Text(c.name,
                          style: TextStyle(
                            fontWeight: isSel ? FontWeight.w700 : FontWeight.w500,
                          )),
                      subtitle: Text('${c.district}, ${c.city}',
                          style: const TextStyle(fontSize: 12)),
                      trailing: isSel
                          ? const Icon(Icons.check, color: AppColors.primary)
                          : null,
                      onTap: () {
                        Navigator.pop(ctx);
                        setState(() => _selectedCenterId = c.centerId);
                        _loadDashboard();
                      },
                    );
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildStatusRow(
    String label,
    int count,
    int total,
    Color color,
  ) {
    final pct = total > 0 ? (count / total * 100).toStringAsFixed(1) : '0.0';
    return Row(
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w500),
          ),
        ),
        Text(
          '$count',
          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700),
        ),
        const SizedBox(width: 8),
        SizedBox(
          width: 44,
          child: Text(
            '($pct%)',
            textAlign: TextAlign.right,
            style: const TextStyle(
              fontSize: 11,
              color: AppColors.textSecondary,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildShortcutButton({
    required IconData icon,
    required String label,
    required VoidCallback onTap,
  }) {
    return AdminCard(
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
      onTap: onTap,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: AppColors.primary, size: 22),
          const SizedBox(height: 6),
          Text(
            label,
            style: const TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
            ),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}
