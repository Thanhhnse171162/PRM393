import 'dart:async';

import 'package:flutter/material.dart';

import 'customer_widgets.dart';

/// Observes edits and asynchronously loaded form values without owning the form.
class ImageUrlPreview extends StatefulWidget {
  const ImageUrlPreview({super.key, required this.controller});
  final TextEditingController controller;

  @override
  State<ImageUrlPreview> createState() => _ImageUrlPreviewState();
}

class _ImageUrlPreviewState extends State<ImageUrlPreview> {
  Timer? _debounce;
  late String _url;

  @override
  void initState() {
    super.initState();
    _url = widget.controller.text.trim();
    widget.controller.addListener(_changed);
  }

  @override
  void didUpdateWidget(ImageUrlPreview oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.controller != widget.controller) {
      oldWidget.controller.removeListener(_changed);
      _debounce?.cancel();
      _url = widget.controller.text.trim();
      widget.controller.addListener(_changed);
    }
  }

  void _changed() {
    _debounce?.cancel();
    final next = widget.controller.text.trim();
    if (next == _url) return;
    _debounce = Timer(const Duration(milliseconds: 400), () {
      if (mounted) setState(() => _url = next);
    });
  }

  @override
  void dispose() {
    _debounce?.cancel();
    widget.controller.removeListener(_changed);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      SportImage(url: _url, height: 160, borderRadius: 12, showMessages: true);
}
