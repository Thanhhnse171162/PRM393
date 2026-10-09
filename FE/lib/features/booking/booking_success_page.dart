import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/currency_utils.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';

class BookingSuccessPage extends StatelessWidget {
  const BookingSuccessPage({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Lịch đặt của bạn'),
    body: RequestView<Booking>(
      cacheKey: id,
      load: () => context.read<CustomerApi>().booking(id),
      builder: (b, reload) => ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 12),
          const Icon(
            Icons.check_circle_rounded,
            size: 72,
            color: AppColors.primary,
          ),
          const SizedBox(height: 20),
          Text(
            b.status == 'Confirmed'
                ? 'Sân đã sẵn sàng!'
                : statusLabel(b.status),
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          const Text(
            'Hẹn gặp bạn tại CourtGo.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textSecondary),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  Text(
                    b.code,
                    style: const TextStyle(
                      fontSize: 19,
                      fontWeight: FontWeight.w800,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(height: 16),
                  InfoLine('Chi nhánh', b.center),
                  InfoLine('Sân', b.court),
                  InfoLine('Ngày', CustomerFormat.date(b.start)),
                  InfoLine('Giờ chơi', CustomerFormat.interval(b.start, b.end)),
                  const Divider(),
                  InfoLine('Đã thanh toán', CurrencyUtils.formatVnd(b.paid)),
                  InfoLine(
                    'Còn lại tại sân',
                    CurrencyUtils.formatVnd(b.remaining),
                    strong: true,
                  ),
                  const SizedBox(height: 8),
                  StatusChip(b.status),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          if (b.hasQr)
            PrimaryButton(
              label: 'Xem mã QR',
              icon: Icons.qr_code,
              onPressed: () => context.push('/customer/bookings/$id/qr'),
            ),
          const SizedBox(height: 12),
          SecondaryButton(
            label: 'Xem lịch đặt',
            onPressed: () => context.go('/customer/bookings'),
          ),
          const SizedBox(height: 8),
          TextButton(
            onPressed: () => context.go('/customer/home'),
            child: const Text('Về trang chủ'),
          ),
        ],
      ),
    ),
  );
}
