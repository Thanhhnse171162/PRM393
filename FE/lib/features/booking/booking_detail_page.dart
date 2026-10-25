import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/storage/local_storage_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/currency_utils.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../auth/presentation/providers/auth_provider.dart';

class BookingDetailPage extends StatefulWidget {
  const BookingDetailPage({super.key, required this.id});
  final String id;
  @override
  State<BookingDetailPage> createState() => _BookingDetailPageState();
}

class _BookingDetailPageState extends State<BookingDetailPage> {
  int revision = 0;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(
      title: 'Chi tiết lịch đặt',
      actions: [
        IconButton(
          tooltip: 'Làm mới',
          onPressed: () => setState(() => revision++),
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    body: RequestView<Booking>(
      cacheKey: '${widget.id}-$revision',
      load: () => context.read<CustomerApi>().booking(widget.id),
      builder: (b, reload) {
        final user = context.read<AuthProvider>().user!;
        final reviewed = context.read<LocalStorageService>().reviewed(
          user.id,
          b.id,
        );
        Future<void> open(String path) async {
          await context.push(path);
          if (mounted) setState(() => revision++);
        }

        return Column(
          children: [
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          b.code,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                      StatusChip(b.status),
                    ],
                  ),
                  const SizedBox(height: 20),
                  Text(
                    b.center,
                    style: const TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${b.court} · ${b.sport}',
                    style: const TextStyle(color: AppColors.textSecondary),
                  ),
                  const SizedBox(height: 20),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        children: [
                          InfoLine('Ngày chơi', CustomerFormat.date(b.start)),
                          InfoLine(
                            'Khung giờ',
                            CustomerFormat.interval(b.start, b.end),
                          ),
                          InfoLine('Thời lượng', '${b.duration ~/ 60} giờ'),
                          for (final slot in b.slots)
                            InfoLine(
                              CustomerFormat.interval(slot.start, slot.end),
                              CurrencyUtils.formatVnd(slot.price),
                            ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 20),
                  const SectionHeader('Trạng thái lịch đặt'),
                  const SizedBox(height: 12),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        children: [
                          const _TimelineLine('Đã tạo lịch đặt', true),
                          _TimelineLine(statusLabel(b.status), true),
                          _TimelineLine(
                            '${CustomerFormat.date(b.start)} · ${CustomerFormat.interval(b.start, b.end)}',
                            b.completed,
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 20),
                  const SectionHeader('Thanh toán'),
                  const SizedBox(height: 12),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        children: [
                          InfoLine(
                            'Tổng tiền',
                            CurrencyUtils.formatVnd(b.total),
                            strong: true,
                          ),
                          InfoLine(
                            'Cọc yêu cầu',
                            CurrencyUtils.formatVnd(b.deposit),
                          ),
                          InfoLine(
                            'Đã thanh toán',
                            CurrencyUtils.formatVnd(b.paid),
                          ),
                          const Divider(),
                          InfoLine(
                            'Còn lại',
                            CurrencyUtils.formatVnd(b.remaining),
                            strong: true,
                          ),
                          const SizedBox(height: 8),
                          StatusChip(b.paymentStatus),
                          const SizedBox(height: 12),
                          TextButton(
                            onPressed: () =>
                                open('/customer/bookings/${b.id}/payment'),
                            child: const Text('Kiểm tra kết quả thanh toán'),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  if (b.canCancel || b.status == 'Cancelled')
                    SecondaryButton(
                      label: b.canCancel
                          ? 'Yêu cầu hủy / Xem tiến trình'
                          : 'Xem yêu cầu hủy & hoàn tiền',
                      onPressed: () =>
                          open('/customer/bookings/${b.id}/cancellation'),
                    ),
                  if (b.completed) ...[
                    const SizedBox(height: 12),
                    reviewed
                        ? const ListTile(
                            leading: Icon(
                              Icons.star_rounded,
                              color: AppColors.primary,
                            ),
                            title: Text('Bạn đã gửi đánh giá'),
                          )
                        : SecondaryButton(
                            label: 'Đánh giá trải nghiệm',
                            onPressed: () =>
                                open('/customer/bookings/${b.id}/review'),
                          ),
                  ],
                ],
              ),
            ),
            if (b.hasQr || b.awaitingPayment)
              ActionDock(
                child: PrimaryButton(
                  label: b.hasQr ? 'Xem mã QR check-in' : 'Tiếp tục đặt cọc',
                  icon: b.hasQr
                      ? Icons.qr_code
                      : Icons.account_balance_wallet_outlined,
                  onPressed: () => open(
                    '/customer/bookings/${b.id}/${b.hasQr ? 'qr' : 'checkout'}',
                  ),
                ),
              ),
          ],
        );
      },
    ),
  );
}

class _TimelineLine extends StatelessWidget {
  const _TimelineLine(this.text, this.done);
  final String text;
  final bool done;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 8),
    child: Row(
      children: [
        Icon(
          done ? Icons.check_circle : Icons.schedule,
          size: 18,
          color: done ? AppColors.primary : AppColors.textSecondary,
        ),
        const SizedBox(width: 12),
        Expanded(child: Text(text, style: const TextStyle(fontSize: 13))),
      ],
    ),
  );
}
