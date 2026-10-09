import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/paged_controller.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import 'staff_api.dart';
import 'staff_widgets.dart';

String courtStateLabel(String value) => switch (value) {
  'Available' => 'Sẵn sàng',
  'WaitingCustomer' => 'Chờ khách',
  'Playing' => 'Đang chơi',
  'Blocked' => 'Đang khóa',
  'Maintenance' => 'Bảo trì',
  'Inactive' => 'Ngừng hoạt động',
  _ => value,
};
String blockLabel(String value) => switch (value) {
  'TemporaryBlock' => 'Khóa tạm',
  'Maintenance' => 'Bảo trì',
  'PrivateEvent' => 'Sự kiện riêng',
  'Other' => 'Khác',
  _ => value,
};

class StaffCourtsPage extends StatefulWidget {
  const StaffCourtsPage({super.key});
  @override
  State<StaffCourtsPage> createState() => _StaffCourtsPageState();
}

class _StaffCourtsPageState extends State<StaffCourtsPage> {
  late final StaffApi api;
  late final PagedController<Json> pages;
  @override
  void initState() {
    super.initState();
    api = context.read<StaffApi>();
    pages = PagedController((page) async {
      await api.ensureCenter();
      return PageResult.fromJson(
        await api.get('courts', query: {'pageNumber': page, 'pageSize': 20}),
        (j) => j,
      );
    });
    api.addListener(reload);
    pages.load();
  }

  void reload() => pages.load(reset: true);
  @override
  void dispose() {
    api.removeListener(reload);
    pages.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(
      title: 'Quản lý sân',
      actions: [staffNotifications(context)],
    ),
    body: ListenableBuilder(
      listenable: pages,
      builder: (context, _) => RefreshIndicator(
        onRefresh: () => pages.load(reset: true),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            if (api.center != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Text(api.center!.name),
              ),
            ...pages.items.map(
              (court) => Card(
                child: ListTile(
                  contentPadding: const EdgeInsets.all(16),
                  leading: const Icon(Icons.sports_tennis),
                  title: Text(
                    court['name'],
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  subtitle: Text(
                    '${court['sportName']}\n${courtStateLabel(court['operationalState'])}',
                  ),
                  isThreeLine: true,
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () =>
                      context.push('/staff/courts/${court['courtId']}'),
                ),
              ),
            ),
            if (pages.loading) const LoadingSkeleton(),
            if (pages.error != null)
              NoticeState(title: pages.error!, action: reload),
            if (!pages.loading && pages.items.isEmpty && pages.error == null)
              const NoticeState(title: 'Chưa có sân được phân công'),
            if (!pages.loading && pages.hasMore)
              TextButton(
                onPressed: () => pages.load(),
                child: const Text('Xem thêm'),
              ),
          ],
        ),
      ),
    ),
  );
}

class StaffCourtDetailPage extends StatefulWidget {
  const StaffCourtDetailPage({super.key, required this.id});
  final String id;
  @override
  State<StaffCourtDetailPage> createState() => _StaffCourtDetailPageState();
}

class _StaffCourtDetailPageState extends State<StaffCourtDetailPage> {
  bool busy = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Vận hành sân'),
    body: StaffData(
      identity: 'court-${widget.id}',
      load: (api) async {
        final result = await Future.wait([
          api.get('courts/${widget.id}'),
          api.allPages('courts/${widget.id}/blocks'),
          api.allPages(
            'bookings',
            query: {'date': CustomerFormat.day(api.now)},
          ),
        ]);
        return {'court': result[0], 'blocks': result[1], 'bookings': result[2]};
      },
      builder: (data, reload) {
        final api = context.read<StaffApi>();
        final court = data['court'] as Json;
        final today = jsonList(data['bookings'])
            .where((b) => b['courtId'] == widget.id)
            .toList();
        final active = today
            .where(
              (b) => [
                'Confirmed',
                'CheckedIn',
                'InProgress',
              ].contains(b['bookingStatus']),
            )
            .toList();
        final current = active.where(
          (b) =>
              !DateTime.parse(b['startAt']).isAfter(api.now) &&
              DateTime.parse(b['endAt']).isAfter(api.now),
        );
        final next = active.where(
          (b) => DateTime.parse(b['startAt']).isAfter(api.now),
        );
        return StaffList(
          refresh: reload,
          children: [
            StaffPanel(
              title: court['name'],
              children: [
                Text(court['sportName']),
                const SizedBox(height: 8),
                Text(
                  courtStateLabel(court['operationalState']),
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
            const Text(
              'Lượt hiện tại',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
            if (current.isEmpty)
              const Text('Không có lượt đang sử dụng.')
            else
              StaffBookingCard(current.first),
            const Text(
              'Lượt tiếp theo hôm nay',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
            if (next.isEmpty)
              const Text('Chưa có lượt tiếp theo.')
            else
              StaffBookingCard(next.first),
            PrimaryButton(
              label: 'Tạo khóa sân / bảo trì',
              onPressed: () => context.push('/staff/courts/${widget.id}/block'),
            ),
            StaffPanel(
              title: 'Lịch khóa sân',
              children: [
                if (jsonList(data['blocks']).isEmpty)
                  const Text('Không có lịch khóa sân.'),
                ...jsonList(data['blocks']).map(
                  (block) => Padding(
                    padding: const EdgeInsets.only(bottom: 16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          blockLabel(block['type']),
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                        Text(
                          '${api.local(block['startAt']).substring(0, 16).replaceFirst('T', ' ')} → ${api.local(block['endAt']).substring(0, 16).replaceFirst('T', ' ')}',
                        ),
                        Text(block['reason']),
                        if (DateTime.parse(block['endAt']).isAfter(api.now))
                          TextButton(
                            onPressed: busy
                                ? null
                                : () async {
                                    if (!await staffConfirm(
                                      context,
                                      'Gỡ khóa sân?',
                                      'Khóa đang diễn ra sẽ kết thúc ngay. Khóa trong tương lai sẽ được gỡ bỏ.',
                                    ))
                                      return;
                                    setState(() => busy = true);
                                    try {
                                      await api.removeBlock(
                                        widget.id,
                                        block['id'],
                                      );
                                    } catch (e) {
                                      if (context.mounted)
                                        showMessage(context, staffError(e));
                                    } finally {
                                      if (mounted) setState(() => busy = false);
                                    }
                                  },
                            child: const Text('Gỡ khóa'),
                          ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
            const Text(
              'Toàn bộ lịch hôm nay',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            if (today.isEmpty)
              const Text('Chưa có lịch đặt.')
            else
              ...today.map(StaffBookingCard.new),
          ],
        );
      },
    ),
  );
}

class StaffCreateBlockPage extends StatefulWidget {
  const StaffCreateBlockPage({super.key, required this.courtId});
  final String courtId;
  @override
  State<StaffCreateBlockPage> createState() => _StaffCreateBlockPageState();
}

class _StaffCreateBlockPageState extends State<StaffCreateBlockPage> {
  final form = GlobalKey<FormState>();
  final reason = TextEditingController();
  String type = 'Maintenance';
  DateTime? start, end;
  bool busy = false;
  @override
  void dispose() {
    reason.dispose();
    super.dispose();
  }

  Future<void> pick(bool beginning) async {
    final api = context.read<StaffApi>();
    final initial =
        (beginning ? start : end) ??
        api.now.add(Duration(hours: beginning ? 1 : 2));
    final date = await showDatePicker(
      context: context,
      initialDate: DateTime(initial.year, initial.month, initial.day),
      firstDate: DateTime(api.now.year, api.now.month, api.now.day),
      lastDate: DateTime(api.now.year + 2, 12, 31),
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(hour: initial.hour, minute: initial.minute),
    );
    if (time != null && mounted)
      setState(() {
        final value = DateTime(
          date.year,
          date.month,
          date.day,
          time.hour,
          time.minute,
        );
        if (beginning) {
          start = value;
        } else {
          end = value;
        }
      });
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Tạo khóa sân'),
    body: StaffData(
      identity: 'block-form',
      load: (_) async => {},
      builder: (_, reload) => Form(
        key: form,
        child: StaffList(
          refresh: reload,
          children: [
            Text(
              'Thời gian theo trung tâm ${context.read<StaffApi>().center!.name}',
            ),
            DropdownButtonFormField<String>(
              initialValue: type,
              decoration: const InputDecoration(labelText: 'Loại khóa'),
              items: [
                for (final t in [
                  'TemporaryBlock',
                  'Maintenance',
                  'PrivateEvent',
                  'Other',
                ])
                  DropdownMenuItem(value: t, child: Text(blockLabel(t))),
              ],
              onChanged: busy ? null : (v) => setState(() => type = v!),
            ),
            SecondaryButton(
              label: start == null
                  ? 'Chọn thời gian bắt đầu'
                  : 'Từ: ${start.toString().substring(0, 16)}',
              onPressed: busy ? null : () => pick(true),
            ),
            SecondaryButton(
              label: end == null
                  ? 'Chọn thời gian kết thúc'
                  : 'Đến: ${end.toString().substring(0, 16)}',
              onPressed: busy ? null : () => pick(false),
            ),
            TextFormField(
              controller: reason,
              maxLength: 500,
              minLines: 2,
              maxLines: 4,
              decoration: const InputDecoration(labelText: 'Lý do khóa sân'),
              validator: (v) =>
                  v == null || v.trim().isEmpty ? 'Nhập lý do' : null,
            ),
            PrimaryButton(
              label: 'Xác nhận khóa sân',
              loading: busy,
              onPressed: () async {
                if (!form.currentState!.validate()) return;
                if (start == null || end == null || !end!.isAfter(start!)) {
                  showMessage(
                    context,
                    'Chọn thời gian bắt đầu và kết thúc hợp lệ.',
                  );
                  return;
                }
                final api = context.read<StaffApi>();
                final startAt = api.instant(start!, start!.hour, start!.minute);
                final endAt = api.instant(end!, end!.hour, end!.minute);
                if (!DateTime.parse(startAt).isAfter(api.now)) {
                  showMessage(context, 'Thời gian bắt đầu phải ở tương lai.');
                  return;
                }
                setState(() => busy = true);
                try {
                  await api.command('courts/${widget.courtId}/blocks', {
                    'type': type,
                    'startAt': startAt,
                    'endAt': endAt,
                    'reason': reason.text.trim(),
                  });
                  if (context.mounted) context.pop();
                } catch (e) {
                  if (context.mounted) showMessage(context, staffError(e));
                } finally {
                  if (mounted) setState(() => busy = false);
                }
              },
            ),
          ],
        ),
      ),
    ),
  );
}
