import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/api/paged_controller.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/models/sport_model.dart';
import '../../shared/widgets/customer_widgets.dart';
import '../auth/presentation/providers/auth_provider.dart';
import 'staff_api.dart';
import 'staff_widgets.dart';

class StaffHomePage extends StatelessWidget {
  const StaffHomePage({super.key});
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(
      title: 'COURTGO STAFF',
      actions: [staffNotifications(context)],
    ),
    body: StaffData(
      identity: 'dashboard',
      load: (api) => api.get('dashboard'),
      builder: (data, reload) => StaffList(
        refresh: reload,
        children: [
          Text(
            context.read<StaffApi>().center!.name,
            style: const TextStyle(
              color: AppColors.primary,
              fontWeight: FontWeight.w700,
            ),
          ),
          Text('Hôm nay · ${CustomerFormat.date(data['date'])}'),
          LayoutBuilder(
            builder: (context, size) => Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                for (final entry in [
                  ('waitingCheckIn', 'Chờ check-in', Icons.login),
                  ('playing', 'Đang chơi', Icons.sports_tennis),
                  ('upcoming', 'Sắp tới', Icons.calendar_month),
                  ('needsAttention', 'Cần chú ý', Icons.error_outline),
                ])
                  SizedBox(
                    width: (size.maxWidth - 12) / 2,
                    child: Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Icon(entry.$3, color: AppColors.primary),
                            const SizedBox(height: 8),
                            Text(
                              '${data[entry.$1]}',
                              style: const TextStyle(
                                fontSize: 28,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                            Text(entry.$2),
                          ],
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          PrimaryButton(
            label: 'Quét QR khách đến',
            icon: Icons.qr_code_scanner,
            onPressed: () => context.go('/staff/check-in'),
          ),
          SecondaryButton(
            label: 'Tạo lịch đặt tại quầy',
            onPressed: () => context.push('/staff/walk-in'),
          ),
          SecondaryButton(
            label: 'Tình trạng sân',
            onPressed: () => context.go('/staff/courts'),
          ),
          const Text(
            'Lượt đặt tiếp theo',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
          ),
          if (jsonList(data['nextBookings']).isEmpty)
            const NoticeState(title: 'Chưa có lượt đặt sắp tới'),
          ...jsonList(data['nextBookings']).map(StaffBookingCard.new),
        ],
      ),
    ),
  );
}

class StaffSchedulePage extends StatefulWidget {
  const StaffSchedulePage({super.key});
  @override
  State<StaffSchedulePage> createState() => _StaffSchedulePageState();
}

class _StaffSchedulePageState extends State<StaffSchedulePage> {
  late final StaffApi api;
  late final PagedController<Json> pages;
  final search = TextEditingController();
  Timer? debounce;
  String? date, status, sport;
  List<SportModel> sports = [];
  @override
  void initState() {
    super.initState();
    api = context.read<StaffApi>();
    pages = PagedController((page) async {
      await api.ensureCenter();
      date ??= api.today;
      return api.schedule(
        page,
        date: date,
        status: status,
        sport: sport,
        search: search.text,
      );
    });
    api.addListener(reload);
    pages.load();
    api.api
        .sports()
        .then((items) {
          if (mounted) setState(() => sports = items);
        })
        .catchError((Object _) {});
  }

  void reload() => pages.load(reset: true);
  @override
  void dispose() {
    debounce?.cancel();
    search.dispose();
    api.removeListener(reload);
    pages.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(
      title: 'Lịch đặt',
      actions: [staffNotifications(context)],
    ),
    floatingActionButton: FloatingActionButton(
      tooltip: 'Tạo lịch tại quầy',
      onPressed: () => context.push('/staff/walk-in'),
      child: const Icon(Icons.add),
    ),
    body: ListenableBuilder(
      listenable: pages,
      builder: (context, _) => RefreshIndicator(
        onRefresh: () => pages.load(reset: true),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 90),
          children: [
            TextField(
              controller: search,
              maxLength: 200,
              decoration: const InputDecoration(
                hintText: 'Mã đặt, tên khách hoặc số điện thoại',
                prefixIcon: Icon(Icons.search),
                counterText: '',
              ),
              onChanged: (_) {
                debounce?.cancel();
                debounce = Timer(const Duration(milliseconds: 350), reload);
              },
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              icon: const Icon(Icons.calendar_month),
              label: Text(
                date == null ? 'Chọn ngày' : CustomerFormat.date(date!),
              ),
              onPressed: () async {
                await api.ensureCenter();
                if (!context.mounted) return;
                final value = await showDatePicker(
                  context: context,
                  initialDate: DateTime.parse(date ?? api.today!),
                  firstDate: DateTime(2020),
                  lastDate: DateTime(2100),
                );
                if (value != null && mounted) {
                  setState(() => date = CustomerFormat.day(value));
                  reload();
                }
              },
            ),
            const SizedBox(height: 8),
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (final entry in <(String?, String)>[
                    (null, 'Tất cả'),
                    ('Confirmed', 'Chờ check-in'),
                    ('CheckedIn', 'Đã check-in'),
                    ('InProgress', 'Đang chơi'),
                    ('PendingPayment', 'Chờ cọc'),
                    ('Completed', 'Hoàn tất'),
                    ('Cancelled', 'Đã hủy'),
                    ('Expired', 'Hết hạn'),
                    ('NoShow', 'Vắng mặt'),
                  ])
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        label: Text(entry.$2),
                        selected: status == entry.$1,
                        onSelected: (_) {
                          setState(() => status = entry.$1);
                          reload();
                        },
                      ),
                    ),
                ],
              ),
            ),
            DropdownButtonFormField<String>(
              initialValue: sport,
              decoration: const InputDecoration(labelText: 'Môn thể thao'),
              items: [
                const DropdownMenuItem(value: '', child: Text('Tất cả môn')),
                ...sports.map(
                  (s) => DropdownMenuItem(value: s.id, child: Text(s.name)),
                ),
              ],
              onChanged: (value) {
                sport = value == '' ? null : value;
                reload();
              },
            ),
            const SizedBox(height: 12),
            ...pages.items.map(
              (b) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: StaffBookingCard(b),
              ),
            ),
            if (pages.loading) const LoadingSkeleton(),
            if (pages.error != null)
              NoticeState(title: pages.error!, action: reload),
            if (!pages.loading && pages.error == null && pages.items.isEmpty)
              const NoticeState(
                title: 'Không có lịch đặt phù hợp',
                message: 'Thử đổi ngày hoặc bộ lọc.',
              ),
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

class StaffAccountPage extends StatelessWidget {
  const StaffAccountPage({super.key});
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Tài khoản'),
    body: StaffData(
      identity: 'account',
      load: (_) async => {},
      builder: (_, reload) {
        final auth = context.watch<AuthProvider>();
        final user = auth.user;
        return StaffList(
          refresh: reload,
          children: [
            StaffPanel(
              title: user?.fullName ?? 'Nhân viên',
              children: [
                const Text('Nhân viên CourtGo'),
                const SizedBox(height: 12),
                Text(user?.email ?? ''),
                Text(user?.phoneNumber ?? ''),
                const Divider(height: 28),
                const Text(
                  'Trung tâm được phân công',
                  style: TextStyle(color: AppColors.textSecondary),
                ),
                const SizedBox(height: 8),
                Text(context.read<StaffApi>().center!.name),
              ],
            ),
            SecondaryButton(
              label: 'Thông báo',
              onPressed: () => context.push('/staff/notifications'),
            ),
            SecondaryButton(
              label: 'Đăng xuất',
              onPressed: () async {
                await auth.logout();
                if (context.mounted) context.go('/login');
              },
            ),
          ],
        );
      },
    ),
  );
}
