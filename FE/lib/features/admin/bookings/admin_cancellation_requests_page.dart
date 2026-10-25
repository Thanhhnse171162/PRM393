import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminCancellationRequestsPage extends StatefulWidget {
  const AdminCancellationRequestsPage({super.key});

  @override
  State<AdminCancellationRequestsPage> createState() =>
      _AdminCancellationRequestsPageState();
}

class _AdminCancellationRequestsPageState
    extends State<AdminCancellationRequestsPage> {
  String? _selectedStatus = 'Pending';
  List<AdminCancellationRequestSummary> _requests = [];
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadRequests();
  }

  Future<void> _loadRequests() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final res = await context.read<AdminApi>().getCancellationRequests(
            status: _selectedStatus,
            page: 1,
            pageSize: 50,
          );
      if (mounted) {
        setState(() {
          _requests = res.items;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải danh sách yêu cầu hủy.';
        });
      }
    }
  }

  void _showRequestDetail(AdminCancellationRequestSummary summary) async {
    showDialog(
      context: context,
      builder: (_) => const Center(child: CircularProgressIndicator()),
    );

    try {
      final detail = await context
          .read<AdminApi>()
          .getCancellationRequest(summary.requestId);
      if (!mounted) return;
      Navigator.pop(context); // dismiss loading dialog

      final isPending = detail.requestStatus.toLowerCase() == 'pending';
      final reasonCtrl = TextEditingController();
      final refundCtrl = TextEditingController(
        text: detail.calculatedRefundAmount.toStringAsFixed(0),
      );

      showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        builder: (ctx) {
          bool processing = false;
          return StatefulBuilder(
            builder: (ctx, setSheetState) {
              return SafeArea(
                child: Padding(
                  padding: EdgeInsets.only(
                    left: 20,
                    right: 20,
                    top: 20,
                    bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
                  ),
                  child: SingleChildScrollView(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              'Yêu cầu hủy: ${detail.bookingCode}',
                              style: const TextStyle(
                                fontSize: 17,
                                fontWeight: FontWeight.w800,
                                color: AppColors.textPrimary,
                              ),
                            ),
                            AdminStatusBadge(
                              label: detail.requestStatus,
                              statusType: detail.requestStatus,
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
                        const Divider(height: 1),
                        const SizedBox(height: 10),
                        _buildRow('Khách hàng',
                            '${detail.customer.customerName} (${detail.customer.customerPhone})'),
                        const SizedBox(height: 6),
                        _buildRow('Cơ sở / Sân',
                            '${detail.facility.courtName} · ${detail.facility.sportCenterName}'),
                        const SizedBox(height: 6),
                        _buildRow('Giờ chơi',
                            '${AdminFormat.time(detail.startAt)} – ${AdminFormat.time(detail.endAt)} (${AdminFormat.date(detail.startAt)})'),
                        const SizedBox(height: 6),
                        _buildRow('Lý do khách hủy', detail.reason),
                        const SizedBox(height: 10),
                        const Divider(height: 1),
                        const SizedBox(height: 10),
                        _buildRow('Đã thanh toán',
                            AdminFormat.currency(detail.paidAmount)),
                        const SizedBox(height: 6),
                        if (detail.policyName != null) ...[
                          _buildRow('Chính sách', detail.policyName!),
                          const SizedBox(height: 6),
                        ],
                        if (detail.matchedRefundPercent != null) ...[
                          _buildRow('Tỷ lệ hoàn',
                              '${(detail.matchedRefundPercent! * 100).toStringAsFixed(0)}%'),
                          const SizedBox(height: 6),
                        ],
                        _buildRow(
                          'Ước tính hoàn',
                          AdminFormat.currency(detail.calculatedRefundAmount),
                          bold: true,
                          highlightColor: AppColors.primary,
                        ),
                        const SizedBox(height: 16),

                        if (isPending) ...[
                          TextField(
                            controller: refundCtrl,
                            keyboardType: TextInputType.number,
                            decoration: const InputDecoration(
                              labelText: 'Số tiền duyệt hoàn (VND) *',
                              border: OutlineInputBorder(),
                            ),
                          ),
                          const SizedBox(height: 10),
                          TextField(
                            controller: reasonCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Ghi chú duyệt / Lý do từ chối',
                              border: OutlineInputBorder(),
                            ),
                          ),
                          const SizedBox(height: 16),
                          Row(
                            children: [
                              Expanded(
                                child: OutlinedButton(
                                  style: OutlinedButton.styleFrom(
                                    foregroundColor: AppColors.error,
                                    side: const BorderSide(
                                        color: AppColors.error),
                                    padding: const EdgeInsets.symmetric(
                                        vertical: 14),
                                  ),
                                  onPressed: processing
                                      ? null
                                      : () async {
                                          setSheetState(
                                              () => processing = true);
                                          try {
                                            await context
                                                .read<AdminApi>()
                                                .decideCancellationRequest(
                                                  detail.requestId,
                                                  decision: 'Reject',
                                                  reason: reasonCtrl.text.trim(),
                                                );
                                            if (ctx.mounted) Navigator.pop(ctx);
                                            _loadRequests();
                                          } catch (e) {
                                            setSheetState(
                                                () => processing = false);
                                            if (ctx.mounted) {
                                              ScaffoldMessenger.of(ctx)
                                                  .showSnackBar(
                                                SnackBar(
                                                  content: Text(e is ApiException
                                                      ? e.message
                                                      : 'Thao tác thất bại'),
                                                  backgroundColor:
                                                      AppColors.error,
                                                ),
                                              );
                                            }
                                          }
                                        },
                                  child: const Text('Từ chối'),
                                ),
                              ),
                              const SizedBox(width: 10),
                              Expanded(
                                child: ElevatedButton(
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: AppColors.primary,
                                    foregroundColor: AppColors.white,
                                    padding: const EdgeInsets.symmetric(
                                        vertical: 14),
                                  ),
                                  onPressed: processing
                                      ? null
                                      : () async {
                                          final approvedAmount =
                                              double.tryParse(refundCtrl.text.trim());
                                          setSheetState(
                                              () => processing = true);
                                          try {
                                            await context
                                                .read<AdminApi>()
                                                .decideCancellationRequest(
                                                  detail.requestId,
                                                  decision: 'Approve',
                                                  approvedRefundAmount:
                                                      approvedAmount,
                                                  reason: reasonCtrl.text.trim(),
                                                );
                                            if (ctx.mounted) Navigator.pop(ctx);
                                            _loadRequests();
                                          } catch (e) {
                                            setSheetState(
                                                () => processing = false);
                                            if (ctx.mounted) {
                                              ScaffoldMessenger.of(ctx)
                                                  .showSnackBar(
                                                SnackBar(
                                                  content: Text(e is ApiException
                                                      ? e.message
                                                      : 'Thao tác thất bại'),
                                                  backgroundColor:
                                                      AppColors.error,
                                                ),
                                              );
                                            }
                                          }
                                        },
                                  child: const Text('Duyệt hoàn tiền'),
                                ),
                              ),
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
              );
            },
          );
        },
      );
    } catch (_) {
      if (mounted) Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Yêu cầu hủy sân',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadRequests,
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter Tabs
          Container(
            color: AppColors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                AdminFilterChipItem(
                  label: 'Chờ duyệt',
                  isSelected: _selectedStatus == 'Pending',
                  onTap: () {
                    setState(() => _selectedStatus = 'Pending');
                    _loadRequests();
                  },
                ),
                const SizedBox(width: 8),
                AdminFilterChipItem(
                  label: 'Đã duyệt',
                  isSelected: _selectedStatus == 'Approved',
                  onTap: () {
                    setState(() => _selectedStatus = 'Approved');
                    _loadRequests();
                  },
                ),
                const SizedBox(width: 8),
                AdminFilterChipItem(
                  label: 'Đã từ chối',
                  isSelected: _selectedStatus == 'Rejected',
                  onTap: () {
                    setState(() => _selectedStatus = 'Rejected');
                    _loadRequests();
                  },
                ),
                const SizedBox(width: 8),
                AdminFilterChipItem(
                  label: 'Tất cả',
                  isSelected: _selectedStatus == null,
                  onTap: () {
                    setState(() => _selectedStatus = null);
                    _loadRequests();
                  },
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
      return ErrorState(message: _error!, onRetry: _loadRequests);
    }
    if (_requests.isEmpty) {
      return const EmptyState(
        title: 'Không có yêu cầu hủy nào',
        message: 'Hiện không có yêu cầu hủy sân nào thuộc danh mục này.',
        icon: Icons.assignment_turned_in_outlined,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadRequests,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _requests.length,
        separatorBuilder: (_, __) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final r = _requests[index];
          return AdminCard(
            onTap: () => _showRequestDetail(r),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Đơn: ${r.bookingCode}',
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w800,
                        color: AppColors.primary,
                      ),
                    ),
                    AdminStatusBadge(
                      label: r.status,
                      statusType: r.status,
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Text(
                  '${r.courtNameSnapshot} · ${r.centerNameSnapshot}',
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Khách: ${r.customerNameSnapshot} (${r.customerPhoneSnapshot})',
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Lý do: "${r.reason}"',
                  style: const TextStyle(
                    fontSize: 12,
                    fontStyle: FontStyle.italic,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 8),
                const Divider(height: 1),
                const SizedBox(height: 6),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Yêu cầu lúc: ${AdminFormat.dateTime(r.createdAt)}',
                      style: const TextStyle(
                        fontSize: 11,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    Text(
                      'Hoàn: ${AdminFormat.currency(r.approvedRefundAmount ?? r.calculatedRefundAmount)}',
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                        color: AppColors.primary,
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

  Widget _buildRow(String label, String value,
      {bool bold = false, Color? highlightColor}) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 105,
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 13,
              fontWeight: bold ? FontWeight.w800 : FontWeight.w600,
              color: highlightColor ?? AppColors.textPrimary,
            ),
          ),
        ),
      ],
    );
  }
}
