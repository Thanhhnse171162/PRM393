import 'package:flutter/material.dart';

import 'app_colors.dart';

class AppTextStyles {
  const AppTextStyles._();

  static const List<String> fontFallback = [
    'Roboto',
    'SF Pro Text',
    'Segoe UI',
    'Arial',
    'sans-serif',
  ];

  static const TextStyle h1 = TextStyle(
    fontSize: 24,
    fontWeight: FontWeight.w700,
    color: AppColors.textPrimary,
    height: 1.3,
    fontFamilyFallback: fontFallback,
  );

  static const TextStyle h2 = TextStyle(
    fontSize: 20,
    fontWeight: FontWeight.w700,
    color: AppColors.textPrimary,
    height: 1.3,
    fontFamilyFallback: fontFallback,
  );

  static const TextStyle title = TextStyle(
    fontSize: 16,
    fontWeight: FontWeight.w600,
    color: AppColors.textPrimary,
    height: 1.35,
    fontFamilyFallback: fontFallback,
  );

  static const TextStyle body = TextStyle(
    fontSize: 14,
    fontWeight: FontWeight.w400,
    color: AppColors.textPrimary,
    height: 1.45,
    fontFamilyFallback: fontFallback,
  );

  static const TextStyle caption = TextStyle(
    fontSize: 12,
    fontWeight: FontWeight.w400,
    color: AppColors.textSecondary,
    height: 1.4,
    fontFamilyFallback: fontFallback,
  );

  static const TextStyle button = TextStyle(
    fontSize: 15,
    fontWeight: FontWeight.w600,
    fontFamilyFallback: fontFallback,
  );

  static const TextTheme textTheme = TextTheme(
    headlineMedium: h1,
    titleLarge: h2,
    titleMedium: title,
    bodyMedium: body,
    bodySmall: caption,
    labelLarge: button,
  );
}
