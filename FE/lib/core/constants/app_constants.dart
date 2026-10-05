class AppConstants {
  const AppConstants._();

  static const String appName = 'CourtGo';

  /// Fixed slot length in minutes (see docs/business-rules.md).
  static const int slotMinutes = 60;

  /// Minimum recommended touch target size.
  static const double minTouchTarget = 44;

  static const Duration networkTimeout = Duration(seconds: 20);
}
