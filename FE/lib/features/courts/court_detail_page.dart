import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/theme/app_colors.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';

class CourtDetailPage extends StatelessWidget {
  const CourtDetailPage({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Chi tiết sân'),
    body: RequestView<Court>(
      cacheKey: id,
      load: () => context.read<CustomerApi>().court(id),
      builder: (court, reload) => Column(
        children: [
          Expanded(
            child: ListView(
              children: [
                SportImage(url: court.image, height: 240),
                Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        court.centerName,
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        court.name,
                        style: const TextStyle(
                          fontSize: 26,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 12),
                      PriceText(court.price, large: true, suffix: '/giờ'),
                      const SizedBox(height: 8),
                      const Text(
                        'Giá từng khung giờ được hiển thị khi chọn lịch.',
                        style: TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 12,
                        ),
                      ),
                      const SizedBox(height: 24),
                      InfoLine('Môn thể thao', court.sportName),
                      if (court.surface != null && court.surface!.isNotEmpty)
                        InfoLine('Mặt sân', court.surface!),
                      if (court.description != null &&
                          court.description!.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        const SectionHeader('Về sân'),
                        const SizedBox(height: 12),
                        Text(
                          court.description!,
                          style: const TextStyle(
                            height: 1.6,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
          ActionDock(
            child: PrimaryButton(
              label: 'Chọn ngày & giờ',
              icon: Icons.calendar_month_outlined,
              onPressed: court.status == 'Active'
                  ? () => context.push('/customer/courts/$id/availability')
                  : null,
            ),
          ),
        ],
      ),
    ),
  );
}
