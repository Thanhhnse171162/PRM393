import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminBookingsPage extends StatefulWidget {
  const AdminBookingsPage({super.key});

  @override
  State<AdminBookingsPage> createState() => _AdminBookingsPageState();
}

class _AdminBookingsPageState extends State<AdminBookingsPage> {
  final TextEditingController _searchCtrl = TextEditingController();
  String? _selectedCenterId;
  String? _selectedStatus;
  DateTime? _dateFrom;
  DateTime? _dateTo;

  List<AdminSportCenterItem> _centers = [];
  List<AdminBookingItem> _bookings = [];
  bool _loading = false;
  String? _error;
  int _totalCount = 0;

  @override
  void initState() {
    super.initState();
    _loadCenters();
    _loadBookings();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  void _loadCenters() async {
    try {
      final res = await context.read<AdminApi>().getCenters(pageSize: 100);
      if (mounted) setState(() => _centers = res.items);
    } catch (_) {}
  }

  Future<void> _loadBookings() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final fromStr = _dateFrom != null
          ? '${_dateFrom!.year}-${_dateFrom!.month.toString().padLeft(2, '0')}-${_dateFrom!.day.toString().padLeft(2, '0')}'
          : null;
      final toStr = _dateTo != null
          ? '${_dateTo!.year}-${_dateTo!.month.toString().padLeft(2, '0')}-${_dateTo!.day.toString().padLeft(2, '0')}'
          : null;

      final res = await context.read<AdminApi>().getBookings(
            centerId: _selectedCenterId,
            status: _selectedStatus,
            dateFrom: fromStr,
            dateTo: toStr,
            search: _searchCtrl.text.trim(),
            page: 1,
            pageSize: 50,
          );
      if (mounted) {
        setState(() {
          _bookings = res.items;
          _totalCount = res.totalCount;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải danh sách đơn đặt sân.';
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Quản lý đặt sân',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.assignment_return_outlined,
                color: AppColors.primary),
            tooltip: 'Yêu cầu hủy sân',
            onPressed: () => context.push('/admin/cancellation-requests'),
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Làm mới',
            onPressed: _loadBookings,
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter & Search bar
          Container(
            color: AppColors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Column(
              children: [
                TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Mã đặt, tên khách, số điện thoại...',
                    prefixIcon: const Icon(Icons.search, size: 20),
                    suffixIcon: _searchCtrl.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear, size: 18),
                            onPressed: () {
                              _searchCtrl.clear();
                              _loadBookings();
                            },
                          )
                        : null,
                    filled: true,
                    fillColor: AppColors.background,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 14),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                      borderSide: BorderSide.none,
                    ),
                  ),
                  onSubmitted: (_) => _loadBookings(),
                ),
                const SizedBox(height: 8),
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      AdminFilterChipItem(
                        label: 'Tất cả (${_totalCount})',
                        isSelected: _selectedStatus == null,
                        onTap: () {
                          setState(() => _selectedStatus = null);
                          _loadBookings();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Đã xác nhận',
                        isSelected: _selectedStatus == 'Confirmed',
                        onTap: () {
                          setState(() => _selectedStatus = 'Confirmed');
                          _loadBookings();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Đang chơi / Check-in',
                        isSelected: _selectedStatus == 'CheckedIn',
                        onTap: () {
                          setState(() => _selectedStatus = 'CheckedIn');
                          _loadBookings();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Hoàn thành',
                        isSelected: _selectedStatus == 'Completed',
                        onTap: () {
                          setState(() => _selectedStatus = 'Completed');
                          _loadBookings();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Đã hủy',
                        isSelected: _selectedStatus == 'Cancelled',
                        onTap: () {
                          setState(() => _selectedStatus = 'Cancelled');
                          _loadBookings();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Vắng mặt',
                        isSelected: _selectedStatus == 'NoShow',
                        onTap: () {
                          setState(() => _selectedStatus = 'NoShow');
                          _loadBookings();
                        },
                      ),
                      const SizedBox(width: 8),
                      ActionChip(
                        avatar: const Icon(Icons.storefront,
                            size: 16, color: AppColors.primary),
                        label: Text(
                          _selectedCenterId == null
                              ? 'Cơ sở: Tất cả'
                              : 'Cơ sở: ${_centers.firstWhere((c) => c.centerId == _selectedCenterId, orElse: () => _centers.first).name}',
                          style: const TextStyle(fontSize: 12),
                        ),
                        onPressed: _showCenterFilter,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const Divider(height: 1),

          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadBookings);
    }
    if (_bookings.isEmpty) {
      return const EmptyState(
        title: 'Không có đơn đặt sân nào',
        message: 'Thử tìm kiếm với từ khóa khác hoặc bỏ lọc trạng thái.',
        icon: Icons.event_note_outlined,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadBookings,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _bookings.length,
        separatorBuilder: (_, __) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final b = _bookings[index];
          return AdminCard(
            onTap: () async {
              await context.push('/admin/bookings/${b.bookingId}');
              _loadBookings();
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 8, vertical: 3),
                          decoration: BoxDecoration(
                            color: AppColors.lightMint,
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            b.bookingCode,
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w800,
                              color: AppColors.primary,
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Text(
                          b.sportName,
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                    AdminStatusBadge(
                      label: AdminFormat.bookingStatusName(b.bookingStatus),
                      statusType: b.bookingStatus,
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                Text(
                  '${b.courtName} · ${b.sportCenterName}',
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.schedule,
                        size: 14, color: AppColors.textSecondary),
                    const SizedBox(width: 4),
                    Text(
                      '${AdminFormat.time(b.startAt)} – ${AdminFormat.time(b.endAt)} · ${AdminFormat.date(b.startAt)}',
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.person_outline,
                        size: 14, color: AppColors.textSecondary),
                    const SizedBox(width: 4),
                    Text(
                      '${b.customerName} (${b.customerPhone})',
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                const Divider(height: 1),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Tổng: ${AdminFormat.currency(b.totalAmount)}',
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    Text(
                      'Đã thu: ${AdminFormat.currency(b.paidAmount)} (${AdminFormat.paymentStatusName(b.paymentStatus)})',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: b.paidAmount >= b.totalAmount
                            ? const Color(0xFF16A34A)
                            : AppColors.warning,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  void _showCenterFilter() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Padding(
              padding: EdgeInsets.all(16),
              child: Text(
                'Lọc theo cơ sở',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
              ),
            ),
            ListTile(
              title: const Text('Tất cả cơ sở'),
              trailing: _selectedCenterId == null
                  ? const Icon(Icons.check, color: AppColors.primary)
                  : null,
              onTap: () {
                Navigator.pop(ctx);
                setState(() => _selectedCenterId = null);
                _loadBookings();
              },
            ),
            const Divider(height: 1),
            Expanded(
              child: ListView.separated(
                itemCount: _centers.length,
                separatorBuilder: (_, __) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final c = _centers[i];
                  final isSel = c.centerId == _selectedCenterId;
                  return ListTile(
                    title: Text(c.name),
                    trailing: isSel
                        ? const Icon(Icons.check, color: AppColors.primary)
                        : null,
                    onTap: () {
                      Navigator.pop(ctx);
                      setState(() => _selectedCenterId = c.centerId);
                      _loadBookings();
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
