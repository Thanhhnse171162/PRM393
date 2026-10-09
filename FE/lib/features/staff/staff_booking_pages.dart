import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/utils/currency_utils.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import 'staff_api.dart';
import 'staff_widgets.dart';

class StaffBookingDetailPage extends StatefulWidget {
  const StaffBookingDetailPage({super.key, required this.id});
  final String id;
  @override
  State<StaffBookingDetailPage> createState() => _StaffBookingDetailPageState();
}

class _StaffBookingDetailPageState extends State<StaffBookingDetailPage> {
  bool busy = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Chi tiết lịch đặt'),
    body: StaffData(
      identity: widget.id,
      load: (api) => api.get('bookings/${widget.id}'),
      builder: (data, reload) {
        final api = context.read<StaffApi>();
        final b = data['booking'] as Json;
        final p = data['payment'] as Json;
        return StaffList(
          refresh: reload,
          children: [
            StaffPanel(
              title: b['bookingCode'],
              children: [
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    StatusChip(b['bookingStatus']),
                    StatusChip(b['paymentStatus']),
                  ],
                ),
                const SizedBox(height: 16),
                Text('${b['customerName']} · ${b['customerPhone']}'),
                const SizedBox(height: 12),
                Text('${b['courtName']} · ${b['sportName']}'),
                Text(api.interval(b)),
              ],
            ),
            StaffPaymentSummary(payment: p),
            if (canCollect(b, p))
              PrimaryButton(
                label: 'Thu tiền còn lại',
                onPressed: busy
                    ? null
                    : () => collectRemaining(context, widget.id),
              ),
            if (b['bookingStatus'] == 'Confirmed')
              SecondaryButton(
                label: 'Quét QR để check-in',
                onPressed: () => context.go('/staff/check-in'),
              ),
            StaffPanel(
              title: 'Lịch sử thanh toán',
              children: [
                if (jsonList(data['payments']).isEmpty)
                  const Text('Chưa có giao dịch.'),
                ...jsonList(data['payments']).map(
                  (payment) => Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${paymentKind(payment['kind'])} · ${paymentMethod(payment['method'])}',
                        ),
                        Row(
                          children: [
                            Expanded(
                              child: PriceText(amount(payment['amount'])),
                            ),
                            StatusChip(payment['status']),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
            if (data['checkIn'] case final Map checked)
              StaffPanel(
                title: 'Đã check-in',
                children: [
                  Text(
                    api
                        .local(checked['checkedInAt'])
                        .substring(0, 16)
                        .replaceFirst('T', ' '),
                  ),
                  if (checked['outstandingPaymentOverride'] == true)
                    Text('Ngoại lệ: ${checked['overrideReason']}'),
                ],
              ),
            if (canMarkNoShow(b, api.now))
              SecondaryButton(
                label: 'Đánh dấu khách vắng mặt',
                onPressed: busy
                    ? null
                    : () async {
                        if (!await staffConfirm(
                          context,
                          'Khách không đến?',
                          'Lượt đặt đã kết thúc. Xác nhận đánh dấu khách vắng mặt.',
                        ))
                          return;
                        setState(() => busy = true);
                        try {
                          await api.command('bookings/${widget.id}/no-show');
                        } catch (e) {
                          if (context.mounted)
                            showMessage(context, staffError(e));
                        } finally {
                          if (mounted) setState(() => busy = false);
                        }
                      },
              ),
          ],
        );
      },
    ),
  );
}

String paymentKind(String value) => switch (value) {
  'Deposit' => 'Tiền cọc',
  'Remaining' => 'Tiền còn lại',
  'Refund' => 'Hoàn tiền',
  _ => value,
};
String paymentMethod(String value) => switch (value) {
  'Cash' => 'Tiền mặt',
  'BankTransfer' => 'Chuyển khoản',
  _ => value,
};

class StaffPaymentSummary extends StatelessWidget {
  const StaffPaymentSummary({super.key, required this.payment});
  final Json payment;
  @override
  Widget build(BuildContext context) => StaffPanel(
    title: 'Thanh toán',
    children: [
      for (final item in [
        ('totalAmount', 'Tổng tiền'),
        ('paidAmount', 'Đã thanh toán'),
        ('remainingAmount', 'Còn phải thu'),
      ])
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 8),
          child: Row(
            children: [
              Expanded(child: Text(item.$2)),
              PriceText(
                amount(payment[item.$1]),
                large: item.$1 == 'remainingAmount',
              ),
            ],
          ),
        ),
    ],
  );
}

Future<void> collectRemaining(BuildContext context, String id) =>
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => RemainingPaymentSheet(id: id),
    );

class RemainingPaymentSheet extends StatefulWidget {
  const RemainingPaymentSheet({super.key, required this.id});
  final String id;
  @override
  State<RemainingPaymentSheet> createState() => _RemainingPaymentSheetState();
}

class _RemainingPaymentSheetState extends State<RemainingPaymentSheet> {
  String method = 'Cash';
  bool busy = false;
  String? error;
  late Future<Json> detail;
  @override
  void initState() {
    super.initState();
    detail = context.read<StaffApi>().get('bookings/${widget.id}');
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !busy,
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: FutureBuilder<Json>(
        future: detail,
        builder: (context, snapshot) {
          if (snapshot.hasError)
            return NoticeState(
              title: staffError(snapshot.error!),
              action: () => setState(
                () => detail = context.read<StaffApi>().get(
                  'bookings/${widget.id}',
                ),
              ),
            );
          if (!snapshot.hasData)
            return const SizedBox(
              height: 180,
              child: Center(child: CircularProgressIndicator()),
            );
          final data = snapshot.data!;
          final payment = data['payment'] as Json;
          return SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Thu tiền còn lại',
                  style: TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 16),
                PriceText(amount(payment['remainingAmount']), large: true),
                const SizedBox(height: 12),
                const Text(
                  'Chỉ xác nhận sau khi đã nhận đủ tiền mặt hoặc kiểm tra tiền vào tài khoản trung tâm.',
                ),
                const SizedBox(height: 16),
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(value: 'Cash', label: Text('Tiền mặt')),
                    ButtonSegment(
                      value: 'BankTransfer',
                      label: Text('Chuyển khoản'),
                    ),
                  ],
                  selected: {method},
                  onSelectionChanged: busy
                      ? null
                      : (s) => setState(() => method = s.single),
                ),
                if (error != null)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    child: Text(
                      error!,
                      style: const TextStyle(color: Colors.red),
                    ),
                  ),
                const SizedBox(height: 24),
                PrimaryButton(
                  label:
                      'Đã nhận ${CurrencyUtils.formatVnd(amount(payment['remainingAmount']))}',
                  loading: busy,
                  onPressed: !canCollect(data['booking'], payment)
                      ? null
                      : () async {
                          setState(() {
                            busy = true;
                            error = null;
                          });
                          try {
                            await context.read<StaffApi>().command(
                              'bookings/${widget.id}/payments/remaining',
                              {'paymentMethod': method},
                            );
                            if (context.mounted) Navigator.pop(context);
                          } catch (e) {
                            if (mounted)
                              setState(() {
                                error = staffError(e);
                                detail = context.read<StaffApi>().get(
                                  'bookings/${widget.id}',
                                );
                              });
                          } finally {
                            if (mounted) setState(() => busy = false);
                          }
                        },
                ),
                TextButton(
                  onPressed: busy ? null : () => Navigator.pop(context),
                  child: const Text('Quay lại'),
                ),
              ],
            ),
          );
        },
      ),
    ),
  );
}

class StaffVerifyPage extends StatefulWidget {
  const StaffVerifyPage({super.key, required this.token});
  final String token;
  @override
  State<StaffVerifyPage> createState() => _StaffVerifyPageState();
}

class _StaffVerifyPageState extends State<StaffVerifyPage> {
  bool busy = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Xác minh lượt đặt'),
    body: StaffData(
      identity: 'verify-${widget.token}',
      load: (api) => api.verify(widget.token),
      builder: (data, reload) {
        final api = context.read<StaffApi>();
        final payment = data['payment'] as Json;
        return StaffList(
          refresh: reload,
          children: [
            StaffPanel(
              title: data['alreadyCheckedIn'] == true
                  ? 'Khách đã check-in'
                  : 'QR hợp lệ',
              children: [
                StatusChip(data['bookingStatus']),
                const SizedBox(height: 12),
                Text(data['bookingCode']),
                const SizedBox(height: 8),
                Text(
                  '${data['customer']['fullName']} · ${data['customer']['phoneNumber']}',
                ),
                Text('${data['court']['name']} · ${data['sport']['name']}'),
                Text(api.interval(data)),
              ],
            ),
            StaffPaymentSummary(payment: payment),
            if (amount(payment['remainingAmount']) > 0 &&
                data['alreadyCheckedIn'] != true)
              PrimaryButton(
                label: 'Thu tiền còn lại',
                onPressed: busy
                    ? null
                    : () => collectRemaining(context, data['bookingId']),
              ),
            if (canCheckIn(data))
              PrimaryButton(
                label: 'Xác nhận check-in',
                loading: busy,
                onPressed: () async {
                  if (busy) return;
                  if (!await staffConfirm(
                    context,
                    'Xác nhận khách vào sân',
                    'Đã đối chiếu khách và lượt đặt ${data['bookingCode']}?',
                  ))
                    return;
                  setState(() => busy = true);
                  try {
                    final latest = await api.verify(widget.token);
                    if (!canCheckIn(latest)) {
                      reload();
                      if (context.mounted)
                        showMessage(
                          context,
                          'Trạng thái đã thay đổi. Vui lòng kiểm tra lại.',
                        );
                      return;
                    }
                    await api.command(
                      'bookings/${latest['bookingId']}/check-ins',
                      {'qrToken': widget.token},
                    );
                    if (context.mounted) {
                      showMessage(context, 'Check-in thành công');
                      context.go('/staff/bookings/${latest['bookingId']}');
                    }
                  } catch (e) {
                    if (context.mounted) {
                      showMessage(context, staffError(e));
                      reload();
                    }
                  } finally {
                    if (mounted) setState(() => busy = false);
                  }
                },
              ),
            SecondaryButton(
              label: 'Chi tiết lịch đặt',
              onPressed: () =>
                  context.push('/staff/bookings/${data['bookingId']}'),
            ),
            TextButton(
              onPressed: busy ? null : () => context.go('/staff/check-in'),
              child: const Text('Quét mã khác'),
            ),
          ],
        );
      },
    ),
  );
}
