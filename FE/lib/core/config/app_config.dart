import 'environment.dart';

/// Central app configuration.
///
/// Values are injected at build/run time with `--dart-define`, so no
/// machine-specific address is hardcoded. Examples:
///
/// ```
/// flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
/// flutter run --dart-define=API_BASE_URL=http://<YOUR_LAN_IP>:5080
/// ```
///
/// The default targets the Android emulator (10.0.2.2 = host machine).
class AppConfig {
  const AppConfig._();

  static const String _envName =
      String.fromEnvironment('APP_ENV', defaultValue: 'dev');

  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5080',
  );

  static Environment get environment => Environment.fromName(_envName);

  static bool get isDev => environment == Environment.dev;
}
