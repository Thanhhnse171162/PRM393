import 'package:flutter/foundation.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/network/network_result.dart';
import '../../../../shared/models/user_model.dart';
import '../../data/repositories/auth_repository.dart';

enum AuthStatus { unknown, authenticated, guest, unauthenticated }

/// Authentication state for the whole app (session, role, guest mode).
class AuthProvider extends ChangeNotifier {
  AuthProvider(this._repository);

  final AuthRepository _repository;

  AuthStatus _status = AuthStatus.unknown;
  UserModel? _user;
  bool _isLoading = false;
  String? _errorMessage;

  AuthStatus get status => _status;
  UserModel? get user => _user;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// `null` for guests and signed-out users.
  UserRole? get role => _user?.role;
  bool get isAuthenticated => _status == AuthStatus.authenticated;
  bool get isGuest => _status == AuthStatus.guest;

  /// Called by Splash: reads the stored token and restores the session.
  Future<void> restoreSession() async {
    final user = await _repository.restoreSession();
    if (user == null) {
      _user = null;
      _status = AuthStatus.unauthenticated;
    } else {
      _user = user;
      _status = AuthStatus.authenticated;
    }
    notifyListeners();
  }

  /// Returns true on success; otherwise [errorMessage] is set.
  Future<bool> login(String emailOrPhone, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    final result = await _repository.login(emailOrPhone.trim(), password);
    switch (result) {
      case NetworkSuccess(:final data):
        _user = data;
        _status = AuthStatus.authenticated;
        _isLoading = false;
        notifyListeners();
        return true;
      case NetworkFailure(:final error):
        _errorMessage = messageFor(error);
        _isLoading = false;
        notifyListeners();
        return false;
    }
  }

  /// Browse as guest. No access to bookings/payments/profile yet.
  void continueAsGuest() {
    _user = null;
    _status = AuthStatus.guest;
    _errorMessage = null;
    notifyListeners();
  }

  Future<void> logout() async {
    await _repository.logout();
    _user = null;
    _status = AuthStatus.unauthenticated;
    _errorMessage = null;
    notifyListeners();
  }

  /// Called when the API answers 401 for an authenticated session
  /// (token expired/invalid): clears the session so the router shows Login.
  Future<void> handleUnauthorized() async {
    if (_status != AuthStatus.authenticated) return;
    await logout();
  }

  void clearError() {
    if (_errorMessage == null) return;
    _errorMessage = null;
    notifyListeners();
  }

  /// Maps API errors to user-facing (Vietnamese) messages.
  @visibleForTesting
  static String messageFor(ApiException error) {
    switch (error.statusCode) {
      case 401:
        return 'Email/số điện thoại hoặc mật khẩu không đúng.';
      case 403:
        return 'Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.';
      case 400:
        return 'Thông tin đăng nhập không hợp lệ.';
      case null:
        return 'Không thể kết nối máy chủ. Vui lòng kiểm tra mạng.';
      default:
        return 'Đã có lỗi xảy ra. Vui lòng thử lại sau.';
    }
  }
}
