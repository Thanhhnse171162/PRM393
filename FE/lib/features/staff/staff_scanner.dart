import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_colors.dart';
import '../../shared/widgets/customer_widgets.dart';
import 'staff_api.dart';

bool validStaffQr(String value) =>
    RegExp(r'^[A-Za-z0-9_-]{16,150}$').hasMatch(value);

class StaffScannerPage extends StatefulWidget {
  const StaffScannerPage({super.key});
  @override
  State<StaffScannerPage> createState() => _StaffScannerPageState();
}

class _StaffScannerPageState extends State<StaffScannerPage>
    with SingleTickerProviderStateMixin {
  late final MobileScannerController _controller;
  late final AnimationController _laserController;
  late final Animation<double> _laserAnimation;
  final TextEditingController _manualController = TextEditingController();
  final ImagePicker _imagePicker = ImagePicker();

  bool _isProcessing = false;
  bool _isTorchOn = false;
  bool _hasCameraError = false;
  String? _cameraErrorMessage;

  @override
  void initState() {
    super.initState();
    _controller = MobileScannerController(
      formats: const [BarcodeFormat.qrCode],
      detectionSpeed: DetectionSpeed.noDuplicates,
    );
    _laserController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2200),
    )..repeat(reverse: true);
    _laserAnimation = Tween<double>(begin: 0.05, end: 0.95).animate(
      CurvedAnimation(parent: _laserController, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    _laserController.dispose();
    _manualController.dispose();
    super.dispose();
  }

  Future<void> _toggleTorch() async {
    try {
      await _controller.toggleTorch();
      if (mounted) setState(() => _isTorchOn = !_isTorchOn);
    } catch (_) {}
  }

  Future<void> _switchCamera() async {
    try {
      await _controller.switchCamera();
    } catch (_) {}
  }

  Future<void> _handleRawValue(
    String raw, {
    bool isFromManualEntry = false,
  }) async {
    final value = raw.trim();
    if (value.isEmpty) {
      showMessage(context, 'Vui lòng cung cấp mã hợp lệ.');
      return;
    }
    if (_isProcessing) return;

    setState(() => _isProcessing = true);

    try {
      final api = context.read<StaffApi>();

      // Trường hợp 1: Token QR thật (chuỗi hex hoặc token bảo mật từ QR)
      if (validStaffQr(value) && !value.toUpperCase().startsWith('CG-')) {
        try {
          await api.verify(value);
          if (mounted) {
            context.push('/staff/verify', extra: value);
          }
          return;
        } catch (e) {
          // Nếu verify token thất bại, thử kiểm tra xem có phải mã booking không trước khi báo lỗi
          final searchResult = await api.schedule(1, search: value);
          if (searchResult.items.isNotEmpty) {
            final first = searchResult.items.first;
            if (mounted) {
              showMessage(
                context,
                'Đã tìm thấy lịch đặt ${first['bookingCode']}.',
              );
              context.push('/staff/bookings/${first['bookingId']}');
            }
            return;
          }
          if (mounted) {
            showMessage(
              context,
              'Mã QR không đúng định dạng hoặc không hợp lệ. Vui lòng dùng QR trong chi tiết lịch đặt của khách.',
            );
          }
          return;
        }
      }

      // Trường hợp 2: Mã đặt sân (ví dụ: CG-20261021-3WRXH3) hoặc mã tìm kiếm
      final searchResult = await api.schedule(1, search: value);
      if (searchResult.items.isNotEmpty) {
        final match = searchResult.items.firstWhere(
          (b) =>
              (b['bookingCode'] as String? ?? '').toUpperCase() ==
              value.toUpperCase(),
          orElse: () => searchResult.items.first,
        );
        if (mounted) {
          showMessage(
            context,
            'Đã tìm thấy lịch đặt ${match['bookingCode']}. Mở chi tiết để đối chiếu.',
          );
          context.push('/staff/bookings/${match['bookingId']}');
        }
        return;
      }

      // Dự phòng: thử verify với backend một lần nữa
      try {
        await api.verify(value);
        if (mounted) {
          context.push('/staff/verify', extra: value);
        }
        return;
      } catch (_) {}

      if (mounted) {
        showMessage(
          context,
          'Không tìm thấy lịch đặt với mã "$value". Vui lòng kiểm tra lại.',
        );
      }
    } catch (e) {
      if (mounted) {
        showMessage(context, staffError(e));
      }
    } finally {
      if (mounted) {
        Future.delayed(const Duration(milliseconds: 1500), () {
          if (mounted) setState(() => _isProcessing = false);
        });
      }
    }
  }

  Future<void> _pickImageAndDecode() async {
    try {
      final XFile? image = await _imagePicker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 1600,
        maxHeight: 1600,
      );
      if (image == null) return;

      setState(() => _isProcessing = true);
      final BarcodeCapture? capture = await _controller.analyzeImage(
        image.path,
      );

      if (capture == null || capture.barcodes.isEmpty) {
        if (mounted) {
          setState(() => _isProcessing = false);
          showMessage(
            context,
            'Không tìm thấy mã QR trong ảnh. Vui lòng chọn ảnh chụp rõ nét.',
          );
        }
        return;
      }

      final String? raw = capture.barcodes.first.rawValue;
      if (raw == null || raw.trim().isEmpty) {
        if (mounted) {
          setState(() => _isProcessing = false);
          showMessage(context, 'Mã QR trong ảnh rỗng hoặc không đọc được.');
        }
        return;
      }

      _isProcessing = false;
      await _handleRawValue(raw);
    } catch (e) {
      if (mounted) {
        setState(() => _isProcessing = false);
        showMessage(context, 'Lỗi đọc ảnh QR: ${e.toString()}');
      }
    }
  }

  void _showManualEntrySheet() {
    _manualController.clear();
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.only(
          left: 20,
          right: 20,
          top: 24,
          bottom: MediaQuery.of(sheetContext).viewInsets.bottom + 24,
        ),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Nhập mã đặt sân',
                    style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
                  ),
                  IconButton(
                    onPressed: () => Navigator.pop(sheetContext),
                    icon: const Icon(Icons.close),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              const Text(
                'Nhập mã booking (ví dụ: CG-20261021-3WRXH3) hoặc nội dung mã QR token của khách để xác minh.',
                style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
              ),
              const SizedBox(height: 18),
              TextField(
                controller: _manualController,
                autofocus: true,
                autocorrect: false,
                textCapitalization: TextCapitalization.characters,
                decoration: InputDecoration(
                  labelText: 'Mã đặt sân hoặc QR raw value',
                  hintText: 'Ví dụ: CG-20261021-3WRXH3',
                  prefixIcon: const Icon(Icons.qr_code_2_rounded),
                  suffixIcon: IconButton(
                    tooltip: 'Dán từ clipboard',
                    icon: const Icon(Icons.content_paste_rounded),
                    onPressed: () async {
                      final data = await Clipboard.getData(
                        Clipboard.kTextPlain,
                      );
                      if (data?.text != null) {
                        _manualController.text = data!.text!.trim();
                      }
                    },
                  ),
                ),
              ),
              const SizedBox(height: 20),
              PrimaryButton(
                label: 'Xác minh ngay',
                onPressed: () {
                  final text = _manualController.text.trim();
                  if (text.isEmpty) {
                    showMessage(context, 'Vui lòng nhập mã đặt sân.');
                    return;
                  }
                  Navigator.pop(sheetContext);
                  _handleRawValue(text, isFromManualEntry: true);
                },
              ),
              const SizedBox(height: 8),
              TextButton(
                onPressed: () => Navigator.pop(sheetContext),
                child: const Text('Hủy'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    const double scanSize = 250.0;

    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        title: const Text(
          'Quét mã đặt sân',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.w700,
            color: Colors.white,
          ),
        ),
        backgroundColor: Colors.black,
        iconTheme: const IconThemeData(color: Colors.white),
        leading: Navigator.of(context).canPop()
            ? IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () => Navigator.of(context).maybePop(),
              )
            : null,
        actions: [
          IconButton(
            tooltip: _isTorchOn ? 'Tắt đèn' : 'Bật đèn',
            icon: Icon(
              _isTorchOn ? Icons.flash_on_rounded : Icons.flash_off_rounded,
              color: _isTorchOn ? AppColors.warning : Colors.white,
            ),
            onPressed: _toggleTorch,
          ),
          IconButton(
            tooltip: 'Đổi camera',
            icon: const Icon(Icons.flip_camera_ios_outlined),
            onPressed: _switchCamera,
          ),
        ],
      ),
      body: Column(
        children: [
          // Khu vực Camera Viewfinder
          Expanded(
            flex: 6,
            child: Stack(
              fit: StackFit.expand,
              children: [
                MobileScanner(
                  controller: _controller,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error) {
                    _hasCameraError = true;
                    _cameraErrorMessage = error.errorDetails?.message;
                    return Container(
                      color: const Color(0xFF1E293B),
                      padding: const EdgeInsets.all(24),
                      child: Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(
                              Icons.videocam_off_rounded,
                              size: 54,
                              color: AppColors.mint,
                            ),
                            const SizedBox(height: 16),
                            const Text(
                              'Chưa thể mở camera',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 18,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              _cameraErrorMessage ?? 'Cho phép CourtGo dùng camera trong cài đặt hoặc sử dụng phương án chọn ảnh / nhập mã bên dưới.',
                              textAlign: TextAlign.center,
                              style: const TextStyle(
                                color: Colors.white70,
                                fontSize: 13,
                              ),
                            ),
                            const SizedBox(height: 20),
                            ElevatedButton.icon(
                              style: ElevatedButton.styleFrom(
                                backgroundColor: AppColors.primary,
                                foregroundColor: Colors.white,
                              ),
                              icon: const Icon(Icons.refresh_rounded),
                              label: const Text('Cấp quyền & Thử lại'),
                              onPressed: () {
                                setState(() => _hasCameraError = false);
                                _controller.start();
                              },
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                  onDetect: (capture) {
                    if (_isProcessing) return;
                    for (final barcode in capture.barcodes) {
                      final raw = barcode.rawValue;
                      if (raw != null && raw.trim().isNotEmpty) {
                        _handleRawValue(raw);
                        break;
                      }
                    }
                  },
                ),

                // Overlay khung quét QR
                if (!_hasCameraError)
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final rect = Rect.fromCenter(
                        center: Offset(
                          constraints.maxWidth / 2,
                          constraints.maxHeight / 2 - 20,
                        ),
                        width: scanSize,
                        height: scanSize,
                      );
                      return AnimatedBuilder(
                        animation: _laserAnimation,
                        builder: (context, child) => CustomPaint(
                          size: Size(
                            constraints.maxWidth,
                            constraints.maxHeight,
                          ),
                          painter: _ScannerOverlayPainter(
                            scanWindow: rect,
                            laserPosition: _laserAnimation.value,
                            cornerColor: AppColors.primary,
                          ),
                        ),
                      );
                    },
                  ),

                // Dòng hướng dẫn bên dưới khung quét
                Align(
                  alignment: Alignment.bottomCenter,
                  child: Container(
                    margin: const EdgeInsets.only(bottom: 24),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 20,
                      vertical: 10,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.black.withValues(alpha: 0.65),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: const [
                        Text(
                          'Đưa mã QR của khách vào trong khung',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          'Hệ thống sẽ tự động xác minh booking',
                          style: TextStyle(color: Colors.white70, fontSize: 12),
                        ),
                      ],
                    ),
                  ),
                ),

                // Trạng thái đang xác minh
                if (_isProcessing)
                  Container(
                    color: Colors.black.withValues(alpha: 0.6),
                    child: Center(
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 24,
                          vertical: 18,
                        ),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: const [
                            SizedBox(
                              width: 22,
                              height: 22,
                              child: CircularProgressIndicator(
                                strokeWidth: 2.5,
                              ),
                            ),
                            SizedBox(width: 14),
                            Text(
                              'Đang xác minh booking...',
                              style: TextStyle(
                                fontWeight: FontWeight.w600,
                                fontSize: 14,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),

          // Khu vực hành động thay thế (Action card & Demo tools)
          Expanded(
            flex: 5,
            child: Container(
              width: double.infinity,
              decoration: const BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
              ),
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(20, 20, 20, 16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: const [
                        Icon(
                          Icons.dashboard_customize_outlined,
                          size: 18,
                          color: AppColors.primary,
                        ),
                        SizedBox(width: 8),
                        Text(
                          'Phương thức xác minh',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 14),

                    // Hai nút thao tác chính
                    Row(
                      children: [
                        // 1. Chọn ảnh QR
                        Expanded(
                          child: _ActionCard(
                            icon: Icons.photo_library_outlined,
                            title: 'Chọn ảnh QR',
                            subtitle: 'Từ thư viện ảnh',
                            onTap: _pickImageAndDecode,
                          ),
                        ),
                        const SizedBox(width: 12),
                        // 2. Nhập mã booking
                        Expanded(
                          child: _ActionCard(
                            icon: Icons.keyboard_outlined,
                            title: 'Nhập mã booking',
                            subtitle: 'ID / Mã đặt sân',
                            onTap: _showManualEntrySheet,
                          ),
                        ),
                      ],
                    ),

                    const SizedBox(height: 14),

                    // Nút hỗ trợ mở lại camera hoặc chuyển màn camera
                    SizedBox(
                      height: 44,
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.qr_code_scanner, size: 18),
                        label: const Text('Mở camera quét QR'),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: AppColors.primary,
                          side: const BorderSide(color: AppColors.primary),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                        ),
                        onPressed: () {
                          _controller.start();
                          setState(() => _hasCameraError = false);
                        },
                      ),
                    ),

                    const SizedBox(height: 14),

                    // Card hướng dẫn demo trên Emulator
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: AppColors.lightMint,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: AppColors.mint, width: 1.2),
                      ),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: const [
                          Icon(
                            Icons.lightbulb_outline_rounded,
                            size: 18,
                            color: AppColors.primaryDark,
                          ),
                          SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'Nếu đang chạy trên emulator, bạn có thể chọn ảnh QR hoặc nhập mã booking (ví dụ: CG-20261021-3WRXH3) để kiểm tra.',
                              style: TextStyle(
                                fontSize: 12.5,
                                height: 1.4,
                                color: AppColors.primaryDark,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ActionCard extends StatelessWidget {
  const _ActionCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(14),
    child: Ink(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppColors.mint,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, size: 22, color: AppColors.primary),
          ),
          const SizedBox(height: 10),
          Text(
            title,
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            subtitle,
            style: const TextStyle(
              fontSize: 11.5,
              color: AppColors.textSecondary,
            ),
          ),
        ],
      ),
    ),
  );
}

class _ScannerOverlayPainter extends CustomPainter {
  _ScannerOverlayPainter({
    required this.scanWindow,
    required this.laserPosition,
    required this.cornerColor,
  });

  final Rect scanWindow;
  final double laserPosition;
  final Color cornerColor;

  @override
  void paint(Canvas canvas, Size size) {
    // 1. Phủ mờ xung quanh vùng quét
    final backgroundPaint = Paint()
      ..color = Colors.black.withValues(alpha: 0.55);
    final backgroundPath = Path()
      ..addRect(Rect.fromLTWH(0, 0, size.width, size.height));
    final cutoutPath = Path()
      ..addRRect(
        RRect.fromRectAndRadius(scanWindow, const Radius.circular(20)),
      );
    final overlayPath = Path.combine(
      PathOperation.difference,
      backgroundPath,
      cutoutPath,
    );
    canvas.drawPath(overlayPath, backgroundPaint);

    // 2. Viền góc khung quét
    final cornerPaint = Paint()
      ..color = cornerColor
      ..style = PaintingStyle.stroke
      ..strokeWidth = 4.0
      ..strokeCap = StrokeCap.round;

    final r = scanWindow;
    const cornerLength = 28.0;
    const cornerRadius = 16.0;

    // Góc trên bên trái
    final tlPath = Path()
      ..moveTo(r.left, r.top + cornerLength)
      ..lineTo(r.left, r.top + cornerRadius)
      ..arcToPoint(
        Offset(r.left + cornerRadius, r.top),
        radius: const Radius.circular(cornerRadius),
      )
      ..lineTo(r.left + cornerLength, r.top);
    canvas.drawPath(tlPath, cornerPaint);

    // Góc trên bên phải
    final trPath = Path()
      ..moveTo(r.right - cornerLength, r.top)
      ..lineTo(r.right - cornerRadius, r.top)
      ..arcToPoint(
        Offset(r.right, r.top + cornerRadius),
        radius: const Radius.circular(cornerRadius),
      )
      ..lineTo(r.right, r.top + cornerLength);
    canvas.drawPath(trPath, cornerPaint);

    // Góc dưới bên trái
    final blPath = Path()
      ..moveTo(r.left, r.bottom - cornerLength)
      ..lineTo(r.left, r.bottom - cornerRadius)
      ..arcToPoint(
        Offset(r.left + cornerRadius, r.bottom),
        radius: const Radius.circular(cornerRadius),
      )
      ..lineTo(r.left + cornerLength, r.bottom);
    canvas.drawPath(blPath, cornerPaint);

    // Góc dưới bên phải
    final brPath = Path()
      ..moveTo(r.right - cornerLength, r.bottom)
      ..lineTo(r.right - cornerRadius, r.bottom)
      ..arcToPoint(
        Offset(r.right, r.bottom + cornerRadius),
        radius: const Radius.circular(cornerRadius),
      )
      ..lineTo(r.right, r.bottom - cornerLength);
    canvas.drawPath(brPath, cornerPaint);

    // 3. Đường laser quét chuyển động
    final laserY = r.top + (r.height * laserPosition);
    final laserPaint = Paint()
      ..shader = LinearGradient(
        colors: [
          cornerColor.withValues(alpha: 0.0),
          cornerColor,
          cornerColor.withValues(alpha: 0.0),
        ],
      ).createShader(Rect.fromLTWH(r.left, laserY, r.width, 2.5))
      ..strokeWidth = 2.5;
    canvas.drawLine(
      Offset(r.left + 10, laserY),
      Offset(r.right - 10, laserY),
      laserPaint,
    );
  }

  @override
  bool shouldRepaint(covariant _ScannerOverlayPainter oldDelegate) =>
      oldDelegate.laserPosition != laserPosition ||
      oldDelegate.scanWindow != scanWindow;
}

/// Trang camera chuyên dụng hoặc fallback route cho /staff/camera
class StaffCameraPage extends StatelessWidget {
  const StaffCameraPage({super.key});

  @override
  Widget build(BuildContext context) => const StaffScannerPage();
}
