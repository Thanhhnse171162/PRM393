import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminStaffDetailPage extends StatefulWidget {
  const AdminStaffDetailPage({super.key, required this.staffId});

  final String staffId;

  @override
  State<AdminStaffDetailPage> createState() => _AdminStaffDetailPageState();
}

class _AdminStaffDetailPageState extends State<AdminStaffDetailPage> {
  Future<AdminStaffDetail>? _detailFuture;
  List<AdminSportCenterItem> _centers = [];
  bool _mutating = false;

  @override
  void initState() {
    super.initState();
    _loadDetail();
    _loadCenters();
  }

  void _loadCenters() async {
    try {
      final res = await context.read<AdminApi>().getCenters(pageSize: 100);
      if (mounted) setState(() => _centers = res.items);
    } catch (_) {}
  }

  void _loadDetail() {
    setState(() {
      _detailFuture = context.read<AdminApi>().getStaffDetail(widget.staffId);
    });
  }

  Future<void> _toggleStatus(AdminStaffDetail detail) async {
    final nextStatus = !detail.isActive;
    final confirmed = await adminConfirmDialog(
      context: context,
      title: nextStatus ? 'Kích hoạt tài khoản?' : 'Vô hiệu hóa tài khoản?',
      message: nextStatus
          ? 'Nhân viên "${detail.fullName}" sẽ có thể đăng nhập vào ứng dụng trở lại.'
          : 'Nhân viên "${detail.fullName}" sẽ bị khóa phiên đăng nhập và không thể truy cập hệ thống.',
      confirmLabel: nextStatus ? 'Kích hoạt' : 'Khóa tài khoản',
      confirmColor: nextStatus ? AppColors.primary : AppColors.error,
    );
    if (!confirmed || !mounted) return;

    setState(() => _mutating = true);
    try {
      await context.read<AdminApi>().updateStaffStatus(widget.staffId, nextStatus);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(nextStatus
                ? 'Đã kích hoạt tài khoản nhân viên'
                : 'Đã khóa tài khoản nhân viên'),
          ),
        );
        _loadDetail();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'Thao tác thất bại'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _mutating = false);
    }
  }

  void _showEditProfileSheet(AdminStaffDetail detail) {
    final nameCtrl = TextEditingController(text: detail.fullName);
    final emailCtrl = TextEditingController(text: detail.email ?? '');
    final phoneCtrl = TextEditingController(text: detail.phoneNumber);
    final formKey = GlobalKey<FormState>();

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
              child: Form(
                key: formKey,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'Chỉnh sửa thông tin nhân viên',
                      style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: nameCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Họ và tên *',
                        border: OutlineInputBorder(),
                      ),
                      validator: (v) =>
                          v == null || v.trim().isEmpty ? 'Vui lòng nhập họ tên' : null,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: phoneCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Số điện thoại *',
                        border: OutlineInputBorder(),
                      ),
                      keyboardType: TextInputType.phone,
                      validator: (v) =>
                          v == null || v.trim().isEmpty ? 'Vui lòng nhập SĐT' : null,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: emailCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Email (tùy chọn)',
                        border: OutlineInputBorder(),
                      ),
                      keyboardType: TextInputType.emailAddress,
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
                              if (!formKey.currentState!.validate()) return;
                              setSheetState(() => saving = true);
                              try {
                                await context.read<AdminApi>().updateStaffProfile(
                                      widget.staffId,
                                      fullName: nameCtrl.text.trim(),
                                      email: emailCtrl.text.trim().isEmpty
                                          ? null
                                          : emailCtrl.text.trim(),
                                      phoneNumber: phoneCtrl.text.trim(),
                                    );
                                if (ctx.mounted) Navigator.pop(ctx);
                                _loadDetail();
                              } catch (e) {
                                setSheetState(() => saving = false);
                                if (ctx.mounted) {
                                  ScaffoldMessenger.of(ctx).showSnackBar(
                                    SnackBar(
                                      content: Text(e is ApiException
                                          ? e.message
                                          : 'Cập nhật thất bại'),
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
                          : const Text('Lưu thay đổi'),
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

  void _showReassignSheet(AdminStaffDetail detail) {
    String? chosenCenterId = detail.assignedCenter?.centerId;
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
            return SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'Phân công cơ sở làm việc',
                      style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: AppColors.lightMint,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: const Text(
                        'Việc phân công lại sẽ lưu lại lịch sử phân công trước đó. Nhân viên sẽ ngay lập tức được cấp quyền kiểm soát và check-in tại cơ sở mới.',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.primaryDark,
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),
                    DropdownButtonFormField<String>(
                      initialValue: chosenCenterId,
                      decoration: const InputDecoration(
                        labelText: 'Chọn cơ sở thể thao *',
                        border: OutlineInputBorder(),
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
                      onChanged: (v) => setSheetState(() => chosenCenterId = v),
                    ),
                    const SizedBox(height: 20),
                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: AppColors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                      onPressed: (saving || chosenCenterId == null || chosenCenterId == detail.assignedCenter?.centerId)
                          ? null
                          : () async {
                              setSheetState(() => saving = true);
                              try {
                                await context.read<AdminApi>().reassignStaff(
                                      widget.staffId,
                                      chosenCenterId!,
                                    );
                                if (ctx.mounted) Navigator.pop(ctx);
                                _loadDetail();
                              } catch (e) {
                                setSheetState(() => saving = false);
                                if (ctx.mounted) {
                                  ScaffoldMessenger.of(ctx).showSnackBar(
                                    SnackBar(
                                      content: Text(e is ApiException
                                          ? e.message
                                          : 'Phân công thất bại'),
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
                          : const Text('Xác nhận phân công'),
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
          'Chi tiết nhân viên',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadDetail,
          ),
        ],
      ),
      body: FutureBuilder<AdminStaffDetail>(
        future: _detailFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const LoadingWidget();
          }
          if (snapshot.hasError) {
            return ErrorState(
              message: 'Không thể tải thông tin nhân viên',
              onRetry: _loadDetail,
            );
          }
          final detail = snapshot.data;
          if (detail == null) {
            return const Center(child: Text('Không tìm thấy nhân viên'));
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Profile Card
                AdminCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          CircleAvatar(
                            radius: 28,
                            backgroundColor: AppColors.lightMint,
                            child: Text(
                              detail.fullName.isNotEmpty
                                  ? detail.fullName[0].toUpperCase()
                                  : 'S',
                              style: const TextStyle(
                                fontSize: 24,
                                fontWeight: FontWeight.w800,
                                color: AppColors.primary,
                              ),
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  detail.fullName,
                                  style: const TextStyle(
                                    fontSize: 18,
                                    fontWeight: FontWeight.w800,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Row(
                                  children: [
                                    AdminStatusBadge(
                                      label: detail.isActive
                                          ? 'Đang hoạt động'
                                          : 'Đã khóa',
                                      statusType: detail.isActive
                                          ? 'Active'
                                          : 'Inactive',
                                    ),
                                    const SizedBox(width: 8),
                                    const Text(
                                      'Vai trò: Nhân viên',
                                      style: TextStyle(
                                        fontSize: 12,
                                        fontWeight: FontWeight.w500,
                                        color: AppColors.textSecondary,
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      const Divider(height: 1),
                      const SizedBox(height: 14),
                      _buildInfoRow(
                          Icons.phone_outlined, 'Số điện thoại', detail.phoneNumber),
                      const SizedBox(height: 10),
                      _buildInfoRow(Icons.email_outlined, 'Email',
                          detail.email?.isNotEmpty == true ? detail.email! : 'Chưa cập nhật'),
                      const SizedBox(height: 10),
                      _buildInfoRow(Icons.calendar_today_outlined, 'Ngày tạo',
                          AdminFormat.date(detail.createdAt)),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              icon: const Icon(Icons.edit_outlined, size: 16),
                              label: const Text('Sửa thông tin'),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: AppColors.primary,
                                side: const BorderSide(color: AppColors.primary),
                              ),
                              onPressed: _mutating
                                  ? null
                                  : () => _showEditProfileSheet(detail),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: OutlinedButton.icon(
                              icon: Icon(
                                detail.isActive
                                    ? Icons.lock_outline
                                    : Icons.lock_open_outlined,
                                size: 16,
                              ),
                              label: Text(
                                  detail.isActive ? 'Khóa' : 'Kích hoạt'),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: detail.isActive
                                    ? AppColors.error
                                    : AppColors.primary,
                                side: BorderSide(
                                  color: detail.isActive
                                      ? AppColors.error
                                      : AppColors.primary,
                                ),
                              ),
                              onPressed: _mutating ? null : () => _toggleStatus(detail),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 20),

                // Current Center Assignment Card
                AdminSectionHeader(
                  title: 'Cơ sở được phân công',
                  actionLabel: 'Đổi cơ sở',
                  onAction: _mutating ? null : () => _showReassignSheet(detail),
                ),
                AdminCard(
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: AppColors.lightMint,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: const Icon(Icons.storefront,
                            color: AppColors.primary, size: 24),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              detail.assignedCenter?.centerName ??
                                  'Chưa phân công cơ sở nào',
                              style: const TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              detail.assignedCenter != null
                                  ? 'Nhân viên có quyền quản lý lịch đặt & check-in tại đây'
                                  : 'Nhấn "Đổi cơ sở" để gán nhân viên vào một cơ sở thể thao',
                              style: const TextStyle(
                                fontSize: 12,
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 20),

                // Assignment History
                const AdminSectionHeader(title: 'Lịch sử phân công cơ sở'),
                if (detail.assignmentHistory.isEmpty)
                  const AdminCard(
                    child: Center(
                      child: Text(
                        'Chưa có lịch sử phân công',
                        style: TextStyle(
                          fontSize: 13,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ),
                  )
                else
                  ...detail.assignmentHistory.map((item) {
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: AdminCard(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                          children: [
                            Icon(
                              item.isActive
                                  ? Icons.check_circle
                                  : Icons.history,
                              size: 18,
                              color: item.isActive
                                  ? AppColors.primary
                                  : AppColors.textSecondary,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    item.centerName,
                                    style: TextStyle(
                                      fontSize: 13,
                                      fontWeight: item.isActive
                                          ? FontWeight.w700
                                          : FontWeight.w500,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                  Text(
                                    'Phân công lúc: ${AdminFormat.dateTime(item.assignedAt)}',
                                    style: const TextStyle(
                                      fontSize: 11,
                                      color: AppColors.textSecondary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            if (item.isActive)
                              const AdminStatusBadge(
                                label: 'Hiện tại',
                                statusType: 'Active',
                              ),
                          ],
                        ),
                      ),
                    );
                  }),
                const SizedBox(height: 24),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value) {
    return Row(
      children: [
        Icon(icon, size: 16, color: AppColors.textSecondary),
        const SizedBox(width: 8),
        SizedBox(
          width: 110,
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
            ),
          ),
        ),
      ],
    );
  }
}
