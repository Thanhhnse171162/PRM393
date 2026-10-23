import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminBookingReportPage extends StatefulWidget {
  const AdminBookingReportPage({super.key});

  @override
  State<AdminBookingReportPage> createState() => _AdminBookingReportPageState();
}

class _AdminBookingReportPageState extends State<AdminBookingReportPage> {
  DateTime _dateFrom = DateTime.now().subtract(const Duration(days: 14));
  DateTime _dateTo = DateTime.now();
  String? _selectedCenterId;

  List<AdminSportCenterItem> _centers = [];
  bool _loadingCenters = true;
  AdminBookingReportData? _data;
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadCenters();
  }

  void _loadCenters() async {
    try {
      final res = await context.read<AdminApi>().getCenters(pageSize: 100);
      if (mounted) {
        setState(() {
          _centers = res.items;
          _loadingCenters = false;
        });
        _loadReport();
      }
    } catch (_) {
      if (mounted) {
        setState(() => _loadingCenters = false);
        _loadReport();
      }
    }
  }

  Future<void> _loadReport() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    final fromStr =
        '${_dateFrom.year}-${_dateFrom.month.toString().padLeft(2, '0')}-${_dateFrom.day.toString().padLeft(2, '0')}';
    final toStr =
        '${_dateTo.year}-${_dateTo.month.toString().padLeft(2, '0')}-${_dateTo.day.toString().padLeft(2, '0')}';

    try {
      final res = await context.read<AdminApi>().getBookingReport(
            dateFrom: fromStr,
            dateTo: toStr,
            centerId: _selectedCenterId,
          );
      if (mounted) {
        setState(() {
          _data = res;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải báo cáo đặt sân.';
        });
      }
    }
  }

  Future<void> _selectDateRange() async {
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2024),
      lastDate: DateTime(2028),
      initialDateRange: DateTimeRange(start: _dateFrom, end: _dateTo),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.primary,
              onPrimary: AppColors.white,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      setState(() {
        _dateFrom = picked.start;
        _dateTo = picked.end;
      });
      _loadReport();
    }
  }

  @override
  Widget build(BuildContext context) {
    final rangeDisplay =
        '${_dateFrom.day}/${_dateFrom.month} – ${_dateTo.day}/${_dateTo.month}/${_dateTo.year}';

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Báo cáo đặt sân',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadReport,
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter Bar
          Container(
            color: AppColors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Column(
              children: [
                Row(
                  children: [
                    Expanded(
                      child: InkWell(
                        onTap: _selectDateRange,
                        borderRadius: BorderRadius.circular(8),
                        child: Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 12, vertical: 8),
                          decoration: BoxDecoration(
                            border: Border.all(color: AppColors.border),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.calendar_today,
                                  size: 16, color: AppColors.textSecondary),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  rangeDisplay,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.w600,
                                      fontSize: 13),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    if (!_loadingCenters && _centers.isNotEmpty)
                      Expanded(
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          decoration: BoxDecoration(
                            border: Border.all(color: AppColors.border),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: DropdownButtonHideUnderline(
                            child: DropdownButton<String?>(
                              value: _selectedCenterId,
                              isExpanded: true,
                              hint: const Text('Tất cả cơ sở',
                                  style: TextStyle(fontSize: 13)),
                              items: [
                                const DropdownMenuItem<String?>(
                                  value: null,
                                  child: Text('Tất cả cơ sở',
                                      style: TextStyle(fontSize: 13)),
                                ),
                                ..._centers.map(
                                  (c) => DropdownMenuItem<String?>(
                                    value: c.id,
                                    child: Text(
                                      c.name,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(fontSize: 13),
                                    ),
                                  ),
                                ),
                              ],
                              onChanged: (val) {
                                setState(() => _selectedCenterId = val);
                                _loadReport();
                              },
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
          const Divider(height: 1, color: AppColors.border),

          // Body
          Expanded(
            child: _buildBody(),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading && _data == null) {
      return const Center(child: LoadingWidget());
    }

    if (_error != null && _data == null) {
      return Center(
        child: ErrorState(
          message: _error!,
          onRetry: _loadReport,
        ),
      );
    }

    final data = _data;
    if (data == null) {
      return const Center(child: Text('Chưa có dữ liệu báo cáo'));
    }

    return RefreshIndicator(
      onRefresh: _loadReport,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Top KPI Cards
          Row(
            children: [
              Expanded(
                child: AdminKpiCard(
                  title: 'Tổng số đơn đặt',
                  value: '${data.totalBookings}',
                  icon: Icons.book_online,
                  iconColor: AppColors.primary,
                  subtitle: 'Đơn trong kỳ',
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: AdminKpiCard(
                  title: 'Đơn đã hủy',
                  value: '${data.cancelledCount}',
                  icon: Icons.cancel_outlined,
                  iconColor: AppColors.error,
                  subtitle: data.totalBookings > 0
                      ? '${((data.cancelledCount / data.totalBookings) * 100).toStringAsFixed(1)}%'
                      : '0%',
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: AdminKpiCard(
                  title: 'Vắng mặt (No-Show)',
                  value: '${data.noShowCount}',
                  icon: Icons.person_off_outlined,
                  iconColor: Colors.orange,
                  subtitle: data.totalBookings > 0
                      ? '${((data.noShowCount / data.totalBookings) * 100).toStringAsFixed(1)}%'
                      : '0%',
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Status Distribution
          AdminCard(
            title: 'Phân bố theo trạng thái',
            child: Column(
              children: [
                if (data.byStatus.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 8),
                    child: Text('Không có dữ liệu trạng thái',
                        style: TextStyle(color: AppColors.textSecondary)),
                  )
                else
                  ...data.byStatus.map((item) {
                    final percent = data.totalBookings > 0
                        ? item.count / data.totalBookings
                        : 0.0;
                    return Padding(
                      padding: const EdgeInsets.symmetric(vertical: 6),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                item.label,
                                style: const TextStyle(
                                    fontWeight: FontWeight.w600, fontSize: 13),
                              ),
                              Text(
                                '${item.count} đơn (${(percent * 100).toStringAsFixed(1)}%)',
                                style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 13,
                                    color: AppColors.textSecondary),
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          ClipRRect(
                            borderRadius: BorderRadius.circular(4),
                            child: LinearProgressIndicator(
                              value: percent,
                              minHeight: 6,
                              backgroundColor: AppColors.surface,
                              valueColor: AlwaysStoppedAnimation<Color>(
                                _statusColor(item.key),
                              ),
                            ),
                          ),
                        ],
                      ),
                    );
                  }),
              ],
            ),
          ),
          const SizedBox(height: 16),

          // Sport Distribution
          AdminCard(
            title: 'Phân bố theo môn thể thao',
            child: Column(
              children: [
                if (data.bySport.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 8),
                    child: Text('Không có dữ liệu môn thể thao',
                        style: TextStyle(color: AppColors.textSecondary)),
                  )
                else
                  ...data.bySport.map((item) {
                    final percent = data.totalBookings > 0
                        ? item.count / data.totalBookings
                        : 0.0;
                    return Padding(
                      padding: const EdgeInsets.symmetric(vertical: 6),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                item.label,
                                style: const TextStyle(
                                    fontWeight: FontWeight.w600, fontSize: 13),
                              ),
                              Text(
                                '${item.count} đơn (${(percent * 100).toStringAsFixed(1)}%)',
                                style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 13,
                                    color: AppColors.primary),
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          ClipRRect(
                            borderRadius: BorderRadius.circular(4),
                            child: LinearProgressIndicator(
                              value: percent,
                              minHeight: 6,
                              backgroundColor: AppColors.surface,
                              valueColor: const AlwaysStoppedAnimation<Color>(
                                AppColors.primary,
                              ),
                            ),
                          ),
                        ],
                      ),
                    );
                  }),
              ],
            ),
          ),
          const SizedBox(height: 16),

          // Center Distribution (if available)
          if (data.byCenter.isNotEmpty) ...[
            AdminCard(
              title: 'Phân bố theo cơ sở',
              child: Column(
                children: data.byCenter.map((item) {
                  final percent = data.totalBookings > 0
                      ? item.count / data.totalBookings
                      : 0.0;
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 6),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Expanded(
                              child: Text(
                                item.label,
                                style: const TextStyle(
                                    fontWeight: FontWeight.w600, fontSize: 13),
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            Text(
                              '${item.count} đơn (${(percent * 100).toStringAsFixed(1)}%)',
                              style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                  fontSize: 13,
                                  color: AppColors.teal),
                            ),
                          ],
                        ),
                        const SizedBox(height: 4),
                        ClipRRect(
                          borderRadius: BorderRadius.circular(4),
                          child: LinearProgressIndicator(
                            value: percent,
                            minHeight: 6,
                            backgroundColor: AppColors.surface,
                            valueColor: const AlwaysStoppedAnimation<Color>(
                              AppColors.teal,
                            ),
                          ),
                        ),
                      ],
                    ),
                  );
                }).toList(),
              ),
            ),
            const SizedBox(height: 16),
          ],

          // Daily Trend
          if (data.trends.isNotEmpty) ...[
            AdminCard(
              title: 'Xu hướng đặt sân theo ngày',
              child: Column(
                children: data.trends.map((t) {
                  return Container(
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    decoration: const BoxDecoration(
                      border: Border(
                        bottom: BorderSide(color: AppColors.border, width: 0.5),
                      ),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.calendar_today,
                                size: 14, color: AppColors.textSecondary),
                            const SizedBox(width: 8),
                            Text(
                              t.date,
                              style: const TextStyle(
                                  fontWeight: FontWeight.w600, fontSize: 13),
                            ),
                          ],
                        ),
                        Row(
                          children: [
                            _trendBadge(
                                '${t.totalBookings}', AppColors.primary),
                            const SizedBox(width: 6),
                            if (t.cancelledBookings > 0)
                              _trendBadge(
                                  '-${t.cancelledBookings}', AppColors.error),
                            if (t.noShowBookings > 0) ...[
                              const SizedBox(width: 6),
                              _trendBadge(
                                  'NS ${t.noShowBookings}', Colors.orange),
                            ],
                          ],
                        ),
                      ],
                    ),
                  );
                }).toList(),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _trendBadge(String text, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withOpacity(0.12),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        text,
        style:
            TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w700),
      ),
    );
  }

  Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'completed':
        return AppColors.success;
      case 'confirmed':
        return AppColors.primary;
      case 'cancelled':
        return AppColors.error;
      case 'noshow':
      case 'no_show':
        return Colors.orange;
      case 'checkedin':
      case 'checked_in':
        return AppColors.teal;
      default:
        return AppColors.textSecondary;
    }
  }
}
