import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_colors.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import 'staff_api.dart';

class StaffData extends StatelessWidget {
  const StaffData({
    super.key,
    required this.identity,
    required this.load,
    required this.builder,
  });
  final String identity;
  final Future<Json> Function(StaffApi) load;
  final Widget Function(Json, VoidCallback) builder;
  @override
  Widget build(BuildContext context) {
    final api = context.watch<StaffApi>();
    return RequestView<Json>(
      cacheKey: '$identity-${api.revision}',
      load: () async {
        await api.ensureCenter();
        return load(api);
      },
      builder: builder,
    );
  }
}

class StaffList extends StatelessWidget {
  const StaffList({super.key, required this.children, required this.refresh});
  final List<Widget> children;
  final VoidCallback refresh;
  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: () async => refresh(),
    child: ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 28),
      children: [
        for (final child in children)
          Padding(padding: const EdgeInsets.only(bottom: 12), child: child),
      ],
    ),
  );
}

class StaffPanel extends StatelessWidget {
  const StaffPanel({super.key, required this.title, required this.children});
  final String title;
  final List<Widget> children;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 12),
          ...children,
        ],
      ),
    ),
  );
}

class StaffBookingCard extends StatelessWidget {
  const StaffBookingCard(this.booking, {super.key});
  final Json booking;
  @override
  Widget build(BuildContext context) {
    final api = context.read<StaffApi>();
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () => context.push('/staff/bookings/${booking['bookingId']}'),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                api.interval(booking),
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  Text('${booking['courtName']} · ${booking['sportName']}'),
                  StatusChip(booking['bookingStatus']),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                '${booking['customerName']} · ${booking['customerPhone']}',
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 8),
              Text(
                booking['bookingCode'],
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const Divider(height: 24),
              Row(
                children: [
                  Expanded(child: StatusChip(booking['paymentStatus'])),
                  const Text('Chi tiết'),
                  const Icon(Icons.chevron_right, size: 20),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

Widget staffNotifications(BuildContext context) => IconButton(
  tooltip: 'Thông báo',
  onPressed: () => context.push('/staff/notifications'),
  icon: const Icon(Icons.notifications_outlined),
);

Future<bool> staffConfirm(
  BuildContext context,
  String title,
  String description,
) async =>
    await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      builder: (sheet) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 12),
              Text(description),
              const SizedBox(height: 24),
              PrimaryButton(
                label: 'Xác nhận',
                onPressed: () => Navigator.pop(sheet, true),
              ),
              TextButton(
                onPressed: () => Navigator.pop(sheet, false),
                child: const Text('Quay lại'),
              ),
            ],
          ),
        ),
      ),
    ) ??
    false;
