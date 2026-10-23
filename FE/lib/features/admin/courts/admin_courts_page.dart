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

class AdminCourtsPage extends StatefulWidget {
  const AdminCourtsPage({super.key, this.initialCenterId});

  final String? initialCenterId;

  @override
  State<AdminCourtsPage> createState() => _AdminCourtsPageState();
}

class _AdminCourtsPageState extends State<AdminCourtsPage> {
  final TextEditingController _searchCtrl = TextEditingController();
  String? _selectedCenterId;
  String? _selectedSportId;
  String? _selectedStatus;

  List<AdminSportCenterItem> _centers = [];
  List<AdminSportItem> _sports = [];
  List<AdminCourtItem> _courts = [];
  bool _loading = false;
  String? _error;
  int _totalCount = 0;

  @override
  void initState() {
    super.initState();
    _selectedCenterId = widget.initialCenterId;
    _loadFilters();
    _loadCourts();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  void _loadFilters() async {
    try {
      final api = context.read<AdminApi>();
      final cRes = await api.getCenters(pageSize: 100);
      final sRes = await api.getSports(pageSize: 100);
      if (mounted) {
        setState(() {
          _centers = cRes.items;
          _sports = sRes.items;
        });
      }
    } catch (_) {}
  }

  Future<void> _loadCourts() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await context.read<AdminApi>().getCourts(
            centerId: _selectedCenterId,
            sportId: _selectedSportId,
            status: _selectedStatus,
            search: _searchCtrl.text.trim(),
            page: 1,
            pageSize: 50,
          );
      if (mounted) {
        setState(() {
          _courts = res.items;
          _totalCount = res.totalCount;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải danh sách sân. Vui lòng thử lại.';
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
          'Sân thể thao',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle_outline, color: AppColors.primary),
            tooltip: 'Thêm sân mới',
            onPressed: () async {
              final created = await context.push<bool>('/admin/courts/create');
              if (created == true) _loadCourts();
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
                    hintText: 'Tìm kiếm tên sân, mã sân...',
                    prefixIcon: const Icon(Icons.search, size: 20),
                    suffixIcon: _searchCtrl.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear, size: 18),
                            onPressed: () {
                              _searchCtrl.clear();
                              _loadCourts();
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
                  onSubmitted: (_) => _loadCourts(),
                ),
                const SizedBox(height: 10),
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      AdminFilterChipItem(
                        label: 'Tất cả trạng thái',
                        isSelected: _selectedStatus == null,
                        onTap: () {
                          setState(() => _selectedStatus = null);
                          _loadCourts();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Hoạt động',
                        isSelected: _selectedStatus == 'Active',
                        onTap: () {
                          setState(() => _selectedStatus = 'Active');
                          _loadCourts();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Bảo trì',
                        isSelected: _selectedStatus == 'Maintenance',
                        onTap: () {
                          setState(() => _selectedStatus = 'Maintenance');
                          _loadCourts();
                        },
                      ),
                      const SizedBox(width: 8),
                      AdminFilterChipItem(
                        label: 'Tạm ngừng',
                        isSelected: _selectedStatus == 'Inactive',
                        onTap: () {
                          setState(() => _selectedStatus = 'Inactive');
                          _loadCourts();
                        },
                      ),
                      const SizedBox(width: 8),
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
                      const SizedBox(width: 8),
                      ActionChip(
                        avatar: const Icon(Icons.sports_tennis,
                            size: 16, color: AppColors.teal),
                        label: Text(
                          _selectedSportId == null
                              ? 'Môn: Tất cả'
                              : 'Môn: ${_sports.firstWhere((s) => s.id == _selectedSportId, orElse: () => _sports.first).name}',
                          style: const TextStyle(fontSize: 12),
                        ),
                        onPressed: _showSportFilter,
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
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 4),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Tổng số: $_totalCount sân',
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),

          Expanded(child: _buildBody()),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
        tooltip: 'Thêm sân',
        onPressed: () async {
          final created = await context.push<bool>('/admin/courts/create');
          if (created == true) _loadCourts();
        },
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadCourts);
    }
    if (_courts.isEmpty) {
      return const EmptyState(
        title: 'Chưa có sân nào',
        message: 'Thêm sân thể thao hoặc thử thay đổi bộ lọc tìm kiếm.',
        icon: Icons.sports_tennis_outlined,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadCourts,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _courts.length,
        separatorBuilder: (context, index) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final c = _courts[index];
          return AdminCard(
            onTap: () async {
              await context.push('/admin/courts/${c.courtId}');
              _loadCourts();
            },
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: AppColors.lightMint,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.sports_tennis,
                      color: AppColors.primary, size: 24),
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
                                fontSize: 15,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textPrimary,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          AdminStatusBadge(
                            label: AdminFormat.courtStatusName(c.status),
                            statusType: c.status,
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Mã: ${c.code} · ${c.sportName}${c.surfaceType != null ? " · ${c.surfaceType}" : ""}',
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          const Icon(Icons.storefront,
                              size: 13, color: AppColors.textSecondary),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              c.sportCenterName,
                              style: const TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w500,
                                color: AppColors.textPrimary,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'Giá cơ sở: ${AdminFormat.currency(c.basePricePerHour)} / giờ',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                          color: AppColors.primary,
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
                _loadCourts();
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
                      _loadCourts();
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

  void _showSportFilter() {
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
                'Lọc theo môn thể thao',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
              ),
            ),
            ListTile(
              title: const Text('Tất cả môn thể thao'),
              trailing: _selectedSportId == null
                  ? const Icon(Icons.check, color: AppColors.primary)
                  : null,
              onTap: () {
                Navigator.pop(ctx);
                setState(() => _selectedSportId = null);
                _loadCourts();
              },
            ),
            const Divider(height: 1),
            Expanded(
              child: ListView.separated(
                itemCount: _sports.length,
                separatorBuilder: (context, index) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final s = _sports[i];
                  final isSel = s.id == _selectedSportId;
                  return ListTile(
                    title: Text(s.name),
                    trailing: isSel
                        ? const Icon(Icons.check, color: AppColors.primary)
                        : null,
                    onTap: () {
                      Navigator.pop(ctx);
                      setState(() => _selectedSportId = s.id);
                      _loadCourts();
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
