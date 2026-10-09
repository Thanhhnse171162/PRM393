import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/network/network_result.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../shared/widgets/customer_widgets.dart';
import '../../data/services/auth_service.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});
  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final form = GlobalKey<FormState>();
  final name = TextEditingController(),
      phone = TextEditingController(),
      email = TextEditingController(),
      password = TextEditingController(),
      confirm = TextEditingController();
  bool busy = false, success = false, obscure = true;
  String? error;
  @override
  void dispose() {
    for (final c in [name, phone, email, password, confirm]) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Tạo tài khoản'),
    body: success
        ? Center(
            child: NoticeState(
              title: 'Chào mừng đến CourtGo!',
              message: 'Tài khoản đã được tạo. Bạn có thể đăng nhập và đặt sân ngay.',
              icon: Icons.check_circle_outline,
              action: () => context.go('/login'),
              actionLabel: 'Đăng nhập',
            ),
          )
        : ListView(
            padding: const EdgeInsets.all(24),
            children: [
              const BrandMark(size: 52),
              const SizedBox(height: 20),
              const Text(
                'Bắt đầu trận đấu\ncủa bạn.',
                style: TextStyle(
                  fontSize: 28,
                  height: 1.2,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Một tài khoản cho mọi chi nhánh CourtGo.',
                style: TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(height: 24),
              Form(
                key: form,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TextFormField(
                      controller: name,
                      textInputAction: TextInputAction.next,
                      maxLength: 150,
                      decoration: const InputDecoration(
                        labelText: 'Họ và tên',
                        counterText: '',
                      ),
                      validator: (v) => v == null || v.trim().isEmpty
                          ? 'Vui lòng nhập họ tên.'
                          : null,
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: phone,
                      keyboardType: TextInputType.phone,
                      textInputAction: TextInputAction.next,
                      decoration: const InputDecoration(
                        labelText: 'Số điện thoại',
                      ),
                      validator: (v) =>
                          RegExp(r'^(0|\+84)\d{9}$').hasMatch((v ?? '').trim())
                          ? null
                          : 'Nhập số điện thoại Việt Nam hợp lệ.',
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: email,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      decoration: const InputDecoration(
                        labelText: 'Email (không bắt buộc)',
                      ),
                      validator: (v) =>
                          v == null ||
                              v.trim().isEmpty ||
                              RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$')
                                  .hasMatch(v.trim())
                          ? null
                          : 'Email chưa hợp lệ.',
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: password,
                      obscureText: obscure,
                      textInputAction: TextInputAction.next,
                      decoration: InputDecoration(
                        labelText: 'Mật khẩu',
                        helperText: '8–128 ký tự, có chữ hoa, chữ thường và số',
                        helperMaxLines: 2,
                        suffixIcon: IconButton(
                          onPressed: () => setState(() => obscure = !obscure),
                          icon: Icon(
                            obscure
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                      validator: (v) =>
                          v == null ||
                              v.length < 8 ||
                              v.length > 128 ||
                              !RegExp(r'[A-Z]').hasMatch(v) ||
                              !RegExp(r'[a-z]').hasMatch(v) ||
                              !RegExp(r'[0-9]').hasMatch(v)
                          ? 'Mật khẩu cần 8–128 ký tự, có chữ hoa, chữ thường và số.'
                          : null,
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: confirm,
                      obscureText: obscure,
                      decoration: const InputDecoration(
                        labelText: 'Nhập lại mật khẩu',
                      ),
                      validator: (v) => v != password.text
                          ? 'Mật khẩu nhập lại chưa khớp.'
                          : null,
                    ),
                    if (error != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 16),
                        child: Text(
                          error!,
                          style: const TextStyle(color: AppColors.error),
                        ),
                      ),
                    const SizedBox(height: 24),
                    PrimaryButton(
                      label: 'Tạo tài khoản',
                      loading: busy,
                      onPressed: () async {
                        if (busy || !form.currentState!.validate()) return;
                        setState(() {
                          busy = true;
                          error = null;
                        });
                        final result = await context
                            .read<AuthService>()
                            .register(
                              fullName: name.text,
                              phoneNumber: phone.text,
                              email: email.text,
                              password: password.text,
                            );
                        if (!mounted) return;
                        setState(() {
                          busy = false;
                          switch (result) {
                            case NetworkSuccess():
                              success = true;
                            case NetworkFailure(:final error):
                              this.error = error.message;
                          }
                        });
                      },
                    ),
                    const SizedBox(height: 16),
                    const Text(
                      'Mật khẩu của bạn được bảo vệ an toàn.',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
  );
}
