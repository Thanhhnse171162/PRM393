import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/routing/route_names.dart';
import '../../../../core/storage/local_storage_service.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/theme/app_text_styles.dart';

/// Placeholder area picker. Real data (districts/cities) comes later.
class LocationPage extends StatelessWidget {
  const LocationPage({super.key});

  static const List<String> _areas = [
    'Quận 7',
    'Thủ Đức',
    'Bình Thạnh',
  ];

  Future<void> _select(BuildContext context, String area) async {
    final storage = context.read<LocalStorageService>();
    await storage.saveSelectedArea(area);
    if (!context.mounted) return;
    context.go(RouteNames.customerHome);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chọn khu vực')),
      body: SafeArea(
        child: ListView(
          padding: AppSpacing.page,
          children: [
            Text('Bạn muốn tìm sân ở đâu?', style: AppTextStyles.h2),
            const SizedBox(height: AppSpacing.md),
            for (final area in _areas) ...[
              Card(
                child: ListTile(
                  minTileHeight: 56,
                  leading: const Icon(Icons.location_on_outlined),
                  title: Text(area),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _select(context, area),
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ],
        ),
      ),
    );
  }
}
