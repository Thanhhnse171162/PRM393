import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/api_client.dart';
import '../../../../core/network/network_result.dart';
import '../../../../shared/models/user_model.dart';
import '../models/auth_response.dart';

/// Talks to /api/auth. No storage or state here (see AuthRepository).
class AuthService {
  AuthService(this._api);

  final ApiClient _api;

  Future<NetworkResult<AuthResponse>> login(
    String emailOrPhone,
    String password,
  ) async {
    final result = await _api.post<Map<String, dynamic>>(
      '${ApiConstants.auth}/login',
      body: {'emailOrPhone': emailOrPhone, 'password': password},
    );
    return result.when(
      success: (data) => NetworkSuccess(AuthResponse.fromJson(data)),
      failure: NetworkFailure.new,
    );
  }

  Future<NetworkResult<UserModel>> me() async {
    final result =
        await _api.get<Map<String, dynamic>>('${ApiConstants.auth}/me');
    return result.when(
      success: (data) => NetworkSuccess(UserModel.fromJson(data)),
      failure: NetworkFailure.new,
    );
  }
}
