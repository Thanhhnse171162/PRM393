import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminOperatingHoursPage extends StatefulWidget {
  const AdminOperatingHoursPage({super.key, this.initialCenterId});

  final String? initialCenterId;

  @override
  State<AdminOperatingHoursPage> createState() =>
      _AdminOperatingHoursPageState();
}

class _AdminOperatingHoursPageState extends State<AdminOperatingHoursPage> {
  String? _selectedCenterId;
  List<AdminSportCenterItem> _centers = [];
  bool _loadingCenters = true;

  bool _loadingHours = false;
  bool _saving = false;
  String? _error;

  final Map<String, bool> _closedDays = {};
  final Map<String, TimeOfDay> _openTimes = {};
  final Map<String, TimeOfDay> _closeTimes = {};

  final List<String> _weekdays = [
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ];

  @override
  void initState() {
    super.initState();
    _selectedCenterId = widget.initialCenterId;
    _loadCenters();
  }

  void _loadCenters() async {
    try {
      final res = await context.read<AdminApi>().getCenters(pageSize: 100);
      if (mounted) {
        setState(() {
          _centers = res.items;
          _loadingCenters = false;
          if (_centers.isNotEmpty) {
            _selectedCenterId ??= _centers.first.centerId;
            _loadOperatingHours();
          }
        });
      }
    } catch (_) {
      if (mounted) setState(() => _loadingCenters = false);
    }
  }

  void _loadOperatingHours() async {
    if (_selectedCenterId == null) return;
    setState(() {
      _loadingHours = true;
      _error = null;
    });

    try {
      final data =
          await context.read<AdminApi>().getOperatingHours(_selectedCenterId!);
      if (mounted) {
        _closedDays.clear();
        _openTimes.clear();
        _closeTimes.clear();

        for (final day in _weekdays) {
          final found = data.days.cast<AdminOperatingHourItem?>().firstWhere(
                (d) => d?.dayOfWeek.toLowerCase() == day.toLowerCase(),
                orElse: () => null,
              );
          if (found != null) {
            _closedDays[day] = found.isClosed;
            _openTimes[day] = _parseTime(found.openTime) ??
                const TimeOfDay(hour: 6, minute: 0);
            _closeTimes[day] = _parseTime(found.closeTime) ??
                const TimeOfDay(hour: 22, minute: 0);
          } else {
            _closedDays[day] = false;
            _openTimes[day] = const TimeOfDay(hour: 6, minute: 0);
            _closeTimes[day] = const TimeOfDay(hour: 22, minute: 0);
          }
        }

        setState(() {
          _loadingHours = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loadingHours = false;
          _error = 'Không thể tải giờ hoạt động của cơ sở này.';
        });
      }
    }
  }

  TimeOfDay? _parseTime(String? str) {
    if (str == null || str.isEmpty) return null;
    final parts = str.split(':');
    if (parts.length >= 2) {
      final h = int.tryParse(parts[0]);
      final m = int.tryParse(parts[1]);
      if (h != null && m != null) return TimeOfDay(hour: h, minute: m);
    }
    return null;
  }

  String _formatTime(TimeOfDay t) {
    final h = t.hour.toString().padLeft(2, '0');
    final m = t.minute.toString().padLeft(2, '0');
    return '$h:$m:00';
  }

  Future<void> _pickTime(String day, bool isOpenTime) async {
    final initial = isOpenTime
        ? (_openTimes[day] ?? const TimeOfDay(hour: 6, minute: 0))
        : (_closeTimes[day] ?? const TimeOfDay(hour: 22, minute: 0));

    final picked = await showTimePicker(
      context: context,
      initialTime: initial,
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.primary,
              onPrimary: AppColors.white,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      setState(() {
        if (isOpenTime) {
          _openTimes[day] = picked;
        } else {
          _closeTimes[day] = picked;
        }
      });
    }
  }

  Future<void> _save() async {
    if (_selectedCenterId == null) return;

    // Validate that openTime < closeTime for non-closed days
    for (final day in _weekdays) {
      final isClosed = _closedDays[day] ?? false;
      if (!isClosed) {
        final open = _openTimes[day] ?? const TimeOfDay(hour: 6, minute: 0);
        final close = _closeTimes[day] ?? const TimeOfDay(hour: 22, minute: 0);
        final openMin = open.hour * 60 + open.minute;
        final closeMin = close.hour * 60 + close.minute;
        if (openMin >= closeMin) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                '${AdminFormat.dayOfWeekName(day)}: Giờ mở cửa ($openMin) phải sớm hơn giờ đóng cửa ($closeMin)',
              ),
              backgroundColor: AppColors.error,
            ),
          );
          return;
        }
      }
    }

    setState(() => _saving = true);
    final daysPayload = _weekdays.map((day) {
      final isClosed = _closedDays[day] ?? false;
      return AdminOperatingHourItem(
        dayOfWeek: day,
        isClosed: isClosed,
        openTime: isClosed ? null : _formatTime(_openTimes[day]!),
        closeTime: isClosed ? null : _formatTime(_closeTimes[day]!),
      );
    }).toList();

    try {
      await context
          .read<AdminApi>()
          .updateOperatingHours(_selectedCenterId!, daysPayload);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đã cập nhật giờ hoạt động 7 ngày thành công!'),
          ),
        );
        _loadOperatingHours();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'Lưu thất bại'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Giờ hoạt động',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          TextButton.icon(
            icon: const Icon(Icons.event_busy, size: 18),
            label: const Text('Ngày ngoại lệ'),
            style: TextButton.styleFrom(foregroundColor: AppColors.primary),
            onPressed: _selectedCenterId == null
                ? null
                : () => context.push(
                      '/admin/operating-hours/exceptions?centerId=$_selectedCenterId',
                    ),
          ),
        ],
      ),
      body: _loadingCenters
          ? const LoadingWidget()
          : Column(
              children: [
                // Center Selector
                Container(
                  color: AppColors.white,
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  child: DropdownButtonFormField<String>(
                    value: _selectedCenterId,
                    decoration: const InputDecoration(
                      labelText: 'Chọn cơ sở thể thao',
                      prefixIcon: Icon(Icons.storefront, size: 20),
                      border: OutlineInputBorder(),
                      contentPadding:
                          EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                    ),
                    items: _centers.map((c) {
                      return DropdownMenuItem(
                        value: c.centerId,
                        child: Text(
                          '${c.name} (${c.district})',
                          overflow: TextOverflow.ellipsis,
                        ),
                      );
                    }).toList(),
                    onChanged: (v) {
                      if (v != null) {
                        setState(() => _selectedCenterId = v);
                        _loadOperatingHours();
                      }
                    },
                  ),
                ),
                const Divider(height: 1),

                Expanded(child: _buildScheduleBody()),

                // Bottom Save Action Bar
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppColors.white,
                    boxShadow: AppShadows.soft,
                    border: const Border(
                      top: BorderSide(color: AppColors.border),
                    ),
                  ),
                  child: ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.primary,
                      foregroundColor: AppColors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    onPressed: (_saving || _loadingHours) ? null : _save,
                    child: _saving
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: AppColors.white,
                            ),
                          )
                        : const Text(
                            'Lưu cấu hình giờ hoạt động',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                  ),
                ),
              ],
            ),
    );
  }

  Widget _buildScheduleBody() {
    if (_loadingHours) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadOperatingHours);
    }
    if (_selectedCenterId == null) {
      return const Center(child: Text('Vui lòng chọn cơ sở thể thao'));
    }

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: AppColors.lightMint,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Row(
            children: [
              const Icon(Icons.info_outline,
                  color: AppColors.primary, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Cấu hình giờ hoạt động chuẩn 7 ngày trong tuần. Khách chỉ có thể đặt sân trong các khung giờ này.',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.primaryDark,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),

        ..._weekdays.map((day) {
          final isClosed = _closedDays[day] ?? false;
          final openTime =
              _openTimes[day] ?? const TimeOfDay(hour: 6, minute: 0);
          final closeTime =
              _closeTimes[day] ?? const TimeOfDay(hour: 22, minute: 0);

          return Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: AdminCard(
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        AdminFormat.dayOfWeekName(day),
                        style: const TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      Row(
                        children: [
                          Text(
                            isClosed ? 'Đóng cửa' : 'Mở cửa',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: isClosed
                                  ? AppColors.error
                                  : AppColors.primary,
                            ),
                          ),
                          const SizedBox(width: 4),
                          Switch(
                            value: !isClosed,
                            activeColor: AppColors.primary,
                            onChanged: (isOpen) {
                              setState(() => _closedDays[day] = !isOpen);
                            },
                          ),
                        ],
                      ),
                    ],
                  ),
                  if (!isClosed) ...[
                    const Divider(height: 16),
                    Row(
                      children: [
                        Expanded(
                          child: InkWell(
                            onTap: () => _pickTime(day, true),
                            borderRadius: BorderRadius.circular(8),
                            child: Container(
                              padding: const EdgeInsets.symmetric(
                                  vertical: 8, horizontal: 10),
                              decoration: BoxDecoration(
                                color: AppColors.background,
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: AppColors.border),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Giờ mở cửa',
                                    style: TextStyle(
                                      fontSize: 11,
                                      color: AppColors.textSecondary,
                                    ),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${openTime.hour.toString().padLeft(2, '0')}:${openTime.minute.toString().padLeft(2, '0')}',
                                    style: const TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.primary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                        const Padding(
                          padding: EdgeInsets.symmetric(horizontal: 10),
                          child: Text('đến',
                              style: TextStyle(
                                  color: AppColors.textSecondary,
                                  fontSize: 12)),
                        ),
                        Expanded(
                          child: InkWell(
                            onTap: () => _pickTime(day, false),
                            borderRadius: BorderRadius.circular(8),
                            child: Container(
                              padding: const EdgeInsets.symmetric(
                                  vertical: 8, horizontal: 10),
                              decoration: BoxDecoration(
                                color: AppColors.background,
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: AppColors.border),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Giờ đóng cửa',
                                    style: TextStyle(
                                      fontSize: 11,
                                      color: AppColors.textSecondary,
                                    ),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${closeTime.hour.toString().padLeft(2, '0')}:${closeTime.minute.toString().padLeft(2, '0')}',
                                    style: const TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.primary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          );
        }),
      ],
    );
  }
}
