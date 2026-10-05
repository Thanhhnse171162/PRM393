import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../../core/routing/route_names.dart';
import '../../../../../core/theme/app_colors.dart';
import '../../../../../core/theme/app_spacing.dart';
import '../providers/customer_home_provider.dart';

class QuickBookingCard extends StatelessWidget {
  const QuickBookingCard({super.key});

  @override
  Widget build(BuildContext context) {
    final homeProvider = context.watch<CustomerHomeProvider>();

    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.xs,
      ),
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: const Color(0xFFE5E7EB)),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
              blurRadius: 10,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        padding: const EdgeInsets.all(16),
        child: Stack(
          children: [
            // Decorative background shuttlecock watermark in top right
            Positioned(
              right: -10,
              top: -10,
              child: Opacity(
                opacity: 0.08,
                child: Icon(
                  Icons.sports_tennis_rounded,
                  size: 110,
                  color: AppColors.primary,
                ),
              ),
            ),

            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Top Header Row
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Flash / Bolt Icon
                    Container(
                      width: 40,
                      height: 40,
                      decoration: const BoxDecoration(
                        color: Color(0xFF064E3B),
                        shape: BoxShape.circle,
                      ),
                      child: const Center(
                        child: Icon(
                          Icons.bolt_rounded,
                          color: Color(0xFF34D399),
                          size: 24,
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),

                    // Title & Description
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: const [
                          Text(
                            'Đặt sân nhanh',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          SizedBox(height: 2),
                          Text(
                            'Chọn môn, thời gian và tìm sân trống ngay',
                            style: TextStyle(
                              fontSize: 11.5,
                              color: Color(0xFF6B7280),
                            ),
                          ),
                        ],
                      ),
                    ),

                    // "Trực tuyến" Live Badge
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 3.5,
                      ),
                      decoration: BoxDecoration(
                        color: const Color(0xFFECFDF5),
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: const Color(0xFFA7F3D0)),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            width: 6,
                            height: 6,
                            decoration: const BoxDecoration(
                              color: Color(0xFF10B981),
                              shape: BoxShape.circle,
                            ),
                          ),
                          const SizedBox(width: 4),
                          const Text(
                            'Trực tuyến',
                            style: TextStyle(
                              color: Color(0xFF047857),
                              fontSize: 10.5,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 16),

                // 3 Selectors (Sport, Date, Time)
                Row(
                  children: [
                    // 1. Sport Selector
                    Expanded(
                      child: _buildSelectorTile(
                        context: context,
                        iconWidget: const Text('🏸', style: TextStyle(fontSize: 16)),
                        label: 'MÔN',
                        value: homeProvider.selectedSport,
                        onTap: () => _showSportPicker(context, homeProvider),
                      ),
                    ),
                    const SizedBox(width: 8),

                    // 2. Date Selector
                    Expanded(
                      child: _buildSelectorTile(
                        context: context,
                        iconWidget: const Icon(
                          Icons.calendar_today_rounded,
                          size: 16,
                          color: Color(0xFF3B82F6),
                        ),
                        label: 'NGÀY',
                        value: homeProvider.quickDate,
                        onTap: () => _pickDate(context, homeProvider),
                      ),
                    ),
                    const SizedBox(width: 8),

                    // 3. Time Selector
                    Expanded(
                      child: _buildSelectorTile(
                        context: context,
                        iconWidget: const Icon(
                          Icons.access_time_rounded,
                          size: 16,
                          color: Color(0xFFF59E0B),
                        ),
                        label: 'GIỜ',
                        value: homeProvider.quickTime,
                        onTap: () => _pickTime(context, homeProvider),
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 14),

                // Find Available Courts CTA Button
                SizedBox(
                  width: double.infinity,
                  height: 48,
                  child: ElevatedButton(
                    onPressed: () {
                      context.go(RouteNames.customerExplore);
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF064E3B),
                      foregroundColor: Colors.white,
                      elevation: 0,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(24),
                      ),
                      padding: const EdgeInsets.symmetric(horizontal: 20),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.search_rounded, size: 20, color: Colors.white),
                        const SizedBox(width: 10),
                        Container(
                          width: 1,
                          height: 18,
                          color: Colors.white.withValues(alpha: 0.3),
                        ),
                        const SizedBox(width: 10),
                        const Text(
                          'Tìm sân trống',
                          style: TextStyle(
                            fontSize: 14.5,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 0.2,
                          ),
                        ),
                        const SizedBox(width: 8),
                        const Icon(
                          Icons.arrow_forward_rounded,
                          size: 18,
                          color: Colors.white,
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSelectorTile({
    required BuildContext context,
    required Widget iconWidget,
    required String label,
    required String value,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        height: 68,
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
        decoration: BoxDecoration(
          color: const Color(0xFFF9FAFB),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFFE5E7EB)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Row(
              children: [
                iconWidget,
                const SizedBox(width: 4),
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 9.5,
                    fontWeight: FontWeight.w700,
                    color: Color(0xFF6B7280),
                  ),
                ),
                const Spacer(),
                const Icon(
                  Icons.keyboard_arrow_down_rounded,
                  size: 14,
                  color: Color(0xFF9CA3AF),
                ),
              ],
            ),
            Text(
              value,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                fontSize: 12.5,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showSportPicker(BuildContext context, CustomerHomeProvider provider) {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 20, vertical: 8),
                  child: Text(
                    'Chọn môn thể thao',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ),
                for (final sport in provider.sports)
                  ListTile(
                    leading: Text(sport.iconUrl ?? '🏸', style: const TextStyle(fontSize: 22)),
                    title: Text(
                      sport.name,
                      style: TextStyle(
                        fontWeight: provider.selectedSport == sport.name
                            ? FontWeight.w700
                            : FontWeight.w500,
                        color: provider.selectedSport == sport.name
                            ? AppColors.primary
                            : AppColors.textPrimary,
                      ),
                    ),
                    trailing: provider.selectedSport == sport.name
                        ? const Icon(Icons.check_circle_rounded, color: AppColors.primary)
                        : null,
                    onTap: () {
                      provider.setSelectedSport(sport.name);
                      Navigator.pop(ctx);
                    },
                  ),
              ],
            ),
          ),
        );
      },
    );
  }

  Future<void> _pickDate(BuildContext context, CustomerHomeProvider provider) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: now,
      firstDate: now,
      lastDate: now.add(const Duration(days: 30)),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.primary,
              onPrimary: Colors.white,
              onSurface: AppColors.textPrimary,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      if (picked.year == now.year && picked.month == now.month && picked.day == now.day) {
        provider.setQuickDate('Hôm nay');
      } else {
        provider.setQuickDate('${picked.day.toString().padLeft(2, '0')}/${picked.month.toString().padLeft(2, '0')}');
      }
    }
  }

  Future<void> _pickTime(BuildContext context, CustomerHomeProvider provider) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: const TimeOfDay(hour: 19, minute: 0),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.primary,
              onPrimary: Colors.white,
              onSurface: AppColors.textPrimary,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      final hour = picked.hour.toString().padLeft(2, '0');
      final minute = picked.minute.toString().padLeft(2, '0');
      provider.setQuickTime('$hour:$minute');
    }
  }
}
