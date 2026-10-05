import 'package:dio/dio.dart';

/// App-level error mapped from an HTTP/Dio failure.
class ApiException implements Exception {
  const ApiException({required this.message, this.statusCode, this.details});

  final String message;
  final int? statusCode;
  final Object? details;

  bool get isUnauthorized => statusCode == 401;
  bool get isForbidden => statusCode == 403;
  bool get isNotFound => statusCode == 404;

  /// Used for slot conflicts (someone else booked the slot first).
  bool get isConflict => statusCode == 409;

  factory ApiException.fromDio(DioException e) {
    final status = e.response?.statusCode;
    final data = e.response?.data;
    String? serverMessage;
    if (data is Map<String, dynamic>) {
      serverMessage = (data['detail'] ?? data['title'] ?? data['message'])
          as String?;
    }
    if (status == null) {
      return ApiException(
        message: 'Cannot reach server. Check your connection.',
        details: e.type,
      );
    }
    return ApiException(
      message: serverMessage ?? 'Request failed ($status)',
      statusCode: status,
      details: data,
    );
  }

  @override
  String toString() => 'ApiException($statusCode): $message';
}
