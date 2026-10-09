import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/api/paged_controller.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';

class BookingHistoryPage extends StatefulWidget {
  const BookingHistoryPage({super.key});
  @override
  State<BookingHistoryPage> createState() => _BookingHistoryPageState();
}

class _BookingHistoryPageState extends State<BookingHistoryPage> {
  String group = 'upcoming';
  late final PagedController<Booking> state;
  late final CustomerApi api;
  void refreshBookings() => state.load(reset: true);
  @override
  void initState() {
    super.initState();
    api = context.read<CustomerApi>();
    api.addListener(refreshBookings);
    state = PagedController(
      (page) => context.read<CustomerApi>().bookings(group, page),
    );
    state.load();
  }

  @override
  void dispose() {
    api.removeListener(refreshBookings);
    state.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Lịch đặt của tôi'),
    body: ListenableBuilder(
      listenable: state,
      builder: (context, _) => Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: SizedBox(
              width: double.infinity,
              child: SegmentedButton<String>(
                segments: const [
                  ButtonSegment(value: 'upcoming', label: Text('Sắp tới')),
                  ButtonSegment(value: 'completed', label: Text('Hoàn thành')),
                  ButtonSegment(value: 'cancelled', label: Text('Đã hủy')),
                ],
                selected: {group},
                showSelectedIcon: false,
                onSelectionChanged: (value) {
                  setState(() => group = value.first);
                  state.load(reset: true);
                },
              ),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => state.load(reset: true),
              child: ListView.builder(
                key: PageStorageKey('bookings-$group'),
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                physics: const AlwaysScrollableScrollPhysics(),
                itemCount: state.items.length + 1,
                itemBuilder: (context, i) {
                  if (i < state.items.length) {
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: BookingCard(
                        booking: state.items[i],
                        onTap: () async {
                          await context.push(
                            '/customer/bookings/${state.items[i].id}',
                          );
                          if (mounted) state.load(reset: true);
                        },
                      ),
                    );
                  }
                  if (state.loading) return const LoadingSkeleton(count: 2);
                  if (state.error != null) {
                    return NoticeState(
                      title: 'Chưa tải được lịch đặt',
                      message: state.error,
                      action: () => state.load(),
                    );
                  }
                  if (state.items.isEmpty) {
                    return NoticeState(
                      title: 'Chưa có lịch đặt',
                      message:
                          'Trận đấu tiếp theo của bạn bắt đầu tại CourtGo.',
                      icon: Icons.calendar_month_outlined,
                      action: () => context.go('/customer/explore'),
                      actionLabel: 'Khám phá sân',
                    );
                  }
                  return state.hasMore
                      ? TextButton(
                          onPressed: () => state.load(),
                          child: const Text('Xem thêm'),
                        )
                      : const SizedBox.shrink();
                },
              ),
            ),
          ),
        ],
      ),
    ),
  );
}
