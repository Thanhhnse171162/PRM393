import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminStaffPage extends StatefulWidget {
  const AdminStaffPage({super.key});

  @override
  State<AdminStaffPage> createState() => _AdminStaffPageState();
}

class _AdminStaffPageState extends State<AdminStaffPage> {
  final TextEditingController _searchController = TextEditingController();
  String? _selectedCenterId;
  bool? _selectedActiveStatus;
  List<AdminSportCenterItem> _centers = [];
  bool _loading = false;
  String? _errorMessage;
  List<AdminStaffItem> _staffList = [];
  int _totalCount = 0;

  @override
  void initState() {
    super.initState();
    _loadCenters();
    _loadStaff();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _loadCenters() async {
    try {
      final api = context.read<AdminApi>();
      final res = await api.getCenters(pageSize: 100);
      if (mounted) setState(() => _centers = res.items);
    } catch (_) {}
  }

  Future<void> _loadStaff() async {
    setState(() {
      _loading = true;
      _errorMessage = null;
    });
    try {
      final api = context.read<AdminApi>();
      final res = await api.getStaffList(
        centerId: _selectedCenterId,
        isActive: _selectedActiveStatus,
        search: _searchController.text.trim(),
        page: 1,
        pageSize: 50,
      );
      if (mounted) {
        setState(() {
          _staffList = res.items;
          _totalCount = res.totalCount;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _errorMessage = 'Không thể tải danh sách nhân viên. Vui lòng thử lại.';
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Nhân viên & Phân công',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle_outline, color: AppColors.primary),
            tooltip: 'Thêm nhân viên',
            onPressed: () async {
              final created = await context.push<bool>('/admin/staff/create');
              if (created == true) _loadStaff();
            },
          ),
        ],
      ),
      body: Column(
        children: [
          // Search & Filter header
          Container(
            color: AppColors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Column(
              children: [
                TextField(
                  controller: _searchController,
                  decoration: InputDecoration(
                    hintText: 'Tìm theo họ tên, SĐT hoặc email...',
                    prefixIcon: const Icon(Icons.search, size: 20),
                    suffixIcon: _searchController.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear, size: 18),
                            onPressed: () {
                              _searchController.clear();
                              _loadStaff();
                            },
                          )
                        : null,
                    filled: true,
                    fillColor: AppColors.background,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 14),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                      borderSide: BorderSide.none,
                    ),
                  ),
                  onSubmitted: (_) => _loadStaff(),
                ),
                const SizedBox(height: 10),
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      AdminFilterChipItem(
                        label: 'Tất cả trạng thái',
                        isSelected: _selectedActiveStatus == null,
                        onTap: () {
                          setState(() => _selectedActiveStatus = null);
                          _loadStaff();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Đang hoạt động',
                        isSelected: _selectedActiveStatus == true,
                        onTap: () {
                          setState(() => _selectedActiveStatus = true);
                          _loadStaff();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Đã vô hiệu hóa',
                        isSelected: _selectedActiveStatus == false,
                        onTap: () {
                          setState(() => _selectedActiveStatus = false);
                          _loadStaff();
                        },
                      ),
                      const SizedBox(width: 8),
                      // Center dropdown filter chip
                      ActionChip(
                        avatar: const Icon(Icons.storefront,
                            size: 16, color: AppColors.primary),
                        label: Text(
                          _selectedCenterId == null
                              ? 'Cơ sở: Tất cả'
                              : 'Cơ sở: ${_centers.firstWhere((c) => c.centerId == _selectedCenterId, orElse: () => _centers.first).name}',
                          style: const TextStyle(fontSize: 12),
                        ),
                        onPressed: _showCenterFilter,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const Divider(height: 1),

          // Total count row
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 6),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Tổng số: $_totalCount nhân viên',
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),

          // Staff List
          Expanded(
            child: _buildBody(),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
        tooltip: 'Thêm nhân viên',
        onPressed: () async {
          final created = await context.push<bool>('/admin/staff/create');
          if (created == true) _loadStaff();
        },
        child: const Icon(Icons.person_add),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const LoadingWidget();
    if (_errorMessage != null) {
      return ErrorState(message: _errorMessage!, onRetry: _loadStaff);
    }
    if (_staffList.isEmpty) {
      return const EmptyState(
        title: 'Chưa có nhân viên nào',
        message: 'Thêm tài khoản nhân viên hoặc thử thay đổi bộ lọc tìm kiếm.',
        icon: Icons.badge_outlined,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadStaff,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        itemCount: _staffList.length,
        separatorBuilder: (context, index) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final s = _staffList[index];
          return AdminCard(
            onTap: () async {
              await context.push('/admin/staff/${s.staffId}');
              _loadStaff();
            },
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CircleAvatar(
                  radius: 20,
                  backgroundColor: AppColors.lightMint,
                  child: Text(
                    s.fullName.isNotEmpty ? s.fullName[0].toUpperCase() : 'S',
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(
                            child: Text(
                              s.fullName,
                              style: const TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textPrimary,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          AdminStatusBadge(
                            label: s.isActive ? 'Hoạt động' : 'Đã khóa',
                            statusType: s.isActive ? 'Active' : 'Inactive',
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          const Icon(Icons.phone_outlined,
                              size: 13, color: AppColors.textSecondary),
                          const SizedBox(width: 4),
                          Text(
                            s.phoneNumber,
                            style: const TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          if (s.email != null && s.email!.isNotEmpty) ...[
                            const SizedBox(width: 8),
                            const Text('·',
                                style:
                                    TextStyle(color: AppColors.textSecondary)),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                s.email!,
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: AppColors.textSecondary,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ],
                      ),
                      const SizedBox(height: 6),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: AppColors.background,
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Icons.storefront,
                                size: 12, color: AppColors.primary),
                            const SizedBox(width: 4),
                            Flexible(
                              child: Text(
                                s.assignedCenter?.centerName ??
                                    'Chưa phân công cơ sở',
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                  color: AppColors.textPrimary,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  void _showCenterFilter() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Padding(
              padding: EdgeInsets.all(16),
              child: Text(
                'Lọc theo cơ sở',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
              ),
            ),
            ListTile(
              title: const Text('Tất cả cơ sở'),
              trailing: _selectedCenterId == null
                  ? const Icon(Icons.check, color: AppColors.primary)
                  : null,
              onTap: () {
                Navigator.pop(ctx);
                setState(() => _selectedCenterId = null);
                _loadStaff();
              },
            ),
            const Divider(height: 1),
            Expanded(
              child: ListView.separated(
                itemCount: _centers.length,
                separatorBuilder: (context, index) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final c = _centers[i];
                  final isSel = c.centerId == _selectedCenterId;
                  return ListTile(
                    title: Text(c.name),
                    trailing: isSel
                        ? const Icon(Icons.check, color: AppColors.primary)
                        : null,
                    onTap: () {
                      Navigator.pop(ctx);
                      setState(() => _selectedCenterId = c.centerId);
                      _loadStaff();
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
