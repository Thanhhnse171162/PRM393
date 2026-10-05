import 'package:courtgo_mobile/core/widgets/app_button.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Widget _wrap(Widget child) => MaterialApp(home: Scaffold(body: child));

void main() {
  testWidgets('AppButton fires onPressed when enabled', (tester) async {
    var taps = 0;
    await tester.pumpWidget(
      _wrap(AppButton(label: 'Đặt sân', onPressed: () => taps++)),
    );
    await tester.tap(find.text('Đặt sân'));
    expect(taps, 1);
  });

  testWidgets('AppButton is disabled while loading', (tester) async {
    var taps = 0;
    await tester.pumpWidget(
      _wrap(AppButton(label: 'Đặt sân', isLoading: true, onPressed: () => taps++)),
    );
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    await tester.tap(find.byType(ElevatedButton), warnIfMissed: false);
    expect(taps, 0);
  });

  testWidgets('AppButton is disabled when onPressed is null', (tester) async {
    await tester.pumpWidget(_wrap(const AppButton(label: 'Tiếp tục', onPressed: null)));
    final button = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
    expect(button.onPressed, isNull);
  });
}
