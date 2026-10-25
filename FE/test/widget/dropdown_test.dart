import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('test dropdown behavior when timeZone changes', (tester) async {
    String timeZoneId = 'Asia/Ho_Chi_Minh';
    late StateSetter outerSetState;

    await tester.pumpWidget(
      MaterialApp(
        home: StatefulBuilder(
          builder: (context, setState) {
            outerSetState = setState;
            return Scaffold(
              body: DropdownButtonFormField<String>(
                initialValue: timeZoneId,
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
                  if (timeZoneId != 'Asia/Ho_Chi_Minh' &&
                      timeZoneId != 'SE Asia Standard Time' &&
                      timeZoneId != 'UTC')
                    DropdownMenuItem(value: timeZoneId, child: Text(timeZoneId)),
                ],
                onChanged: (v) {
                  if (v != null) setState(() => timeZoneId = v);
                },
              ),
            );
          },
        ),
      ),
    );

    // Now simulate _fetchInitial updating timeZoneId to 'Other/Timezone'
    outerSetState(() {
      timeZoneId = 'Other/Timezone';
    });
    await tester.pump();
    final error1 = tester.takeException();
    print('Error on change to Other/Timezone: $error1');

    // What if timeZoneId is updated to 'SE Asia Standard Time'?
    outerSetState(() {
      timeZoneId = 'SE Asia Standard Time';
    });
    await tester.pump();
    final error2 = tester.takeException();
    print('Error on change to SE Asia Standard Time: $error2');
  });
}
