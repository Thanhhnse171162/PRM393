import '../../../../core/network/network_result.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../../../shared/models/user_model.dart';
import '../services/auth_service.dart';

/// Combines the auth API with secure token storage.
class AuthRepository {
  AuthRepository({
    required this.service,
    required this.storage,
  });

  final AuthService service;
  final SecureStorageService storage;

  /// Logs in and persists the JWT securely on success.
  Future<NetworkResult<UserModel>> login(
    String emailOrPhone,
    String password,
  ) async {
    final result = await service.login(emailOrPhone, password);
    switch (result) {
      case NetworkSuccess(:final data):
        await storage.saveAccessToken(data.accessToken);
        return NetworkSuccess(data.user);
      case NetworkFailure(:final error):
        return NetworkFailure(error);
    }
  }

  /// Restores a session from the stored token.
  /// Returns null when there is no token or it is no longer valid
  /// (an invalid token is deleted). A network failure keeps the token.
  Future<UserModel?> restoreSession() async {
    final token = await storage.readAccessToken();
    if (token == null || token.isEmpty) return null;

    final result = await service.me();
    switch (result) {
      case NetworkSuccess(:final data):
        return data;
      case NetworkFailure(:final error):
        if (error.isUnauthorized || error.isForbidden || error.isNotFound) {
          await storage.clear();
        }
        return null;
    }
  }

  Future<void> logout() => storage.clear();
}
