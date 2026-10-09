import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api/customer_api.dart';
import '../../core/api/paged_controller.dart';
import '../../core/network/api_exception.dart';
import '../../core/storage/local_storage_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../auth/presentation/providers/auth_provider.dart';

class ReviewPage extends StatefulWidget {
  const ReviewPage({super.key, required this.bookingId});
  final String bookingId;
  @override
  State<ReviewPage> createState() => _ReviewPageState();
}

class _ReviewPageState extends State<ReviewPage> {
  int rating = 5;
  bool busy = false, submitted = false;
  String? error;
  final comment = TextEditingController();
  @override
  void dispose() {
    comment.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Đánh giá trải nghiệm'),
    body: RequestView<Booking>(
      cacheKey: widget.bookingId,
      load: () => context.read<CustomerApi>().booking(widget.bookingId),
      builder: (b, reload) {
        final userId = context.read<AuthProvider>().user!.id;
        final reviewed =
            submitted ||
            context.read<LocalStorageService>().reviewed(userId, b.id);
        if (!b.completed) {
          return const NoticeState(
            title: 'Chưa thể đánh giá',
            message: 'Bạn có thể đánh giá sau khi lịch đặt hoàn thành.',
          );
        }
        if (reviewed) {
          return const NoticeState(
            title: 'Cảm ơn đánh giá của bạn',
            message: 'Mỗi lịch đặt được gửi một đánh giá.',
            icon: Icons.star_rounded,
          );
        }
        return ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const Icon(
              Icons.sports_tennis_rounded,
              size: 52,
              color: AppColors.primary,
            ),
            const SizedBox(height: 20),
            const Text(
              'Trận đấu vừa rồi thế nào?',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 23, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 8),
            Text(
              '${b.center} · ${b.court}',
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 24),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                for (var star = 1; star <= 5; star++)
                  IconButton(
                    tooltip: '$star sao',
                    onPressed: busy
                        ? null
                        : () => setState(() => rating = star),
                    icon: Icon(
                      star <= rating
                          ? Icons.star_rounded
                          : Icons.star_outline_rounded,
                      color: const Color(0xFFE9A020),
                      size: 36,
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 24),
            TextField(
              controller: comment,
              maxLines: 5,
              maxLength: 1000,
              decoration: const InputDecoration(
                hintText: 'Chia sẻ trải nghiệm của bạn (không bắt buộc)',
              ),
            ),
            if (error != null)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Text(
                  error!,
                  style: const TextStyle(color: AppColors.error),
                ),
              ),
            const SizedBox(height: 20),
            PrimaryButton(
              label: 'Gửi đánh giá',
              loading: busy,
              onPressed: () async {
                if (busy) return;
                setState(() {
                  busy = true;
                  error = null;
                });
                try {
                  await context.read<CustomerApi>().review(
                    b.id,
                    rating,
                    comment.text,
                  );
                  if (!context.mounted) return;
                  await context.read<LocalStorageService>().saveReviewed(
                    userId,
                    b.id,
                  );
                  if (mounted) setState(() => submitted = true);
                } on ApiException catch (e) {
                  if (!context.mounted) return;
                  if (e.code == 'REVIEW_ALREADY_SUBMITTED') {
                    await context.read<LocalStorageService>().saveReviewed(
                      userId,
                      b.id,
                    );
                    if (mounted) setState(() => submitted = true);
                  } else {
                    setState(() => error = e.message);
                  }
                } finally {
                  if (mounted) setState(() => busy = false);
                }
              },
            ),
          ],
        );
      },
    ),
  );
}

class CenterReviewsPage extends StatefulWidget {
  const CenterReviewsPage({super.key, required this.centerId});
  final String centerId;
  @override
  State<CenterReviewsPage> createState() => _CenterReviewsPageState();
}

class _CenterReviewsPageState extends State<CenterReviewsPage> {
  late final PagedController<Json> state;
  double average = 0;
  int total = 0;
  @override
  void initState() {
    super.initState();
    state = PagedController((page) async {
      final response = await context.read<CustomerApi>().reviews(
        widget.centerId,
        page,
      );
      average = amount(response['averageRating']);
      total = response['totalReviews'] as int;
      return PageResult(
        jsonList(response['items']),
        response['pageNumber'],
        response['totalPages'],
      );
    });
    state.load();
  }

  @override
  void dispose() {
    state.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Đánh giá chi nhánh'),
    body: ListenableBuilder(
      listenable: state,
      builder: (context, _) => RefreshIndicator(
        onRefresh: () => state.load(reset: true),
        child: ListView.builder(
          padding: const EdgeInsets.all(20),
          itemCount: state.items.length + 2,
          itemBuilder: (context, i) {
            if (i == 0) {
              return Padding(
                padding: const EdgeInsets.only(bottom: 24),
                child: Row(
                  children: [
                    Text(
                      total == 0 ? '—' : average.toStringAsFixed(1),
                      style: const TextStyle(
                        fontSize: 40,
                        fontWeight: FontWeight.w800,
                        color: AppColors.primary,
                      ),
                    ),
                    const SizedBox(width: 16),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Trải nghiệm tại CourtGo',
                          style: TextStyle(fontWeight: FontWeight.w700),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '$total đánh giá',
                          style: const TextStyle(
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              );
            }
            if (i <= state.items.length) {
              final r = state.items[i - 1];
              return Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                r['reviewerDisplayName'],
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                            const Icon(
                              Icons.star,
                              size: 16,
                              color: Color(0xFFE9A020),
                            ),
                            Text(' ${r['rating']}'),
                          ],
                        ),
                        const SizedBox(height: 8),
                        if (r['comment'] != null)
                          Text(
                            r['comment'],
                            style: const TextStyle(height: 1.5),
                          ),
                        const SizedBox(height: 8),
                        Text(
                          CustomerFormat.date(r['createdAt']),
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            }
            if (state.loading) return const LoadingSkeleton(count: 2);
            if (state.error != null) {
              return NoticeState(
                title: 'Chưa tải được đánh giá',
                message: state.error,
                action: () => state.load(),
              );
            }
            if (state.items.isEmpty) {
              return const NoticeState(
                title: 'Chưa có đánh giá',
                icon: Icons.star_outline,
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
