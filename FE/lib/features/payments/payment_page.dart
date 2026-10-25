import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import 'payment_links.dart';

import '../../core/api/customer_api.dart';
import '../../core/config/app_config.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';

class PaymentPage extends StatefulWidget {
  const PaymentPage({super.key, required this.id, this.attempt});
  final String id;
  final Json? attempt;
  @override
  State<PaymentPage> createState() => _PaymentPageState();
}

class _PaymentPageState extends State<PaymentPage> with WidgetsBindingObserver {
  Timer? timer;
  Json? status;
  String? error;
  bool checking = false, simulating = false, foreground = true;
  bool opening = false;
  bool recheckRequested = false;
  int polls = 0;
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _check();
    if (widget.attempt?['paymentUrl'] is String) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _openProvider());
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    timer?.cancel();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    foreground = state == AppLifecycleState.resumed;
    timer?.cancel();
    if (foreground) _check(manual: true);
  }

  Future<void> _check({bool manual = false}) async {
    if (checking && manual) recheckRequested = true;
    if (checking || !mounted) return;
    if (manual) polls = 0;
    polls++;
    checking = true;
    try {
      final api = context.read<CustomerApi>();
      var result = await api.paymentStatus(widget.id);
      if (!mounted) return;
      String? verificationError;
      final pendingVnPay = jsonList(result['payments']).any(
        (p) =>
            p['paymentKind'] == 'Deposit' &&
            p['paymentMethod'] == 'VNPay' &&
            p['transactionStatus'] == 'Pending',
      );
      // Recover a lost IPN on return, resume and manual checks. Never create a new payment here.
      if (pendingVnPay && (manual || (widget.attempt == null && polls == 1))) {
        try {
          result = await api.reconcilePayment(widget.id);
        } catch (_) {
          verificationError = 'Chưa xác minh được với VNPAY. Vui lòng kiểm tra lại sau 5 phút; không cần thanh toán thêm.';
        }
      }
      if (!mounted) return;
      setState(() {
        status = result;
        error = verificationError;
      });
      final paidDeposit = jsonList(result['payments']).any(
        (p) =>
            p['paymentKind'] == 'Deposit' &&
            p['transactionStatus'] == 'Succeeded',
      );
      if (paidDeposit &&
          [
            'Confirmed',
            'CheckedIn',
            'InProgress',
            'Completed',
          ].contains(result['bookingStatus']) &&
          ['DepositPaid', 'FullyPaid'].contains(result['paymentStatus'])) {
        context.go('/customer/bookings/${widget.id}/success');
        return;
      }
    } on ApiException catch (e) {
      if (mounted) setState(() => error = e.message);
    } catch (_) {
      if (mounted) {
        setState(() => error = 'Chưa lấy được trạng thái thanh toán.');
      }
    } finally {
      checking = false;
      if (mounted && foreground && recheckRequested) {
        recheckRequested = false;
        unawaited(_check(manual: true));
      } else if (mounted && foreground && !terminal && polls < 30) {
        timer?.cancel();
        timer = Timer(const Duration(seconds: 4), _check);
      }
    }
  }

  String get transactionState {
    final payments = jsonList(status?['payments']);
    final id = widget.attempt?['paymentId'];
    final matching = payments
        .where(
          (p) =>
              p['paymentKind'] == 'Deposit' &&
              (id == null || p['paymentId'] == id),
        )
        .toList();
    return matching.isEmpty
        ? 'Pending'
        : matching.last['transactionStatus'] as String;
  }

  bool get terminal =>
      ['Failed', 'Cancelled'].contains(transactionState) ||
      ['Expired', 'Cancelled'].contains(status?['bookingStatus']);
  bool get reconciliation => status?['paymentStatus'] == 'RefundPending';
  Future<void> _openProvider() async {
    if (opening || !mounted || terminal) return;
    setState(() {
      opening = true;
      error = null;
    });
    try {
      String? url = widget.attempt?['paymentUrl'];
      if (url == null) {
        final pending = jsonList(status?['payments'])
            .where(
              (p) =>
                  p['paymentKind'] == 'Deposit' &&
                  p['transactionStatus'] == 'Pending',
            )
            .toList();
        if (pending.isEmpty) throw StateError('No pending payment');
        final attempt = await context.read<CustomerApi>().startDeposit(
          widget.id,
          pending.last['paymentMethod'],
        );
        url = attempt['paymentUrl'];
      }
      final uri = Uri.tryParse(url ?? '');
      if (uri == null ||
          !trustedCheckoutUrl(uri) ||
          !await launchUrl(uri, mode: LaunchMode.externalApplication)) {
        throw StateError('Checkout is unavailable');
      }
    } catch (e) {
      if (mounted) {
        setState(
          () => error = e is ApiException
              ? e.message
              : 'Chưa mở được trang thanh toán. Vui lòng thử lại.',
        );
      }
    } finally {
      if (mounted) {
        setState(() => opening = false);
      }
    }
  }

  Future<void> simulate(String result) async {
    if (simulating || widget.attempt == null) return;
    setState(() {
      simulating = true;
      error = null;
    });
    try {
      await context.read<CustomerApi>().simulate(
        widget.attempt!['paymentId'] as String,
        result,
      );
      await _check();
    } on ApiException catch (e) {
      if (mounted) setState(() => error = e.message);
    } finally {
      if (mounted) setState(() => simulating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final dev =
        kDebugMode &&
        AppConfig.isDev &&
        widget.attempt?['gateway'] == 'Development';
    return Scaffold(
      appBar: CourtGoAppBar(title: 'Trạng thái thanh toán'),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 24),
          Container(
            padding: const EdgeInsets.all(24),
            decoration: const BoxDecoration(
              color: AppColors.lightMint,
              shape: BoxShape.circle,
            ),
            child: Icon(
              terminal ? Icons.info_outline : Icons.hourglass_top_rounded,
              size: 52,
              color: AppColors.primary,
            ),
          ),
          const SizedBox(height: 24),
          Text(
            reconciliation
                ? 'Đang chờ đối soát hoàn tiền'
                : status?['bookingStatus'] == 'Expired'
                ? 'Lượt giữ sân đã hết hạn'
                : transactionState == 'Cancelled'
                ? 'Đã hủy thanh toán'
                : transactionState == 'Failed'
                ? 'Thanh toán chưa thành công'
                : 'Đang chờ xác nhận cọc',
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 23, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 12),
          Center(
            child: StatusChip(
              status?['bookingStatus'] == 'Expired'
                  ? 'Expired'
                  : transactionState,
            ),
          ),
          const SizedBox(height: 16),
          const Text(
            'Lịch đặt chỉ được xác nhận sau khi hệ thống nhận được thanh toán thành công.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textSecondary, height: 1.5),
          ),
          if (widget.attempt != null) ...[
            const SizedBox(height: 20),
            Center(
              child: PriceText(amount(widget.attempt!['amount']), large: true),
            ),
          ],
          if (error != null)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 16),
              child: Text(
                error!,
                style: const TextStyle(color: AppColors.error),
                textAlign: TextAlign.center,
              ),
            ),
          const SizedBox(height: 24),
          if (reconciliation)
            const Padding(
              padding: EdgeInsets.only(bottom: 16),
              child: Text(
                'Đã ghi nhận tiền cọc, nhưng lượt giữ sân đã kết thúc khi hệ thống xác minh. Lịch đặt chưa được xác nhận; yêu cầu hoàn tiền đang chờ xử lý. Không thanh toán lại cho lịch này.',
              ),
            ),
          if (polls >= 30 && !terminal)
            const Padding(
              padding: EdgeInsets.only(bottom: 16),
              child: Text(
                'Chưa có kết quả cuối cùng. Bạn có thể kiểm tra lại hoặc xem trong Lịch đặt. Không cần thanh toán thêm lần nữa.',
              ),
            ),
          if (!dev &&
              !terminal &&
              (widget.attempt?['paymentUrl'] != null ||
                  jsonList(status?['payments'])
                      .any((p) => p['transactionStatus'] == 'Pending'))) ...[
            PrimaryButton(
              label: 'Mở trang thanh toán',
              loading: opening,
              onPressed: _openProvider,
            ),
            const SizedBox(height: 12),
          ],
          SecondaryButton(
            label: 'Kiểm tra trạng thái',
            onPressed: checking ? null : () => _check(manual: true),
          ),
          if (terminal &&
              !reconciliation &&
              status?['bookingStatus'] != 'Cancelled') ...[
            const SizedBox(height: 12),
            PrimaryButton(
              label: status?['bookingStatus'] == 'Expired'
                  ? 'Chọn lại sân'
                  : 'Thử thanh toán lại',
              onPressed: () => context.go(
                status?['bookingStatus'] == 'Expired'
                    ? '/customer/explore'
                    : '/customer/bookings/${widget.id}/checkout',
              ),
            ),
          ],
          if (dev && !terminal) ...[
            const SizedBox(height: 32),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFFFFF4DC),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    'MÔI TRƯỜNG THỬ NGHIỆM',
                    style: TextStyle(
                      fontSize: 11,
                      letterSpacing: 1,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF9A6700),
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Đây là giao dịch giả lập. Không chuyển tiền thật qua MoMo hoặc VNPay.',
                    style: TextStyle(fontSize: 13, height: 1.5),
                  ),
                  const SizedBox(height: 16),
                  PrimaryButton(
                    label: 'Giả lập cọc thành công',
                    loading: simulating,
                    onPressed: () => simulate('success'),
                  ),
                  const SizedBox(height: 8),
                  Wrap(
                    alignment: WrapAlignment.center,
                    children: [
                      TextButton(
                        onPressed: simulating ? null : () => simulate('failed'),
                        child: const Text('Giả lập thất bại'),
                      ),
                      TextButton(
                        onPressed: simulating
                            ? null
                            : () => simulate('cancelled'),
                        child: const Text('Giả lập hủy'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
          const SizedBox(height: 16),
          TextButton(
            onPressed: () => context.go('/customer/bookings'),
            child: const Text('Về lịch đặt'),
          ),
        ],
      ),
    );
  }
}
