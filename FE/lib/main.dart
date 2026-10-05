import 'package:flutter/material.dart';

import 'app.dart';
import 'core/storage/local_storage_service.dart';
import 'core/storage/secure_storage_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final localStorage = await LocalStorageService.create();
  runApp(
    CourtGoApp(
      secureStorage: SecureStorageService(),
      localStorage: localStorage,
    ),
  );
}
