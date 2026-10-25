import '../../../shared/widgets/customer_widgets.dart';

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

class AdminCentersPage extends StatefulWidget {
  const AdminCentersPage({super.key});

  @override
  State<AdminCentersPage> createState() => _AdminCentersPageState();
}

class _AdminCentersPageState extends State<AdminCentersPage> {
  final TextEditingController _searchCtrl = TextEditingController();
  String? _selectedStatus;
  List<AdminSportCenterItem> _centers = [];
  bool _loading = false;
  String? _error;
  int _totalCount = 0;

  @override
  void initState() {
    super.initState();
    _loadCenters();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadCenters() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await context.read<AdminApi>().getCenters(
        status: _selectedStatus,
        search: _searchCtrl.text.trim(),
        page: 1,
        pageSize: 50,
      );
      if (mounted) {
        setState(() {
          _centers = res.items;
          _totalCount = res.totalCount;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải danh sách cơ sở. Vui lòng thử lại.';
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
          'Cơ sở thể thao',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(
              Icons.add_circle_outline,
              color: AppColors.primary,
            ),
            tooltip: 'Thêm cơ sở mới',
            onPressed: () async {
              final created = await context.push<bool>('/admin/centers/create');
              if (created == true) _loadCenters();
            },
          ),
        ],
      ),
      body: Column(
        children: [
          Container(
            color: AppColors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Column(
              children: [
                TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Tìm kiếm tên cơ sở, địa chỉ...',
                    prefixIcon: const Icon(Icons.search, size: 20),
                    suffixIcon: _searchCtrl.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear, size: 18),
                            onPressed: () {
                              _searchCtrl.clear();
                              _loadCenters();
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
                  onSubmitted: (_) => _loadCenters(),
                ),
                const SizedBox(height: 10),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    AdminFilterChipItem(
                      label: 'Tất cả ($_totalCount)',
                      isSelected: _selectedStatus == null,
                      onTap: () {
                        setState(() => _selectedStatus = null);
                        _loadCenters();
                      },
                    ),
                    AdminFilterChipItem(
                      label: 'Đang mở',
                      isSelected: _selectedStatus == 'Active',
                      onTap: () {
                        setState(() => _selectedStatus = 'Active');
                        _loadCenters();
                      },
                    ),
                    AdminFilterChipItem(
                      label: 'Tạm ngừng',
                      isSelected: _selectedStatus == 'Inactive',
                      onTap: () {
                        setState(() => _selectedStatus = 'Inactive');
                        _loadCenters();
                      },
                    ),
                  ],
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
        tooltip: 'Thêm cơ sở',
        onPressed: () async {
          final created = await context.push<bool>('/admin/centers/create');
          if (created == true) _loadCenters();
        },
        child: const Icon(Icons.add_business),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadCenters);
    }
    if (_centers.isEmpty) {
      return const EmptyState(
        title: 'Chưa có cơ sở nào',
        message: 'Thêm cơ sở thể thao đầu tiên để bắt đầu vận hành hệ thống.',
        icon: Icons.storefront_outlined,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadCenters,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _centers.length,
        separatorBuilder: (context, index) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final c = _centers[index];
          return AdminCard(
            onTap: () async {
              await context.push('/admin/centers/${c.centerId}');
              _loadCenters();
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SportImage(
                      url: c.coverImageUrl,
                      width: 64,
                      height: 64,
                      borderRadius: 10,
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
                                  c.name,
                                  style: const TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.w700,
                                    color: AppColors.textPrimary,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                              AdminStatusBadge(
                                label: AdminFormat.centerStatusName(c.status),
                                statusType: c.status,
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Text(
                            '${c.address}, ${c.district}, ${c.city}',
                            style: const TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                const Divider(height: 1),
                const SizedBox(height: 10),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.sports_tennis,
                          size: 15,
                          color: AppColors.primary,
                        ),
                        const SizedBox(width: 6),
                        Text(
                          '${c.activeCourtCount}/${c.courtCount} sân hoạt động',
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ],
                    ),
                    if (c.phone != null && c.phone!.isNotEmpty)
                      Row(
                        children: [
                          const Icon(
                            Icons.phone_outlined,
                            size: 13,
                            color: AppColors.textSecondary,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            c.phone!,
                            style: const TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                        ],
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
