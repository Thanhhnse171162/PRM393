import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminSummaryReportPage extends StatefulWidget {
  const AdminSummaryReportPage({super.key});

  @override
  State<AdminSummaryReportPage> createState() => _AdminSummaryReportPageState();
}

class _AdminSummaryReportPageState extends State<AdminSummaryReportPage> {
  DateTime _dateFrom = DateTime.now().subtract(const Duration(days: 30));
  DateTime _dateTo = DateTime.now();
  String? _selectedCenterId;

  List<AdminSportCenterItem> _centers = [];
  bool _loadingCenters = true;
  AdminReportSummaryData? _data;
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
      final res = await context.read<AdminApi>().getSummaryReport(
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
          _error = 'Không thể tải báo cáo tổng quan.';
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
        '${_dateFrom.day}/${_dateFrom.month}/${_dateFrom.year} – ${_dateTo.day}/${_dateTo.month}/${_dateTo.year}';

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Báo cáo tổng quan',
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
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                Expanded(
                  child: InkWell(
                    onTap: _selectDateRange,
                    borderRadius: BorderRadius.circular(8),
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          vertical: 8, horizontal: 10),
                      decoration: BoxDecoration(
                        color: AppColors.background,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: AppColors.border),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.calendar_today,
                              size: 14, color: AppColors.primary),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              rangeDisplay,
                              style: const TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: DropdownButtonFormField<String?>(
                    initialValue: _selectedCenterId,
                    isExpanded: true,
                    decoration: const InputDecoration(
                      contentPadding:
                          EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                      border: OutlineInputBorder(),
                    ),
                    items: [
                      const DropdownMenuItem(
                        value: null,
                        child: Text('Tất cả cơ sở',
                            style: TextStyle(fontSize: 12)),
                      ),
                      ..._centers.map((c) {
                        return DropdownMenuItem(
                          value: c.centerId,
                          child: Text(
                            c.name,
                            style: const TextStyle(fontSize: 12),
                            overflow: TextOverflow.ellipsis,
                          ),
                        );
                      }),
                    ],
                    onChanged: (v) {
                      setState(() => _selectedCenterId = v);
                      _loadReport();
                    },
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
    if (_loading || _loadingCenters) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadReport);
    }
    if (_data == null) {
      return const Center(child: Text('Chưa có dữ liệu báo cáo'));
    }

    final d = _data!;
    return RefreshIndicator(
      onRefresh: _loadReport,
      color: AppColors.primary,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Revenue Summary Section
          const AdminSectionHeader(title: 'Chỉ số tài chính'),
          AdminCard(
            child: Column(
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Doanh thu ròng (Net)',
                            style: TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            AdminFormat.currency(d.netRevenue),
                            style: const TextStyle(
                              fontSize: 22,
                              fontWeight: FontWeight.w800,
                              color: AppColors.primary,
                              letterSpacing: -0.5,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                const Divider(height: 1),
                const SizedBox(height: 10),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Tổng thu thực tế (Gross):',
                        style: TextStyle(fontSize: 13)),
                    Text(
                      AdminFormat.currency(d.grossCollected),
                      style: const TextStyle(
                          fontSize: 13, fontWeight: FontWeight.w700),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Đã hoàn tiền (Refund):',
                        style: TextStyle(fontSize: 13)),
                    Text(
                      AdminFormat.currency(d.refundAmount),
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                        color: AppColors.error,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Giá trị TB mỗi đơn:',
                        style: TextStyle(fontSize: 13)),
                    Text(
                      AdminFormat.currency(d.averageBookingValue),
                      style: const TextStyle(
                          fontSize: 13, fontWeight: FontWeight.w700),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),

          // Operational KPIs
          const AdminSectionHeader(title: 'Vận hành & Đặt sân'),
          Row(
            children: [
              Expanded(
                child: AdminKpiCard(
                  title: 'Tổng số lượt đặt',
                  value: '${d.totalBookings}',
                  icon: Icons.calendar_month,
                  color: const Color(0xFF2563EB),
                  subtitle: 'Trong giai đoạn đã chọn',
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: AdminKpiCard(
                  title: 'Tỷ lệ lấp đầy sân',
                  value: AdminFormat.percent(d.occupancyRate),
                  icon: Icons.pie_chart_outline,
                  color: AppColors.warning,
                  subtitle: 'Bao gồm giữ chỗ & No-show',
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),

          // Booking Status Breakdown
          AdminCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Kết quả đơn đặt sân',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 12),
                _buildRow(
                  'Lượt chơi hoàn thành',
                  d.completedBookings,
                  d.totalBookings,
                  const Color(0xFF16A34A),
                ),
                const Divider(height: 16),
                _buildRow(
                  'Lượt hủy đơn',
                  d.cancelledBookings,
                  d.totalBookings,
                  AppColors.error,
                ),
                const Divider(height: 16),
                _buildRow(
                  'Khách vắng mặt (No-show)',
                  d.noShowBookings,
                  d.totalBookings,
                  const Color(0xFFEA580C),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }

  Widget _buildRow(String label, int count, int total, Color color) {
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
}
