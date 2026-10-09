import 'package:shared_preferences/shared_preferences.dart';

import '../constants/storage_keys.dart';

/// Non-sensitive preferences (selected search area, etc.).
class LocalStorageService {
  LocalStorageService(this._prefs);

  final SharedPreferences _prefs;

  static Future<LocalStorageService> create() async =>
      LocalStorageService(await SharedPreferences.getInstance());

  String? get selectedArea => _prefs.getString(StorageKeys.selectedArea);

  List<String> get recentAreas => _prefs.getStringList('recent_areas') ?? [];
  Future<void> saveSelectedArea(String area) async {
    await _prefs.setString(StorageKeys.selectedArea, area);
    if (area.isNotEmpty) {
      await _prefs.setStringList(
        'recent_areas',
        [area, ...recentAreas.where((a) => a != area)].take(5).toList(),
      );
    }
  }

  bool reviewed(String userId, String bookingId) =>
      _prefs.getBool('reviewed_${userId}_$bookingId') ?? false;
  Future<void> saveReviewed(String userId, String bookingId) =>
      _prefs.setBool('reviewed_${userId}_$bookingId', true);
}
