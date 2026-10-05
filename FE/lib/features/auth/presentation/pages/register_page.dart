import 'package:flutter/material.dart';

import '../../../../core/widgets/empty_state.dart';

class RegisterPage extends StatelessWidget {
  const RegisterPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đăng ký')),
      body: const SafeArea(
        child: EmptyState(
          icon: Icons.person_add_alt_1_outlined,
          title: 'Registration',
          message: 'Customer registration will be implemented in feature/api-auth.',
        ),
      ),
    );
  }
}
