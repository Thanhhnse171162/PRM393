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

class AdminOperatingExceptionsPage extends StatefulWidget {
  const AdminOperatingExceptionsPage({super.key, this.initialCenterId});

  final String? initialCenterId;

  @override
  State<AdminOperatingExceptionsPage> createState() =>
      _AdminOperatingExceptionsPageState();
}

class _AdminOperatingExceptionsPageState
    extends State<AdminOperatingExceptionsPage> {
  String? _selectedCenterId;
  List<AdminSportCenterItem> _centers = [];
  bool _loadingCenters = true;

  List<AdminOperatingHourExceptionItem> _exceptions = [];
  bool _loading = false;
  String? _error;

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
            _loadExceptions();
          }
        });
      }
    } catch (_) {
      if (mounted) setState(() => _loadingCenters = false);
    }
  }

  Future<void> _loadExceptions() async {
    if (_selectedCenterId == null) return;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final list = await context
          .read<AdminApi>()
          .getOperatingHourExceptions(_selectedCenterId!);
      if (mounted) {
        setState(() {
          _exceptions = list;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải danh sách ngày ngoại lệ.';
        });
      }
    }
  }

  Future<void> _deleteException(AdminOperatingHourExceptionItem item) async {
    final confirmed = await adminConfirmDialog(
      context: context,
      title: 'Xóa ngày ngoại lệ?',
      message:
          'Ngày ${AdminFormat.date(item.date)} sẽ trở lại áp dụng lịch hoạt động chuẩn 7 ngày.',
      confirmLabel: 'Xóa',
      confirmColor: AppColors.error,
    );
    if (!confirmed || !mounted) return;

    try {
      await context
          .read<AdminApi>()
          .deleteOperatingHourException(_selectedCenterId!, item.exceptionId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa ngày ngoại lệ thành công!')),
        );
        _loadExceptions();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'Xóa thất bại'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }

  void _showAddExceptionSheet() {
    DateTime chosenDate = DateTime.now().add(const Duration(days: 1));
    bool isClosed = true;
    TimeOfDay openTime = const TimeOfDay(hour: 8, minute: 0);
    TimeOfDay closeTime = const TimeOfDay(hour: 20, minute: 0);
    final reasonCtrl = TextEditingController();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        bool saving = false;
        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            final dateStr =
                '${chosenDate.year}-${chosenDate.month.toString().padLeft(2, '0')}-${chosenDate.day.toString().padLeft(2, '0')}';
            return Padding(
              padding: EdgeInsets.only(
                left: 20,
                right: 20,
                top: 20,
                bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    'Thêm ngày ngoại lệ / Ngày lễ',
                    style: TextStyle(
                      fontSize: 17,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 16),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Ngày áp dụng',
                        style: TextStyle(fontSize: 13, color: AppColors.textSecondary)),
                    subtitle: Text(
                      '${chosenDate.day.toString().padLeft(2, '0')}/${chosenDate.month.toString().padLeft(2, '0')}/${chosenDate.year}',
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    trailing: const Icon(Icons.calendar_today,
                        color: AppColors.primary),
                    onTap: () async {
                      final picked = await showDatePicker(
                        context: ctx,
                        initialDate: chosenDate,
                        firstDate: DateTime(2024),
                        lastDate: DateTime(2028),
                      );
                      if (picked != null) {
                        setSheetState(() => chosenDate = picked);
                      }
                    },
                  ),
                  const Divider(height: 1),
                  const SizedBox(height: 10),
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Đóng cửa cả ngày'),
                    subtitle: const Text(
                      'Bật nếu cơ sở nghỉ lễ hoặc không đón khách cả ngày',
                      style: TextStyle(fontSize: 12),
                    ),
                    activeColor: AppColors.primary,
                    value: isClosed,
                    onChanged: (v) => setSheetState(() => isClosed = v),
                  ),
                  if (!isClosed) ...[
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Expanded(
                          child: InkWell(
                            onTap: () async {
                              final p = await showTimePicker(
                                context: ctx,
                                initialTime: openTime,
                              );
                              if (p != null) {
                                setSheetState(() => openTime = p);
                              }
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
                                  const Text('Giờ mở cửa',
                                      style: TextStyle(
                                          fontSize: 11,
                                          color: AppColors.textSecondary)),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${openTime.hour.toString().padLeft(2, '0')}:${openTime.minute.toString().padLeft(2, '0')}',
                                    style: const TextStyle(
                                        fontSize: 15,
                                        fontWeight: FontWeight.w700,
                                        color: AppColors.primary),
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
                                initialTime: closeTime,
                              );
                              if (p != null) {
                                setSheetState(() => closeTime = p);
                              }
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
                                  const Text('Giờ đóng cửa',
                                      style: TextStyle(
                                          fontSize: 11,
                                          color: AppColors.textSecondary)),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${closeTime.hour.toString().padLeft(2, '0')}:${closeTime.minute.toString().padLeft(2, '0')}',
                                    style: const TextStyle(
                                        fontSize: 15,
                                        fontWeight: FontWeight.w700,
                                        color: AppColors.primary),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                  const SizedBox(height: 12),
                  TextField(
                    controller: reasonCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Lý do (Lễ Quốc Khánh, Tết, Bảo dưỡng toàn khu...)',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 20),
                  ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.primary,
                      foregroundColor: AppColors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                    ),
                    onPressed: saving
                        ? null
                        : () async {
                            setSheetState(() => saving = true);
                            final payload = {
                              'date': dateStr,
                              'isClosed': isClosed,
                              'openTime': isClosed
                                  ? null
                                  : '${openTime.hour.toString().padLeft(2, '0')}:${openTime.minute.toString().padLeft(2, '0')}:00',
                              'closeTime': isClosed
                                  ? null
                                  : '${closeTime.hour.toString().padLeft(2, '0')}:${closeTime.minute.toString().padLeft(2, '0')}:00',
                              'reason': reasonCtrl.text.trim().isEmpty
                                  ? null
                                  : reasonCtrl.text.trim(),
                            };
                            try {
                              await context
                                  .read<AdminApi>()
                                  .createOperatingHourException(
                                    _selectedCenterId!,
                                    payload,
                                  );
                              if (ctx.mounted) Navigator.pop(ctx);
                              _loadExceptions();
                            } catch (e) {
                              setSheetState(() => saving = false);
                              if (ctx.mounted) {
                                ScaffoldMessenger.of(ctx).showSnackBar(
                                  SnackBar(
                                    content: Text(e is ApiException
                                        ? e.message
                                        : 'Thêm ngoại lệ thất bại'),
                                    backgroundColor: AppColors.error,
                                  ),
                                );
                              }
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
                        : const Text('Thêm ngày ngoại lệ'),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Ngày ngoại lệ',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle_outline, color: AppColors.primary),
            tooltip: 'Thêm ngày ngoại lệ',
            onPressed:
                _selectedCenterId == null ? null : _showAddExceptionSheet,
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
                      labelText: 'Cơ sở thể thao',
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
                        _loadExceptions();
                      }
                    },
                  ),
                ),
                const Divider(height: 1),

                Expanded(child: _buildBody()),
              ],
            ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
        tooltip: 'Thêm ngày ngoại lệ',
        onPressed: _selectedCenterId == null ? null : _showAddExceptionSheet,
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadExceptions);
    }
    if (_exceptions.isEmpty) {
      return const EmptyState(
        title: 'Chưa có ngày ngoại lệ',
        message:
            'Cơ sở đang hoạt động theo lịch chuẩn 7 ngày. Nhấn nút thêm để đặt lịch cho ngày nghỉ lễ hoặc giờ mở cửa đặc biệt.',
        icon: Icons.event_available,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadExceptions,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _exceptions.length,
        separatorBuilder: (_, __) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final item = _exceptions[index];
          return AdminCard(
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: item.isClosed
                        ? const Color(0xFFFEE2E2)
                        : const Color(0xFFFEF3C7),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(
                    item.isClosed ? Icons.event_busy : Icons.schedule,
                    color: item.isClosed ? AppColors.error : AppColors.warning,
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
                            AdminFormat.date(item.date),
                            style: const TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          AdminStatusBadge(
                            label: item.isClosed ? 'Đóng cửa' : 'Giờ đặc biệt',
                            statusType:
                                item.isClosed ? 'Cancelled' : 'Pending',
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      if (!item.isClosed &&
                          item.openTime != null &&
                          item.closeTime != null)
                        Text(
                          'Giờ mở: ${AdminFormat.time(item.openTime!)} – ${AdminFormat.time(item.closeTime!)}',
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.primary,
                          ),
                        ),
                      if (item.reason?.isNotEmpty == true)
                        Text(
                          'Lý do: ${item.reason!}',
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textSecondary,
                          ),
                        ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.delete_outline,
                      size: 20, color: AppColors.error),
                  tooltip: 'Xóa ngoại lệ',
                  onPressed: () => _deleteException(item),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
