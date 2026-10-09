import 'package:dio/dio.dart';

class ApiException implements Exception {
  const ApiException({
    required this.message,
    this.statusCode,
    this.details,
    this.code,
  });
  final String message;
  final int? statusCode;
  final Object? details;
  final String? code;
  bool get isUnauthorized => statusCode == 401;
  bool get isForbidden => statusCode == 403;
  bool get isNotFound => statusCode == 404;
  bool get isConflict => statusCode == 409;

  factory ApiException.fromDio(DioException e) {
    final status = e.response?.statusCode;
    final data = e.response?.data;
    final code =
        status == 409 &&
            data is Map &&
            data['detail'] == 'Booking has already been reviewed.'
        ? 'REVIEW_ALREADY_SUBMITTED'
        : data is Map
        ? data['code'] as String?
        : null;
    final message = switch (code) {
      'BOOKING_SLOT_CONFLICT' =>
        'Khung giờ vừa được người khác chọn. Vui lòng chọn lại.',
      'QR_NOT_AVAILABLE' => 'Mã QR chưa khả dụng cho trạng thái lịch đặt này.',
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
        null => 'Không thể kết nối máy chủ. Kiểm tra mạng và thử lại.',
        400 => 'Thông tin chưa hợp lệ. Vui lòng kiểm tra và thử lại.',
        401 => 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
        403 => 'Bạn không có quyền thực hiện thao tác này.',
        404 => 'Không tìm thấy nội dung. Nội dung có thể không còn khả dụng.',
        409 => 'Trạng thái đã thay đổi hoặc thao tác đã được thực hiện. Vui lòng tải lại.',
        429 => 'Bạn thao tác quá nhanh. Vui lòng thử lại sau.',
        _ => 'Không thể xử lý lúc này. Vui lòng thử lại sau.',
      },
    };
    return ApiException(message: message, statusCode: status, code: code);
  }
  @override
  String toString() => message;
}
