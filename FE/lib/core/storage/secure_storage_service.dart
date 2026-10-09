import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../constants/storage_keys.dart';

/// Stores sensitive values (auth tokens) in the platform keystore/keychain.
class SecureStorageService {
  SecureStorageService([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  Future<String?> readAccessToken() =>
      _storage.read(key: StorageKeys.accessToken);

  Future<void> saveAccessToken(String token) =>
      _storage.write(key: StorageKeys.accessToken, value: token);

  Future<String?> readRefreshToken() =>
      _storage.read(key: StorageKeys.refreshToken);
  Future<void> saveRefreshToken(String token) =>
      _storage.write(key: StorageKeys.refreshToken, value: token);
  Future<void> saveTokens(String access, String refresh) async {
    await saveRefreshToken(refresh);
    await saveAccessToken(access);
  }

  Future<void> clear() => _storage.deleteAll();
}
