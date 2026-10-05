import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../constants/app_constants.dart';
import '../storage/secure_storage_service.dart';
import 'api_exception.dart';
import 'auth_interceptor.dart';
import 'network_result.dart';

/// Thin wrapper around Dio. Feature repositories depend on this class.
class ApiClient {
  ApiClient({required SecureStorageService storage, Dio? dio})
      : _dio = dio ??
            Dio(
              BaseOptions(
                baseUrl: AppConfig.apiBaseUrl,
                connectTimeout: AppConstants.networkTimeout,
                receiveTimeout: AppConstants.networkTimeout,
                headers: {'Accept': 'application/json'},
              ),
            ) {
    _dio.interceptors.add(
      AuthInterceptor(
        storage,
        onUnauthorized: () => onUnauthorized?.call(),
      ),
    );
  }

  final Dio _dio;

  /// Set by the app to react to expired/invalid tokens (e.g. force logout).
  void Function()? onUnauthorized;

  Future<NetworkResult<T>> get<T>(
    String path, {
    Map<String, dynamic>? query,
  }) =>
      _send(() => _dio.get<dynamic>(path, queryParameters: query));

  Future<NetworkResult<T>> post<T>(String path, {Object? body}) =>
      _send(() => _dio.post<dynamic>(path, data: body));

  Future<NetworkResult<T>> put<T>(String path, {Object? body}) =>
      _send(() => _dio.put<dynamic>(path, data: body));

  Future<NetworkResult<T>> delete<T>(String path) =>
      _send(() => _dio.delete<dynamic>(path));

  Future<NetworkResult<T>> _send<T>(
    Future<Response<dynamic>> Function() request,
  ) async {
    try {
      final response = await request();
      return NetworkSuccess<T>(response.data as T);
    } on DioException catch (e) {
      return NetworkFailure<T>(ApiException.fromDio(e));
    }
  }
}
