import 'package:dio/dio.dart';

class ApiException implements Exception {
  const ApiException({
    required this.message,
    this.statusCode,
    this.details,
    this.code,
    this.fieldErrors,
  });

  final String message;
  final int? statusCode;
  final Object? details;
  final String? code;
  final Map<String, List<String>>? fieldErrors;

  bool get isUnauthorized => statusCode == 401;
  bool get isForbidden => statusCode == 403;
  bool get isNotFound => statusCode == 404;
  bool get isConflict => statusCode == 409;

  /// Helper to get the first error for a specific field, case-insensitive.
  String? getFieldError(String fieldName) {
    if (fieldErrors == null) return null;
    final lower = fieldName.toLowerCase();
    for (final entry in fieldErrors!.entries) {
      if (entry.key.toLowerCase() == lower && entry.value.isNotEmpty) {
        return entry.value.first;
      }
    }
    return null;
  }

  factory ApiException.fromDio(DioException e) {
    final status = e.response?.statusCode;
    final data = e.response?.data;

    // Check timeout / network connection errors first
    if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.sendTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      return const ApiException(
        message: 'Hết thời gian chờ phản hồi. Vui lòng kiểm tra kết nối mạng và thử lại.',
        code: 'NETWORK_TIMEOUT',
      );
    }
    if (e.type == DioExceptionType.connectionError || status == null) {
      return const ApiException(
        message: 'Không thể kết nối máy chủ. Vui lòng kiểm tra kết nối mạng và thử lại.',
        code: 'NETWORK_ERROR',
      );
    }

    final code =
        status == 409 &&
            data is Map &&
            data['detail'] == 'Booking has already been reviewed.'
        ? 'REVIEW_ALREADY_SUBMITTED'
        : data is Map
        ? data['code'] as String?
        : null;

    final parsedFieldErrors = <String, List<String>>{};
    if (data is Map) {
      final rawErrors = data['errors'] ?? data['validationErrors'] ?? data['fieldErrors'];
      if (rawErrors is Map) {
        rawErrors.forEach((key, value) {
          if (value is List) {
            parsedFieldErrors[key.toString()] =
                value.map((item) => _translateErrorMessage(item.toString())).toList();
          } else if (value != null) {
            parsedFieldErrors[key.toString()] = [
              _translateErrorMessage(value.toString())
            ];
          }
        });
      }
    }

    // Map known duplicate codes to field errors if not already mapped
    if (code == 'PHONE_ALREADY_EXISTS') {
      parsedFieldErrors['phoneNumber'] = ['Số điện thoại này đã được đăng ký.'];
    } else if (code == 'EMAIL_ALREADY_EXISTS') {
      parsedFieldErrors['email'] = ['Email này đã được đăng ký.'];
    }

    final message = switch (code) {
      'BOOKING_SLOT_CONFLICT' =>
        'Khung giờ vừa được người khác chọn. Vui lòng chọn lại.',
      'QR_NOT_AVAILABLE' => 'Mã QR chưa khả dụng cho trạng thái lịch đặt này.',
      'QR_INVALID' => 'Mã QR không hợp lệ hoặc không còn sử dụng được.',
      'STAFF_CENTER_ACCESS_DENIED' => 'Lượt đặt thuộc trung tâm khác.',
      'STAFF_NOT_ASSIGNED' => 'Tài khoản chưa được phân công trung tâm. Vui lòng liên hệ quản trị viên.',
      'BOOKING_NOT_CHECKIN_ELIGIBLE' =>
        'Lượt đặt đã hết hiệu lực hoặc không ở trạng thái có thể check-in.',
      'CHECKIN_ALREADY_COMPLETED' =>
        'Lượt đặt đã được check-in. Vui lòng tải lại chi tiết.',
      'PAYMENT_ALREADY_FULLY_PAID' =>
        'Lượt đặt đã thanh toán đủ. Vui lòng tải lại.',
      'OUTSTANDING_PAYMENT_REQUIRED' =>
        'Cần thu đủ tiền còn lại trước khi check-in.',
      'PAYMENT_STATE_CONFLICT' => 'Giao dịch đang chờ xử lý hoặc cần đối soát. Hãy kiểm tra trạng thái trước khi thanh toán tiếp.',
      'REVIEW_ALREADY_SUBMITTED' => 'Bạn đã gửi đánh giá cho lịch đặt này.',
      'BOOKING_HOLD_EXPIRED' || 'PAYMENT_AFTER_HOLD_EXPIRED' =>
        'Thời gian giữ sân đã hết. Vui lòng chọn lại giờ.',
      'PHONE_ALREADY_EXISTS' => 'Số điện thoại này đã được đăng ký.',
      'EMAIL_ALREADY_EXISTS' => 'Email này đã được đăng ký.',
      'COURT_NOT_BOOKABLE' =>
        'Sân hiện chưa thể đặt. Vui lòng chọn sân hoặc ngày khác.',
      'INVALID_CREDENTIALS' => 'Email/số điện thoại hoặc mật khẩu không đúng.',
      'PAYMENT_GATEWAY_NOT_CONFIGURED' =>
        'Thanh toán hiện chưa sẵn sàng. Vui lòng thử lại sau.',
      _ => switch (status) {
        400 || 422 => _resolveValidationMessage(data, parsedFieldErrors),
        401 => 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
        403 => 'Bạn không có quyền thực hiện thao tác này.',
        404 => 'Không tìm thấy nội dung. Nội dung có thể không còn khả dụng.',
        409 => 'Dữ liệu bị trùng lặp hoặc thao tác xung đột. Vui lòng thử lại.',
        429 => 'Bạn thao tác quá nhanh. Vui lòng thử lại sau.',
        500 => 'Đã có lỗi xảy ra trên hệ thống. Vui lòng thử lại sau.',
        _ => 'Không thể xử lý lúc này. Vui lòng thử lại sau.',
      },
    };

    return ApiException(
      message: message,
      statusCode: status,
      code: code,
      details: data,
      fieldErrors: parsedFieldErrors.isEmpty ? null : parsedFieldErrors,
    );
  }

  static String _translateErrorMessage(String raw) {
    final trimmed = raw.trim();
    return switch (trimmed) {
      'Full name is required.' => 'Vui lòng nhập họ và tên.',
      'Full name must be at most 150 characters.' =>
        'Họ và tên tối đa 150 ký tự.',
      'Phone number is required.' => 'Vui lòng nhập số điện thoại.',
      'Phone number is invalid.' => 'Số điện thoại không hợp lệ.',
      'Phone number is already registered.' =>
        'Số điện thoại này đã được đăng ký.',
      'Email is invalid.' => 'Email không đúng định dạng.',
      'Email is already registered.' => 'Email này đã được đăng ký.',
      'Password is required.' => 'Vui lòng nhập mật khẩu.',
      'Password must be 8-128 characters and contain uppercase, lowercase, and digits.' =>
        'Mật khẩu từ 8-128 ký tự, phải gồm chữ hoa, chữ thường và chữ số.',
      'Sport center is required.' => 'Vui lòng chọn cơ sở thể thao.',
      'Sport center not found or is inactive.' =>
        'Cơ sở thể thao không tồn tại hoặc đã ngừng hoạt động.',
      _ => _sanitizeMessage(trimmed),
    };
  }

  static String _sanitizeMessage(String msg) {
    final lower = msg.toLowerCase();
    if (lower.contains('exception') ||
        lower.contains('stack trace') ||
        lower.contains('npgsql') ||
        lower.contains('sql') ||
        lower.contains('entityframework') ||
        lower.contains('syntax error') ||
        lower.contains('internal server error')) {
      return 'Đã có lỗi xảy ra trên hệ thống. Vui lòng thử lại sau.';
    }
    return msg;
  }

  static String _resolveValidationMessage(
    dynamic data,
    Map<String, List<String>> parsedFieldErrors,
  ) {
    if (parsedFieldErrors.isNotEmpty) {
      final firstError = parsedFieldErrors.values.first.firstOrNull;
      if (firstError != null && firstError.isNotEmpty) {
        return firstError;
      }
    }
    if (data is Map && data['detail'] is String) {
      final detail = data['detail'] as String;
      if (detail.isNotEmpty) {
        return _translateErrorMessage(detail);
      }
    }
    return 'Thông tin chưa hợp lệ. Vui lòng kiểm tra và thử lại.';
  }

  @override
  String toString() => message;
}

