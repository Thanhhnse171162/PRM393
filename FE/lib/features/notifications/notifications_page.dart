import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/api/paged_controller.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import 'notification_provider.dart';
import '../auth/presentation/providers/auth_provider.dart';
import '../../shared/models/user_model.dart';

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});
  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage> {
  late final PagedController<CustomerNotification> state;
  late final CustomerApi api;
  bool busy = false;
  @override
  void initState() {
    super.initState();
    api = context.read<CustomerApi>();
    api.addListener(refresh);
    state = PagedController(
      (page) => context.read<CustomerApi>().notifications(page),
    );
    state.load();
  }

  @override
  void dispose() {
    api.removeListener(refresh);
    state.dispose();
    super.dispose();
  }

  Future<void> refresh() async {
    await state.load(reset: true);
    if (mounted) context.read<NotificationProvider>().refresh();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(
      title: 'Thông báo',
      actions: [
        IconButton(
          tooltip: 'Đánh dấu tất cả đã đọc',
          onPressed: busy
              ? null
              : () async {
                  setState(() => busy = true);
                  try {
                    await context.read<CustomerApi>().readAll();
                    await refresh();
                  } on ApiException catch (e) {
                    if (context.mounted) showMessage(context, e.message);
                  } finally {
                    if (mounted) setState(() => busy = false);
                  }
                },
          icon: const Icon(Icons.done_all),
        ),
      ],
    ),
    body: ListenableBuilder(
      listenable: state,
      builder: (context, _) => RefreshIndicator(
        onRefresh: refresh,
        child: ListView.builder(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          itemCount: state.items.length + 1,
          itemBuilder: (context, i) {
            if (i < state.items.length) {
              final n = state.items[i];
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Card(
                  color: n.read ? Colors.white : AppColors.lightMint,
                  child: InkWell(
                    onTap: busy
                        ? null
                        : () async {
                            setState(() => busy = true);
                            try {
                              if (!n.read) {
                                await context
                                    .read<CustomerApi>()
                                    .readNotification(n.id);
                              }
                              if (!context.mounted) return;
                              context.read<NotificationProvider>().refresh();
                              if (n.referenceType == 'Booking' &&
                                  n.referenceId != null) {
                                await context.push(
                                  '/${context.read<AuthProvider>().role == UserRole.staff ? 'staff' : 'customer'}/bookings/${n.referenceId}',
                                );
                              }
                              if (mounted) await refresh();
                            } on ApiException catch (e) {
                              if (context.mounted) {
                                showMessage(context, e.message);
                              }
                            } finally {
                              if (mounted) setState(() => busy = false);
                            }
                          },
                    borderRadius: BorderRadius.circular(16),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: AppColors.mint,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Icon(
                              switch (n.type) {
                                'Payment' || 'Refund' =>
                                  Icons.account_balance_wallet_outlined,
                                'Cancellation' => Icons.event_busy_outlined,
                                'Booking' => Icons.event_available_outlined,
                                _ => Icons.notifications_none,
                              },
                              color: AppColors.primary,
                              size: 20,
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  n.title,
                                  style: TextStyle(
                                    fontWeight: n.read
                                        ? FontWeight.w600
                                        : FontWeight.w800,
                                    fontSize: 14,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  n.message,
                                  style: const TextStyle(
                                    color: AppColors.textSecondary,
                                    fontSize: 13,
                                    height: 1.5,
                                  ),
                                ),
                                const SizedBox(height: 8),
                                Text(
                                  CustomerFormat.date(n.createdAt),
                                  style: const TextStyle(
                                    color: AppColors.textSecondary,
                                    fontSize: 11,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          if (!n.read)
                            const Padding(
                              padding: EdgeInsets.only(left: 8, top: 4),
                              child: Icon(
                                Icons.circle,
                                size: 7,
                                color: AppColors.primary,
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                ),
              );
            }
            if (state.loading) return const LoadingSkeleton(count: 2);
            if (state.error != null) {
              return NoticeState(
                title: 'Chưa tải được thông báo',
                message: state.error,
                action: () => state.load(),
              );
            }
            if (state.items.isEmpty) {
              return const NoticeState(
                title: 'Bạn đã cập nhật mọi thứ',
                message:
                    'Thông báo về lịch đặt và thanh toán sẽ xuất hiện tại đây.',
                icon: Icons.notifications_none_rounded,
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
  );
}
