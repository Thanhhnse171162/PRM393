import '../shared/admin_image_field.dart';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/loading_widget.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import '../shared/admin_widgets.dart';

class AdminCourtFormPage extends StatefulWidget {
  const AdminCourtFormPage({super.key, this.courtId, this.initialData});

  final String? courtId;
  final AdminCourtDetail? initialData;

  @override
  State<AdminCourtFormPage> createState() => _AdminCourtFormPageState();
}

class _AdminCourtFormPageState extends State<AdminCourtFormPage> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _codeCtrl;
  late final TextEditingController _nameCtrl;
  late final TextEditingController _surfaceCtrl;
  late final TextEditingController _priceCtrl;
  late final TextEditingController _descCtrl;
  late final TextEditingController _imageCtrl;

  String? _selectedCenterId;
  String? _selectedSportId;

  List<AdminSportCenterItem> _centers = [];
  List<AdminSportItem> _sports = [];
  bool _loading = true;
  bool _saving = false;
  bool _uploadingImage = false;

  bool get _isEdit => widget.courtId != null;

  @override
  void initState() {
    super.initState();
    final d = widget.initialData;
    _codeCtrl = TextEditingController(text: d?.code ?? '');
    _nameCtrl = TextEditingController(text: d?.name ?? '');
    _surfaceCtrl = TextEditingController(text: d?.surfaceType ?? '');
    _priceCtrl = TextEditingController(
      text: d != null ? d.basePricePerHour.toStringAsFixed(0) : '150000',
    );
    _descCtrl = TextEditingController(text: d?.description ?? '');
    _imageCtrl = TextEditingController(text: d?.coverImageUrl ?? '');
    _selectedCenterId = d?.sportCenterId;
    _selectedSportId = d?.sportId;

    _loadData();
  }

  void _loadData() async {
    try {
      final api = context.read<AdminApi>();
      final cRes = await api.getCenters(pageSize: 100);
      final sRes = await api.getSports(pageSize: 100);

      AdminCourtDetail? fetched = widget.initialData;
      if (_isEdit && fetched == null) {
        fetched = await api.getCourt(widget.courtId!);
        _codeCtrl.text = fetched.code;
        _nameCtrl.text = fetched.name;
        _surfaceCtrl.text = fetched.surfaceType ?? '';
        _priceCtrl.text = fetched.basePricePerHour.toStringAsFixed(0);
        _descCtrl.text = fetched.description ?? '';
        _imageCtrl.text = fetched.coverImageUrl ?? '';
        _selectedCenterId = fetched.sportCenterId;
        _selectedSportId = fetched.sportId;
      }

      if (mounted) {
        setState(() {
          _centers = cRes.items;
          _sports = sRes.items;
          if (!_isEdit) {
            if (_centers.isNotEmpty && _selectedCenterId == null) {
              _selectedCenterId = _centers.first.centerId;
            }
            if (_sports.isNotEmpty && _selectedSportId == null) {
              _selectedSportId = _sports.first.id;
            }
          }
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose();
    _nameCtrl.dispose();
    _surfaceCtrl.dispose();
    _priceCtrl.dispose();
    _descCtrl.dispose();
    _imageCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_saving || _uploadingImage) return;
    if (!_formKey.currentState!.validate()) return;
    if (!_isEdit && _selectedCenterId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Vui lòng chọn cơ sở thể thao'),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }
    if (_selectedSportId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Vui lòng chọn môn thể thao'),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }

    setState(() => _saving = true);
    final price = double.tryParse(_priceCtrl.text.trim()) ?? 0.0;

    try {
      final api = context.read<AdminApi>();
      if (_isEdit) {
        await api.updateCourt(widget.courtId!, {
          'name': _nameCtrl.text.trim(),
          'sportId': _selectedSportId,
          'surfaceType': _surfaceCtrl.text.trim().isEmpty
              ? null
              : _surfaceCtrl.text.trim(),
          'description': _descCtrl.text.trim().isEmpty
              ? null
              : _descCtrl.text.trim(),
          'coverImageUrl': _imageCtrl.text.trim().isEmpty
              ? null
              : _imageCtrl.text.trim(),
          'basePricePerHour': price,
        });
      } else {
        await api.createCourt({
          'sportCenterId': _selectedCenterId,
          'sportId': _selectedSportId,
          'code': _codeCtrl.text.trim().toUpperCase(),
          'name': _nameCtrl.text.trim(),
          'surfaceType': _surfaceCtrl.text.trim().isEmpty
              ? null
              : _surfaceCtrl.text.trim(),
          'description': _descCtrl.text.trim().isEmpty
              ? null
              : _descCtrl.text.trim(),
          'coverImageUrl': _imageCtrl.text.trim().isEmpty
              ? null
              : _imageCtrl.text.trim(),
          'basePricePerHour': price,
        });
      }

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              _isEdit
                  ? 'Đã cập nhật sân thành công!'
                  : 'Đã tạo sân thể thao mới!',
            ),
          ),
        );
        Navigator.pop(context, true);
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
        title: Text(
          _isEdit ? 'Chỉnh sửa sân' : 'Thêm sân thể thao',
          style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
      ),
      body: _loading
          ? const LoadingWidget()
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    AdminCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Phân loại & Định danh',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 14),
                          if (!_isEdit) ...[
                            DropdownButtonFormField<String>(
                              value: _selectedCenterId,
                              decoration: const InputDecoration(
                                labelText: 'Cơ sở trực thuộc *',
                                prefixIcon: Icon(Icons.storefront, size: 20),
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
                              onChanged: (v) =>
                                  setState(() => _selectedCenterId = v),
                            ),
                            const SizedBox(height: 12),
                          ],
                          DropdownButtonFormField<String>(
                            value: _selectedSportId,
                            decoration: const InputDecoration(
                              labelText: 'Môn thể thao *',
                              prefixIcon: Icon(
                                Icons.sports_basketball,
                                size: 20,
                              ),
                              border: OutlineInputBorder(),
                            ),
                            items: _sports.map((s) {
                              return DropdownMenuItem(
                                value: s.id,
                                child: Text(s.name),
                              );
                            }).toList(),
                            onChanged: (v) =>
                                setState(() => _selectedSportId = v),
                          ),
                          const SizedBox(height: 12),
                          if (!_isEdit) ...[
                            TextFormField(
                              controller: _codeCtrl,
                              decoration: const InputDecoration(
                                labelText: 'Mã sân (Code) *',
                                hintText: 'VD: SAN-01, BAD-01',
                                border: OutlineInputBorder(),
                              ),
                              validator: (v) => v == null || v.trim().isEmpty
                                  ? 'Vui lòng nhập mã sân'
                                  : null,
                            ),
                            const SizedBox(height: 12),
                          ],
                          TextFormField(
                            controller: _nameCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Tên hiển thị sân *',
                              hintText: 'VD: Sân 1 - Thảm PVC',
                              border: OutlineInputBorder(),
                            ),
                            validator: (v) => v == null || v.trim().isEmpty
                                ? 'Vui lòng nhập tên sân'
                                : null,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),

                    AdminCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Đặc tính & Giá cơ bản',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 14),
                          TextFormField(
                            controller: _priceCtrl,
                            keyboardType: TextInputType.number,
                            decoration: const InputDecoration(
                              labelText: 'Giá cơ sở mỗi giờ (VND) *',
                              hintText: '150000',
                              prefixIcon: Icon(
                                Icons.payments_outlined,
                                size: 20,
                              ),
                              border: OutlineInputBorder(),
                            ),
                            validator: (v) {
                              if (v == null || v.trim().isEmpty) {
                                return 'Vui lòng nhập giá cơ sở';
                              }
                              final val = double.tryParse(v.trim());
                              if (val == null || val < 0) {
                                return 'Giá phải là số không âm';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 12),
                          TextFormField(
                            controller: _surfaceCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Loại mặt sân (tùy chọn)',
                              hintText: 'VD: Thảm PVC, Gỗ, Cỏ nhân tạo',
                              border: OutlineInputBorder(),
                            ),
                          ),
                          const SizedBox(height: 12),
                          TextFormField(
                            controller: _descCtrl,
                            maxLines: 2,
                            decoration: const InputDecoration(
                              labelText: 'Mô tả chi tiết sân',
                              hintText:
                                  'VD: Sân trong nhà, đèn LED chuẩn thi đấu',
                              border: OutlineInputBorder(),
                            ),
                          ),
                          const SizedBox(height: 12),
                          AdminImageField(
                            controller: _imageCtrl,
                            enabled: !_saving,
                            onBusyChanged: (value) =>
                                setState(() => _uploadingImage = value),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),

                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: AppColors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      onPressed: _saving || _uploadingImage ? null : _submit,
                      child: _saving
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: AppColors.white,
                              ),
                            )
                          : Text(
                              _isEdit ? 'Lưu thay đổi' : 'Tạo sân thể thao',
                              style: const TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
