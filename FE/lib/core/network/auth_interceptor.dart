import 'package:dio/dio.dart';

import '../storage/secure_storage_service.dart';

/// Attaches the JWT access token (if any) to outgoing requests and reports
/// 401 responses of authenticated requests via [onUnauthorized].
class AuthInterceptor extends Interceptor {
  AuthInterceptor(this._storage, {this.onUnauthorized});

  final SecureStorageService _storage;
  final void Function()? onUnauthorized;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final token = await _storage.readAccessToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final isLogin = err.requestOptions.path.endsWith('/login');
    final hadToken = err.requestOptions.headers.containsKey('Authorization');
    if (err.response?.statusCode == 401 && hadToken && !isLogin) {
      onUnauthorized?.call();
    }
    handler.next(err);
  }
}
