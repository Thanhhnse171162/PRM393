import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../../core/network/api_client.dart';
import '../../data/vnpay_service.dart';

class VnPayPaymentPage extends StatefulWidget {
  const VnPayPaymentPage({super.key, required this.bookingId});

  final String bookingId;

  @override
  State<VnPayPaymentPage> createState() => _VnPayPaymentPageState();
}

class _VnPayPaymentPageState extends State<VnPayPaymentPage>
    with WidgetsBindingObserver {
  late final VnPayService _service;
  String? _paymentId;
  String? _paymentUrl;
  String? _status;
  String? _error;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _service = VnPayService(context.read<ApiClient>());
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && _paymentId != null) {
      _refresh();
    }
  }

  Future<void> _pay() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      if (_paymentUrl == null) {
        final result = await _service.create(widget.bookingId);
        if (!mounted) return;
        final data = result.when(
          success: (data) => data,
          failure: (error) {
            throw error;
          },
        );
        _paymentId = data['paymentId'] as String;
        _paymentUrl = data['paymentUrl'] as String;
      }
      final url = Uri.parse(_paymentUrl!);
      if (url.scheme != 'https' ||
          !await launchUrl(url, mode: LaunchMode.externalApplication)) {
        throw StateError('Không thể mở cổng VNPay.');
      }
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _refresh() async {
    final paymentId = _paymentId;
    if (paymentId == null || _busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    final result = await _service.status(paymentId);
    if (!mounted) return;
    setState(() {
      result.when(
        success: (data) {
          _status = data['status'] as String?;
          if (_status == 'Failed') _paymentUrl = null;
        },
        failure: (error) => _error = error.message,
      );
      _busy = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final message = switch (_status) {
      'DepositPaid' => 'Đã thanh toán cọc. Đặt sân đã được xác nhận.',
      'RefundPending' => 'Thanh toán đến sau khi giữ sân hết hạn. Vui lòng liên hệ để hoàn tiền.',
      'Failed' => 'Thanh toán chưa thành công. Bạn có thể thử lại.',
      _ => 'Thanh toán cọc cho đơn đặt sân bằng VNPay.',
    };
    return Scaffold(
      appBar: AppBar(title: const Text('Thanh toán VNPay')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(message),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(
                _error!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
            const SizedBox(height: 24),
            if (_busy) const Center(child: CircularProgressIndicator()),
            if (_status != 'DepositPaid' && _status != 'RefundPending')
              ElevatedButton(
                onPressed: _busy ? null : _pay,
                child: const Text('Thanh toán với VNPay'),
              ),
            if (_paymentId != null)
              TextButton(
                onPressed: _busy ? null : _refresh,
                child: const Text('Kiểm tra trạng thái thanh toán'),
              ),
          ],
        ),
      ),
    );
  }
}
