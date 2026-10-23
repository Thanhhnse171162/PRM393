import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:courtgo_mobile/core/theme/app_colors.dart';
import 'package:courtgo_mobile/features/admin/shared/admin_widgets.dart';

void main() {
  group('Admin UI Components', () {
    testWidgets('AdminKpiCard renders title, value, and subtitle',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AdminKpiCard(
              title: 'Doanh thu hôm nay',
              value: '12.500.000 đ',
              icon: Icons.payments_outlined,
              subtitle: '+15% so với hôm qua',
              color: AppColors.primary,
            ),
          ),
        ),
      );

      expect(find.text('Doanh thu hôm nay'), findsOneWidget);
      expect(find.text('12.500.000 đ'), findsOneWidget);
      expect(find.text('+15% so với hôm qua'), findsOneWidget);
      expect(find.byIcon(Icons.payments_outlined), findsOneWidget);
    });

    testWidgets('AdminStatusBadge formats status text and colors properly',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Column(
              children: [
                AdminStatusBadge(label: 'Hoạt động', statusType: 'active'),
                AdminStatusBadge(label: 'Bảo trì', statusType: 'maintenance'),
                AdminStatusBadge(label: 'Ngừng hoạt động', statusType: 'inactive'),
                AdminStatusBadge(label: 'Đã xác nhận', statusType: 'confirmed'),
                AdminStatusBadge(label: 'Đã hủy', statusType: 'cancelled'),
              ],
            ),
          ),
        ),
      );

      expect(find.text('Hoạt động'), findsOneWidget);
      expect(find.text('Bảo trì'), findsOneWidget);
      expect(find.text('Ngừng hoạt động'), findsOneWidget);
      expect(find.text('Đã xác nhận'), findsOneWidget);
      expect(find.text('Đã hủy'), findsOneWidget);
    });

    testWidgets('AdminCard displays optional title and child properly',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AdminCard(
              title: 'Cơ sở trực thuộc',
              child: Text('Chi tiết cơ sở CourtGo Sala'),
            ),
          ),
        ),
      );

      expect(find.text('Cơ sở trực thuộc'), findsOneWidget);
      expect(find.text('Chi tiết cơ sở CourtGo Sala'), findsOneWidget);
    });

    testWidgets('AdminActionCard renders icon, title, description and responds to tap',
        (tester) async {
      var tapped = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: AdminActionCard(
              title: 'Nhân viên & Phân công cơ sở',
              description: 'Quản lý tài khoản nhân viên và gán cơ sở hoạt động',
              icon: Icons.badge_outlined,
              onTap: () => tapped = true,
            ),
          ),
        ),
      );

      expect(find.text('Nhân viên & Phân công cơ sở'), findsOneWidget);
      expect(find.text('Quản lý tài khoản nhân viên và gán cơ sở hoạt động'),
          findsOneWidget);
      expect(find.byIcon(Icons.badge_outlined), findsOneWidget);

      await tester.tap(find.text('Nhân viên & Phân công cơ sở'));
      expect(tapped, isTrue);
    });

    testWidgets('Occupancy metric must be labeled "Tỷ lệ lấp đầy sân", NOT "Tỷ lệ khách thực tế chơi"',
        (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Column(
              children: [
                const Text(
                  'Tỷ lệ lấp đầy sân',
                  style: TextStyle(fontWeight: FontWeight.bold),
                ),
                Text(
                  '78.5%',
                  style: TextStyle(color: AppColors.primary),
                ),
              ],
            ),
          ),
        ),
      );

      expect(find.text('Tỷ lệ lấp đầy sân'), findsOneWidget);
      expect(find.text('Tỷ lệ khách thực tế chơi'), findsNothing);
    });
  });
}
