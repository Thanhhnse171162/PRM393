import 'package:shared_preferences/shared_preferences.dart';

import '../constants/storage_keys.dart';

/// Non-sensitive preferences (selected search area, etc.).
class LocalStorageService {
  LocalStorageService(this._prefs);

  final SharedPreferences _prefs;

  static Future<LocalStorageService> create() async =>
      LocalStorageService(await SharedPreferences.getInstance());

  String? get selectedArea => _prefs.getString(StorageKeys.selectedArea);

  Future<void> saveSelectedArea(String area) =>
      _prefs.setString(StorageKeys.selectedArea, area);
}
