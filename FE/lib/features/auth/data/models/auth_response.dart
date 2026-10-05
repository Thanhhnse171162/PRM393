import '../../../../shared/models/user_model.dart';

/// Response of POST /api/auth/login.
class AuthResponse {
  const AuthResponse({required this.accessToken, required this.user});

  final String accessToken;
  final UserModel user;

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      accessToken: json['accessToken'] as String,
      user: UserModel.fromJson(json['user'] as Map<String, dynamic>),
    );
  }
}
