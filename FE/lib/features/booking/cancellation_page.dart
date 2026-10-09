import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/network/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/currency_utils.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';

class CancellationPage extends StatefulWidget {
  const CancellationPage({super.key, required this.id});
  final String id;
  @override
  State<CancellationPage> createState() => _CancellationPageState();
}

class _CancellationPageState extends State<CancellationPage> {
  final reason = TextEditingController();
  final form = GlobalKey<FormState>();
  bool busy = false;
  String? error;
  int revision = 0;
  @override
  void dispose() {
    reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Yêu cầu hủy lịch'),
    body: RequestView<(Booking, Json)>(
      cacheKey: revision,
      load: () async {
        final api = context.read<CustomerApi>();
        final b = await api.booking(widget.id);
        return (b, await api.cancellationRequests(widget.id));
      },
      builder: (data, reload) {
        final booking = data.$1;
        final requests = jsonList(data.$2['items']);
        final pending = requests.any(
          (r) => [
            'Pending',
            'Approved',
            'RefundPending',
            'Completed',
          ].contains(r['status']),
        );
        return ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Text(
              booking.code,
              style: const TextStyle(
                color: AppColors.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              booking.center,
              style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 20),
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppColors.lightMint,
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Text(
                'Gửi yêu cầu chưa đồng nghĩa với hủy lịch. Sân vẫn được giữ cho đến khi yêu cầu được duyệt.',
                style: TextStyle(
                  fontSize: 13,
                  height: 1.6,
                  color: AppColors.primaryDark,
                ),
              ),
            ),
            const SizedBox(height: 20),
            for (final request in requests)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        StatusChip(request['status']),
                        const SizedBox(height: 12),
                        Text(request['reason']),
                        InfoLine(
                          'Mức hoàn tính theo chính sách',
                          CurrencyUtils.formatVnd(
                            amount(request['calculatedRefundAmount']),
                          ),
                        ),
                        if (request['approvedRefundAmount'] != null)
                          InfoLine(
                            'Số tiền hoàn được duyệt',
                            CurrencyUtils.formatVnd(
                              amount(request['approvedRefundAmount']),
                            ),
                            strong: true,
                          ),
                      ],
                    ),
                  ),
                ),
              ),
            if (booking.canCancel && !pending)
              Form(
                key: form,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'Lý do hủy',
                      style: TextStyle(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: reason,
                      maxLines: 4,
                      maxLength: 500,
                      decoration: const InputDecoration(
                        hintText: 'Chia sẻ lý do bạn muốn hủy lịch',
                      ),
                      validator: (v) => v == null || v.trim().isEmpty
                          ? 'Vui lòng nhập lý do hủy.'
                          : null,
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Số tiền hoàn được hệ thống tính theo chính sách của lịch đặt sau khi gửi yêu cầu.',
                      style: TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                        height: 1.5,
                      ),
                    ),
                    const SizedBox(height: 20),
                    PrimaryButton(
                      label: 'Gửi yêu cầu hủy',
                      loading: busy,
                      onPressed: () async {
                        if (busy || !form.currentState!.validate()) return;
                        setState(() {
                          busy = true;
                          error = null;
                        });
                        try {
                          await context.read<CustomerApi>().cancel(
                            widget.id,
                            reason.text.trim(),
                          );
                          if (mounted) setState(() => revision++);
                        } on ApiException catch (e) {
                          if (mounted) setState(() => error = e.message);
                        } finally {
                          if (mounted) setState(() => busy = false);
                        }
                      },
                    ),
                  ],
                ),
              )
            else if (requests.isEmpty)
              const NoticeState(
                title: 'Lịch đặt không thể yêu cầu hủy',
                message: 'Chỉ lịch đã xác nhận và chưa bắt đầu chơi mới có thể gửi yêu cầu.',
              ),
            if (error != null)
              Padding(
                padding: const EdgeInsets.only(top: 16),
                child: Text(
                  error!,
                  style: const TextStyle(color: AppColors.error),
                ),
              ),
          ],
        );
      },
    ),
  );
}
