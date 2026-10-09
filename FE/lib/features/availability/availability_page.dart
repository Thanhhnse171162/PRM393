import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_tokens.dart';
import '../../core/utils/customer_format.dart';
import '../../core/utils/currency_utils.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../auth/presentation/providers/auth_provider.dart';
import '../home/discovery_provider.dart';
import 'availability_provider.dart';

class AvailabilityPage extends StatelessWidget {
  const AvailabilityPage({super.key, required this.courtId});
  final String courtId;
  @override
  Widget build(BuildContext context) => ChangeNotifierProvider(
    create: (c) => AvailabilityProvider(
      c.read<CustomerApi>(),
      courtId,
      c.read<DiscoveryProvider>().quickDate ?? DateTime.now(),
    )..load(),
    child: const _AvailabilityView(),
  );
}

class _AvailabilityView extends StatelessWidget {
  const _AvailabilityView();
  @override
  Widget build(BuildContext context) {
    final state = context.watch<AvailabilityProvider>();
    final data = state.data;
    final today = DateTime.now();
    final dates = List.generate(
      7,
      (i) =>
          DateTime(today.year, today.month, today.day).add(Duration(days: i)),
    );
    return Scaffold(
      appBar: CourtGoAppBar(
        title: 'Chọn ngày & giờ',
        actions: [
          IconButton(
            tooltip: 'Làm mới giờ trống',
            onPressed: state.holding ? null : () => state.load(),
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: CustomScrollView(
        slivers: [
          SliverPadding(
            padding: const EdgeInsets.all(16),
            sliver: SliverList.list(
              children: [
                if (data != null) ...[
                  Text(
                    data.court['centerName'] as String,
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    data.court['name'] as String,
                    style: const TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 16),
                ],
                Row(
                  children: [
                    const Expanded(
                      child: Text(
                        'Chọn ngày chơi',
                        style: TextStyle(fontWeight: FontWeight.w700),
                      ),
                    ),
                    TextButton.icon(
                      onPressed: state.holding
                          ? null
                          : () async {
                              final date = await showDatePicker(
                                context: context,
                                initialDate: state.date.isBefore(today)
                                    ? today
                                    : state.date,
                                firstDate: DateTime(
                                  today.year,
                                  today.month,
                                  today.day,
                                ),
                                lastDate: today.add(const Duration(days: 180)),
                              );
                              if (date != null) state.load(date);
                            },
                      icon: const Icon(Icons.calendar_month, size: 17),
                      label: Text(
                        CustomerFormat.date(CustomerFormat.day(state.date)),
                      ),
                    ),
                  ],
                ),
                SizedBox(
                  height: 78,
                  child: ListView.separated(
                    scrollDirection: Axis.horizontal,
                    itemCount: dates.length,
                    separatorBuilder: (_, _) => const SizedBox(width: 8),
                    itemBuilder: (context, i) {
                      final date = dates[i];
                      final active =
                          CustomerFormat.day(date) ==
                          CustomerFormat.day(state.date);
                      return InkWell(
                        key: ValueKey('date-${CustomerFormat.day(date)}'),
                        onTap: state.holding ? null : () => state.load(date),
                        borderRadius: BorderRadius.circular(12),
                        child: AnimatedContainer(
                          duration: AppDurations.fast,
                          width: 62,
                          decoration: BoxDecoration(
                            color: active ? AppColors.primary : Colors.white,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: active
                                  ? AppColors.primary
                                  : AppColors.border,
                            ),
                          ),
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Text(
                                i == 0
                                    ? 'Hôm nay'
                                    : date.weekday == 7
                                    ? 'CN'
                                    : 'Th ${date.weekday + 1}',
                                style: TextStyle(
                                  fontSize: 11,
                                  color: active
                                      ? Colors.white
                                      : AppColors.textSecondary,
                                ),
                              ),
                              const SizedBox(height: 6),
                              Text(
                                '${date.day}',
                                style: TextStyle(
                                  fontSize: 22,
                                  fontWeight: FontWeight.w800,
                                  color: active
                                      ? Colors.white
                                      : AppColors.textPrimary,
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
                const SizedBox(height: 16),
                const Wrap(
                  spacing: 16,
                  runSpacing: 8,
                  children: [
                    _Legend(AppColors.primary, 'Đang chọn'),
                    _Legend(AppColors.border, 'Đã đặt'),
                    _Legend(AppColors.warning, 'Đang giữ'),
                    _Legend(AppColors.textSecondary, 'Bị khóa'),
                  ],
                ),
                const SizedBox(height: 16),
                const Text(
                  'Mỗi khung giờ 60 phút. Chọn các giờ liên tiếp trên cùng sân.',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                if (state.error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 12),
                    child: Text(
                      state.error!,
                      style: const TextStyle(color: AppColors.error),
                    ),
                  ),
                const SizedBox(height: 8),
              ],
            ),
          ),
          if (state.loading)
            const SliverToBoxAdapter(child: LoadingSkeleton(count: 2))
          else if (data == null)
            SliverToBoxAdapter(
              child: NoticeState(
                title: 'Chưa tải được lịch sân',
                action: () => state.load(),
              ),
            )
          else if (data.closed || data.slots.isEmpty)
            const SliverToBoxAdapter(
              child: NoticeState(
                title: 'Không có khung giờ khả dụng',
                message: 'Chi nhánh đóng cửa hoặc không còn giờ có thể đặt. Hãy chọn ngày khác.',
              ),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
              sliver: SliverLayoutBuilder(
                builder: (context, constraints) => SliverGrid.builder(
                  gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: constraints.crossAxisExtent < 330 ? 1 : 2,
                    mainAxisExtent: MediaQuery.textScalerOf(context).scale(105),
                    mainAxisSpacing: 10,
                    crossAxisSpacing: 10,
                  ),
                  itemCount: data.slots.length,
                  itemBuilder: (context, i) {
                    final slot = data.slots[i];
                    final selected = state.selected.any(
                      (s) => s.start == slot.start,
                    );
                    final held = slot.status == 'Held';
                    final color = selected
                        ? AppColors.primary
                        : held
                        ? const Color(0xFFFFF4DC)
                        : slot.available
                        ? Colors.white
                        : const Color(0xFFF0F2F5);
                    return Semantics(
                      key: ValueKey('slot-${slot.start}'),
                      selected: selected,
                      enabled: slot.available,
                      button: true,
                      child: InkWell(
                        onTap: slot.available && !state.holding
                            ? () {
                                final error = state.toggle(slot);
                                if (error != null) showMessage(context, error);
                              }
                            : null,
                        borderRadius: BorderRadius.circular(12),
                        child: AnimatedContainer(
                          duration: AppDurations.fast,
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: color,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: selected
                                  ? AppColors.primary
                                  : AppColors.border,
                            ),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Text(
                                      CustomerFormat.interval(
                                        slot.start,
                                        slot.end,
                                      ),
                                      style: TextStyle(
                                        fontWeight: FontWeight.w700,
                                        fontSize: 14,
                                        color: selected
                                            ? Colors.white
                                            : slot.available
                                            ? AppColors.textPrimary
                                            : AppColors.textSecondary,
                                      ),
                                    ),
                                  ),
                                  Icon(
                                    selected
                                        ? Icons.check_circle
                                        : slot.available
                                        ? Icons.circle_outlined
                                        : held
                                        ? Icons.schedule
                                        : Icons.lock_outline,
                                    size: 15,
                                    color: selected
                                        ? Colors.white
                                        : AppColors.textSecondary,
                                  ),
                                ],
                              ),
                              Text(
                                slot.available
                                    ? CurrencyUtils.formatVnd(slot.price)
                                    : {
                                            'Held': 'Đang giữ chỗ',
                                            'Booked': 'Đã đặt',
                                            'Blocked': 'Không khả dụng',
                                          }[slot.status] ??
                                          'Không khả dụng',
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: selected
                                      ? Colors.white
                                      : held
                                      ? const Color(0xFF9A6700)
                                      : AppColors.primary,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ),
        ],
      ),
      bottomNavigationBar: ActionDock(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    '${state.selected.length} giờ đã chọn',
                    style: const TextStyle(fontSize: 13),
                  ),
                ),
                PriceText(state.estimatedTotal, large: true),
              ],
            ),
            const SizedBox(height: 4),
            const Text(
              'Tạm tính · Giá chính thức xác nhận khi giữ sân',
              style: TextStyle(fontSize: 11, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 12),
            PrimaryButton(
              label: 'Tiếp tục',
              loading: state.holding,
              onPressed: state.selected.isEmpty || state.loading
                  ? null
                  : () async {
                      final auth = context.read<AuthProvider>();
                      if (!auth.isAuthenticated) {
                        context.push('/login');
                        return;
                      }
                      final booking = await state.hold();
                      if (context.mounted && booking != null) {
                        context.push(
                          '/customer/bookings/${booking.id}/checkout',
                          extra: booking,
                        );
                      }
                    },
            ),
          ],
        ),
      ),
    );
  }
}

class _Legend extends StatelessWidget {
  const _Legend(this.color, this.text);
  final Color color;
  final String text;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Container(
        width: 9,
        height: 9,
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(3),
        ),
      ),
      const SizedBox(width: 6),
      Text(
        text,
        style: const TextStyle(fontSize: 11, color: AppColors.textSecondary),
      ),
    ],
  );
}
