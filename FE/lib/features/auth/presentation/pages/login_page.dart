import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/routing/route_guard.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../shared/widgets/customer_widgets.dart';
import '../../../../shared/models/user_model.dart';
import '../providers/auth_provider.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final form = GlobalKey<FormState>();
  final login = TextEditingController(), password = TextEditingController();
  bool obscure = true;
  @override
  void dispose() {
    login.dispose();
    password.dispose();
    super.dispose();
  }

  Future<void> submit() async {
    final auth = context.read<AuthProvider>();
    if (auth.isLoading || !form.currentState!.validate()) return;
    if (await auth.login(login.text, password.text) && mounted) {
      final redirect =
          GoRouterState.of(context).uri.queryParameters['redirect'];
      if (auth.role == UserRole.customer) {
        if (redirect != null && redirect.isNotEmpty) {
          context.go(redirect);
        } else if (context.canPop()) {
          context.pop(true);
        } else {
          context.go(RouteGuard.homeFor(auth.role));
        }
      } else {
        context.go(RouteGuard.homeFor(auth.role));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: ListView(
          children: [
            SizedBox(
              height: 190,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  const BrandHeroImage(),
                  Container(color: const Color(0x99003D32)),
                  const Padding(
                    padding: EdgeInsets.all(24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        Row(
                          children: [
                            BrandMark(),
                            SizedBox(width: 10),
                            Text(
                              'CourtGo',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 25,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: 12),
                        Text(
                          'Sân quen. Trận mới. Năng lượng mới.',
                          style: TextStyle(color: Colors.white, fontSize: 13),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(24),
              child: Form(
                key: form,
                child: AutofillGroup(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Text(
                        'Chào mừng trở lại',
                        style: TextStyle(
                          fontSize: 27,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'Đăng nhập để đặt sân và quản lý lịch chơi của bạn.',
                        style: TextStyle(
                          color: AppColors.textSecondary,
                          height: 1.5,
                        ),
                      ),
                      const SizedBox(height: 24),
                      TextFormField(
                        key: const Key('login-identity'),
                        controller: login,
                        autofillHints: const [AutofillHints.username],
                        textInputAction: TextInputAction.next,
                        decoration: const InputDecoration(
                          labelText: 'Email hoặc số điện thoại',
                          prefixIcon: Icon(Icons.person_outline),
                        ),
                        validator: (v) => v == null || v.trim().isEmpty
                            ? 'Vui lòng nhập email hoặc số điện thoại.'
                            : null,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        key: const Key('login-password'),
                        controller: password,
                        obscureText: obscure,
                        autofillHints: const [AutofillHints.password],
                        textInputAction: TextInputAction.done,
                        onFieldSubmitted: (_) => submit(),
                        decoration: InputDecoration(
                          labelText: 'Mật khẩu',
                          prefixIcon: const Icon(Icons.lock_outline),
                          suffixIcon: IconButton(
                            tooltip: obscure ? 'Hiện mật khẩu' : 'Ẩn mật khẩu',
                            onPressed: () => setState(() => obscure = !obscure),
                            icon: Icon(
                              obscure
                                  ? Icons.visibility_outlined
                                  : Icons.visibility_off_outlined,
                            ),
                          ),
                        ),
                        validator: (v) => v == null || v.isEmpty
                            ? 'Vui lòng nhập mật khẩu.'
                            : null,
                      ),
                      if (auth.errorMessage != null)
                        Padding(
                          padding: const EdgeInsets.only(top: 16),
                          child: Text(
                            auth.errorMessage!,
                            style: const TextStyle(color: AppColors.error),
                          ),
                        ),
                      const SizedBox(height: 24),
                      PrimaryButton(
                        label: 'Đăng nhập',
                        loading: auth.isLoading,
                        onPressed: submit,
                      ),
                      const SizedBox(height: 12),
                      TextButton(
                        onPressed: auth.isLoading
                            ? null
                            : () => context.push('/register'),
                        child: const Text('Chưa có tài khoản? Đăng ký'),
                      ),
                      const Divider(height: 32),
                      SecondaryButton(
                        label: 'Khám phá CourtGo',
                        onPressed: auth.isLoading
                            ? null
                            : () {
                                auth.continueAsGuest();
                                context.go('/customer/home');
                              },
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
