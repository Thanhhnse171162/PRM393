import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:courtgo_mobile/core/storage/secure_storage_service.dart';

class MemoryTokens extends SecureStorageService {
  String? access, refresh;
  @override
  Future<String?> readAccessToken() async => access;
  @override
  Future<String?> readRefreshToken() async => refresh;
  @override
  Future<void> saveAccessToken(String value) async {
    access = value;
  }

  @override
  Future<void> saveRefreshToken(String value) async {
    refresh = value;
  }

  @override
  Future<void> clear() async {
    access = null;
    refresh = null;
  }
}

class TestAdapter implements HttpClientAdapter {
  TestAdapter(this.respond);
  final Future<(int, Object?)> Function(RequestOptions) respond;
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    final result = await respond(options);
    return ResponseBody.fromString(
      jsonEncode(result.$2),
      result.$1,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
