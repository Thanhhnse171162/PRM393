import 'package:courtgo_mobile/core/theme/app_theme.dart';
import 'package:courtgo_mobile/shared/models/customer_models.dart';
import 'package:courtgo_mobile/shared/widgets/customer_widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets(
    'Center card has CourtGo branding and real price without fabricated rating or amenities',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          home: Scaffold(
            body: CenterCard(
              center: CenterInfo.fromJson({
                'id': 'c',
                'name': 'CourtGo Quận 7',
                'addressLine': 'Đường A',
                'district': 'Quận 7',
                'city': 'TP.HCM',
                'sports': ['Cầu lông'],
                'priceFrom': 120000,
              }),
              onTap: () {},
            ),
          ),
        ),
      );
      expect(find.text('COURTGO'), findsOneWidget);
      expect(find.textContaining('120.000'), findsOneWidget);
      expect(find.textContaining('4.8'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets('Primary action blocks duplicate interaction while busy', (
    tester,
  ) async {
    var calls = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PrimaryButton(
            label: 'Gửi yêu cầu',
            loading: true,
            onPressed: () => calls++,
          ),
        ),
      ),
    );
    await tester.tap(find.byType(FilledButton));
    expect(calls, 0);
  });
}
