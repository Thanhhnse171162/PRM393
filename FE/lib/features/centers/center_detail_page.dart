import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/theme/app_colors.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../home/discovery_provider.dart';

class CenterDetailPage extends StatelessWidget {
  const CenterDetailPage({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Chi nhánh CourtGo'),
    body: RequestView<CenterInfo>(
      cacheKey: id,
      load: () => context.read<CustomerApi>().center(id),
      builder: (center, reload) {
        final sportId = context.read<DiscoveryProvider>().sportId;
        final courts = sportId == null
            ? center.courts
            : center.courts.where((c) => c.sportId == sportId).toList();
        return CustomScrollView(
          slivers: [
            SliverToBoxAdapter(
              child: SportImage(url: center.image, height: 210),
            ),
            SliverPadding(
              padding: const EdgeInsets.all(20),
              sliver: SliverList.list(
                children: [
                  const Text(
                    'COURTGO · CHI NHÁNH',
                    style: TextStyle(
                      fontSize: 11,
                      letterSpacing: 1.4,
                      color: AppColors.primary,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    center.name,
                    style: const TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    center.address,
                    style: const TextStyle(
                      color: AppColors.textSecondary,
                      height: 1.5,
                    ),
                  ),
                  const SizedBox(height: 16),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: center.sports
                        .map(
                          (s) => Chip(
                            label: Text(s),
                            backgroundColor: AppColors.lightMint,
                          ),
                        )
                        .toList(),
                  ),
                  if (center.phone != null) InfoLine('Liên hệ', center.phone!),
                  const SizedBox(height: 16),
                  const SectionHeader('Giờ hoạt động'),
                  const SizedBox(height: 8),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Column(
                        children: [
                          for (final h in center.hours)
                            InfoLine(
                              _day(h['dayOfWeek']),
                              h['isClosed'] == true
                                  ? 'Đóng cửa'
                                  : '${(h['openTime'] as String).substring(0, 5)} – ${(h['closeTime'] as String).substring(0, 5)}',
                            ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  SecondaryButton(
                    label: 'Xem đánh giá',
                    onPressed: () =>
                        context.push('/customer/centers/$id/reviews'),
                  ),
                  const SizedBox(height: 24),
                  SectionHeader('Chọn sân · ${courts.length}'),
                  const SizedBox(height: 12),
                  if (courts.isEmpty)
                    const NoticeState(
                      title: 'Chưa có sân phù hợp',
                      message: 'Thử chọn môn thể thao khác.',
                    ),
                ],
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
              sliver: SliverList.separated(
                itemCount: courts.length,
                separatorBuilder: (_, _) => const SizedBox(height: 12),
                itemBuilder: (context, i) => CourtCard(
                  court: courts[i],
                  onTap: () => context.push('/customer/courts/${courts[i].id}'),
                ),
              ),
            ),
          ],
        );
      },
    ),
  );
  String _day(dynamic day) => switch (day.toString()) {
    'Monday' => 'Thứ Hai',
    'Tuesday' => 'Thứ Ba',
    'Wednesday' => 'Thứ Tư',
    'Thursday' => 'Thứ Năm',
    'Friday' => 'Thứ Sáu',
    'Saturday' => 'Thứ Bảy',
    'Sunday' => 'Chủ nhật',
    _ => day.toString(),
  };
}
