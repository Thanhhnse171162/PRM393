import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminRevenueReportPage extends StatefulWidget {
  const AdminRevenueReportPage({super.key});

  @override
  State<AdminRevenueReportPage> createState() => _AdminRevenueReportPageState();
}

class _AdminRevenueReportPageState extends State<AdminRevenueReportPage> {
  DateTime _dateFrom = DateTime.now().subtract(const Duration(days: 14));
  DateTime _dateTo = DateTime.now();
  String? _selectedCenterId;
  String _groupBy = 'day';

  List<AdminSportCenterItem> _centers = [];
  bool _loadingCenters = true;
  AdminRevenueReportData? _data;
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
      final res = await context.read<AdminApi>().getRevenueReport(
            dateFrom: fromStr,
            dateTo: toStr,
            centerId: _selectedCenterId,
            groupBy: _groupBy,
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
          _error = 'Không thể tải báo cáo doanh thu.';
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
        '${_dateFrom.day}/${_dateFrom.month} – ${_dateTo.day}/${_dateTo.month}';

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Báo cáo doanh thu',
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
                const SizedBox(height: 8),
                Row(
                  children: [
                    AdminFilterChipItem(
                      label: 'Theo ngày',
                      isSelected: _groupBy == 'day',
                      onTap: () {
                        setState(() => _groupBy = 'day');
                        _loadReport();
                      },
                    ),
                    const SizedBox(width: 8),
                    AdminFilterChipItem(
                      label: 'Theo tháng',
                      isSelected: _groupBy == 'month',
                      onTap: () {
                        setState(() => _groupBy = 'month');
                        _loadReport();
                      },
                    ),
                  ],
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
    final chartItems = d.items.map((item) {
      final label = item.label.length > 5
          ? item.label.substring(item.label.length - 5)
          : item.label;
      return AdminBarChartItem(
        label: label,
        value: item.netRevenue.clamp(0.0, double.infinity),
      );
    }).toList();

    return RefreshIndicator(
      onRefresh: _loadReport,
      color: AppColors.primary,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Total KPI Summary Card
          AdminCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Tổng doanh thu ròng',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  AdminFormat.currency(d.netRevenue),
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary,
                    letterSpacing: -0.5,
                  ),
                ),
                const SizedBox(height: 12),
                const Divider(height: 1),
                const SizedBox(height: 10),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Container(
                          width: 8,
                          height: 8,
                          decoration: const BoxDecoration(
                            color: Color(0xFF16A34A),
                            shape: BoxShape.circle,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Text(
                          'Thu thực tế: ${AdminFormat.currency(d.grossCollected)}',
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                    Row(
                      children: [
                        Container(
                          width: 8,
                          height: 8,
                          decoration: const BoxDecoration(
                            color: AppColors.error,
                            shape: BoxShape.circle,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Text(
                          'Hoàn tiền: ${AdminFormat.currency(d.refundAmount)}',
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.error,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),

          // Primary Bar Chart
          const AdminSectionHeader(title: 'Biểu đồ doanh thu ròng'),
          AdminCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AdminBarChart(
                  items: chartItems,
                  primaryColor: AppColors.primary,
                  height: 180,
                  valueFormatter: (val) {
                    if (val >= 1000000) {
                      return '${(val / 1000000).toStringAsFixed(1)}M';
                    } else if (val >= 1000) {
                      return '${(val / 1000).toStringAsFixed(0)}k';
                    }
                    return val.toStringAsFixed(0);
                  },
                ),
                const SizedBox(height: 10),
                const Center(
                  child: Text(
                    'Đơn vị tính: VND (Net Revenue sau khi trừ tiền hoàn)',
                    style: TextStyle(
                      fontSize: 11,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),

          // Series List Breakdown
          const AdminSectionHeader(title: 'Chi tiết theo mốc thời gian'),
          if (d.items.isEmpty)
            const AdminCard(
              child: Center(
                child: Text('Không có giao dịch phát sinh trong giai đoạn này'),
              ),
            )
          else
            ...d.items.map((item) {
              return Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: AdminCard(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            item.label,
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            'Thu: ${AdminFormat.currency(item.grossCollected)} · ${item.transactionCount} GD',
                            style: const TextStyle(
                              fontSize: 11,
                              color: AppColors.textSecondary,
                            ),
                          ),
                        ],
                      ),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            AdminFormat.currency(item.netRevenue),
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w800,
                              color: AppColors.primary,
                            ),
                          ),
                          if (item.refundAmount > 0)
                            Text(
                              '-${AdminFormat.currency(item.refundAmount)}',
                              style: const TextStyle(
                                fontSize: 11,
                                color: AppColors.error,
                              ),
                            ),
                        ],
                      ),
                    ],
                  ),
                ),
              );
            }),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}
