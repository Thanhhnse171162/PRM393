import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/currency_utils.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';

class CheckoutPage extends StatelessWidget {
  const CheckoutPage({super.key, required this.id, this.hold});
  final String id;
  final Booking? hold;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Xác nhận & đặt cọc'),
    body: RequestView<Booking>(
      cacheKey: id,
      load: () async => hold ?? await context.read<CustomerApi>().booking(id),
      builder: (booking, reload) => _Checkout(booking: booking),
    ),
  );
}

class _Checkout extends StatefulWidget {
  const _Checkout({required this.booking});
  final Booking booking;
  @override
  State<_Checkout> createState() => _CheckoutState();
}

class _CheckoutState extends State<_Checkout> {
  String method = 'MoMo';
  bool busy = false;
  String? error;
  Timer? timer;
  @override
  void initState() {
    super.initState();
    timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final b = widget.booking;
    final seconds = b.expires == null
        ? null
        : DateTime.parse(b.expires!).difference(DateTime.now()).inSeconds;
    final expired = seconds != null && seconds <= 0;
    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Row(
                children: [
                  const Text(
                    'BƯỚC 2 / 2',
                    style: TextStyle(
                      color: AppColors.primary,
                      fontWeight: FontWeight.w700,
                      fontSize: 11,
                    ),
                  ),
                  const Spacer(),
                  StatusChip(b.status),
                ],
              ),
              const SizedBox(height: 16),
              if (seconds != null)
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFFF4DC),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Row(
                    children: [
                      const Icon(
                        Icons.timer_outlined,
                        color: Color(0xFF9A6700),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          expired
                              ? 'Thời gian giữ sân đã hết'
                              : 'Sân đang được giữ cho bạn',
                          style: const TextStyle(
                            fontWeight: FontWeight.w600,
                            fontSize: 13,
                          ),
                        ),
                      ),
                      if (!expired)
                        Text(
                          '${(seconds ~/ 60).toString().padLeft(2, '0')}:${(seconds % 60).toString().padLeft(2, '0')}',
                          style: const TextStyle(
                            fontWeight: FontWeight.w800,
                            fontSize: 22,
                            color: Color(0xFF9A6700),
                          ),
                        ),
                    ],
                  ),
                ),
              const SizedBox(height: 16),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        b.center,
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        b.sport,
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontSize: 12,
                        ),
                      ),
                      const SizedBox(height: 12),
                      InfoLine('Sân', b.court),
                      InfoLine('Ngày chơi', CustomerFormat.date(b.start)),
                      InfoLine(
                        'Khung giờ',
                        CustomerFormat.interval(b.start, b.end),
                      ),
                      InfoLine(
                        'Thời lượng',
                        '${b.duration ~/ 60} giờ liên tiếp',
                      ),
                      for (final s in b.slots)
                        InfoLine(
                          CustomerFormat.interval(s.start, s.end),
                          CurrencyUtils.formatVnd(s.price),
                        ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SectionHeader('Chi tiết thanh toán'),
                      const SizedBox(height: 12),
                      InfoLine(
                        'Tổng tiền sân',
                        CurrencyUtils.formatVnd(b.total),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12),
                        decoration: BoxDecoration(
                          color: AppColors.lightMint,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: InfoLine(
                          'Tiền cọc cần thanh toán',
                          CurrencyUtils.formatVnd(b.deposit),
                          strong: true,
                        ),
                      ),
                      InfoLine(
                        'Còn lại thanh toán tại sân',
                        CurrencyUtils.formatVnd(b.total - b.deposit),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'Nhân viên sẽ thu phần còn lại trước khi check-in.',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),
              const SectionHeader('Phương thức đặt cọc'),
              const SizedBox(height: 12),
              for (final choice in ['MoMo', 'VNPay'])
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Card(
                    child: ListTile(
                      minTileHeight: 64,
                      leading: Icon(
                        choice == 'MoMo'
                            ? Icons.account_balance_wallet_outlined
                            : Icons.account_balance_outlined,
                        color: AppColors.primary,
                      ),
                      title: Text(
                        choice,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      trailing: Icon(
                        method == choice
                            ? Icons.radio_button_checked
                            : Icons.radio_button_off,
                        color: AppColors.primary,
                      ),
                      onTap: busy
                          ? null
                          : () => setState(() => method = choice),
                    ),
                  ),
                ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text(
                    error!,
                    style: const TextStyle(color: AppColors.error),
                  ),
                ),
            ],
          ),
        ),
        ActionDock(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (expired)
                PrimaryButton(
                  label: 'Chọn lại giờ',
                  onPressed: () => context.go('/customer/explore'),
                )
              else if (!b.awaitingPayment)
                PrimaryButton(
                  label: 'Xem lịch đặt',
                  onPressed: () => context.go('/customer/bookings/${b.id}'),
                )
              else
                PrimaryButton(
                  label: 'Thanh toán cọc ${CurrencyUtils.formatVnd(b.deposit)}',
                  loading: busy,
                  onPressed: () async {
                    if (busy) return;
                    setState(() {
                      busy = true;
                      error = null;
                    });
                    try {
                      final payment = await context
                          .read<CustomerApi>()
                          .startDeposit(b.id, method);
                      if (context.mounted) {
                        context.push(
                          '/customer/bookings/${b.id}/payment',
                          extra: payment,
                        );
                      }
                    } on ApiException catch (e) {
                      if (mounted) setState(() => error = e.message);
                    } catch (_) {
                      if (mounted) {
                        setState(
                          () => error =
                              'Chưa tạo được giao dịch. Vui lòng kiểm tra lại.',
                        );
                      }
                    } finally {
                      if (mounted) setState(() => busy = false);
                    }
                  },
                ),
            ],
          ),
        ),
      ],
    );
  }
}
