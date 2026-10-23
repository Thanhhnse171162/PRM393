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

class AdminCancellationPoliciesPage extends StatefulWidget {
  const AdminCancellationPoliciesPage({super.key});

  @override
  State<AdminCancellationPoliciesPage> createState() =>
      _AdminCancellationPoliciesPageState();
}

class _AdminCancellationPoliciesPageState
    extends State<AdminCancellationPoliciesPage> {
  List<AdminCancellationPolicySummary> _policies = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadPolicies();
  }

  Future<void> _loadPolicies() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final list = await context.read<AdminApi>().getCancellationPolicies();
      if (mounted) {
        setState(() {
          _policies = list;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải danh sách chính sách hủy.';
        });
      }
    }
  }

  Future<void> _activatePolicy(AdminCancellationPolicySummary policy) async {
    final confirmed = await adminConfirmDialog(
      context: context,
      title: 'Kích hoạt phiên bản này?',
      message:
          'Chính sách "${policy.name}" (v${policy.version}) sẽ trở thành chính sách hoạt động chính thức. Các đơn đặt sân mới tạo sau thời điểm này sẽ áp dụng chính sách này.',
      confirmLabel: 'Kích hoạt',
      confirmColor: AppColors.primary,
    );
    if (!confirmed || !mounted) return;

    try {
      await context.read<AdminApi>().activateCancellationPolicy(policy.id);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
                'Đã kích hoạt phiên bản chính sách v${policy.version} thành công!'),
          ),
        );
        _loadPolicies();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'Kích hoạt thất bại'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }

  void _showPolicyDetail(AdminCancellationPolicySummary policy) async {
    showDialog(
      context: context,
      builder: (_) => const Center(child: CircularProgressIndicator()),
    );

    try {
      final detail =
          await context.read<AdminApi>().getCancellationPolicy(policy.id);
      if (!mounted) return;
      Navigator.pop(context); // dismiss loading dialog

      showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        builder: (ctx) {
          return SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          '${detail.name} (v${detail.version})',
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w800,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ),
                      AdminStatusBadge(
                        label: detail.isActive ? 'Đang kích hoạt' : 'Lưu trữ',
                        statusType: detail.isActive ? 'Active' : 'Inactive',
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Tạo ngày: ${AdminFormat.dateTime(detail.createdAt)}',
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 16),
                  const Divider(height: 1),
                  const SizedBox(height: 14),
                  const Text(
                    'Quy tắc hoàn tiền theo mốc giờ trước khi chơi:',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 10),
                  ...detail.rules.map((r) {
                    final range = r.maxHoursBeforeStart != null
                        ? 'Từ ${r.minHoursBeforeStart}h đến ${r.maxHoursBeforeStart}h trước giờ bắt đầu'
                        : 'Trước giờ bắt đầu trên ${r.minHoursBeforeStart}h';
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 14, vertical: 10),
                        decoration: BoxDecoration(
                          color: AppColors.background,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              range,
                              style: const TextStyle(
                                  fontSize: 13, fontWeight: FontWeight.w500),
                            ),
                            Text(
                              'Hoàn ${(r.refundPercent * 100).toStringAsFixed(0)}%',
                              style: const TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w800,
                                color: AppColors.primary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  }),
                  const SizedBox(height: 16),
                  if (!detail.isActive)
                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: AppColors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                      onPressed: () {
                        Navigator.pop(ctx);
                        _activatePolicy(policy);
                      },
                      child: const Text('Kích hoạt phiên bản này'),
                    ),
                ],
              ),
            ),
          );
        },
      );
    } catch (_) {
      if (mounted) Navigator.pop(context);
    }
  }

  void _showCreatePolicySheet() {
    final nameCtrl = TextEditingController(text: 'Chính sách hoàn tiền tiêu chuẩn');
    final List<Map<String, dynamic>> rules = [
      {'min': 24, 'max': null, 'percent': 100.0},
      {'min': 12, 'max': 24, 'percent': 50.0},
      {'min': 0, 'max': 12, 'percent': 0.0},
    ];

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
                    const Text(
                      'Tạo phiên bản chính sách mới',
                      style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 6),
                    const Text(
                      'Lưu ý: Các phiên bản chính sách là bất biến (immutable). Sau khi tạo và kích hoạt, chính sách mới chỉ áp dụng cho các đơn đặt sân phát sinh sau đó.',
                      style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: 14),
                    TextField(
                      controller: nameCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Tên chính sách *',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 16),
                    const Text(
                      'Khung quy tắc hoàn tiền (mặc định):',
                      style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 8),
                    ...rules.map((r) {
                      final max = r['max'];
                      final label = max != null
                          ? 'Từ ${r['min']}h đến ${max}h trước giờ chơi'
                          : 'Trước giờ chơi trên ${r['min']}h';
                      return Container(
                        margin: const EdgeInsets.only(bottom: 6),
                        padding: const EdgeInsets.symmetric(
                            horizontal: 12, vertical: 8),
                        decoration: BoxDecoration(
                          color: AppColors.background,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(label, style: const TextStyle(fontSize: 12)),
                            Text(
                              'Hoàn ${r['percent'].toStringAsFixed(0)}%',
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w700,
                                color: AppColors.primary,
                              ),
                            ),
                          ],
                        ),
                      );
                    }),
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
                              final name = nameCtrl.text.trim();
                              if (name.isEmpty) return;
                              setSheetState(() => saving = true);

                              final payload = {
                                'name': name,
                                'rules': rules.map((r) {
                                  return {
                                    'minHoursBeforeStart': r['min'],
                                    'maxHoursBeforeStart': r['max'],
                                    'refundPercent': (r['percent'] as double) / 100.0,
                                  };
                                }).toList(),
                              };

                              try {
                                await context
                                    .read<AdminApi>()
                                    .createCancellationPolicy(payload);
                                if (ctx.mounted) Navigator.pop(ctx);
                                _loadPolicies();
                              } catch (e) {
                                setSheetState(() => saving = false);
                                if (ctx.mounted) {
                                  ScaffoldMessenger.of(ctx).showSnackBar(
                                    SnackBar(
                                      content: Text(e is ApiException
                                          ? e.message
                                          : 'Tạo chính sách thất bại'),
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
                          : const Text('Tạo phiên bản chính sách'),
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Chính sách hủy sân',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle_outline, color: AppColors.primary),
            tooltip: 'Tạo phiên bản mới',
            onPressed: _showCreatePolicySheet,
          ),
        ],
      ),
      body: _loading
          ? const LoadingWidget()
          : _error != null
              ? ErrorState(message: _error!, onRetry: _loadPolicies)
              : _policies.isEmpty
                  ? const EmptyState(
                      title: 'Chưa có chính sách hủy sân',
                      message:
                          'Tạo phiên bản chính sách đầu tiên để quy định tỷ lệ hoàn tiền khi khách hủy.',
                      icon: Icons.policy_outlined,
                    )
                  : RefreshIndicator(
                      onRefresh: _loadPolicies,
                      color: AppColors.primary,
                      child: ListView(
                        padding: const EdgeInsets.all(16),
                        children: [
                          // Explanatory Banner
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppColors.lightMint,
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: AppColors.primary.withValues(alpha: 0.2),
                              ),
                            ),
                            child: Row(
                              children: [
                                const Icon(Icons.verified_user_outlined,
                                    color: AppColors.primary, size: 22),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: Text(
                                    'Chính sách mới chỉ áp dụng cho đơn đặt sau khi kích hoạt. Các chính sách đã ban hành là bất biến.',
                                    style: TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                      color: AppColors.primaryDark,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 16),

                          ..._policies.map((p) {
                            return Padding(
                              padding: const EdgeInsets.only(bottom: 12),
                              child: AdminCard(
                                onTap: () => _showPolicyDetail(p),
                                borderColor: p.isActive ? AppColors.primary : null,
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      mainAxisAlignment:
                                          MainAxisAlignment.spaceBetween,
                                      children: [
                                        Expanded(
                                          child: Row(
                                            children: [
                                              Container(
                                                padding: const EdgeInsets.symmetric(
                                                    horizontal: 8, vertical: 4),
                                                decoration: BoxDecoration(
                                                  color: AppColors.lightMint,
                                                  borderRadius:
                                                      BorderRadius.circular(6),
                                                ),
                                                child: Text(
                                                  'Phiên bản v${p.version}',
                                                  style: const TextStyle(
                                                    fontSize: 11,
                                                    fontWeight: FontWeight.w700,
                                                    color: AppColors.primary,
                                                  ),
                                                ),
                                              ),
                                              const SizedBox(width: 8),
                                              Expanded(
                                                child: Text(
                                                  p.name,
                                                  style: const TextStyle(
                                                    fontSize: 15,
                                                    fontWeight: FontWeight.w700,
                                                    color: AppColors.textPrimary,
                                                  ),
                                                  maxLines: 1,
                                                  overflow:
                                                      TextOverflow.ellipsis,
                                                ),
                                              ),
                                            ],
                                          ),
                                        ),
                                        AdminStatusBadge(
                                          label: p.isActive
                                              ? 'Đang áp dụng'
                                              : 'Lưu trữ',
                                          statusType:
                                              p.isActive ? 'Active' : 'Inactive',
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 10),
                                    const Divider(height: 1),
                                    const SizedBox(height: 10),
                                    Row(
                                      mainAxisAlignment:
                                          MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          'Bao gồm ${p.ruleCount} quy tắc hoàn tiền',
                                          style: const TextStyle(
                                            fontSize: 12,
                                            color: AppColors.textSecondary,
                                          ),
                                        ),
                                        Text(
                                          'Tạo ngày: ${AdminFormat.date(p.createdAt)}',
                                          style: const TextStyle(
                                            fontSize: 12,
                                            color: AppColors.textSecondary,
                                          ),
                                        ),
                                      ],
                                    ),
                                    if (!p.isActive) ...[
                                      const SizedBox(height: 10),
                                      Align(
                                        alignment: Alignment.centerRight,
                                        child: TextButton.icon(
                                          icon: const Icon(Icons.check_circle_outline,
                                              size: 16),
                                          label: const Text('Kích hoạt phiên bản này'),
                                          style: TextButton.styleFrom(
                                            foregroundColor: AppColors.primary,
                                            visualDensity:
                                                VisualDensity.compact,
                                          ),
                                          onPressed: () => _activatePolicy(p),
                                        ),
                                      ),
                                    ],
                                  ],
                                ),
                              ),
                            );
                          }),
                        ],
                      ),
                    ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
        tooltip: 'Tạo phiên bản chính sách',
        onPressed: _showCreatePolicySheet,
        child: const Icon(Icons.add),
      ),
    );
  }
}
