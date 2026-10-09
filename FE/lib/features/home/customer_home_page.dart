import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/routing/route_names.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_tokens.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../auth/presentation/providers/auth_provider.dart';
import 'discovery_provider.dart';

class CustomerHomePage extends StatefulWidget {
  const CustomerHomePage({super.key});
  @override
  State<CustomerHomePage> createState() => _CustomerHomePageState();
}

class _CustomerHomePageState extends State<CustomerHomePage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final d = context.read<DiscoveryProvider>();
      if (!d.loaded && !d.loading) d.load();
    });
  }

  @override
  Widget build(BuildContext context) {
    final d = context.watch<DiscoveryProvider>();
    final name = context.select<AuthProvider, String>(
      (a) => a.user?.fullName.split(' ').last ?? 'bạn',
    );
    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: d.load,
          child: CustomScrollView(
            key: const PageStorageKey('home-scroll'),
            slivers: [
              SliverPadding(
                padding: const EdgeInsets.all(16),
                sliver: SliverList.list(
                  children: [
                    Row(
                      children: [
                        const BrandMark(),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'COURTGO',
                                style: AppTypography.eyebrow,
                              ),
                              Text(
                                'Xin chào, $name',
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              InkWell(
                                onTap: () => context.push(RouteNames.location),
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    vertical: 8,
                                  ),
                                  child: Row(
                                    children: [
                                      const Icon(
                                        Icons.location_on_outlined,
                                        size: 14,
                                        color: AppColors.primary,
                                      ),
                                      const SizedBox(width: 4),
                                      Flexible(
                                        child: Text(
                                          d.position != null
                                              ? 'Gần vị trí của bạn'
                                              : d.area.isEmpty
                                              ? 'Chọn khu vực tìm sân'
                                              : d.area,
                                          style: const TextStyle(
                                            fontSize: 12,
                                            color: AppColors.textSecondary,
                                          ),
                                        ),
                                      ),
                                      const Icon(Icons.expand_more, size: 16),
                                    ],
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: 'Thông báo',
                          onPressed: () =>
                              context.go(RouteNames.customerNotifications),
                          icon: const Icon(Icons.notifications_none_rounded),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    SearchField(
                      onSubmitted: (value) {
                        d.setSearch(value);
                        context.go(RouteNames.customerExplore);
                      },
                    ),
                    const SizedBox(height: 20),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(16),
                      child: SizedBox(
                        height: MediaQuery.textScalerOf(context).scale(150),
                        child: Stack(
                          fit: StackFit.expand,
                          children: [
                            const BrandHeroImage(),
                            Container(color: const Color(0xAA003D32)),
                            Padding(
                              padding: const EdgeInsets.all(20),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'SẴN SÀNG RA SÂN',
                                    style: TextStyle(
                                      color: AppColors.mint,
                                      fontSize: 10,
                                      letterSpacing: 1.5,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                  const SizedBox(height: 6),
                                  const Text(
                                    'Một trận hay.\nMột ngày khỏe.',
                                    style: TextStyle(
                                      color: Colors.white,
                                      fontSize: 23,
                                      fontWeight: FontWeight.w800,
                                      height: 1.15,
                                    ),
                                  ),
                                  const Spacer(),
                                  InkWell(
                                    onTap: () =>
                                        context.go(RouteNames.customerExplore),
                                    child: const Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Text(
                                          'Khám phá CourtGo',
                                          style: TextStyle(
                                            color: Colors.white,
                                            fontWeight: FontWeight.w600,
                                            fontSize: 12,
                                          ),
                                        ),
                                        SizedBox(width: 8),
                                        Icon(
                                          Icons.arrow_forward,
                                          color: Colors.white,
                                          size: 16,
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),
                    _QuickBooking(),
                    const SizedBox(height: 24),
                    const SectionHeader('Môn thể thao'),
                    const SizedBox(height: 12),
                    if (d.sports.isNotEmpty)
                      SingleChildScrollView(
                        scrollDirection: Axis.horizontal,
                        child: Row(
                          children: [
                            SportChip(
                              label: 'Tất cả',
                              selected: d.sportId == null,
                              onTap: () => d.selectSport(null),
                            ),
                            for (final sport in d.sports)
                              SportChip(
                                label: sport.name,
                                selected: d.sportId == sport.id,
                                onTap: () => d.selectSport(sport.id),
                              ),
                          ],
                        ),
                      ),
                    const SizedBox(height: 20),
                    SectionHeader(
                      d.position != null
                          ? 'Chi nhánh gần bạn'
                          : 'Chi nhánh CourtGo',
                      action: 'Xem tất cả',
                      onTap: () => context.go(RouteNames.customerExplore),
                    ),
                    const SizedBox(height: 8),
                  ],
                ),
              ),
              if (d.loading)
                const SliverToBoxAdapter(child: LoadingSkeleton(count: 2))
              else if (d.error != null)
                SliverToBoxAdapter(
                  child: NoticeState(
                    title: 'Chưa tải được chi nhánh',
                    message: d.error,
                    action: d.load,
                  ),
                )
              else if (d.centers.isEmpty)
                SliverToBoxAdapter(
                  child: NoticeState(
                    title: 'Chưa có chi nhánh phù hợp',
                    message: 'Thử chọn môn thể thao hoặc khu vực khác.',
                    action: () => d.selectArea(''),
                    actionLabel: 'Xem tất cả khu vực',
                  ),
                )
              else
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                  sliver: SliverList.separated(
                    itemCount: d.centers.length > 4 ? 4 : d.centers.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 16),
                    itemBuilder: (context, index) {
                      final c = d.centers[index];
                      final km = d.distance(c);
                      return CenterCard(
                        center: c,
                        distance: km == null
                            ? null
                            : '${km.toStringAsFixed(1)} km',
                        onTap: () => context.push('/customer/centers/${c.id}'),
                      );
                    },
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _QuickBooking extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final d = context.watch<DiscoveryProvider>();
    final date = d.quickDate ?? DateTime.now();
    final hour = d.quickHour ?? 19;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.bolt_rounded, color: AppColors.primary),
                SizedBox(width: 8),
                Text(
                  'Đặt sân nhanh',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: d.sportId,
                    isExpanded: true,
                    decoration: const InputDecoration(
                      labelText: 'Môn',
                      isDense: true,
                    ),
                    items: [
                      const DropdownMenuItem(
                        value: null,
                        child: Text('Tất cả'),
                      ),
                      ...d.sports.map(
                        (s) => DropdownMenuItem(
                          value: s.id,
                          child: Text(s.name, overflow: TextOverflow.ellipsis),
                        ),
                      ),
                    ],
                    onChanged: d.selectSport,
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _QuickField(
                    label: 'Ngày',
                    value: '${date.day}/${date.month}',
                    icon: Icons.calendar_today_outlined,
                    onTap: () async {
                      final now = DateTime.now();
                      final selected = await showDatePicker(
                        context: context,
                        initialDate: date,
                        firstDate: DateTime(now.year, now.month, now.day),
                        lastDate: now.add(const Duration(days: 90)),
                      );
                      if (selected != null) d.setQuick(selected, hour);
                    },
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _QuickField(
                    label: 'Giờ',
                    value: '${hour.toString().padLeft(2, '0')}:00',
                    icon: Icons.schedule,
                    onTap: () async {
                      final time = await showTimePicker(
                        context: context,
                        initialTime: TimeOfDay(hour: hour, minute: 0),
                      );
                      if (time != null) d.setQuick(date, time.hour);
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            PrimaryButton(
              label: 'Tìm sân',
              icon: Icons.search,
              onPressed: () {
                d.setQuick(date, hour);
                context.go(RouteNames.customerExplore);
              },
            ),
            const SizedBox(height: 8),
            const Text(
              'Chọn sân để xem giờ trống thực tế.',
              style: TextStyle(fontSize: 11, color: AppColors.textSecondary),
            ),
          ],
        ),
      ),
    );
  }
}

class _QuickField extends StatelessWidget {
  const _QuickField({
    required this.label,
    required this.value,
    required this.icon,
    required this.onTap,
  });
  final String label, value;
  final IconData icon;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(12),
    child: Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.lightMint,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 13, color: AppColors.primary),
              const SizedBox(width: 4),
              Text(
                label,
                style: const TextStyle(
                  fontSize: 11,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
          ),
        ],
      ),
    ),
  );
}
