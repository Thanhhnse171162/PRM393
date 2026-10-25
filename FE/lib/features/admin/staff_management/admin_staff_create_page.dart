import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminStaffCreatePage extends StatefulWidget {
  final String? initialCenterId;
  const AdminStaffCreatePage({super.key, this.initialCenterId});

  @override
  State<AdminStaffCreatePage> createState() => _AdminStaffCreatePageState();
}

class _AdminStaffCreatePageState extends State<AdminStaffCreatePage> {
  final _formKey = GlobalKey<FormState>();
  final _scrollController = ScrollController();

  final _fullNameCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();

  final _fullNameFocus = FocusNode();
  final _phoneFocus = FocusNode();
  final _emailFocus = FocusNode();
  final _passwordFocus = FocusNode();
  final _centerFocus = FocusNode();

  final _fullNameKey = GlobalKey();
  final _phoneKey = GlobalKey();
  final _emailKey = GlobalKey();
  final _passwordKey = GlobalKey();
  final _centerKey = GlobalKey();

  bool _obscurePassword = true;
  String? _selectedCenterId;
  List<AdminSportCenterItem> _centers = [];
  bool _loadingCenters = true;
  bool _saving = false;
  AutovalidateMode _autovalidateMode = AutovalidateMode.disabled;

  // Server error messages per field
  String? _fullNameServerError;
  String? _phoneServerError;
  String? _emailServerError;
  String? _passwordServerError;
  String? _centerServerError;

  @override
  void initState() {
    super.initState();
    _selectedCenterId = widget.initialCenterId;
    _fullNameCtrl.addListener(_onFullNameChanged);
    _phoneCtrl.addListener(_onPhoneChanged);
    _emailCtrl.addListener(_onEmailChanged);
    _passwordCtrl.addListener(_onPasswordChanged);
    _loadCenters();
  }

  @override
  void dispose() {
    _fullNameCtrl.removeListener(_onFullNameChanged);
    _phoneCtrl.removeListener(_onPhoneChanged);
    _emailCtrl.removeListener(_onEmailChanged);
    _passwordCtrl.removeListener(_onPasswordChanged);

    _fullNameCtrl.dispose();
    _phoneCtrl.dispose();
    _emailCtrl.dispose();
    _passwordCtrl.dispose();

    _fullNameFocus.dispose();
    _phoneFocus.dispose();
    _emailFocus.dispose();
    _passwordFocus.dispose();
    _centerFocus.dispose();

    _scrollController.dispose();
    super.dispose();
  }

  void _onFullNameChanged() {
    if (_fullNameServerError != null) {
      setState(() => _fullNameServerError = null);
    }
  }

  void _onPhoneChanged() {
    if (_phoneServerError != null) {
      setState(() => _phoneServerError = null);
    }
  }

  void _onEmailChanged() {
    if (_emailServerError != null) {
      setState(() => _emailServerError = null);
    }
  }

  void _onPasswordChanged() {
    if (_passwordServerError != null) {
      setState(() => _passwordServerError = null);
    } else {
      setState(() {}); // Update password requirement indicators
    }
  }

  void _loadCenters() async {
    try {
      final res = await context.read<AdminApi>().getCenters(pageSize: 100);
      if (mounted) {
        setState(() {
          _centers = res.items;
          _loadingCenters = false;
          if (_centers.isNotEmpty && _selectedCenterId == null) {
            _selectedCenterId = _centers.first.centerId;
          }
        });
      }
    } catch (_) {
      if (mounted) setState(() => _loadingCenters = false);
    }
  }

  String? _validateFullName(String? v) {
    if (_fullNameServerError != null) return _fullNameServerError;
    final val = v?.trim() ?? '';
    if (val.isEmpty) return 'Vui lòng nhập họ và tên';
    if (val.length > 150) return 'Họ và tên tối đa 150 ký tự';
    return null;
  }

  String? _validatePhone(String? v) {
    if (_phoneServerError != null) return _phoneServerError;
    final val = v?.trim() ?? '';
    if (val.isEmpty) return 'Vui lòng nhập số điện thoại';
    final isValid = RegExp(r'^(0|\+84)[0-9]{9,10}$').hasMatch(val) ||
        RegExp(r'^\+?[0-9]{9,15}$').hasMatch(val);
    if (!isValid) return 'Số điện thoại không hợp lệ (9-11 chữ số, ví dụ 0901234567)';
    return null;
  }

  String? _validateEmail(String? v) {
    if (_emailServerError != null) return _emailServerError;
    final val = v?.trim() ?? '';
    if (val.isEmpty) return null; // Email không bắt buộc
    if (val.length > 255) return 'Email tối đa 255 ký tự';
    if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(val)) {
      return 'Email không đúng định dạng (ví dụ: staff@courtgo.vn)';
    }
    return null;
  }

  String? _validatePassword(String? v) {
    if (_passwordServerError != null) return _passwordServerError;
    final val = v ?? '';
    if (val.isEmpty) return 'Vui lòng nhập mật khẩu';
    if (val.length < 8 || val.length > 128) {
      return 'Mật khẩu phải từ 8 đến 128 ký tự';
    }
    if (!RegExp(r'[A-Z]').hasMatch(val)) {
      return 'Mật khẩu phải chứa ít nhất 1 chữ hoa (A-Z)';
    }
    if (!RegExp(r'[a-z]').hasMatch(val)) {
      return 'Mật khẩu phải chứa ít nhất 1 chữ thường (a-z)';
    }
    if (!RegExp(r'[0-9]').hasMatch(val)) {
      return 'Mật khẩu phải chứa ít nhất 1 chữ số (0-9)';
    }
    return null;
  }

  String? _validateCenter(String? v) {
    if (_centerServerError != null) return _centerServerError;
    if (v == null || v.trim().isEmpty) return 'Vui lòng chọn cơ sở trực thuộc';
    return null;
  }

  void _scrollToFirstError({
    bool hasFullNameError = false,
    bool hasPhoneError = false,
    bool hasEmailError = false,
    bool hasPasswordError = false,
    bool hasCenterError = false,
  }) {
    GlobalKey? targetKey;
    FocusNode? targetFocus;

    if (hasFullNameError) {
      targetKey = _fullNameKey;
      targetFocus = _fullNameFocus;
    } else if (hasPhoneError) {
      targetKey = _phoneKey;
      targetFocus = _phoneFocus;
    } else if (hasEmailError) {
      targetKey = _emailKey;
      targetFocus = _emailFocus;
    } else if (hasPasswordError) {
      targetKey = _passwordKey;
      targetFocus = _passwordFocus;
    } else if (hasCenterError) {
      targetKey = _centerKey;
      targetFocus = _centerFocus;
    }

    if (targetKey != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        final ctx = targetKey?.currentContext;
        if (ctx != null) {
          Scrollable.ensureVisible(
            ctx,
            duration: const Duration(milliseconds: 300),
            curve: Curves.easeInOut,
            alignment: 0.1,
          );
        }
        targetFocus?.requestFocus();
      });
    }
  }

  Future<void> _submit() async {
    if (_saving) return;

    setState(() {
      _autovalidateMode = AutovalidateMode.onUserInteraction;
      _fullNameServerError = null;
      _phoneServerError = null;
      _emailServerError = null;
      _passwordServerError = null;
      _centerServerError = null;
    });

    final isFormValid = _formKey.currentState?.validate() ?? false;
    final isCenterValid = _selectedCenterId != null && _selectedCenterId!.isNotEmpty;

    if (!isCenterValid) {
      setState(() {
        _centerServerError = 'Vui lòng chọn cơ sở trực thuộc';
      });
    }

    if (!isFormValid || !isCenterValid) {
      final nameErr = _validateFullName(_fullNameCtrl.text) != null;
      final phoneErr = _validatePhone(_phoneCtrl.text) != null;
      final emailErr = _validateEmail(_emailCtrl.text) != null;
      final passErr = _validatePassword(_passwordCtrl.text) != null;
      final centerErr = !isCenterValid;

      _scrollToFirstError(
        hasFullNameError: nameErr,
        hasPhoneError: phoneErr,
        hasEmailError: emailErr,
        hasPasswordError: passErr,
        hasCenterError: centerErr,
      );

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Vui lòng kiểm tra lại các trường được đánh dấu đỏ.'),
          backgroundColor: AppColors.error,
          behavior: SnackBarBehavior.floating,
        ),
      );
      return;
    }

    setState(() => _saving = true);
    try {
      await context.read<AdminApi>().createStaff(
            fullName: _fullNameCtrl.text.trim(),
            phoneNumber: _phoneCtrl.text.trim(),
            email: _emailCtrl.text.trim().isEmpty ? null : _emailCtrl.text.trim(),
            password: _passwordCtrl.text,
            sportCenterId: _selectedCenterId!,
          );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Tạo nhân viên thành công!'),
            backgroundColor: AppColors.primary,
            behavior: SnackBarBehavior.floating,
          ),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        String displayMessage = 'Tạo nhân viên thất bại. Vui lòng thử lại.';
        bool hasFullNameErr = false;
        bool hasPhoneErr = false;
        bool hasEmailErr = false;
        bool hasPasswordErr = false;
        bool hasCenterErr = false;

        if (e is ApiException) {
          displayMessage = e.message;

          final fn = e.getFieldError('fullName');
          final pn = e.getFieldError('phoneNumber') ?? e.getFieldError('phone');
          final em = e.getFieldError('email');
          final pw = e.getFieldError('password');
          final sc = e.getFieldError('sportCenterId') ?? e.getFieldError('centerId');

          if (fn != null) {
            _fullNameServerError = fn;
            hasFullNameErr = true;
          }
          if (pn != null || e.code == 'PHONE_ALREADY_EXISTS') {
            _phoneServerError = pn ?? 'Số điện thoại này đã được đăng ký.';
            hasPhoneErr = true;
          }
          if (em != null || e.code == 'EMAIL_ALREADY_EXISTS') {
            _emailServerError = em ?? 'Email này đã được đăng ký.';
            hasEmailErr = true;
          }
          if (pw != null) {
            _passwordServerError = pw;
            hasPasswordErr = true;
          }
          if (sc != null) {
            _centerServerError = sc;
            hasCenterErr = true;
          }
        }

        setState(() {});
        _formKey.currentState?.validate();

        if (hasFullNameErr || hasPhoneErr || hasEmailErr || hasPasswordErr || hasCenterErr) {
          _scrollToFirstError(
            hasFullNameError: hasFullNameErr,
            hasPhoneError: hasPhoneErr,
            hasEmailError: hasEmailErr,
            hasPasswordError: hasPasswordErr,
            hasCenterError: hasCenterErr,
          );
        }

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(displayMessage),
            backgroundColor: AppColors.error,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Widget _buildRuleItem(String text, bool met) {
    return Padding(
      padding: const EdgeInsets.only(right: 8, bottom: 4),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            met ? Icons.check_circle : Icons.radio_button_unchecked,
            size: 14,
            color: met ? AppColors.success : AppColors.textSecondary,
          ),
          const SizedBox(width: 4),
          Text(
            text,
            style: TextStyle(
              fontSize: 11,
              fontWeight: met ? FontWeight.w600 : FontWeight.w400,
              color: met ? AppColors.success : AppColors.textSecondary,
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final passwordText = _passwordCtrl.text;
    final hasMinLength = passwordText.length >= 8 && passwordText.length <= 128;
    final hasUpper = RegExp(r'[A-Z]').hasMatch(passwordText);
    final hasLower = RegExp(r'[a-z]').hasMatch(passwordText);
    final hasDigit = RegExp(r'[0-9]').hasMatch(passwordText);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Thêm nhân viên mới',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
      ),
      body: _loadingCenters
          ? const LoadingWidget()
          : SingleChildScrollView(
              controller: _scrollController,
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                autovalidateMode: _autovalidateMode,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    AdminCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Thông tin cá nhân',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 14),

                          // Full Name Field
                          TextFormField(
                            key: _fullNameKey,
                            focusNode: _fullNameFocus,
                            controller: _fullNameCtrl,
                            enabled: !_saving,
                            decoration: InputDecoration(
                              labelText: 'Họ và tên *',
                              hintText: 'Ví dụ: Nguyễn Văn A',
                              prefixIcon: const Icon(Icons.person_outline, size: 20),
                              border: const OutlineInputBorder(),
                              errorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 1.5),
                              ),
                              focusedErrorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 2),
                              ),
                              errorMaxLines: 2,
                            ),
                            validator: _validateFullName,
                          ),
                          const SizedBox(height: 14),

                          // Phone Number Field
                          TextFormField(
                            key: _phoneKey,
                            focusNode: _phoneFocus,
                            controller: _phoneCtrl,
                            enabled: !_saving,
                            keyboardType: TextInputType.phone,
                            decoration: InputDecoration(
                              labelText: 'Số điện thoại đăng nhập *',
                              hintText: 'Ví dụ: 0901234567',
                              prefixIcon: const Icon(Icons.phone_outlined, size: 20),
                              border: const OutlineInputBorder(),
                              errorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 1.5),
                              ),
                              focusedErrorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 2),
                              ),
                              errorMaxLines: 2,
                            ),
                            validator: _validatePhone,
                          ),
                          const SizedBox(height: 14),

                          // Email Field
                          TextFormField(
                            key: _emailKey,
                            focusNode: _emailFocus,
                            controller: _emailCtrl,
                            enabled: !_saving,
                            keyboardType: TextInputType.emailAddress,
                            decoration: InputDecoration(
                              labelText: 'Email (tùy chọn)',
                              hintText: 'Ví dụ: staff@courtgo.vn',
                              prefixIcon: const Icon(Icons.email_outlined, size: 20),
                              border: const OutlineInputBorder(),
                              errorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 1.5),
                              ),
                              focusedErrorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 2),
                              ),
                              errorMaxLines: 2,
                            ),
                            validator: _validateEmail,
                          ),
                          const SizedBox(height: 14),

                          // Password Field
                          TextFormField(
                            key: _passwordKey,
                            focusNode: _passwordFocus,
                            controller: _passwordCtrl,
                            enabled: !_saving,
                            obscureText: _obscurePassword,
                            decoration: InputDecoration(
                              labelText: 'Mật khẩu khởi tạo *',
                              prefixIcon: const Icon(Icons.lock_outline, size: 20),
                              suffixIcon: IconButton(
                                icon: Icon(_obscurePassword
                                    ? Icons.visibility_off
                                    : Icons.visibility),
                                onPressed: () => setState(
                                    () => _obscurePassword = !_obscurePassword),
                              ),
                              border: const OutlineInputBorder(),
                              errorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 1.5),
                              ),
                              focusedErrorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 2),
                              ),
                              errorMaxLines: 3,
                            ),
                            validator: _validatePassword,
                          ),
                          const SizedBox(height: 8),

                          // Live Password Requirements Box
                          Container(
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: AppColors.surface,
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(color: AppColors.border),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Yêu cầu bảo mật mật khẩu:',
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w700,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Wrap(
                                  children: [
                                    _buildRuleItem('Tối thiểu 8 ký tự', hasMinLength),
                                    _buildRuleItem('Chữ hoa (A-Z)', hasUpper),
                                    _buildRuleItem('Chữ thường (a-z)', hasLower),
                                    _buildRuleItem('Chữ số (0-9)', hasDigit),
                                  ],
                                ),
                              ],
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
                            'Phân công cơ sở ban đầu',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 8),
                          const Text(
                            'Hệ thống tự động gán vai trò Nhân viên (Staff). Vui lòng chọn cơ sở mà nhân viên này trực tiếp làm việc.',
                            style: TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const SizedBox(height: 14),

                          // Sports Center Dropdown Field
                          DropdownButtonFormField<String>(
                            key: _centerKey,
                            focusNode: _centerFocus,
                            initialValue: _selectedCenterId,
                            decoration: InputDecoration(
                              labelText: 'Cơ sở trực thuộc *',
                              prefixIcon: const Icon(Icons.storefront, size: 20),
                              border: const OutlineInputBorder(),
                              errorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 1.5),
                              ),
                              focusedErrorBorder: const OutlineInputBorder(
                                borderSide: BorderSide(color: AppColors.error, width: 2),
                              ),
                              errorMaxLines: 2,
                            ),
                            items: _centers.map((c) {
                              return DropdownMenuItem(
                                value: c.centerId,
                                child: Text(
                                  '${c.name} (${c.district})',
                                  overflow: TextOverflow.ellipsis,
                                ),
                              );
                            }).toList(),
                            onChanged: _saving
                                ? null
                                : (v) {
                                    setState(() {
                                      _selectedCenterId = v;
                                      _centerServerError = null;
                                    });
                                  },
                            validator: _validateCenter,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Submit Button
                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: AppColors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      onPressed: _saving ? null : _submit,
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
                              'Tạo nhân viên',
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
