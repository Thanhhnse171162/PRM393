import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../core/api/customer_api.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';

class BookingQrPage extends StatelessWidget {
  const BookingQrPage({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Mã QR check-in'),
    body: RequestView<Json>(
      cacheKey: id,
      load: () => context.read<CustomerApi>().qr(id),
      builder: (qr, reload) => ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 16),
          const Text(
            'Sẵn sàng ra sân',
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 25, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          const Text(
            'Đưa mã này cho nhân viên CourtGo khi đến sân.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textSecondary, height: 1.5),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                children: [
                  LayoutBuilder(
                    builder: (context, constraints) => QrImageView(
                      data: qr['qrValue'] as String,
                      size: constraints.maxWidth.clamp(160, 300),
                      backgroundColor: Colors.white,
                      padding: const EdgeInsets.all(16),
                      semanticsLabel: 'Mã QR check-in lịch đặt',
                    ),
                  ),
                  const Divider(height: 32),
                  Text(
                    qr['bookingCode'] as String,
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 18,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(qr['centerName'] as String, textAlign: TextAlign.center),
                  const SizedBox(height: 6),
                  Text(qr['courtName'] as String, textAlign: TextAlign.center),
                  const SizedBox(height: 16),
                  Text(
                    CustomerFormat.date(qr['startAt']),
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 6),
                  Text(CustomerFormat.interval(qr['startAt'], qr['endAt'])),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          const Text(
            'Vui lòng thanh toán phần còn lại với nhân viên trước khi check-in.',
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
        ],
      ),
    ),
  );
}
