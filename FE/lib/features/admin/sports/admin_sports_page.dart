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

class AdminSportsPage extends StatefulWidget {
  const AdminSportsPage({super.key});

  @override
  State<AdminSportsPage> createState() => _AdminSportsPageState();
}

class _AdminSportsPageState extends State<AdminSportsPage> {
  final TextEditingController _searchCtrl = TextEditingController();
  bool? _selectedActive;
  List<AdminSportItem> _sports = [];
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadSports();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadSports() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await context.read<AdminApi>().getSports(
            isActive: _selectedActive,
            search: _searchCtrl.text.trim(),
            page: 1,
            pageSize: 50,
          );
      if (mounted) {
        setState(() {
          _sports = res.items;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = 'Không thể tải danh sách môn thể thao';
        });
      }
    }
  }

  Future<void> _toggleStatus(AdminSportItem item) async {
    final next = !item.isActive;
    try {
      await context.read<AdminApi>().updateSportStatus(item.id, next);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(next
                ? 'Đã kích hoạt môn ${item.name}'
                : 'Đã tạm ngừng môn ${item.name}'),
          ),
        );
        _loadSports();
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
    }
  }

  void _showFormSheet({AdminSportItem? existing}) {
    final isEdit = existing != null;
    final codeCtrl = TextEditingController(text: existing?.code ?? '');
    final nameCtrl = TextEditingController(text: existing?.name ?? '');
    final iconCtrl = TextEditingController(text: existing?.iconUrl ?? '');
    final orderCtrl =
        TextEditingController(text: (existing?.displayOrder ?? 0).toString());
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
                    Text(
                      isEdit ? 'Chỉnh sửa môn thể thao' : 'Thêm môn thể thao mới',
                      style: const TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 16),
                    if (!isEdit) ...[
                      TextFormField(
                        controller: codeCtrl,
                        decoration: const InputDecoration(
                          labelText: 'Mã định danh (Code) *',
                          hintText: 'VD: BADMINTON, PICKLEBALL',
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) => v == null || v.trim().isEmpty
                            ? 'Vui lòng nhập mã code'
                            : null,
                      ),
                      const SizedBox(height: 12),
                    ],
                    TextFormField(
                      controller: nameCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Tên môn thể thao *',
                        hintText: 'VD: Cầu lông, Pickleball',
                        border: OutlineInputBorder(),
                      ),
                      validator: (v) => v == null || v.trim().isEmpty
                          ? 'Vui lòng nhập tên môn'
                          : null,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: iconCtrl,
                      decoration: const InputDecoration(
                        labelText: 'URL biểu tượng / hình ảnh',
                        hintText: 'https://...',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: orderCtrl,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Thứ tự ưu tiên hiển thị (0, 1, 2...)',
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
                              if (!formKey.currentState!.validate()) return;
                              setSheetState(() => saving = true);
                              final payload = {
                                if (!isEdit) 'code': codeCtrl.text.trim().toUpperCase(),
                                'name': nameCtrl.text.trim(),
                                'iconUrl': iconCtrl.text.trim().isEmpty
                                    ? null
                                    : iconCtrl.text.trim(),
                                'displayOrder':
                                    int.tryParse(orderCtrl.text.trim()) ?? 0,
                              };
                              try {
                                final api = context.read<AdminApi>();
                                if (isEdit) {
                                  await api.updateSport(existing.id, payload);
                                } else {
                                  await api.createSport(payload);
                                }
                                if (ctx.mounted) Navigator.pop(ctx);
                                _loadSports();
                              } catch (e) {
                                setSheetState(() => saving = false);
                                if (ctx.mounted) {
                                  ScaffoldMessenger.of(ctx).showSnackBar(
                                    SnackBar(
                                      content: Text(e is ApiException
                                          ? e.message
                                          : 'Lưu thất bại'),
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
                          : Text(isEdit ? 'Lưu thay đổi' : 'Thêm môn thể thao'),
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
          'Môn thể thao',
          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle_outline, color: AppColors.primary),
            tooltip: 'Thêm môn thể thao',
            onPressed: () => _showFormSheet(),
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
                    hintText: 'Tìm kiếm môn thể thao...',
                    prefixIcon: const Icon(Icons.search, size: 20),
                    suffixIcon: _searchCtrl.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear, size: 18),
                            onPressed: () {
                              _searchCtrl.clear();
                              _loadSports();
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
                  onSubmitted: (_) => _loadSports(),
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    AdminFilterChipItem(
                      label: 'Tất cả (${_sports.length})',
                      isSelected: _selectedActive == null,
                      onTap: () {
                        setState(() => _selectedActive = null);
                        _loadSports();
                      },
                    ),
                    const SizedBox(width: 8),
                    AdminFilterChipItem(
                      label: 'Đang mở',
                      isSelected: _selectedActive == true,
                      onTap: () {
                        setState(() => _selectedActive = true);
                        _loadSports();
                      },
                    ),
                    const SizedBox(width: 8),
                    AdminFilterChipItem(
                      label: 'Đã khóa',
                      isSelected: _selectedActive == false,
                      onTap: () {
                        setState(() => _selectedActive = false);
                        _loadSports();
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
        tooltip: 'Thêm môn thể thao',
        onPressed: () => _showFormSheet(),
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const LoadingWidget();
    if (_error != null) {
      return ErrorState(message: _error!, onRetry: _loadSports);
    }
    if (_sports.isEmpty) {
      return const EmptyState(
        title: 'Chưa có môn thể thao nào',
        message: 'Thêm các môn như Cầu lông, Pickleball, Tennis...',
        icon: Icons.sports_tennis_outlined,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadSports,
      color: AppColors.primary,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _sports.length,
        separatorBuilder: (context, index) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final s = _sports[index];
          return AdminCard(
            child: Row(
              children: [
                Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: AppColors.lightMint,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Center(
                    child: s.iconUrl != null && s.iconUrl!.isNotEmpty
                        ? Image.network(
                            s.iconUrl!,
                            width: 24,
                            height: 24,
                            errorBuilder: (context, error, stackTrace) =>
                                const Icon(
                              Icons.sports_basketball,
                              color: AppColors.primary,
                              size: 24,
                            ),
                          )
                        : const Icon(
                            Icons.sports_basketball,
                            color: AppColors.primary,
                            size: 24,
                          ),
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              s.name,
                              style: const TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ),
                          AdminStatusBadge(
                            label: s.isActive ? 'Hoạt động' : 'Tạm khóa',
                            statusType: s.isActive ? 'Active' : 'Inactive',
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Mã: ${s.code} · ${s.courtCount} sân thuộc môn này · Thứ tự: ${s.displayOrder}',
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 6),
                PopupMenuButton<String>(
                  icon: const Icon(Icons.more_vert, size: 20),
                  onSelected: (v) {
                    if (v == 'edit') {
                      _showFormSheet(existing: s);
                    } else if (v == 'toggle') {
                      _toggleStatus(s);
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
                            s.isActive
                                ? Icons.lock_outline
                                : Icons.lock_open_outlined,
                            size: 16,
                            color: s.isActive ? AppColors.error : AppColors.primary,
                          ),
                          const SizedBox(width: 8),
                          Text(s.isActive ? 'Tạm khóa' : 'Kích hoạt'),
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
