import '../shared/admin_image_field.dart';
import '../../../core/widgets/error_state.dart';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_tokens.dart';
import '../admin_api.dart';
import '../shared/admin_models.dart';
import 'admin_location_picker_page.dart';

class AdminCenterFormPage extends StatefulWidget {
  const AdminCenterFormPage({super.key, this.centerId, this.initialData});

  final String? centerId;
  final AdminSportCenterDetail? initialData;

  @override
  State<AdminCenterFormPage> createState() => _AdminCenterFormPageState();
}

class _AdminCenterFormPageState extends State<AdminCenterFormPage> {
  final _formKey = GlobalKey<FormState>();

  late final TextEditingController _nameCtrl;
  late final TextEditingController _addressCtrl;
  late final TextEditingController _wardCtrl;
  late final TextEditingController _districtCtrl;
  late final TextEditingController _cityCtrl;
  late final TextEditingController _phoneCtrl;
  late final TextEditingController _imageCtrl;

  final MapController _previewMapController = MapController();

  String _timeZoneId = 'Asia/Ho_Chi_Minh';
  LatLng? _selectedLocation;
  String? _pickedAddressHint;

  bool _saving = false;
  bool _uploadingImage = false;
  String? _loadError;
  bool _loading = false;
  bool _locationError = false;

  bool get _isEdit => widget.centerId != null;

  static final RegExp _phoneRegex = RegExp(r'^\+?[0-9]{9,15}$');

  @override
  void initState() {
    super.initState();
    final d = widget.initialData;
    _nameCtrl = TextEditingController(text: d?.name ?? '');
    _addressCtrl = TextEditingController(text: d?.addressLine ?? '');
    _wardCtrl = TextEditingController(text: d?.ward ?? '');
    _districtCtrl = TextEditingController(text: d?.district ?? 'Quận 7');
    _cityCtrl = TextEditingController(text: d?.city ?? 'TP. Hồ Chí Minh');
    _phoneCtrl = TextEditingController(text: d?.phoneNumber ?? '');
    _imageCtrl = TextEditingController(text: d?.coverImageUrl ?? '');
    _timeZoneId = d?.timeZoneId ?? 'Asia/Ho_Chi_Minh';

    if (d?.latitude != null && d?.longitude != null) {
      _selectedLocation = LatLng(d!.latitude!, d.longitude!);
    }

    if (_isEdit && widget.initialData == null) {
      _fetchInitial();
    }
  }

  void _fetchInitial() async {
    setState(() {
      _loading = true;
      _loadError = null;
    });
    try {
      final d = await context.read<AdminApi>().getCenter(widget.centerId!);
      if (mounted) {
        setState(() {
          _nameCtrl.text = d.name;
          _addressCtrl.text = d.addressLine;
          _wardCtrl.text = d.ward ?? '';
          _districtCtrl.text = d.district;
          _cityCtrl.text = d.city;
          _phoneCtrl.text = d.phoneNumber ?? '';
          _imageCtrl.text = d.coverImageUrl ?? '';
          _timeZoneId = d.timeZoneId;

          if (d.latitude != null && d.longitude != null) {
            _selectedLocation = LatLng(d.latitude!, d.longitude!);
          }
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted)
        setState(() {
          _loading = false;
          _loadError = 'Không thể tải cơ sở. Hãy thử lại để tránh sửa dữ liệu chưa đầy đủ.';
        });
    }
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _addressCtrl.dispose();
    _wardCtrl.dispose();
    _districtCtrl.dispose();
    _cityCtrl.dispose();
    _phoneCtrl.dispose();
    _imageCtrl.dispose();
    _previewMapController.dispose();
    super.dispose();
  }

  Future<void> _openLocationPicker() async {
    final result = await Navigator.of(context).push<AdminLocationPickerResult>(
      MaterialPageRoute(
        builder: (_) =>
            AdminLocationPickerPage(initialLocation: _selectedLocation),
      ),
    );

    if (result != null && mounted) {
      setState(() {
        _selectedLocation = LatLng(result.latitude, result.longitude);
        _locationError = false;
        if (result.displayName != null && result.displayName!.isNotEmpty) {
          _pickedAddressHint = result.displayName;
        }
      });
    }
  }

  Future<void> _submit() async {
    if (_saving || _uploadingImage || _loading || _loadError != null) return;
    final isFormValid = _formKey.currentState!.validate();

    if (_selectedLocation == null) {
      setState(() => _locationError = true);
    }

    if (!isFormValid || _selectedLocation == null) {
      if (_selectedLocation == null && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Vui lòng chọn vị trí cơ sở trên bản đồ.'),
            backgroundColor: AppColors.warning,
          ),
        );
      }
      return;
    }

    // Validate coordinate ranges
    final lat = _selectedLocation!.latitude;
    final lng = _selectedLocation!.longitude;
    if (lat < -90 || lat > 90 || lng < -180 || lng > 180) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Tọa độ vị trí không hợp lệ (-90..90, -180..180).'),
          backgroundColor: AppColors.error,
        ),
      );
      return;
    }

    setState(() => _saving = true);

    final payload = {
      'name': _nameCtrl.text.trim(),
      'addressLine': _addressCtrl.text.trim(),
      'ward': _wardCtrl.text.trim().isEmpty ? null : _wardCtrl.text.trim(),
      'district': _districtCtrl.text.trim(),
      'city': _cityCtrl.text.trim(),
      'timeZoneId': _timeZoneId,
      'phoneNumber': _phoneCtrl.text.trim().isEmpty
          ? null
          : _phoneCtrl.text.trim(),
      'coverImageUrl': _imageCtrl.text.trim().isEmpty
          ? null
          : _imageCtrl.text.trim(),
      'latitude': lat,
      'longitude': lng,
    };

    try {
      final api = context.read<AdminApi>();
      if (_isEdit) {
        await api.updateCenter(widget.centerId!, payload);
      } else {
        await api.createCenter(payload);
      }

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              _isEdit
                  ? 'Đã cập nhật cơ sở thành công!'
                  : 'Đã tạo cơ sở mới thành công!',
            ),
            backgroundColor: AppColors.primary,
          ),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              e is ApiException
                  ? e.message
                  : 'Không thể lưu thông tin cơ sở. Vui lòng thử lại.',
            ),
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
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              _isEdit ? 'Chỉnh sửa cơ sở thể thao' : 'Thêm cơ sở thể thao',
              style: const TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 18,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              _isEdit
                  ? 'Cập nhật thông tin cơ sở thể thao trên hệ thống'
                  : 'Tạo thông tin cơ sở thể thao để quản lý trên hệ thống',
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w400,
                color: AppColors.textSecondary,
              ),
            ),
          ],
        ),
        elevation: 0,
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.textPrimary,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: AppColors.border, height: 1),
        ),
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            )
          : _loadError != null
          ? ErrorState(message: _loadError!, onRetry: _fetchInitial)
          : Column(
              children: [
                Expanded(
                  child: SingleChildScrollView(
                    keyboardDismissBehavior:
                        ScrollViewKeyboardDismissBehavior.onDrag,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 18,
                    ),
                    child: Form(
                      key: _formKey,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          // SECTION B: THÔNG TIN CHUNG
                          _buildGeneralInfoCard(),
                          const SizedBox(height: 18),

                          // SECTION C: ĐỊA CHỈ & VỊ TRÍ
                          _buildAddressAndLocationCard(),
                          const SizedBox(height: 18),

                          // SECTION D: HÌNH ẢNH ĐẠI DIỆN
                          _buildCoverImageCard(),
                          const SizedBox(height: 24),
                        ],
                      ),
                    ),
                  ),
                ),

                // SECTION E: BOTTOM FIXED SUBMIT BAR
                _buildBottomSubmitBar(),
              ],
            ),
    );
  }

  // Section B Widget
  Widget _buildGeneralInfoCard() {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.border),
        boxShadow: AppShadows.soft,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: AppColors.lightMint,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.storefront_outlined,
                  color: AppColors.primary,
                  size: 20,
                ),
              ),
              const SizedBox(width: 10),
              const Text(
                'Thông tin chung',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Tên cơ sở thể thao (*)
          TextFormField(
            controller: _nameCtrl,
            decoration: InputDecoration(
              labelText: 'Tên cơ sở thể thao *',
              hintText: 'VD: CourtGo Tân Bình Center',
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 14,
                vertical: 14,
              ),
            ),
            validator: (v) {
              if (v == null || v.trim().isEmpty) {
                return 'Vui lòng nhập tên cơ sở thể thao';
              }
              if (v.trim().length > 200) {
                return 'Tên cơ sở không được vượt quá 200 ký tự';
              }
              return null;
            },
          ),
          const SizedBox(height: 14),

          // Số điện thoại liên hệ
          TextFormField(
            controller: _phoneCtrl,
            keyboardType: TextInputType.phone,
            decoration: InputDecoration(
              labelText: 'Số điện thoại liên hệ',
              hintText: 'VD: 0901234567',
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 14,
                vertical: 14,
              ),
            ),
            validator: (v) {
              if (v != null && v.trim().isNotEmpty) {
                final phone = v.trim();
                if (phone.length > 20) {
                  return 'Số điện thoại không được vượt quá 20 ký tự';
                }
                if (!_phoneRegex.hasMatch(phone)) {
                  return 'Số điện thoại không hợp lệ (9-15 chữ số)';
                }
              }
              return null;
            },
          ),
          const SizedBox(height: 14),

          // Múi giờ vận hành (*)
          DropdownButtonFormField<String>(
            initialValue: _timeZoneId,
            isExpanded: true,
            decoration: InputDecoration(
              labelText: 'Múi giờ vận hành *',
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 14,
                vertical: 14,
              ),
            ),
            items: [
              const DropdownMenuItem(
                value: 'Asia/Ho_Chi_Minh',
                child: Text('Asia/Ho_Chi_Minh (UTC+7 - Việt Nam)'),
              ),
              const DropdownMenuItem(
                value: 'SE Asia Standard Time',
                child: Text('SE Asia Standard Time (UTC+7)'),
              ),
              const DropdownMenuItem(
                value: 'UTC',
                child: Text('UTC (Giờ phối hợp quốc tế)'),
              ),
              // Support legacy timezone if loaded from backend
              if (_timeZoneId != 'Asia/Ho_Chi_Minh' &&
                  _timeZoneId != 'SE Asia Standard Time' &&
                  _timeZoneId != 'UTC')
                DropdownMenuItem(value: _timeZoneId, child: Text(_timeZoneId)),
            ],
            onChanged: (v) {
              if (v != null) setState(() => _timeZoneId = v);
            },
          ),
        ],
      ),
    );
  }

  // Section C Widget
  Widget _buildAddressAndLocationCard() {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.border),
        boxShadow: AppShadows.soft,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: AppColors.lightMint,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.place_outlined,
                  color: AppColors.primary,
                  size: 20,
                ),
              ),
              const SizedBox(width: 10),
              const Text(
                'Địa chỉ & Vị trí',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Địa chỉ (Số nhà, tên đường) (*)
          TextFormField(
            controller: _addressCtrl,
            decoration: InputDecoration(
              labelText: 'Địa chỉ (Số nhà, tên đường) *',
              hintText: 'VD: 123 Nguyễn Thị Thập',
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 14,
                vertical: 14,
              ),
            ),
            validator: (v) {
              if (v == null || v.trim().isEmpty) {
                return 'Vui lòng nhập số nhà và tên đường';
              }
              if (v.trim().length > 300) {
                return 'Địa chỉ không được vượt quá 300 ký tự';
              }
              return null;
            },
          ),
          const SizedBox(height: 14),

          // Phường / Xã
          TextFormField(
            controller: _wardCtrl,
            decoration: InputDecoration(
              labelText: 'Phường / Xã',
              hintText: 'VD: Phường Tân Phú',
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 14,
                vertical: 14,
              ),
            ),
            validator: (v) {
              if (v != null && v.trim().length > 100) {
                return 'Phường/Xã không được vượt quá 100 ký tự';
              }
              return null;
            },
          ),
          const SizedBox(height: 14),

          // Quận / Huyện (*) và Tỉnh / Thành phố (*)
          Row(
            children: [
              Expanded(
                child: TextFormField(
                  controller: _districtCtrl,
                  decoration: InputDecoration(
                    labelText: 'Quận / Huyện *',
                    hintText: 'VD: Quận 7',
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 14,
                      vertical: 14,
                    ),
                  ),
                  validator: (v) {
                    if (v == null || v.trim().isEmpty) {
                      return 'Nhập quận/huyện';
                    }
                    if (v.trim().length > 100) {
                      return 'Tối đa 100 ký tự';
                    }
                    return null;
                  },
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: TextFormField(
                  controller: _cityCtrl,
                  decoration: InputDecoration(
                    labelText: 'Tỉnh / Thành phố *',
                    hintText: 'VD: TP. Hồ Chí Minh',
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 14,
                      vertical: 14,
                    ),
                  ),
                  validator: (v) {
                    if (v == null || v.trim().isEmpty) {
                      return 'Nhập tỉnh/thành';
                    }
                    if (v.trim().length > 100) {
                      return 'Tối đa 100 ký tự';
                    }
                    return null;
                  },
                ),
              ),
            ],
          ),
          const SizedBox(height: 18),

          const Divider(height: 1),
          const SizedBox(height: 16),

          // Map Header Row
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Row(
                children: [
                  Text(
                    'Vị trí trên bản đồ *',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
              if (_selectedLocation != null)
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.lightMint,
                    borderRadius: BorderRadius.circular(6),
                    border: Border.all(
                      color: AppColors.primary.withValues(alpha: 0.3),
                    ),
                  ),
                  child: const Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        Icons.check_circle_rounded,
                        color: AppColors.primary,
                        size: 14,
                      ),
                      SizedBox(width: 4),
                      Text(
                        'Đã ghim vị trí',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: AppColors.primary,
                        ),
                      ),
                    ],
                  ),
                ),
            ],
          ),
          const SizedBox(height: 6),
          const Text(
            'Ghim vị trí chính xác để khách hàng dễ dàng tìm kiếm và chỉ đường.',
            style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
          ),
          const SizedBox(height: 12),

          // Map Preview
          _buildMapPreview(),

          if (_locationError && _selectedLocation == null) ...[
            const SizedBox(height: 8),
            const Row(
              children: [
                Icon(Icons.error_outline, color: AppColors.error, size: 15),
                SizedBox(width: 4),
                Text(
                  'Vui lòng chọn vị trí cơ sở trên bản đồ.',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.error,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  // Interactive Map Preview Card
  Widget _buildMapPreview() {
    final border = _locationError && _selectedLocation == null
        ? Border.all(color: AppColors.error, width: 1.5)
        : Border.all(color: AppColors.border);

    if (_selectedLocation == null) {
      // Placeholder state when no location selected
      return Container(
        height: 185,
        decoration: BoxDecoration(
          color: AppColors.background,
          borderRadius: BorderRadius.circular(14),
          border: border,
        ),
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.lightMint,
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: AppColors.primary.withValues(alpha: 0.2),
                  ),
                ),
                child: const Icon(
                  Icons.add_location_alt_outlined,
                  color: AppColors.primary,
                  size: 32,
                ),
              ),
              const SizedBox(height: 10),
              const Text(
                'Chưa chọn vị trí trên bản đồ',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 4),
              const Text(
                'Mở bản đồ để ghim tọa độ cơ sở',
                style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 12),
              ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: AppColors.white,
                  elevation: 0,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 10,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                onPressed: _openLocationPicker,
                icon: const Icon(Icons.pin_drop, size: 18),
                label: const Text(
                  'Chọn vị trí trên bản đồ',
                  style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
                ),
              ),
            ],
          ),
        ),
      );
    }

    // Active map preview showing OpenStreetMap tiles + pin marker
    return Container(
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(14),
        border: border,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          ClipRRect(
            borderRadius: const BorderRadius.vertical(top: Radius.circular(13)),
            child: SizedBox(
              height: 180,
              child: Stack(
                children: [
                  FlutterMap(
                    mapController: _previewMapController,
                    options: MapOptions(
                      initialCenter: _selectedLocation!,
                      initialZoom: 15.0,
                      interactionOptions: const InteractionOptions(
                        flags: InteractiveFlag.pinchZoom | InteractiveFlag.drag,
                      ),
                    ),
                    children: [
                      TileLayer(
                        urlTemplate:
                            'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                        userAgentPackageName: 'vn.courtgo.mobile',
                      ),
                      MarkerLayer(
                        markers: [
                          Marker(
                            point: _selectedLocation!,
                            width: 44,
                            height: 44,
                            alignment: Alignment.topCenter,
                            child: Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(4),
                                  decoration: BoxDecoration(
                                    color: AppColors.primary,
                                    shape: BoxShape.circle,
                                    boxShadow: [
                                      BoxShadow(
                                        color: Colors.black.withValues(
                                          alpha: 0.3,
                                        ),
                                        blurRadius: 6,
                                        offset: const Offset(0, 3),
                                      ),
                                    ],
                                  ),
                                  child: const Icon(
                                    Icons.sports_tennis,
                                    color: Colors.white,
                                    size: 16,
                                  ),
                                ),
                                const Icon(
                                  Icons.arrow_drop_down,
                                  color: AppColors.primary,
                                  size: 18,
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),

          // Coordinates & Change Button Row
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            decoration: const BoxDecoration(
              color: AppColors.white,
              borderRadius: BorderRadius.vertical(bottom: Radius.circular(13)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Tọa độ đã chọn',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w500,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const SizedBox(height: 2),
                          SelectableText(
                            '${_selectedLocation!.latitude.toStringAsFixed(6)}, ${_selectedLocation!.longitude.toStringAsFixed(6)}',
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                    ),
                    OutlinedButton.icon(
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.primary,
                        side: const BorderSide(color: AppColors.primary),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 8,
                        ),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                      onPressed: _openLocationPicker,
                      icon: const Icon(
                        Icons.edit_location_alt_outlined,
                        size: 16,
                      ),
                      label: const Text(
                        'Đổi vị trí',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
                ),
                if (_pickedAddressHint != null) ...[
                  const SizedBox(height: 6),
                  Text(
                    _pickedAddressHint!,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 11,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  // Section D Widget
  Widget _buildCoverImageCard() {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.border),
        boxShadow: AppShadows.soft,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: AppColors.lightMint,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.photo_library_outlined,
                  color: AppColors.primary,
                  size: 20,
                ),
              ),
              const SizedBox(width: 10),
              const Text(
                'Hình ảnh đại diện',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          AdminImageField(
            controller: _imageCtrl,
            enabled: !_saving,
            onBusyChanged: (value) => setState(() => _uploadingImage = value),
          ),
        ],
      ),
    );
  }

  // Section E Widget
  Widget _buildBottomSubmitBar() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: AppColors.white,
        border: const Border(top: BorderSide(color: AppColors.border)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, -3),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: AppColors.white,
            elevation: 0,
            padding: const EdgeInsets.symmetric(vertical: 15),
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
                  _isEdit ? 'Lưu thay đổi' : 'Tạo cơ sở thể thao',
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
        ),
      ),
    );
  }
}
