import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/routing/route_guard.dart';
import '../../../../core/routing/route_names.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../core/utils/validators.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../providers/auth_provider.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _loginController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _loginController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final auth = context.read<AuthProvider>();
    auth.clearError();

    if (!_formKey.currentState!.validate()) return;

    final success = await auth.login(
      _loginController.text,
      _passwordController.text,
    );

    if (success && mounted) {
      context.go(RouteGuard.homeFor(auth.role));
    }
  }

  void _fillDemo(String emailOrPhone) {
    _loginController.text = emailOrPhone;
    _passwordController.text = 'Demo@123456';
    context.read<AuthProvider>().clearError();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Đăng nhập')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: AppSpacing.page,
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: AppSpacing.md),
                Text('Chào mừng đến CourtGo', style: AppTextStyles.h1),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  'Đặt sân thể thao nhanh chóng và tiện lợi',
                  style: AppTextStyles.caption,
                ),
                const SizedBox(height: AppSpacing.lg),

                if (auth.errorMessage != null) ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.error.withValues(alpha: 0.08),
                      borderRadius:
                          BorderRadius.circular(AppSpacing.radiusButton),
                      border: Border.all(
                          color: AppColors.error.withValues(alpha: 0.3)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.error_outline,
                            color: AppColors.error, size: 20),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            auth.errorMessage!,
                            style: AppTextStyles.caption
                                .copyWith(color: AppColors.error),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                ],

                AppTextField(
                  label: 'Email hoặc Số điện thoại',
                  controller: _loginController,
                  prefixIcon: Icons.person_outline,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  validator: (v) =>
                      Validators.required(v, field: 'Email hoặc số điện thoại'),
                ),
                const SizedBox(height: AppSpacing.md),

                TextFormField(
                  controller: _passwordController,
                  obscureText: _obscurePassword,
                  textInputAction: TextInputAction.done,
                  onFieldSubmitted: (_) => _submit(),
                  validator: (v) =>
                      Validators.required(v, field: 'Mật khẩu'),
                  decoration: InputDecoration(
                    labelText: 'Mật khẩu',
                    prefixIcon: const Icon(Icons.lock_outline),
                    suffixIcon: IconButton(
                      icon: Icon(
                        _obscurePassword
                            ? Icons.visibility_outlined
                            : Icons.visibility_off_outlined,
                      ),
                      onPressed: () =>
                          setState(() => _obscurePassword = !_obscurePassword),
                    ),
                  ),
                ),
                const SizedBox(height: AppSpacing.lg),

                AppButton(
                  label: 'Đăng nhập',
                  isLoading: auth.isLoading,
                  onPressed: _submit,
                ),
                const SizedBox(height: AppSpacing.md),

                AppButton(
                  label: 'Khám phá với tư cách khách',
                  isOutlined: true,
                  onPressed: () {
                    auth.continueAsGuest();
                    context.go(RouteNames.customerHome);
                  },
                ),
                const SizedBox(height: AppSpacing.md),

                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Text('Chưa có tài khoản? ',
                        style: AppTextStyles.body),
                    GestureDetector(
                      onTap: () => context.push(RouteNames.register),
                      child: Text(
                        'Đăng ký ngay',
                        style: AppTextStyles.button
                            .copyWith(color: AppColors.primary),
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: AppSpacing.xl),
                const Divider(),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  'Tài khoản Demo (mật khẩu: Demo@123456)',
                  style: AppTextStyles.caption,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.sm),

                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  alignment: WrapAlignment.center,
                  children: [
                    ActionChip(
                      label: const Text('Customer'),
                      onPressed: () => _fillDemo('customer@courtgo.local'),
                    ),
                    ActionChip(
                      label: const Text('Staff'),
                      onPressed: () => _fillDemo('staff@courtgo.local'),
                    ),
                    ActionChip(
                      label: const Text('Admin'),
                      onPressed: () => _fillDemo('admin@courtgo.local'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
