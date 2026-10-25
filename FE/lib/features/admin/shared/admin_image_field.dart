import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../../core/network/api_exception.dart';
import '../../../shared/widgets/image_url_preview.dart';
import '../admin_api.dart';

class AdminImageField extends StatefulWidget {
  const AdminImageField({
    super.key,
    required this.controller,
    required this.onBusyChanged,
    this.pickImage,
    this.enabled = true,
  });
  final TextEditingController controller;
  final ValueChanged<bool> onBusyChanged;
  final Future<XFile?> Function()? pickImage;
  final bool enabled;

  @override
  State<AdminImageField> createState() => _AdminImageFieldState();
}

class _AdminImageFieldState extends State<AdminImageField> {
  bool _busy = false;
  String? _error;

  Future<void> _choose() async {
    if (_busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    widget.onBusyChanged(true);
    try {
      final picked =
          await (widget.pickImage?.call() ??
              ImagePicker().pickImage(
                source: ImageSource.gallery,
                maxWidth: 1920,
                maxHeight: 1920,
                imageQuality: 85,
              ));
      if (picked == null || !mounted) return;
      if (await picked.length() > 5 * 1024 * 1024) {
        throw const ApiException(message: 'Ảnh không được vượt quá 5 MB.');
      }
      final bytes = await picked.readAsBytes();
      if (!mounted) return;
      final result = await context.read<AdminApi>().post(
        '/api/admin/images',
        FormData.fromMap({
          'file': MultipartFile.fromBytes(bytes, filename: 'cover-image'),
        }),
      );
      if (!mounted) return;
      widget.controller.text = result['imageUrl'] as String;
    } catch (error) {
      if (mounted) {
        setState(
          () => _error = error is ApiException ? error.message : 'Không thể chọn hoặc tải ảnh. Hãy kiểm tra quyền truy cập ảnh và thử lại.',
        );
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
        widget.onBusyChanged(false);
      }
    }
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      OutlinedButton.icon(
        onPressed: _busy || !widget.enabled ? null : _choose,
        icon: _busy
            ? const SizedBox.square(
                dimension: 18,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : const Icon(Icons.add_photo_alternate_outlined),
        label: Text(_busy ? 'Đang tải ảnh...' : 'Chọn ảnh từ thiết bị'),
      ),
      const SizedBox(height: 6),
      const Text(
        'JPEG hoặc PNG, tối đa 5 MB. Bấm Lưu để áp dụng ảnh cho cơ sở hoặc sân.',
        style: TextStyle(fontSize: 12),
      ),
      if (_error != null)
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 8),
          child: Text(
            _error!,
            style: TextStyle(color: Theme.of(context).colorScheme.error),
          ),
        ),
      const SizedBox(height: 12),
      ImageUrlPreview(controller: widget.controller),
      ValueListenableBuilder<TextEditingValue>(
        valueListenable: widget.controller,
        builder: (_, value, _) => value.text.isEmpty
            ? const SizedBox.shrink()
            : Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: _busy || !widget.enabled
                      ? null
                      : widget.controller.clear,
                  icon: const Icon(Icons.delete_outline),
                  label: const Text('Bỏ ảnh'),
                ),
              ),
      ),
      Material(
        color: Colors.transparent,
        child: ExpansionTile(
          tilePadding: EdgeInsets.zero,
          title: const Text('Nhập URL ảnh (tùy chọn)'),
          children: [
            TextFormField(
              controller: widget.controller,
              enabled: !_busy && widget.enabled,
              keyboardType: TextInputType.url,
              maxLength: 500,
              decoration: const InputDecoration(
                labelText: 'Đường dẫn ảnh (URL)',
                hintText: 'https://...',
                border: OutlineInputBorder(),
              ),
            ),
          ],
        ),
      ),
    ],
  );
}
