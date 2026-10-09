import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/routing/route_names.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../home/discovery_provider.dart';

class ExplorePage extends StatefulWidget {
  const ExplorePage({super.key});
  @override
  State<ExplorePage> createState() => _ExplorePageState();
}

class _ExplorePageState extends State<ExplorePage> {
  late final TextEditingController search;
  @override
  void initState() {
    super.initState();
    final d = context.read<DiscoveryProvider>();
    search = TextEditingController(text: d.search);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!d.loaded && !d.loading) d.load();
    });
  }

  @override
  void dispose() {
    search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final d = context.watch<DiscoveryProvider>();
    return Scaffold(
      appBar: CourtGoAppBar(
        title: 'Khám phá CourtGo',
        actions: [
          IconButton(
            tooltip: 'Chọn khu vực',
            onPressed: () => context.push(RouteNames.location),
            icon: const Icon(Icons.tune_rounded),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: d.load,
        child: CustomScrollView(
          key: const PageStorageKey('explore-scroll'),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.all(16),
              sliver: SliverList.list(
                children: [
                  SearchField(controller: search, onChanged: d.setSearch),
                  const SizedBox(height: 12),
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: [
                        SportChip(
                          label: 'Tất cả',
                          selected: d.sportId == null,
                          onTap: () => d.selectSport(null),
                        ),
                        for (final s in d.sports)
                          SportChip(
                            label: s.name,
                            selected: d.sportId == s.id,
                            onTap: () => d.selectSport(s.id),
                          ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      const Icon(
                        Icons.location_on_outlined,
                        size: 16,
                        color: AppColors.primary,
                      ),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          d.area.isEmpty ? 'Tất cả khu vực' : d.area,
                          style: const TextStyle(
                            fontSize: 13,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ),
                      Text(
                        '${d.centers.length} chi nhánh',
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                  if (d.quickDate != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 12),
                      child: Text(
                        'Ngày dự kiến: ${CustomerFormat.date(CustomerFormat.day(d.quickDate!))} · ${d.quickHour}:00\nGiờ trống được kiểm tra khi chọn sân.',
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.primary,
                        ),
                      ),
                    ),
                ],
              ),
            ),
            if (d.loading)
              const SliverToBoxAdapter(child: LoadingSkeleton())
            else if (d.error != null)
              SliverToBoxAdapter(
                child: NoticeState(
                  title: 'Chưa thể tải chi nhánh',
                  message: d.error,
                  action: d.load,
                ),
              )
            else if (d.centers.isEmpty)
              const SliverToBoxAdapter(
                child: NoticeState(
                  title: 'Không có kết quả phù hợp',
                  message: 'Thử thay đổi môn thể thao hoặc khu vực.',
                ),
              )
            else
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                sliver: SliverList.separated(
                  itemCount: d.centers.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 16),
                  itemBuilder: (context, i) => CenterCard(
                    center: d.centers[i],
                    onTap: () =>
                        context.push('/customer/centers/${d.centers[i].id}'),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
