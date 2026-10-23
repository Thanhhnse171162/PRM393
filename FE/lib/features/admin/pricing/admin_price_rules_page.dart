import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminPriceRulesPage extends StatefulWidget {
  const AdminPriceRulesPage({
    super.key,
    this.initialCourtId,
    this.courtName,
  });

  final String? initialCourtId;
  final String? courtName;

  @override
  State<AdminPriceRulesPage> createState() => _AdminPriceRulesPageState();
}

class _AdminPriceRulesPageState extends State<AdminPriceRulesPage> {
  String? _selectedCenterId;
  String? _selectedCourtId;
  List<AdminSportCenterItem> _centers = [];
  List<AdminCourtItem> _courts = [];
  bool _loadingCenters = true;
  bool _loadingCourts = false;

  List<AdminPriceRuleItem> _rules = [];
  bool _loadingRules = false;
  String? _error;

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
    _selectedCourtId = widget.initialCourtId;
    _initHierarchy();
  }

  void _initHierarchy() async {
    try {
      final api = context.read<AdminApi>();
      final cRes = await api.getCenters(pageSize: 100);
      if (mounted) {
        setState(() {
          _centers = cRes.items;
          _loadingCenters = false;
        });

        if (_selectedCourtId != null) {
          // Find court's center
          final courtDetail = await api.getCourt(_selectedCourtId!);
          if (mounted) {
            setState(() => _selectedCenterId = courtDetail.sportCenterId);
            _loadCourtsForCenter(_selectedCenterId!);
          }
        } else if (_centers.isNotEmpty) {
          _selectedCenterId = _centers.first.centerId;
          _loadCourtsForCenter(_selectedCenterId!);
        }
      }
    } catch (_) {
      if (mounted) setState(() => _loadingCenters = false);
    }
  }

  void _loadCourtsForCenter(String centerId) async {
    setState(() => _loadingCourts = true);
    try {
      final res = await context
          .read<AdminApi>()
          .getCourts(centerId: centerId, pageSize: 100);
      if (mounted) {
        setState(() {
          _courts = res.items;
          _loadingCourts = false;
          if (_courts.isNotEmpty) {
            if (_selectedCourtId == null ||
                !_courts.any((c) => c.courtId == _selectedCourtId)) {
              _selectedCourtId = _courts.first.courtId;
            }
            _loadRules();
          } else {
            _selectedCourtId = null;
            _rules = [];
          }
        });
      }
    } catch (_) {
      if (mounted) setState(() => _loadingCourts = false);
    }
  }

  Future<void> _loadRules() async {
    if (_selectedCourtId == null) return;
    setState(() {
      _loadingRules = true;
      _error = null;
    });

    try {
      final list =
          await context.read<AdminApi>().getPriceRules(_selectedCourtId!);
      if (mounted) {
        setState(() {
          _rules = list;
          _loadingRules = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loadingRules = false;
          _error = 'Không thể tải bảng giá của sân này.';
        });
      }
    }
  }

  Future<void> _toggleRuleStatus(AdminPriceRuleItem rule) async {
    final next = !rule.isActive;
    try {
      await context
          .read<AdminApi>()
          .updatePriceRuleStatus(_selectedCourtId!, rule.id, next);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(next
                ? 'Đã kích hoạt khung giá này'
                : 'Đã tạm tắt khung giá này'),
          ),
        );
        _loadRules();
      }
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException && e.isConflict
            ? 'Khung giá này bị trùng thời gian với một khung giá đang hoạt động.'
            : (e is ApiException ? e.message : 'Thao tác thất bại');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg), backgroundColor: AppColors.error),
        );
      }
    }
  }

  void _showRuleFormSheet({AdminPriceRuleItem? existing}) {
    final isEdit = existing != null;
    String day = existing?.dayOfWeek ?? 'Monday';
    TimeOfDay start = existing != null
        ? _parseTime(existing.startTime)
        : const TimeOfDay(hour: 17, minute: 0);
    TimeOfDay end = existing != null
        ? _parseTime(existing.endTime)
        : const TimeOfDay(hour: 21, minute: 0);
    final priceCtrl = TextEditingController(
      text: existing != null
          ? existing.pricePerHour.toStringAsFixed(0)
          : '200000',
    );
    DateTime? effFrom = existing?.effectiveFrom != null
        ? DateTime.tryParse(existing!.effectiveFrom!)
        : null;
    DateTime? effTo = existing?.effectiveTo != null
        ? DateTime.tryParse(existing!.effectiveTo!)
        : null;
    bool isActive = existing?.isActive ?? true;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        bool saving = false;
        String? conflictError;

        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            return Padding(
              padding: EdgeInsets.only(
                left: 20,
                right: 20,
                top: 20,
                bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
              ),
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      isEdit ? 'Chỉnh sửa quy tắc giá' : 'Thêm quy tắc giá mới',
                      style: const TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 14),

                    if (conflictError != null) ...[
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFEE2E2),
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: AppColors.error),
                        ),
                        child: Text(
                          conflictError!,
                          style: const TextStyle(
                            color: AppColors.error,
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                    ],

                    DropdownButtonFormField<String>(
                      value: day,
                      decoration: const InputDecoration(
                        labelText: 'Thứ trong tuần *',
                        border: OutlineInputBorder(),
                      ),
                      items: _weekdays.map((d) {
                        return DropdownMenuItem(
                          value: d,
                          child: Text(AdminFormat.dayOfWeekName(d)),
                        );
                      }).toList(),
                      onChanged: (v) {
                        if (v != null) setSheetState(() => day = v);
                      },
                    ),
                    const SizedBox(height: 12),

                    // Time range pickers
                    Row(
                      children: [
                        Expanded(
                          child: InkWell(
                            onTap: () async {
                              final p = await showTimePicker(
                                context: ctx,
                                initialTime: start,
                              );
                              if (p != null) setSheetState(() => start = p);
                            },
                            child: Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                border: Border.all(color: AppColors.border),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text('Bắt đầu',
                                      style: TextStyle(
                                          fontSize: 11,
                                          color: AppColors.textSecondary)),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${start.hour.toString().padLeft(2, '0')}:${start.minute.toString().padLeft(2, '0')}',
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
                        const SizedBox(width: 10),
                        Expanded(
                          child: InkWell(
                            onTap: () async {
                              final p = await showTimePicker(
                                context: ctx,
                                initialTime: end,
                              );
                              if (p != null) setSheetState(() => end = p);
                            },
                            child: Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                border: Border.all(color: AppColors.border),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text('Kết thúc',
                                      style: TextStyle(
                                          fontSize: 11,
                                          color: AppColors.textSecondary)),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${end.hour.toString().padLeft(2, '0')}:${end.minute.toString().padLeft(2, '0')}',
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
                    const SizedBox(height: 12),

                    TextFormField(
                      controller: priceCtrl,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Đơn giá mỗi giờ (VND) *',
                        prefixIcon: Icon(Icons.payments_outlined, size: 20),
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 12),

                    // Effective Dates
                    Row(
                      children: [
                        Expanded(
                          child: InkWell(
                            onTap: () async {
                              final p = await showDatePicker(
                                context: ctx,
                                initialDate: effFrom ?? DateTime.now(),
                                firstDate: DateTime(2024),
                                lastDate: DateTime(2028),
                              );
                              if (p != null) setSheetState(() => effFrom = p);
                            },
                            child: Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                border: Border.all(color: AppColors.border),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text('Hiệu lực từ',
                                      style: TextStyle(
                                          fontSize: 11,
                                          color: AppColors.textSecondary)),
                                  const SizedBox(height: 2),
                                  Text(
                                    effFrom != null
                                        ? '${effFrom!.day}/${effFrom!.month}/${effFrom!.year}'
                                        : 'Vô thời hạn',
                                    style: const TextStyle(
                                        fontSize: 13,
                                        fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: InkWell(
                            onTap: () async {
                              final p = await showDatePicker(
                                context: ctx,
                                initialDate:
                                    effTo ?? DateTime.now().add(const Duration(days: 90)),
                                firstDate: DateTime(2024),
                                lastDate: DateTime(2028),
                              );
                              if (p != null) setSheetState(() => effTo = p);
                            },
                            child: Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                border: Border.all(color: AppColors.border),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text('Đến ngày',
                                      style: TextStyle(
                                          fontSize: 11,
                                          color: AppColors.textSecondary)),
                                  const SizedBox(height: 2),
                                  Text(
                                    effTo != null
                                        ? '${effTo!.day}/${effTo!.month}/${effTo!.year}'
                                        : 'Vô thời hạn',
                                    style: const TextStyle(
                                        fontSize: 13,
                                        fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),

                    SwitchListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Kích hoạt quy tắc giá này'),
                      value: isActive,
                      activeColor: AppColors.primary,
                      onChanged: (v) => setSheetState(() => isActive = v),
                    ),
                    const SizedBox(height: 16),

                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: AppColors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                      onPressed: saving
                          ? null
                          : () async {
                              final price =
                                  double.tryParse(priceCtrl.text.trim()) ?? 0.0;
                              if (price <= 0) {
                                setSheetState(() => conflictError =
                                    'Vui lòng nhập giá hợp lệ lớn hơn 0');
                                return;
                              }
                              setSheetState(() {
                                saving = true;
                                conflictError = null;
                              });

                              final payload = {
                                'dayOfWeek': day,
                                'startTime':
                                    '${start.hour.toString().padLeft(2, '0')}:${start.minute.toString().padLeft(2, '0')}:00',
                                'endTime':
                                    '${end.hour.toString().padLeft(2, '0')}:${end.minute.toString().padLeft(2, '0')}:00',
                                'pricePerHour': price,
                                'effectiveFrom': effFrom != null
                                    ? '${effFrom!.year}-${effFrom!.month.toString().padLeft(2, '0')}-${effFrom!.day.toString().padLeft(2, '0')}'
                                    : null,
                                'effectiveTo': effTo != null
                                    ? '${effTo!.year}-${effTo!.month.toString().padLeft(2, '0')}-${effTo!.day.toString().padLeft(2, '0')}'
                                    : null,
                                'isActive': isActive,
                              };

                              try {
                                final api = context.read<AdminApi>();
                                if (isEdit) {
                                  await api.updatePriceRule(
                                      _selectedCourtId!, existing.id, payload);
                                } else {
                                  await api.createPriceRule(
                                      _selectedCourtId!, payload);
                                }
                                if (ctx.mounted) Navigator.pop(ctx);
                                _loadRules();
                              } catch (e) {
                                setSheetState(() {
                                  saving = false;
                                  if (e is ApiException && e.isConflict) {
                                    conflictError =
                                        'Khung giá này bị trùng thời gian với một khung giá đang hoạt động.';
                                  } else {
                                    conflictError = e is ApiException
                                        ? e.message
                                        : 'Lưu quy tắc giá thất bại.';
                                  }
                                });
                              }
                            },
                      child: saving
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: AppColors.white,
                              ),
                            )
                          : Text(isEdit ? 'Lưu thay đổi' : 'Thêm khung giá'),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  TimeOfDay _parseTime(String str) {
    final parts = str.split(':');
    return TimeOfDay(
      hour: int.tryParse(parts[0]) ?? 0,
      minute: int.tryParse(parts[1]) ?? 0,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Bảng giá linh hoạt',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle_outline, color: AppColors.primary),
            tooltip: 'Thêm khung giá',
            onPressed:
                _selectedCourtId == null ? null : () => _showRuleFormSheet(),
          ),
        ],
      ),
      body: _loadingCenters
          ? const LoadingWidget()
          : Column(
              children: [
                // Center & Court Selectors
                Container(
                  color: AppColors.white,
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                  child: Column(
                    children: [
                      DropdownButtonFormField<String>(
                        value: _selectedCenterId,
                        decoration: const InputDecoration(
                          labelText: 'Cơ sở',
                          prefixIcon: Icon(Icons.storefront, size: 20),
                          border: OutlineInputBorder(),
                          contentPadding: EdgeInsets.symmetric(
                              horizontal: 12, vertical: 8),
                        ),
                        items: _centers.map((c) {
                          return DropdownMenuItem(
                            value: c.centerId,
                            child: Text(c.name, overflow: TextOverflow.ellipsis),
                          );
                        }).toList(),
                        onChanged: (v) {
                          if (v != null) {
                            setState(() => _selectedCenterId = v);
                            _loadCourtsForCenter(v);
                          }
                        },
                      ),
                      const SizedBox(height: 10),
                      DropdownButtonFormField<String>(
                        value: _selectedCourtId,
                        decoration: const InputDecoration(
                          labelText: 'Sân thể thao',
                          prefixIcon: Icon(Icons.sports_tennis, size: 20),
                          border: OutlineInputBorder(),
                          contentPadding: EdgeInsets.symmetric(
                              horizontal: 12, vertical: 8),
                        ),
                        items: _courts.map((c) {
                          return DropdownMenuItem(
                            value: c.courtId,
                            child: Text(
                              '${c.name} (${c.code} - ${c.sportName})',
                              overflow: TextOverflow.ellipsis,
                            ),
                          );
                        }).toList(),
                        onChanged: (v) {
                          if (v != null) {
                            setState(() => _selectedCourtId = v);
                            _loadRules();
                          }
                        },
                      ),
                    ],
                  ),
                ),
                const Divider(height: 1),

                Expanded(child: _buildBody()),
              ],
            ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
        tooltip: 'Thêm khung giá',
        onPressed:
            _selectedCourtId == null ? null : () => _showRuleFormSheet(),
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildBody() {
    if (_loadingRules || _loadingCourts) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadRules);
    }
    if (_selectedCourtId == null) {
      return const Center(child: Text('Vui lòng chọn sân thể thao'));
    }
    if (_rules.isEmpty) {
      return const EmptyState(
        title: 'Chưa có khung giá đặc biệt',
        message:
            'Sân này hiện đang áp dụng giá cơ sở cho mọi khung giờ. Bạn có thể thêm khung giá giờ cao điểm hoặc cuối tuần.',
        icon: Icons.price_change_outlined,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadRules,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _rules.length,
        separatorBuilder: (_, __) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final r = _rules[index];
          return AdminCard(
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: r.isActive
                        ? AppColors.lightMint
                        : const Color(0xFFF1F5F9),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(
                    Icons.schedule,
                    color: r.isActive
                        ? AppColors.primary
                        : AppColors.textSecondary,
                    size: 22,
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            AdminFormat.dayOfWeekName(r.dayOfWeek),
                            style: const TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          AdminStatusBadge(
                            label: r.isActive ? 'Áp dụng' : 'Tạm tắt',
                            statusType: r.isActive ? 'Active' : 'Inactive',
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${AdminFormat.time(r.startTime)} – ${AdminFormat.time(r.endTime)}',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.primary,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Giá: ${AdminFormat.currency(r.pricePerHour)} / giờ',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      if (r.effectiveFrom != null || r.effectiveTo != null) ...[
                        const SizedBox(height: 2),
                        Text(
                          'Hiệu lực: ${r.effectiveFrom != null ? AdminFormat.date(r.effectiveFrom!) : "Bắt đầu"} đến ${r.effectiveTo != null ? AdminFormat.date(r.effectiveTo!) : "Vô hạn"}',
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                PopupMenuButton<String>(
                  icon: const Icon(Icons.more_vert, size: 20),
                  onSelected: (v) {
                    if (v == 'edit') {
                      _showRuleFormSheet(existing: r);
                    } else if (v == 'toggle') {
                      _toggleRuleStatus(r);
                    }
                  },
                  itemBuilder: (_) => [
                    const PopupMenuItem(
                      value: 'edit',
                      child: Row(
                        children: [
                          Icon(Icons.edit_outlined, size: 16),
                          SizedBox(width: 8),
                          Text('Chỉnh sửa'),
                        ],
                      ),
                    ),
                    PopupMenuItem(
                      value: 'toggle',
                      child: Row(
                        children: [
                          Icon(
                            r.isActive
                                ? Icons.pause_circle_outline
                                : Icons.play_circle_outline,
                            size: 16,
                            color: r.isActive
                                ? AppColors.error
                                : AppColors.primary,
                          ),
                          const SizedBox(width: 8),
                          Text(r.isActive ? 'Tạm tắt' : 'Bật lại'),
                        ],
                      ),
                    ),
                  ],
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
