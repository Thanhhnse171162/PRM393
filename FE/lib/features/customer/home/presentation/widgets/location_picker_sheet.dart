import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../../../core/storage/local_storage_service.dart';
import '../../../../../core/theme/app_colors.dart';
import '../../../../../core/theme/app_spacing.dart';
import '../../../../../core/theme/app_text_styles.dart';
import '../providers/customer_home_provider.dart';

/// Bottom sheet allowing the customer to switch their preferred search area.
class LocationPickerSheet extends StatelessWidget {
  const LocationPickerSheet({
    super.key,
    this.currentLocation,
    this.onSelected,
  });

  final String? currentLocation;
  final ValueChanged<String>? onSelected;

  static const List<String> popularAreas = [
    'Quận 7, TP. Hồ Chí Minh',
    'TP. Thủ Đức, TP. Hồ Chí Minh',
    'Quận Bình Thạnh, TP. Hồ Chí Minh',
    'Quận 1, TP. Hồ Chí Minh',
    'Quận Phú Nhuận, TP. Hồ Chí Minh',
    'Quận Tân Bình, TP. Hồ Chí Minh',
  ];

  static Future<void> show(
    BuildContext context, {
    String? currentLocation,
    ValueChanged<String>? onSelected,
  }) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppSpacing.radiusSheet)),
      ),
      builder: (_) => LocationPickerSheet(
        currentLocation: currentLocation,
        onSelected: onSelected,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    CustomerHomeProvider? homeProvider;
    try {
      homeProvider = context.watch<CustomerHomeProvider>();
    } catch (_) {
      homeProvider = null;
    }

    final activeArea = currentLocation ?? homeProvider?.selectedArea ?? popularAreas.first;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: 12),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                margin: const EdgeInsets.only(bottom: AppSpacing.md),
                decoration: BoxDecoration(
                  color: AppColors.border,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            Row(
              children: [
                const Icon(Icons.location_on, color: AppColors.primary, size: 22),
                const SizedBox(width: 8),
                Text('Chọn khu vực tìm sân', style: AppTextStyles.title),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              'Chọn quận/huyện để xem các cụm sân và lịch trống gần bạn nhất',
              style: AppTextStyles.caption,
            ),
            const SizedBox(height: AppSpacing.md),
            const Divider(),
            ...popularAreas.map((area) {
              final isSelected = area == activeArea;
              return ListTile(
                contentPadding: EdgeInsets.zero,
                leading: Icon(
                  isSelected ? Icons.radio_button_checked : Icons.radio_button_off,
                  color: isSelected ? AppColors.primary : AppColors.textSecondary,
                  size: 20,
                ),
                title: Text(
                  area,
                  style: AppTextStyles.body.copyWith(
                    fontWeight: isSelected ? FontWeight.w600 : FontWeight.w400,
                    color: isSelected ? AppColors.primaryDark : AppColors.textPrimary,
                  ),
                ),
                trailing: isSelected
                    ? const Icon(Icons.check, color: AppColors.primary, size: 18)
                    : null,
                onTap: () async {
                  if (onSelected != null) {
                    onSelected!(area);
                  } else {
                    homeProvider?.setArea(area);
                  }
                  try {
                    await context.read<LocalStorageService>().saveSelectedArea(area);
                  } catch (_) {}
                  if (context.mounted) {
                    Navigator.of(context).pop();
                  }
                },
              );
            }),
            const SizedBox(height: AppSpacing.lg),
          ],
        ),
      ),
    );
  }
}
