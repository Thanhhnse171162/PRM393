import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../shared/widgets/customer_widgets.dart';
import '../../../home/discovery_provider.dart';

class LocationPage extends StatefulWidget {
  const LocationPage({super.key});
  @override
  State<LocationPage> createState() => _LocationPageState();
}

class _LocationPageState extends State<LocationPage> {
  String search = '';
  bool locating = false;
  String? error;
  Future<void> select(String area) async {
    await context.read<DiscoveryProvider>().selectArea(area);
    if (mounted) {
      if (context.canPop()) {
        context.pop();
      } else {
        context.go('/customer/explore');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final d = context.watch<DiscoveryProvider>();
    final areas =
        {
              ...d.centers.map((c) => c.district),
              ...d.storage.recentAreas,
              'Quận 7',
              'Thủ Đức',
              'Bình Thạnh',
              'Quận 1',
            }
            .where(
              (a) =>
                  a.isNotEmpty &&
                  a.toLowerCase().contains(search.toLowerCase()),
            )
            .toList();
    return Scaffold(
      appBar: CourtGoAppBar(title: 'Tìm sân ở đâu?'),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const Text(
            'Chọn khu vực tìm sân',
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          const Text(
            'Khám phá các chi nhánh CourtGo phù hợp với bạn.',
            style: TextStyle(color: AppColors.textSecondary),
          ),
          const SizedBox(height: 20),
          SearchField(
            hint: 'Nhập quận hoặc khu vực',
            onChanged: (v) => setState(() => search = v),
            onSubmitted: select,
          ),
          const SizedBox(height: 16),
          PrimaryButton(
            label: 'Sử dụng vị trí hiện tại',
            icon: Icons.my_location,
            loading: locating,
            onPressed: () async {
              setState(() {
                locating = true;
                error = null;
              });
              try {
                await d.locate();
                if (context.mounted) context.pop();
              } on ApiException catch (e) {
                if (mounted) setState(() => error = e.message);
              } catch (_) {
                if (mounted) {
                  setState(
                    () => error =
                        'Chưa lấy được vị trí. Hãy chọn khu vực bên dưới.',
                  );
                }
              } finally {
                if (mounted) setState(() => locating = false);
              }
            },
          ),
          if (error != null)
            Padding(
              padding: const EdgeInsets.only(top: 12),
              child: Text(
                error!,
                style: const TextStyle(color: AppColors.error),
              ),
            ),
          const SizedBox(height: 24),
          const SectionHeader('Khu vực tìm kiếm'),
          const SizedBox(height: 8),
          ListTile(
            leading: const Icon(Icons.public),
            title: const Text('Tất cả khu vực'),
            onTap: () => select(''),
          ),
          for (final area in areas)
            ListTile(
              leading: Icon(
                d.storage.recentAreas.contains(area)
                    ? Icons.history
                    : Icons.location_on_outlined,
                color: AppColors.primary,
              ),
              title: Text(area),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => select(area),
            ),
          if (search.trim().isNotEmpty && !areas.contains(search.trim()))
            ListTile(
              title: Text('Tìm tại "${search.trim()}"'),
              onTap: () => select(search.trim()),
            ),
        ],
      ),
    );
  }
}
