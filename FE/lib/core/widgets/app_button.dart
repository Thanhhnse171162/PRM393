import 'package:flutter/material.dart';

import '../constants/app_constants.dart';

/// Primary CourtGo button with loading + disabled support.
class AppButton extends StatelessWidget {
  const AppButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.isLoading = false,
    this.isOutlined = false,
    this.icon,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool isLoading;
  final bool isOutlined;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final VoidCallback? effective = isLoading ? null : onPressed;

    final Widget child = isLoading
        ? const SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          )
        : Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (icon != null) ...[Icon(icon, size: 18), const SizedBox(width: 8)],
              Flexible(child: Text(label, overflow: TextOverflow.ellipsis)),
            ],
          );

    return ConstrainedBox(
      constraints: const BoxConstraints(minHeight: AppConstants.minTouchTarget),
      child: isOutlined
          ? OutlinedButton(onPressed: effective, child: child)
          : ElevatedButton(onPressed: effective, child: child),
    );
  }
}
