import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class TestWidget extends StatefulWidget {
  const TestWidget({super.key});

  @override
  State<TestWidget> createState() => _TestWidgetState();
}

class _TestWidgetState extends State<TestWidget> {
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _fetch();
  }

  void _fetch() async {
    setState(() => _loading = true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Title')),
      body: _loading ? const CircularProgressIndicator() : const Text('Content'),
    );
  }
}

void main() {
  testWidgets('setState in initState', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: TestWidget()));
    final err = tester.takeException();
    print('Exception from setState in initState: $err');
  });
}
