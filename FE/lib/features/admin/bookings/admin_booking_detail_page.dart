import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminBookingDetailPage extends StatefulWidget {
  const AdminBookingDetailPage({super.key, required this.bookingId});

  final String bookingId;

  @override
  State<AdminBookingDetailPage> createState() => _AdminBookingDetailPageState();
}

class _AdminBookingDetailPageState extends State<AdminBookingDetailPage> {
  Future<AdminBookingDetail>? _detailFuture;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  void _loadDetail() {
    setState(() {
      _detailFuture = context.read<AdminApi>().getBooking(widget.bookingId);
    });
  }

  void _showCancelSheet(AdminBookingDetail detail) {
    final maxRefund =
        (detail.financialSummary.paidAmount - detail.financialSummary.refundedAmount)
            .clamp(0.0, double.infinity);
    final reasonCtrl = TextEditingController();
    final refundCtrl =
        TextEditingController(text: maxRefund.toStringAsFixed(0));
    final formKey = GlobalKey<FormState>();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        bool saving = false;
        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            return Padding(
              padding: EdgeInsets.only(
                left: 20,
                right: 20,
                top: 20,
                bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
              ),
              child: Form(
                key: formKey,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Hủy đơn đặt sân (Admin)',
                          style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w700,
                            color: AppColors.error,
                          ),
                        ),
                        IconButton(
                          icon: const Icon(Icons.close),
                          onPressed: () => Navigator.pop(ctx),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFEF2F2),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: const Color(0xFFFCA5A5)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Khách đã thanh toán: ${AdminFormat.currency(detail.financialSummary.paidAmount)}',
                            style: const TextStyle(
                                fontSize: 13, fontWeight: FontWeight.w600),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            'Mức hoàn tối đa hợp lệ: ${AdminFormat.currency(maxRefund)}',
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w700,
                              color: AppColors.error,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 14),
                    TextFormField(
                      controller: reasonCtrl,
                      maxLines: 2,
                      decoration: const InputDecoration(
                        labelText: 'Lý do hủy đơn (Bắt buộc) *',
                        hintText: 'VD: Khách yêu cầu hủy do sự cố, sân bảo trì đột xuất...',
                        border: OutlineInputBorder(),
                      ),
                      validator: (v) => v == null || v.trim().isEmpty
                          ? 'Vui lòng nhập lý do hủy'
                          : null,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: refundCtrl,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Số tiền hoàn lại cho khách (VND)',
                        border: OutlineInputBorder(),
                        helperText: 'Không được vượt quá số tiền tối đa hợp lệ.',
                      ),
                      validator: (v) {
                        if (v == null || v.trim().isEmpty) return null;
                        final val = double.tryParse(v.trim());
                        if (val == null || val < 0) {
                          return 'Số tiền hoàn không hợp lệ';
                        }
                        if (val > maxRefund) {
                          return 'Không thể hoàn vượt quá ${AdminFormat.currency(maxRefund)}';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 20),
                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.error,
                        foregroundColor: AppColors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                      onPressed: saving
                          ? null
                          : () async {
                              if (!formKey.currentState!.validate()) return;
                              setSheetState(() => saving = true);

                              final refundAmount =
                                  double.tryParse(refundCtrl.text.trim());

                              try {
                                final res = await context
                                    .read<AdminApi>()
                                    .cancelBooking(
                                      detail.bookingId,
                                      reason: reasonCtrl.text.trim(),
                                      refundAmount: refundAmount,
                                    );
                                if (ctx.mounted) Navigator.pop(ctx);
                                if (mounted) {
                                  ScaffoldMessenger.of(context).showSnackBar(
                                    SnackBar(
                                      content: Text(
                                        'Đã hủy đơn thành công! Trạng thái mới: ${res['bookingStatus'] ?? "Cancelled"}',
                                      ),
                                    ),
                                  );
                                  _loadDetail();
                                }
                              } catch (e) {
                                setSheetState(() => saving = false);
                                if (ctx.mounted) {
                                  ScaffoldMessenger.of(ctx).showSnackBar(
                                    SnackBar(
                                      content: Text(e is ApiException
                                          ? e.message
                                          : 'Hủy đơn thất bại'),
                                      backgroundColor: AppColors.error,
                                    ),
                                  );
                                }
                              }
                            },
                      child: saving
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: AppColors.white,
                              ),
                            )
                          : const Text('Xác nhận hủy đơn'),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Chi tiết đặt sân',
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
      body: FutureBuilder<AdminBookingDetail>(
        future: _detailFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const LoadingWidget();
          }
          if (snapshot.hasError) {
            return ErrorState(
              message: 'Không thể tải chi tiết đơn đặt',
              onRetry: _loadDetail,
            );
          }
          final detail = snapshot.data;
          if (detail == null) {
            return const Center(child: Text('Không tìm thấy đơn đặt'));
          }

          final canCancel = detail.bookingStatus != 'Cancelled' &&
              detail.bookingStatus != 'Completed';

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Top Summary Card
                AdminCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            'Mã: ${detail.bookingCode}',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                              color: AppColors.primary,
                            ),
                          ),
                          AdminStatusBadge(
                            label: AdminFormat.bookingStatusName(
                                detail.bookingStatus),
                            statusType: detail.bookingStatus,
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      const Divider(height: 1),
                      const SizedBox(height: 12),
                      _buildRow(
                          'Khách hàng',
                          '${detail.customer.customerName} (${detail.customer.customerPhone})'),
                      const SizedBox(height: 6),
                      _buildRow('Cơ sở', detail.facility.sportCenterName),
                      const SizedBox(height: 6),
                      _buildRow('Sân & Môn',
                          '${detail.facility.courtName} · ${detail.facility.sportName}'),
                      const SizedBox(height: 6),
                      _buildRow('Thời gian',
                          '${AdminFormat.time(detail.startAt)} – ${AdminFormat.time(detail.endAt)} (${AdminFormat.date(detail.startAt)})'),
                      const SizedBox(height: 6),
                      _buildRow('Nguồn đặt', detail.source),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // Financial Summary Card
                const AdminSectionHeader(title: 'Tài chính & Thanh toán'),
                AdminCard(
                  child: Column(
                    children: [
                      _buildPriceRow('Tổng giá trị',
                          detail.financialSummary.totalAmount, bold: true),
                      const SizedBox(height: 8),
                      _buildPriceRow(
                          'Đã thanh toán', detail.financialSummary.paidAmount,
                          color: const Color(0xFF16A34A)),
                      const SizedBox(height: 8),
                      _buildPriceRow(
                          'Còn nợ', detail.financialSummary.remainingAmount,
                          color: detail.financialSummary.remainingAmount > 0
                              ? AppColors.warning
                              : AppColors.textSecondary),
                      if (detail.financialSummary.refundedAmount > 0) ...[
                        const SizedBox(height: 8),
                        _buildPriceRow(
                            'Đã hoàn tiền', detail.financialSummary.refundedAmount,
                            color: AppColors.error),
                      ],
                      const SizedBox(height: 12),
                      const Divider(height: 1),
                      const SizedBox(height: 10),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text(
                            'Trạng thái thanh toán:',
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          AdminStatusBadge(
                            label: AdminFormat.paymentStatusName(
                                detail.paymentStatus),
                            statusType: detail.paymentStatus,
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // Slot Details
                const AdminSectionHeader(title: 'Chi tiết khung giờ'),
                ...detail.slots.map((s) {
                  return Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: AdminCard(
                      padding: const EdgeInsets.all(12),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Row(
                            children: [
                              const Icon(Icons.access_time,
                                  size: 16, color: AppColors.primary),
                              const SizedBox(width: 8),
                              Text(
                                '${AdminFormat.time(s.startAt)} – ${AdminFormat.time(s.endAt)}',
                                style: const TextStyle(
                                    fontSize: 13, fontWeight: FontWeight.w600),
                              ),
                            ],
                          ),
                          Text(
                            AdminFormat.currency(s.unitPrice),
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }),
                const SizedBox(height: 16),

                // Payments History
                if (detail.payments.isNotEmpty) ...[
                  const AdminSectionHeader(title: 'Lịch sử giao dịch'),
                  ...detail.payments.map((p) {
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: AdminCard(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                          children: [
                            Icon(
                              p.kind.toLowerCase().contains('refund')
                                  ? Icons.assignment_return
                                  : Icons.payment,
                              size: 18,
                              color: p.kind.toLowerCase().contains('refund')
                                  ? AppColors.error
                                  : AppColors.primary,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    '${p.kind} · ${p.method}',
                                    style: const TextStyle(
                                        fontSize: 13,
                                        fontWeight: FontWeight.w700),
                                  ),
                                  Text(
                                    'Lúc: ${AdminFormat.dateTime(p.createdAt)}',
                                    style: const TextStyle(
                                        fontSize: 11,
                                        color: AppColors.textSecondary),
                                  ),
                                ],
                              ),
                            ),
                            Text(
                              AdminFormat.currency(p.amount),
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w700,
                                color: p.kind.toLowerCase().contains('refund')
                                    ? AppColors.error
                                    : AppColors.primary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  }),
                  const SizedBox(height: 16),
                ],

                // Status Timeline
                const AdminSectionHeader(title: 'Lịch sử thay đổi trạng thái'),
                AdminCard(
                  child: Column(
                    children: detail.statusHistories.map((h) {
                      return Padding(
                        padding: const EdgeInsets.symmetric(vertical: 6),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(Icons.radio_button_checked,
                                size: 14, color: AppColors.primary),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    'Chuyển sang: ${AdminFormat.bookingStatusName(h.toStatus)}',
                                    style: const TextStyle(
                                      fontSize: 13,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                  if (h.reason != null &&
                                      h.reason!.isNotEmpty)
                                    Text(
                                      'Lý do: ${h.reason!}',
                                      style: const TextStyle(
                                        fontSize: 12,
                                        color: AppColors.textSecondary,
                                      ),
                                    ),
                                  Text(
                                    AdminFormat.dateTime(h.createdAt),
                                    style: const TextStyle(
                                      fontSize: 11,
                                      color: AppColors.textSecondary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      );
                    }).toList(),
                  ),
                ),
                const SizedBox(height: 24),

                // Direct Cancellation Button
                if (canCancel) ...[
                  ElevatedButton.icon(
                    icon: const Icon(Icons.cancel_outlined),
                    label: const Text('Hủy đơn đặt sân (Trực tiếp)'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.error,
                      foregroundColor: AppColors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    onPressed: () => _showCancelSheet(detail),
                  ),
                  const SizedBox(height: 24),
                ],
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
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

  Widget _buildPriceRow(
    String label,
    num value, {
    bool bold = false,
    Color? color,
  }) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: bold ? 14 : 13,
            fontWeight: bold ? FontWeight.w700 : FontWeight.w500,
            color: bold ? AppColors.textPrimary : AppColors.textSecondary,
          ),
        ),
        Text(
          AdminFormat.currency(value),
          style: TextStyle(
            fontSize: bold ? 15 : 13,
            fontWeight: bold ? FontWeight.w800 : FontWeight.w600,
            color: color ?? AppColors.textPrimary,
          ),
        ),
      ],
    );
  }
}
