import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../../core/config/app_config.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/theme/app_tokens.dart';
import '../../core/utils/currency_utils.dart';
import '../../core/utils/customer_format.dart';
import '../../core/network/api_exception.dart';
import '../models/customer_models.dart';

class CourtGoAppBar extends AppBar {
  CourtGoAppBar({super.key, required String title, super.actions})
    : super(
        title: Text(
          title,
          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
        ),
      );
}

/// Decorative CourtGo artwork from the supplied Stitch login export.
/// The viewport excludes the baked-in navigation at the top of the artwork.
class BrandHeroImage extends StatelessWidget {
  const BrandHeroImage({super.key});
  @override
  Widget build(BuildContext context) => ClipRect(
    child: Transform.scale(
      scale: 1.5,
      alignment: Alignment.bottomCenter,
      child: Image.asset(
        'assets/images/auth/stitch-courtgo.jpg',
        fit: BoxFit.cover,
        alignment: Alignment.bottomCenter,
        excludeFromSemantics: true,
      ),
    ),
  );
}

class BrandMark extends StatelessWidget {
  const BrandMark({super.key, this.size = 40});
  final double size;
  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(
      color: AppColors.primary,
      borderRadius: BorderRadius.circular(12),
    ),
    child: Icon(
      Icons.sports_tennis_rounded,
      color: AppColors.mint,
      size: size * .6,
    ),
  );
}

class PrimaryButton extends StatelessWidget {
  const PrimaryButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.icon,
  });
  final String label;
  final VoidCallback? onPressed;
  final bool loading;
  final IconData? icon;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: double.infinity,
    height: 50,
    child: FilledButton(
      onPressed: loading ? null : onPressed,
      style: FilledButton.styleFrom(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.button),
        ),
      ),
      child: loading
          ? const SizedBox.square(
              dimension: 20,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (icon != null) ...[
                  Icon(icon, size: 19),
                  const SizedBox(width: 8),
                ],
                Flexible(
                  child: Text(
                    label,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
              ],
            ),
    ),
  );
}

class SecondaryButton extends StatelessWidget {
  const SecondaryButton({
    super.key,
    required this.label,
    required this.onPressed,
  });
  final String label;
  final VoidCallback? onPressed;
  @override
  Widget build(BuildContext context) => SizedBox(
    height: 48,
    child: OutlinedButton(onPressed: onPressed, child: Text(label)),
  );
}

class SearchField extends StatelessWidget {
  const SearchField({
    super.key,
    this.controller,
    this.onChanged,
    this.onSubmitted,
    this.hint = 'Tìm chi nhánh CourtGo',
  });
  final TextEditingController? controller;
  final ValueChanged<String>? onChanged, onSubmitted;
  final String hint;
  @override
  Widget build(BuildContext context) => TextField(
    controller: controller,
    onChanged: onChanged,
    onSubmitted: onSubmitted,
    textInputAction: TextInputAction.search,
    decoration: InputDecoration(
      hintText: hint,
      prefixIcon: const Icon(Icons.search_rounded),
      isDense: true,
    ),
  );
}

String statusLabel(String status) => switch (status) {
  'PendingPayment' => 'Chờ đặt cọc',
  'Confirmed' => 'Đã xác nhận',
  'CheckedIn' => 'Đã check-in',
  'InProgress' => 'Đang chơi',
  'Completed' => 'Hoàn thành',
  'Cancelled' => 'Đã hủy',
  'Expired' => 'Hết thời gian giữ',
  'NoShow' => 'Không đến',
  'Unpaid' => 'Chưa thanh toán',
  'DepositPaid' => 'Đã đặt cọc',
  'FullyPaid' => 'Đã thanh toán đủ',
  'RefundPending' => 'Chờ hoàn tiền',
  'PartiallyRefunded' => 'Đã hoàn một phần',
  'Refunded' => 'Đã hoàn tiền',
  'Pending' => 'Đang chờ',
  'Succeeded' => 'Thành công',
  'Failed' => 'Chưa thành công',
  'Rejected' => 'Đã từ chối',
  'Approved' => 'Đã duyệt',
  _ => status,
};

class StatusChip extends StatelessWidget {
  const StatusChip(this.status, {super.key});
  final String status;
  @override
  Widget build(BuildContext context) {
    final waiting = [
      'PendingPayment',
      'Pending',
      'RefundPending',
      'Unpaid',
    ].contains(status);
    final bad = [
      'Cancelled',
      'Expired',
      'NoShow',
      'Failed',
      'Rejected',
    ].contains(status);
    final color = bad
        ? AppColors.error
        : waiting
        ? const Color(0xFF9A6700)
        : AppColors.primary;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .08),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        statusLabel(status),
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class SportChip extends StatelessWidget {
  const SportChip({
    super.key,
    required this.label,
    required this.selected,
    required this.onTap,
  });
  final String label;
  final bool selected;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(right: 8),
    child: ChoiceChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) => onTap(),
      selectedColor: AppColors.mint,
      showCheckmark: false,
      side: BorderSide(color: selected ? AppColors.primary : AppColors.border),
    ),
  );
}

String resolveSportImageUrl(String value) {
  final trimmed = value.trim();
  if (RegExp(r'^/api/images/[a-f0-9]{32}\.(jpg|png)$').hasMatch(trimmed)) {
    return Uri.parse(AppConfig.apiBaseUrl).resolve(trimmed).toString();
  }
  return trimmed;
}

bool isSportImageUrl(String value) {
  value = resolveSportImageUrl(value);
  final uri = Uri.tryParse(value.trim());
  return uri != null &&
      (uri.scheme == 'http' || uri.scheme == 'https') &&
      uri.host.isNotEmpty &&
      !RegExp(r'\s').hasMatch(value.trim());
}

class SportImage extends StatelessWidget {
  const SportImage({
    super.key,
    this.url,
    this.height = 144,
    this.width = double.infinity,
    this.borderRadius = 0,
    this.showMessages = false,
  });
  final String? url;
  final double height;
  final double width;
  final double borderRadius;
  final bool showMessages;

  @override
  Widget build(BuildContext context) {
    final source = resolveSportImageUrl(url ?? '');
    Widget fallback(String message, {bool loading = false}) => ColoredBox(
      color: AppColors.lightMint,
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (loading)
              const SizedBox.square(
                dimension: 24,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            else
              Icon(
                Icons.sports_tennis_rounded,
                size: height < 80 ? 28 : 48,
                color: AppColors.primary,
              ),
            if (showMessages) ...[
              const SizedBox(height: 8),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Text(
                  message,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
    return ClipRRect(
      borderRadius: BorderRadius.circular(borderRadius),
      child: SizedBox(
        height: height,
        width: width,
        child: source.isEmpty
            ? fallback('Chưa có ảnh cover')
            : !isSportImageUrl(source)
            ? fallback(
                'URL ảnh không hợp lệ. Hãy nhập địa chỉ http:// hoặc https://.',
              )
            : CachedNetworkImage(
                key: ValueKey(source),
                imageUrl: source,
                height: height,
                width: width,
                fit: BoxFit.cover,
                memCacheWidth: width.isFinite ? (width * 3).round() : 1000,
                placeholder: (_, _) =>
                    fallback('Đang tải ảnh...', loading: true),
                errorWidget: (_, _, _) => fallback(
                  'Không thể tải ảnh. Kiểm tra URL và kết nối mạng.',
                ),
              ),
      ),
    );
  }
}

class CenterCard extends StatelessWidget {
  const CenterCard({
    super.key,
    required this.center,
    required this.onTap,
    this.distance,
  });
  final CenterInfo center;
  final VoidCallback onTap;
  final String? distance;
  @override
  Widget build(BuildContext context) => Card(
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Stack(
            children: [
              SportImage(url: center.image),
              Positioned(
                left: 12,
                top: 12,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 9,
                    vertical: 5,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(7),
                  ),
                  child: const Text(
                    'COURTGO',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 10,
                      letterSpacing: 1,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ),
            ],
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  center.name,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  center.address,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: AppTextStyles.caption.copyWith(
                    color: AppColors.textSecondary,
                    height: 1.4,
                  ),
                ),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 4,
                  children: [
                    Text(
                      center.sports.join(' · '),
                      style: const TextStyle(fontSize: 12),
                    ),
                    if (distance != null)
                      Text(
                        distance!,
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                  ],
                ),
                if (center.priceFrom != null) ...[
                  const SizedBox(height: 8),
                  PriceText(center.priceFrom!, prefix: 'Từ ', suffix: '/giờ'),
                ],
              ],
            ),
          ),
        ],
      ),
    ),
  );
}

class CourtCard extends StatelessWidget {
  const CourtCard({super.key, required this.court, required this.onTap});
  final Court court;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Card(
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(10),
              child: SizedBox(
                width: 76,
                child: SportImage(url: court.image, height: 76),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    court.name,
                    maxLines: 2,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    court.sportName,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 6),
                  PriceText(court.price, suffix: '/giờ'),
                ],
              ),
            ),
            const Icon(Icons.chevron_right_rounded, color: AppColors.primary),
          ],
        ),
      ),
    ),
  );
}

class BookingCard extends StatelessWidget {
  const BookingCard({super.key, required this.booking, required this.onTap});
  final Booking booking;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Card(
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    booking.code,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
                StatusChip(booking.status),
              ],
            ),
            const SizedBox(height: 12),
            Text(
              booking.center,
              style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
            ),
            const SizedBox(height: 4),
            Text(
              booking.court,
              style: const TextStyle(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                const Icon(
                  Icons.calendar_today_outlined,
                  size: 16,
                  color: AppColors.primary,
                ),
                const SizedBox(width: 8),
                Flexible(
                  child: Text(
                    '${CustomerFormat.date(booking.start)} · ${CustomerFormat.interval(booking.start, booking.end)}',
                    style: const TextStyle(fontSize: 13),
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            Row(
              children: [
                Expanded(child: PriceText(booking.total)),
                Text(
                  'Còn ${CurrencyUtils.formatVnd(booking.remaining)}',
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

class PriceText extends StatelessWidget {
  const PriceText(
    this.amount, {
    super.key,
    this.prefix = '',
    this.suffix = '',
    this.large = false,
  });
  final num amount;
  final String prefix, suffix;
  final bool large;
  @override
  Widget build(BuildContext context) => Text(
    '$prefix${CurrencyUtils.formatVnd(amount)}$suffix',
    style: TextStyle(
      fontSize: large ? 24 : 14,
      fontWeight: FontWeight.w800,
      color: AppColors.primary,
    ),
  );
}

class SectionHeader extends StatelessWidget {
  const SectionHeader(this.title, {super.key, this.action, this.onTap});
  final String title;
  final String? action;
  final VoidCallback? onTap;
  @override
  Widget build(BuildContext context) => Row(
    children: [
      Expanded(
        child: Text(
          title,
          style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w700),
        ),
      ),
      if (action != null) TextButton(onPressed: onTap, child: Text(action!)),
    ],
  );
}

class LoadingSkeleton extends StatelessWidget {
  const LoadingSkeleton({super.key, this.count = 3});
  final int count;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(16),
    child: Column(
      children: [
        for (var i = 0; i < count; i++)
          Padding(
            padding: const EdgeInsets.only(bottom: 16),
            child: Container(
              height: 120,
              decoration: BoxDecoration(
                color: AppColors.border.withValues(alpha: .6),
                borderRadius: BorderRadius.circular(16),
              ),
            ),
          ),
      ],
    ),
  );
}

class NoticeState extends StatelessWidget {
  const NoticeState({
    super.key,
    required this.title,
    this.message,
    this.icon = Icons.search_off_rounded,
    this.action,
    this.actionLabel = 'Thử lại',
  });
  final String title;
  final String? message;
  final IconData icon;
  final VoidCallback? action;
  final String actionLabel;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 40, color: AppColors.primary),
        const SizedBox(height: 16),
        Text(
          title,
          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 17),
          textAlign: TextAlign.center,
        ),
        if (message != null) ...[
          const SizedBox(height: 8),
          Text(
            message!,
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.textSecondary),
          ),
        ],
        if (action != null) ...[
          const SizedBox(height: 16),
          TextButton.icon(
            onPressed: action,
            icon: const Icon(Icons.refresh),
            label: Text(actionLabel),
          ),
        ],
      ],
    ),
  );
}

class RequestView<T> extends StatefulWidget {
  const RequestView({
    super.key,
    required this.load,
    required this.builder,
    required this.cacheKey,
  });
  final Future<T> Function() load;
  final Widget Function(T data, VoidCallback reload) builder;
  final Object cacheKey;
  @override
  State<RequestView<T>> createState() => _RequestViewState<T>();
}

class _RequestViewState<T> extends State<RequestView<T>> {
  late Future<T> future;
  @override
  void initState() {
    super.initState();
    future = widget.load();
  }

  @override
  void didUpdateWidget(covariant RequestView<T> oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.cacheKey != oldWidget.cacheKey) future = widget.load();
  }

  void reload() => setState(() => future = widget.load());
  @override
  Widget build(BuildContext context) => FutureBuilder<T>(
    future: future,
    builder: (context, snapshot) {
      if (snapshot.connectionState != ConnectionState.done) {
        return const SingleChildScrollView(child: LoadingSkeleton());
      }
      if (snapshot.hasError) {
        return Center(
          child: NoticeState(
            title: 'Chưa thể tải nội dung',
            message: snapshot.error is ApiException
                ? (snapshot.error as ApiException).message
                : 'Vui lòng thử lại sau.',
            icon: Icons.wifi_off_rounded,
            action: reload,
          ),
        );
      }
      return widget.builder(snapshot.data as T, reload);
    },
  );
}

class BottomSheetContainer extends StatelessWidget {
  const BottomSheetContainer({super.key, required this.child});
  final Widget child;
  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    child: Padding(
      padding: EdgeInsets.fromLTRB(
        20,
        12,
        20,
        MediaQuery.viewInsetsOf(context).bottom + 20,
      ),
      child: child,
    ),
  );
}

class ActionDock extends StatelessWidget {
  const ActionDock({super.key, required this.child});
  final Widget child;
  @override
  Widget build(BuildContext context) => Container(
    decoration: const BoxDecoration(
      color: Colors.white,
      border: Border(top: BorderSide(color: AppColors.border)),
    ),
    child: SafeArea(
      top: false,
      child: Padding(padding: const EdgeInsets.all(16), child: child),
    ),
  );
}

class InfoLine extends StatelessWidget {
  const InfoLine(this.label, this.value, {super.key, this.strong = false});
  final String label, value;
  final bool strong;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 7),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Text(
            label,
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 13,
            ),
          ),
        ),
        const SizedBox(width: 12),
        Flexible(
          child: Text(
            value,
            textAlign: TextAlign.right,
            style: TextStyle(
              fontSize: 14,
              fontWeight: strong ? FontWeight.w700 : FontWeight.w500,
            ),
          ),
        ),
      ],
    ),
  );
}

void showMessage(BuildContext context, String text) =>
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));
