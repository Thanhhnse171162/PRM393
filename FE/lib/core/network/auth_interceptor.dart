import 'package:dio/dio.dart';

import '../storage/secure_storage_service.dart';

/// One refresh for concurrent 401s. Tokens and QR data are never logged.
class AuthInterceptor extends Interceptor {
  AuthInterceptor(this._storage, {required Dio dio, this.onUnauthorized})
    : _dio = dio,
      _refreshDio = (Dio(dio.options.copyWith())
        ..httpClientAdapter = dio.httpClientAdapter);
  final SecureStorageService _storage;
  final Dio _dio;
  final Dio _refreshDio;
  final void Function()? onUnauthorized;
  Future<String?>? _refreshing;

  bool _publicAuth(String path) =>
      path.endsWith('/login') ||
      path.endsWith('/register') ||
      path.endsWith('/refresh');
  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (!_publicAuth(options.path)) {
      final token = await _storage.readAccessToken();
      if (token != null && token.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    handler.next(options);
  }

  Future<String?> _rotate() async {
    final old = await _storage.readRefreshToken();
    if (old == null || old.isEmpty) return null;
    try {
      final response = await _refreshDio.post<Map<String, dynamic>>(
        '/api/auth/refresh',
        data: {'refreshToken': old},
      );
      final data = response.data!;
      // A logout while the request was in flight must not restore the session.
      if (await _storage.readRefreshToken() != old) return null;
      await _storage.saveTokens(
        data['accessToken'] as String,
        data['refreshToken'] as String,
      );
      return data['accessToken'] as String;
    } on DioException {
      return null;
    }
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final request = err.requestOptions;
    if (err.response?.statusCode != 401 ||
        _publicAuth(request.path) ||
        request.path.endsWith('/logout') ||
        !request.headers.containsKey('Authorization')) {
      handler.next(err);
      return;
    }
    if (request.extra['authRetried'] == true) {
      onUnauthorized?.call();
      handler.next(err);
      return;
    }
    try {
      var token = await _storage.readAccessToken();
      if (token == null ||
          'Bearer $token' == request.headers['Authorization']) {
        final flight = _refreshing ??= _rotate();
        try {
          token = await flight;
        } finally {
          if (identical(_refreshing, flight)) _refreshing = null;
        }
      }
      if (token == null) {
        onUnauthorized?.call();
        handler.next(err);
        return;
      }
      request.headers['Authorization'] = 'Bearer $token';
      request.extra['authRetried'] = true;
      // Multipart streams have already been consumed by the first request.
      if (request.data is FormData) {
        request.data = (request.data as FormData).clone();
      }
      handler.resolve(await _dio.fetch<dynamic>(request));
    } on DioException catch (e) {
      handler.next(e);
    } catch (_) {
      onUnauthorized?.call();
      handler.next(err);
    }
  }
}
