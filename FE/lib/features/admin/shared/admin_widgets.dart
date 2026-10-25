import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_tokens.dart';

class AdminFormat {
  static String currency(num amount) {
    final value = amount.round().toString();
    final buffer = StringBuffer();
    for (int i = 0; i < value.length; i++) {
      if (i > 0 && (value.length - i) % 3 == 0) {
        buffer.write('.');
      }
      buffer.write(value[i]);
    }
    return '${buffer.toString()} đ';
  }

  static String percent(num rate) {
    // Admin APIs return percentage points (0–100), not a 0–1 fraction.
    return '${rate.toStringAsFixed(1)}%';
  }

  static String date(String iso) {
    if (iso.length < 10) return iso;
    return '${iso.substring(8, 10)}/${iso.substring(5, 7)}/${iso.substring(0, 4)}';
  }

  static String time(String iso) {
    if (iso.length >= 16) return iso.substring(11, 16);
    if (iso.length >= 5) return iso.substring(0, 5);
    return iso;
  }

  static String dateTime(String iso) {
    if (iso.length < 16) return iso;
    return '${time(iso)} · ${date(iso)}';
  }

  static String dayOfWeekName(String code) {
    return switch (code.toLowerCase()) {
      'monday' || '1' => 'Thứ Hai',
      'tuesday' || '2' => 'Thứ Ba',
      'wednesday' || '3' => 'Thứ Tư',
      'thursday' || '4' => 'Thứ Năm',
      'friday' || '5' => 'Thứ Sáu',
      'saturday' || '6' => 'Thứ Bảy',
      'sunday' || '7' => 'Chủ Nhật',
      _ => code,
    };
  }

  static String bookingStatusName(String status) {
    return switch (status.toLowerCase()) {
      'pending' || 'pendingpayment' => 'Chờ thanh toán',
      'expired' => 'Hết thời gian giữ',
      'confirmed' => 'Đã xác nhận',
      'checkedin' => 'Đã check-in',
      'inprogress' => 'Đang chơi',
      'completed' => 'Hoàn thành',
      'cancelled' => 'Đã hủy',
      'noshow' => 'Vắng mặt',
      _ => status,
    };
  }

  static String paymentStatusName(String status) {
    return switch (status.toLowerCase()) {
      'pending' || 'unpaid' => 'Chưa thanh toán',
      'partiallypaid' || 'depositpaid' => 'Đã cọc',
      'refundpending' => 'Chờ hoàn tiền',
      'completed' || 'fullypaid' => 'Đã thanh toán đủ',
      'failed' => 'Thất bại',
      'cancelled' => 'Đã hủy',
      'refunded' => 'Đã hoàn tiền',
      'partiallyrefunded' => 'Hoàn tiền 1 phần',
      _ => status,
    };
  }

  static String courtStatusName(String status) {
    return switch (status.toLowerCase()) {
      'active' => 'Hoạt động',
      'maintenance' => 'Bảo trì',
      'inactive' => 'Tạm ngừng',
      _ => status,
    };
  }

  static String centerStatusName(String status) {
    return switch (status.toLowerCase()) {
      'active' => 'Hoạt động',
      'inactive' => 'Ngừng hoạt động',
      _ => status,
    };
  }
}

class AdminCard extends StatelessWidget {
  const AdminCard({
    super.key,
    required this.child,
    this.title,
    this.padding = const EdgeInsets.all(16),
    this.onTap,
    this.color = AppColors.white,
    this.borderColor,
  });

  final String? title;
  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;
  final Color color;
  final Color? borderColor;

  @override
  Widget build(BuildContext context) {
    final border = Border.all(
      color: borderColor ?? AppColors.border.withValues(alpha: 0.6),
    );
    final content = title != null
        ? Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                title!,
                style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  fontSize: 15,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 12),
              child,
            ],
          )
        : child;

    final cardWidget = Container(
      padding: padding,
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: border,
        boxShadow: AppShadows.soft,
      ),
      child: content,
    );

    if (onTap == null) return cardWidget;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppRadius.card),
      child: cardWidget,
    );
  }
}

class AdminKpiCard extends StatelessWidget {
  const AdminKpiCard({
    super.key,
    required this.title,
    required this.value,
    required this.icon,
    this.subtitle,
    this.color = AppColors.primary,
    this.iconColor,
    this.onTap,
  });

  final String title;
  final String value;
  final IconData icon;
  final String? subtitle;
  final Color color;
  final Color? iconColor;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return AdminCard(
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Text(
                  title,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textSecondary,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: (iconColor ?? color).withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, size: 16, color: iconColor ?? color),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: const TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
              letterSpacing: -0.5,
            ),
          ),
          if (subtitle != null) ...[
            const SizedBox(height: 4),
            Text(
              subtitle!,
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w500,
                color: AppColors.textSecondary,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class AdminStatusBadge extends StatelessWidget {
  const AdminStatusBadge({
    super.key,
    required this.label,
    required this.statusType,
  });

  final String label;
  final String statusType;

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = _colors(statusType);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: fg),
      ),
    );
  }

  static (Color, Color) _colors(String raw) {
    return switch (raw.toLowerCase()) {
      'active' ||
      'confirmed' ||
      'completed' ||
      'approved' ||
      'fullypaid' => (const Color(0xFFDCFCE7), const Color(0xFF15803D)),
      'inprogress' ||
      'checkedin' => (const Color(0xFFE0E7FF), const Color(0xFF4338CA)),
      'pending' ||
      'pendingpayment' ||
      'refundpending' ||
      'depositpaid' ||
      'maintenance' ||
      'partiallypaid' => (const Color(0xFFFEF3C7), const Color(0xFFB45309)),
      'cancelled' ||
      'rejected' ||
      'inactive' ||
      'failed' => (const Color(0xFFFEE2E2), const Color(0xFFB91C1C)),
      'noshow' => (const Color(0xFFFFEDD5), const Color(0xFFC2410C)),
      _ => (const Color(0xFFF1F5F9), const Color(0xFF475569)),
    };
  }
}

class AdminSectionHeader extends StatelessWidget {
  const AdminSectionHeader({
    super.key,
    required this.title,
    this.actionLabel,
    this.onAction,
  });

  final String title;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Text(
              title,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
          ),
          if (actionLabel != null && onAction != null)
            TextButton(
              onPressed: onAction,
              style: TextButton.styleFrom(
                visualDensity: VisualDensity.compact,
                foregroundColor: AppColors.primary,
              ),
              child: Text(
                actionLabel!,
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class AdminFilterChipItem extends StatelessWidget {
  const AdminFilterChipItem({
    super.key,
    required this.label,
    required this.isSelected,
    required this.onTap,
  });

  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: AppDurations.fast,
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary : AppColors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.border,
          ),
          boxShadow: isSelected ? AppShadows.soft : null,
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 12,
            fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
            color: isSelected ? AppColors.white : AppColors.textSecondary,
          ),
        ),
      ),
    );
  }
}

class AdminBarChartItem {
  const AdminBarChartItem({
    required this.label,
    required this.value,
    this.secondaryValue,
    this.subLabel,
  });

  final String label;
  final double value;
  final double? secondaryValue;
  final String? subLabel;
}

class AdminBarChart extends StatelessWidget {
  const AdminBarChart({
    super.key,
    required this.items,
    this.primaryColor = AppColors.primary,
    this.secondaryColor = AppColors.warning,
    this.height = 180,
    this.valueFormatter,
  });

  final List<AdminBarChartItem> items;
  final Color primaryColor;
  final Color secondaryColor;
  final double height;
  final String Function(double)? valueFormatter;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return SizedBox(
        height: height,
        child: const Center(
          child: Text(
            'Không có dữ liệu',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
          ),
        ),
      );
    }

    double maxValue = 0;
    for (final item in items) {
      if (item.value > maxValue) maxValue = item.value;
      if (item.secondaryValue != null && item.secondaryValue! > maxValue) {
        maxValue = item.secondaryValue!;
      }
    }
    if (maxValue <= 0) maxValue = 1;

    return SizedBox(
      height: height,
      child: LayoutBuilder(
        builder: (context, constraints) {
          final barWidth = ((constraints.maxWidth / items.length) - 8).clamp(
            8.0,
            36.0,
          );
          return Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            children: [
              for (final item in items)
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 2),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        if (item.value > 0)
                          Text(
                            valueFormatter != null
                                ? valueFormatter!(item.value)
                                : item.value.toStringAsFixed(0),
                            style: const TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textSecondary,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        const SizedBox(height: 4),
                        Container(
                          width: barWidth,
                          height: (item.value / maxValue * (height - 40)).clamp(
                            4.0,
                            height - 40,
                          ),
                          decoration: BoxDecoration(
                            color: primaryColor,
                            borderRadius: const BorderRadius.vertical(
                              top: Radius.circular(4),
                            ),
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          item.label,
                          style: const TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.w500,
                            color: AppColors.textSecondary,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}

class AdminActionCard extends StatelessWidget {
  const AdminActionCard({
    super.key,
    required this.title,
    required this.description,
    required this.icon,
    required this.onTap,
    this.badgeText,
    this.badgeColor,
  });

  final String title;
  final String description;
  final IconData icon;
  final VoidCallback onTap;
  final String? badgeText;
  final Color? badgeColor;

  @override
  Widget build(BuildContext context) {
    return AdminCard(
      onTap: onTap,
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.lightMint,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: AppColors.primary, size: 24),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        title,
                        style: const TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ),
                    if (badgeText != null)
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 6,
                          vertical: 2,
                        ),
                        decoration: BoxDecoration(
                          color: (badgeColor ?? AppColors.primary).withValues(
                            alpha: 0.15,
                          ),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          badgeText!,
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.w700,
                            color: badgeColor ?? AppColors.primary,
                          ),
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  description,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          const Icon(
            Icons.chevron_right,
            color: AppColors.textSecondary,
            size: 20,
          ),
        ],
      ),
    );
  }
}

Future<bool> adminConfirmDialog({
  required BuildContext context,
  required String title,
  required String message,
  String confirmLabel = 'Xác nhận',
  Color confirmColor = AppColors.primary,
}) async {
  final result = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
      content: Text(message),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(ctx, false),
          child: const Text(
            'Hủy',
            style: TextStyle(color: AppColors.textSecondary),
          ),
        ),
        FilledButton(
          onPressed: () => Navigator.pop(ctx, true),
          style: FilledButton.styleFrom(backgroundColor: confirmColor),
          child: Text(confirmLabel),
        ),
      ],
    ),
  );
  return result ?? false;
}
