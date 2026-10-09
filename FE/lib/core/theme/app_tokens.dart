import 'package:flutter/material.dart';

import 'app_colors.dart';

class AppRadius {
  static const card = 16.0;
  static const button = 12.0;
  static const sheet = 24.0;
}

class AppDurations {
  static const fast = Duration(milliseconds: 180);
  static const normal = Duration(milliseconds: 220);
  static const search = Duration(milliseconds: 350);
}

class AppShadows {
  static const soft = [
    BoxShadow(color: Color(0x080F172A), blurRadius: 16, offset: Offset(0, 4)),
  ];
}

class AppTypography {
  static const eyebrow = TextStyle(
    fontSize: 11,
    fontWeight: FontWeight.w700,
    letterSpacing: 1.4,
    color: AppColors.primary,
  );
  static const title = TextStyle(
    fontSize: 22,
    fontWeight: FontWeight.w800,
    height: 1.3,
    color: AppColors.textPrimary,
  );
}
