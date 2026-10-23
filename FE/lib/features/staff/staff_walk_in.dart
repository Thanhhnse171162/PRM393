import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/widgets/customer_widgets.dart';
import 'staff_api.dart';
import 'staff_widgets.dart';

bool consecutiveStaffSlots(List<Slot> slots) {
  if (slots.isEmpty ||
      slots.any(
        (s) =>
            !s.available ||
            DateTime.parse(s.end)
                    .difference(DateTime.parse(s.start))
                    .inMinutes !=
                60,
      )) {
    return false;
  }
  final sorted = [
    ...slots,
  ]..sort((a, b) => DateTime.parse(a.start).compareTo(DateTime.parse(b.start)));
  for (var i = 1; i < sorted.length; i++) {
    if (DateTime.parse(sorted[i - 1].end) != DateTime.parse(sorted[i].start)) {
      return false;
    }
  }
  return true;
}

class StaffWalkInPage extends StatefulWidget {
  const StaffWalkInPage({super.key});
  @override
  State<StaffWalkInPage> createState() => _StaffWalkInPageState();
}

class _StaffWalkInPageState extends State<StaffWalkInPage> {
  final form = GlobalKey<FormState>();
  final name = TextEditingController(), phone = TextEditingController();
  String? courtId, sportId, date;
  String method = 'Cash';
  bool busy = false, loading = false;
  Availability? availability;
  String? error;
  final selected = <String>{};
  int generation = 0;
  @override
  void dispose() {
    name.dispose();
    phone.dispose();
    super.dispose();
  }

  Future<void> loadSlots() async {
    final version = ++generation;
    selected.clear();
    availability = null;
    if (courtId == null || date == null) {
      setState(() {});
      return;
    }
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final data = await context.read<StaffApi>().api.availability(
        courtId!,
        date!,
      );
      if (mounted && version == generation) setState(() => availability = data);
    } catch (e) {
      if (mounted && version == generation) {
        setState(() => error = staffError(e));
      }
    } finally {
      if (mounted && version == generation) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: CourtGoAppBar(title: 'Đặt sân tại quầy'),
    body: StaffData(
      identity: 'walk-in',
      load: (api) async {
        date ??= CustomerFormat.day(api.now);
        return {'courts': await api.allPages('courts')};
      },
      builder: (data, reload) {
        final api = context.read<StaffApi>();
        final courts = api.center!.courts;
        final assignedIds = jsonList(data['courts'])
            .map((c) => c['courtId'])
            .toSet();
        final usable = courts
            .where((c) => assignedIds.contains(c.id) && c.status == 'Active')
            .toList();
        final sports = {for (final c in usable) c.sportId: c.sportName};
        final slots =
            availability?.slots
                .where((s) => selected.contains(s.start))
                .toList() ??
            <Slot>[];
        return Form(
          key: form,
          child: StaffList(
            refresh: () {
              reload();
              loadSlots();
            },
            children: [
              Text(
                api.center!.name,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              TextFormField(
                controller: name,
                maxLength: 150,
                decoration: const InputDecoration(labelText: 'Tên khách'),
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'Nhập tên khách' : null,
              ),
              TextFormField(
                controller: phone,
                maxLength: 20,
                keyboardType: TextInputType.phone,
                decoration: const InputDecoration(labelText: 'Số điện thoại'),
                validator: (v) =>
                    !RegExp(r'^\+?[0-9]{8,19}$').hasMatch(v?.trim() ?? '')
                    ? 'Nhập số điện thoại hợp lệ'
                    : null,
              ),
              DropdownButtonFormField<String>(
                initialValue: sportId,
                decoration: const InputDecoration(labelText: 'Môn thể thao'),
                items: sports.entries
                    .map(
                      (e) =>
                          DropdownMenuItem(value: e.key, child: Text(e.value)),
                    )
                    .toList(),
                onChanged: busy
                    ? null
                    : (v) {
                        sportId = v;
                        courtId = null;
                        loadSlots();
                      },
              ),
              DropdownButtonFormField<String>(
                key: ValueKey(sportId),
                initialValue: courtId,
                decoration: const InputDecoration(labelText: 'Sân'),
                items: usable
                    .where((c) => sportId == null || c.sportId == sportId)
                    .map(
                      (c) => DropdownMenuItem(value: c.id, child: Text(c.name)),
                    )
                    .toList(),
                onChanged: busy
                    ? null
                    : (v) {
                        courtId = v;
                        loadSlots();
                      },
              ),
              SecondaryButton(
                label: 'Ngày: ${CustomerFormat.date(date!)}',
                onPressed: busy
                    ? null
                    : () async {
                        final now = api.now;
                        final value = await showDatePicker(
                          context: context,
                          initialDate: DateTime.parse(date!),
                          firstDate: DateTime(now.year, now.month, now.day),
                          lastDate: DateTime(now.year + 1, 12, 31),
                        );
                        if (value != null && mounted) {
                          date = CustomerFormat.day(value);
                          loadSlots();
                        }
                      },
              ),
              if (loading) const LinearProgressIndicator(),
              if (error != null) NoticeState(title: error!, action: loadSlots),
              if (availability != null) ...[
                const Text('Chọn các khung 60 phút liên tiếp'),
                if (availability!.closed || availability!.slots.isEmpty)
                  const NoticeState(
                    title: 'Sân không có giờ mở cửa trong ngày này',
                  ),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: availability!.slots
                      .map(
                        (slot) => FilterChip(
                          label: Text(
                            '${CustomerFormat.interval(api.local(slot.start), api.local(slot.end))}\n${slot.available ? '' : statusLabel(slot.status)}',
                          ),
                          selected: selected.contains(slot.start),
                          onSelected: !slot.available || busy
                              ? null
                              : (value) => setState(() {
                                  if (value) {
                                    selected.add(slot.start);
                                  } else {
                                    selected.remove(slot.start);
                                  }
                                }),
                        ),
                      )
                      .toList(),
                ),
                if (selected.isNotEmpty)
                  StaffPanel(
                    title: 'Tổng tiền theo giá hiện tại',
                    children: [
                      PriceText(
                        slots.fold<double>(0, (sum, slot) => sum + slot.price),
                        large: true,
                      ),
                      const Text(
                        'Hệ thống kiểm tra lại giá và chỗ trống khi xác nhận. Thu đủ tiền trước khi tạo lịch tại quầy.',
                      ),
                    ],
                  ),
              ],
              SegmentedButton<String>(
                segments: const [
                  ButtonSegment(value: 'Cash', label: Text('Tiền mặt')),
                  ButtonSegment(
                    value: 'BankTransfer',
                    label: Text('Chuyển khoản'),
                  ),
                ],
                selected: {method},
                onSelectionChanged: busy
                    ? null
                    : (v) => setState(() => method = v.single),
              ),
              PrimaryButton(
                label: 'Đã thu đủ tiền · Tạo lịch',
                loading: busy,
                onPressed: () async {
                  if (!form.currentState!.validate()) return;
                  if (courtId == null || !consecutiveStaffSlots(slots)) {
                    showMessage(
                      context,
                      'Chọn sân và các khung 60 phút còn trống, liên tiếp.',
                    );
                    return;
                  }
                  if (!await staffConfirm(
                    context,
                    'Xác nhận đặt tại quầy',
                    'Đã nhận đủ tiền và đối chiếu thông tin khách?',
                  )) {
                    return;
                  }
                  setState(() => busy = true);
                  try {
                    final result = await api.command('bookings/walk-in', {
                      'customerName': name.text.trim(),
                      'customerPhone': phone.text.trim(),
                      'courtId': courtId,
                      'slotStartAts': slots.map((s) => s.start).toList(),
                      'paymentMethod': method,
                    });
                    if (context.mounted) {
                      context.go(
                        '/staff/bookings/${result['booking']['bookingId']}',
                      );
                    }
                  } catch (e) {
                    if (context.mounted) {
                      showMessage(context, staffError(e));
                      await loadSlots();
                    }
                  } finally {
                    if (mounted) setState(() => busy = false);
                  }
                },
              ),
            ],
          ),
        );
      },
    ),
  );
}
