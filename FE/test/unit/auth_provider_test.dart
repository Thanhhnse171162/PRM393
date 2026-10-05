import 'package:courtgo_mobile/core/network/api_exception.dart';
import 'package:courtgo_mobile/core/network/network_result.dart';
import 'package:courtgo_mobile/core/routing/route_guard.dart';
import 'package:courtgo_mobile/core/routing/route_names.dart';
import 'package:courtgo_mobile/core/storage/secure_storage_service.dart';
import 'package:courtgo_mobile/features/auth/data/models/auth_response.dart';
import 'package:courtgo_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:courtgo_mobile/features/auth/data/services/auth_service.dart';
import 'package:courtgo_mobile/features/auth/presentation/providers/auth_provider.dart';
import 'package:courtgo_mobile/shared/models/user_model.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  late FakeAuthService fakeService;
  late FakeSecureStorage fakeStorage;
  late AuthRepository repository;
  late AuthProvider provider;

  setUp(() {
    fakeService = FakeAuthService();
    fakeStorage = FakeSecureStorage();
    repository = AuthRepository(service: fakeService, storage: fakeStorage);
    provider = AuthProvider(repository);
  });

  group('AuthProvider login flow', () {
    test('successful login sets authenticated status, user role, and persists token', () async {
      fakeService.nextLoginResult = const NetworkSuccess(
        AuthResponse(
          accessToken: 'jwt-token-123',
          user: UserModel(
            id: 'u1',
            fullName: 'Nguyen Van A',
            email: 'a@courtgo.local',
            role: UserRole.customer,
          ),
        ),
      );

      final success = await provider.login('a@courtgo.local', 'password');

      expect(success, isTrue);
      expect(provider.isAuthenticated, isTrue);
      expect(provider.role, UserRole.customer);
      expect(provider.errorMessage, isNull);
      expect(await fakeStorage.readAccessToken(), 'jwt-token-123');
    });

    test('failed login sets error message and leaves user unauthenticated', () async {
      fakeService.nextLoginResult = const NetworkFailure(
        ApiException(message: 'Unauthorized', statusCode: 401),
      );

      final success = await provider.login('bad@login.com', 'wrong');

      expect(success, isFalse);
      expect(provider.isAuthenticated, isFalse);
      expect(provider.errorMessage, contains('không đúng'));
      expect(await fakeStorage.readAccessToken(), isNull);
    });

    test('guest action sets guest status without credentials', () {
      provider.continueAsGuest();
      expect(provider.isGuest, isTrue);
      expect(provider.isAuthenticated, isFalse);
      expect(provider.role, isNull);
    });

    test('logout clears secure storage and resets state', () async {
      await fakeStorage.saveAccessToken('token');
      provider.continueAsGuest();

      await provider.logout();

      expect(provider.isAuthenticated, isFalse);
      expect(provider.isGuest, isFalse);
      expect(await fakeStorage.readAccessToken(), isNull);
    });
  });

  group('RouteGuard role redirects', () {
    test('Customer routes to /customer/home', () {
      expect(RouteGuard.homeFor(UserRole.customer), RouteNames.customerHome);
    });

    test('Staff routes to /staff/home', () {
      expect(RouteGuard.homeFor(UserRole.staff), RouteNames.staffHome);
    });

    test('Admin routes to /admin/home', () {
      expect(RouteGuard.homeFor(UserRole.admin), RouteNames.adminHome);
    });

    test('Guest is directed to /customer/home by default', () {
      expect(RouteGuard.homeFor(null), RouteNames.customerHome);
    });
  });
}

class FakeAuthService implements AuthService {
  NetworkResult<AuthResponse>? nextLoginResult;
  NetworkResult<UserModel>? nextMeResult;

  @override
  Future<NetworkResult<AuthResponse>> login(String emailOrPhone, String password) async {
    return nextLoginResult ??
        const NetworkFailure(ApiException(message: 'Default error', statusCode: 500));
  }

  @override
  Future<NetworkResult<UserModel>> me() async {
    return nextMeResult ??
        const NetworkFailure(ApiException(message: 'No token', statusCode: 401));
  }
}

class FakeSecureStorage extends SecureStorageService {
  String? _token;

  @override
  Future<String?> readAccessToken() async => _token;

  @override
  Future<void> saveAccessToken(String token) async {
    _token = token;
  }

  @override
  Future<void> clear() async {
    _token = null;
  }
}
