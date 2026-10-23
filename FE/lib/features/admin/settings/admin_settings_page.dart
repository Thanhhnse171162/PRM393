import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminSettingsPage extends StatefulWidget {
  const AdminSettingsPage({super.key});

  @override
  State<AdminSettingsPage> createState() => _AdminSettingsPageState();
}

class _AdminSettingsPageState extends State<AdminSettingsPage> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _holdMinutesCtrl;
  late final TextEditingController _leadMinutesCtrl;
  late final TextEditingController _depositPercentCtrl;
  bool _allowOutstandingCheckIn = false;

  bool _loading = true;
  bool _saving = false;
  String? _error;
  SystemSettingsData? _currentSettings;

  @override
  void initState() {
    super.initState();
    _holdMinutesCtrl = TextEditingController(text: '10');
    _leadMinutesCtrl = TextEditingController(text: '30');
    _depositPercentCtrl = TextEditingController(text: '30');
    _loadSettings();
  }

  @override
  void dispose() {
    _holdMinutesCtrl.dispose();
    _leadMinutesCtrl.dispose();
    _depositPercentCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadSettings() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final s = await context.read<AdminApi>().getSystemSettings();
      if (mounted) {
        setState(() {
          _currentSettings = s;
          _holdMinutesCtrl.text = s.holdDurationMinutes.toString();
          _leadMinutesCtrl.text = s.minBookingLeadMinutes.toString();
          _depositPercentCtrl.text = s.defaultDepositPercent.toStringAsFixed(0);
          _allowOutstandingCheckIn = s.allowOutstandingCheckIn;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải cấu hình hệ thống.';
        });
      }
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);

    final hold = int.tryParse(_holdMinutesCtrl.text.trim()) ?? 10;
    final lead = int.tryParse(_leadMinutesCtrl.text.trim()) ?? 30;
    final deposit = double.tryParse(_depositPercentCtrl.text.trim()) ?? 30.0;

    final payload = {
      'holdDurationMinutes': hold,
      'minBookingLeadMinutes': lead,
      'defaultDepositPercent': deposit,
      'allowOutstandingCheckIn': _allowOutstandingCheckIn,
    };

    try {
      final updated =
          await context.read<AdminApi>().updateSystemSettings(payload);
      if (mounted) {
        setState(() => _currentSettings = updated);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã cập nhật cấu hình hệ thống!')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'Cập nhật thất bại'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Cấu hình hệ thống',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadSettings,
          ),
        ],
      ),
      body: _loading
          ? const LoadingWidget()
          : _error != null
              ? ErrorState(message: _error!, onRetry: _loadSettings)
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // Impact helper notice
                        Container(
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            color: AppColors.lightMint,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: AppColors.primary.withValues(alpha: 0.2),
                            ),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.info_outline,
                                  color: AppColors.primary, size: 24),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Text(
                                  'Lưu ý: Các thay đổi cấu hình chỉ áp dụng cho các đơn đặt sân mới tạo. Không thay đổi hoặc ghi đè các đơn đã tạo trước đó.',
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w500,
                                    color: AppColors.primaryDark,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),

                        AdminCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Quy tắc giữ chỗ & Đặt sân',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 14),

                              // Hold Duration
                              TextFormField(
                                controller: _holdMinutesCtrl,
                                keyboardType: TextInputType.number,
                                decoration: const InputDecoration(
                                  labelText: 'Thời gian giữ sân tạm thời (Phút) *',
                                  hintText: '10',
                                  prefixIcon: Icon(Icons.timer_outlined, size: 20),
                                  border: OutlineInputBorder(),
                                  helperText:
                                      'Khoảng thời gian khách có để hoàn tất thanh toán trước khi khung giờ bị giải phóng.',
                                ),
                                validator: (v) {
                                  final val = int.tryParse(v ?? '');
                                  if (val == null || val <= 0) {
                                    return 'Phải là số nguyên dương';
                                  }
                                  return null;
                                },
                              ),
                              const SizedBox(height: 16),

                              // Min Booking Lead
                              TextFormField(
                                controller: _leadMinutesCtrl,
                                keyboardType: TextInputType.number,
                                decoration: const InputDecoration(
                                  labelText: 'Thời gian đặt trước tối thiểu (Phút) *',
                                  hintText: '30',
                                  prefixIcon: Icon(Icons.alarm, size: 20),
                                  border: OutlineInputBorder(),
                                  helperText:
                                      'Khách phải đặt trước giờ bắt đầu chơi ít nhất số phút này.',
                                ),
                                validator: (v) {
                                  final val = int.tryParse(v ?? '');
                                  if (val == null || val < 0) {
                                    return 'Phải là số nguyên không âm';
                                  }
                                  return null;
                                },
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),

                        AdminCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Đặt cọc & Thanh toán tại quầy',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 14),

                              // Default Deposit Percent
                              TextFormField(
                                controller: _depositPercentCtrl,
                                keyboardType: TextInputType.number,
                                decoration: const InputDecoration(
                                  labelText: 'Tỷ lệ tiền cọc mặc định (%) *',
                                  hintText: '30',
                                  prefixIcon: Icon(Icons.percent, size: 20),
                                  border: OutlineInputBorder(),
                                  helperText:
                                      'Tỷ lệ phần trăm tổng tiền mà khách bắt buộc phải thanh toán online để xác nhận đơn.',
                                ),
                                validator: (v) {
                                  final val = double.tryParse(v ?? '');
                                  if (val == null || val <= 0 || val > 100) {
                                    return 'Tỷ lệ cọc phải từ 1 đến 100%';
                                  }
                                  return null;
                                },
                              ),
                              const SizedBox(height: 16),

                              // Allow Outstanding Check-in Switch
                              SwitchListTile(
                                contentPadding: EdgeInsets.zero,
                                title: const Text(
                                  'Cho phép check-in khi còn nợ tiền',
                                  style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                                subtitle: const Text(
                                  'Nếu bật, nhân viên có thể cho khách vào sân ngay cả khi chưa thu hết tiền còn lại.',
                                  style: TextStyle(fontSize: 12),
                                ),
                                activeThumbColor: AppColors.primary,
                                value: _allowOutstandingCheckIn,
                                onChanged: (v) => setState(
                                    () => _allowOutstandingCheckIn = v),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 24),

                        ElevatedButton(
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            foregroundColor: AppColors.white,
                            padding: const EdgeInsets.symmetric(vertical: 16),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                          ),
                          onPressed: _saving ? null : _save,
                          child: _saving
                              ? const SizedBox(
                                  height: 20,
                                  width: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: AppColors.white,
                                  ),
                                )
                              : const Text(
                                  'Lưu cấu hình hệ thống',
                                  style: TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                        ),
                        if (_currentSettings?.updatedAt.isNotEmpty == true) ...[
                          const SizedBox(height: 14),
                          Center(
                            child: Text(
                              'Cập nhật lần cuối: ${AdminFormat.dateTime(_currentSettings!.updatedAt)}',
                              style: const TextStyle(
                                fontSize: 11,
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ),
                        ],
                        const SizedBox(height: 24),
                      ],
                    ),
                  ),
                ),
    );
  }
}
