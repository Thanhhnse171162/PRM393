import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../shared/widgets/customer_widgets.dart';

bool validStaffQr(String value) =>
    RegExp(r'^[A-Za-z0-9_-]{16,150}$').hasMatch(value);

class StaffScannerPage extends StatefulWidget {
  const StaffScannerPage({super.key});
  @override
  State<StaffScannerPage> createState() => _StaffScannerPageState();
}

class _StaffScannerPageState extends State<StaffScannerPage> {
  final token = TextEditingController();
  @override
  void dispose() {
    token.dispose();
    super.dispose();
  }

  void verify(String raw) {
    final value = raw.trim();
    if (!validStaffQr(value)) {
      showMessage(
        context,
        'Mã QR không đúng định dạng. Vui lòng dùng QR trong chi tiết lịch đặt của khách.',
      );
      return;
    }
    context.push('/staff/verify', extra: value);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Quét QR'),
    body: ListView(
      padding: const EdgeInsets.all(24),
      children: [
        const SizedBox(height: 24),
        const Icon(Icons.qr_code_scanner, size: 112),
        const SizedBox(height: 24),
        const Text(
          'Xác minh khách đến sân',
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 12),
        const Text(
          'Quét mã QR trên ứng dụng của khách để xem lượt đặt và số tiền cần thu. Sau khi xác minh, bạn xác nhận check-in riêng.',
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 24),
        PrimaryButton(
          label: 'Mở camera quét QR',
          onPressed: () async {
            final value = await context.push<String>('/staff/camera');
            if (value != null && mounted) verify(value);
          },
        ),
        const SizedBox(height: 32),
        TextField(
          controller: token,
          maxLength: 150,
          autocorrect: false,
          decoration: const InputDecoration(
            labelText: 'Hoặc dán nội dung mã QR',
            helperText: 'Nội dung QR; không phải mã đặt sân',
          ),
        ),
        const SizedBox(height: 12),
        SecondaryButton(
          label: 'Xác minh QR',
          onPressed: () => verify(token.text),
        ),
      ],
    ),
  );
}

class StaffCameraPage extends StatefulWidget {
  const StaffCameraPage({super.key});
  @override
  State<StaffCameraPage> createState() => _StaffCameraPageState();
}

class _StaffCameraPageState extends State<StaffCameraPage> {
  final scanner = MobileScannerController(
    formats: [BarcodeFormat.qrCode],
    detectionSpeed: DetectionSpeed.noDuplicates,
  );
  bool detected = false;
  @override
  void dispose() {
    scanner.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(
      title: 'Đưa QR vào khung',
      actions: [
        IconButton(
          tooltip: 'Bật / tắt đèn',
          onPressed: scanner.toggleTorch,
          icon: const Icon(Icons.flash_on),
        ),
      ],
    ),
    body: Column(
      children: [
        Expanded(
          child: MobileScanner(
            controller: scanner,
            errorBuilder: (context, error) => Center(
              child: NoticeState(
                title: 'Chưa thể mở camera',
                message: 'Cho phép CourtGo dùng camera trong cài đặt ứng dụng, sau đó thử lại. Bạn vẫn có thể dán nội dung QR.',
                action: () => scanner.start(),
              ),
            ),
            onDetect: (capture) {
              if (detected) return;
              for (final barcode in capture.barcodes) {
                final value = barcode.rawValue;
                if (value != null) {
                  detected = true;
                  context.pop(value);
                  break;
                }
              }
            },
          ),
        ),
        const SafeArea(
          child: Padding(
            padding: EdgeInsets.all(20),
            child: Text('Giữ điện thoại ổn định. Không cần chạm vào mã QR.'),
          ),
        ),
      ],
    ),
  );
}
