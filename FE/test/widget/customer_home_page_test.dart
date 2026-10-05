import 'package:courtgo_mobile/core/network/api_exception.dart';
import 'package:courtgo_mobile/core/network/network_result.dart';
import 'package:courtgo_mobile/core/storage/secure_storage_service.dart';
import 'package:courtgo_mobile/features/auth/data/models/auth_response.dart';
import 'package:courtgo_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:courtgo_mobile/features/auth/data/services/auth_service.dart';
import 'package:courtgo_mobile/features/auth/presentation/providers/auth_provider.dart';
import 'package:courtgo_mobile/features/customer/home/data/services/customer_home_service.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/pages/customer_home_page.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/providers/customer_home_provider.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/widgets/home_header.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/widgets/home_search_bar.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/widgets/promo_banner_card.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/widgets/quick_booking_card.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/widgets/sport_categories_section.dart';
import 'package:courtgo_mobile/shared/models/user_model.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

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

Widget _wrapWithProviders({
  required Widget child,
  required AuthProvider auth,
  required CustomerHomeProvider home,
}) {
  return MaterialApp(
    home: Scaffold(
      body: MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthProvider>.value(value: auth),
          ChangeNotifierProvider<CustomerHomeProvider>.value(value: home),
        ],
        child: child,
      ),
    ),
  );
}

void main() {
  late AuthProvider authProvider;
  late CustomerHomeService homeService;
  late CustomerHomeProvider homeProvider;

  setUp(() {
    final storage = FakeSecureStorage();
    final authRepo = AuthRepository(service: FakeAuthService(), storage: storage);
    authProvider = AuthProvider(authRepo);
    homeService = CustomerHomeService();
    homeProvider = CustomerHomeProvider(service: homeService);
  });

  group('HomeHeader widget tests', () {
    testWidgets('displays brand name, location and guest greeting', (tester) async {
      await tester.pumpWidget(
        _wrapWithProviders(
          child: const HomeHeader(),
          auth: authProvider,
          home: homeProvider,
        ),
      );

      expect(find.text('COURTGO'), findsOneWidget);
      expect(find.textContaining('Xin chào, Khách'), findsOneWidget);
      expect(find.text('Quận 7, TP. Hồ Chí Minh'), findsOneWidget);
    });

    testWidgets('displays user name when authenticated', (tester) async {
      final fakeService = FakeAuthService();
      fakeService.nextLoginResult = const NetworkSuccess(
        AuthResponse(
          accessToken: 'mock-token',
          user: UserModel(
            id: 'u1',
            fullName: 'Nguyen Minh',
            email: 'minh@courtgo.local',
            role: UserRole.customer,
          ),
        ),
      );

      final storage = FakeSecureStorage();
      final authRepo = AuthRepository(service: fakeService, storage: storage);
      final loggedInAuth = AuthProvider(authRepo);
      await loggedInAuth.login('minh@courtgo.local', 'password');

      await tester.pumpWidget(
        _wrapWithProviders(
          child: const HomeHeader(),
          auth: loggedInAuth,
          home: homeProvider,
        ),
      );

      expect(find.text('COURTGO'), findsOneWidget);
      expect(find.textContaining('Xin chào, Minh'), findsOneWidget);
    });
  });

  group('HomeSearchBar widget tests', () {
    testWidgets('allows typing search query and updating provider', (tester) async {
      await tester.pumpWidget(
        _wrapWithProviders(
          child: const HomeSearchBar(),
          auth: authProvider,
          home: homeProvider,
        ),
      );

      expect(find.text('Tìm sân hoặc trung tâm thể thao'), findsOneWidget);
      await tester.enterText(find.byType(TextField), 'Sân Cầu Lông');
      expect(homeProvider.searchQuery, 'Sân Cầu Lông');
    });
  });

  group('QuickBookingCard widget tests', () {
    testWidgets('renders all 3 selectors and find courts button', (tester) async {
      await homeProvider.loadHomeData();

      await tester.pumpWidget(
        _wrapWithProviders(
          child: const QuickBookingCard(),
          auth: authProvider,
          home: homeProvider,
        ),
      );

      expect(find.text('Đặt sân nhanh'), findsOneWidget);
      expect(find.text('Trực tuyến'), findsOneWidget);
      expect(find.text('MÔN'), findsOneWidget);
      expect(find.text('NGÀY'), findsOneWidget);
      expect(find.text('GIỜ'), findsOneWidget);
      expect(find.text('Tìm sân trống'), findsOneWidget);
    });
  });

  group('SportCategoriesSection widget tests', () {
    testWidgets('displays sport categories and handles selection', (tester) async {
      await homeProvider.loadHomeData();

      await tester.pumpWidget(
        _wrapWithProviders(
          child: const SportCategoriesSection(),
          auth: authProvider,
          home: homeProvider,
        ),
      );

      expect(find.text('Môn thể thao'), findsOneWidget);
      expect(find.text('Cầu lông'), findsOneWidget);
      expect(find.text('Pickleball'), findsOneWidget);

      await tester.tap(find.text('Pickleball'));
      expect(homeProvider.selectedSport, 'Pickleball');
    });
  });

  group('PromoBannerCard widget tests', () {
    testWidgets('renders promo title and subtitle', (tester) async {
      await tester.pumpWidget(
        _wrapWithProviders(
          child: const PromoBannerCard(),
          auth: authProvider,
          home: homeProvider,
        ),
      );

      expect(find.text('Ưu đãi giờ vàng: Giảm 20%'), findsOneWidget);
      expect(find.text('Khung giờ sáng các ngày trong tuần'), findsOneWidget);
      expect(find.text('Chi tiết'), findsOneWidget);
    });
  });

  group('CustomerHomePage states tests', () {
    testWidgets('shows empty state when data is empty', (tester) async {
      final emptyProvider = CustomerHomeProvider(service: homeService);

      await tester.pumpWidget(
        _wrapWithProviders(
          child: const CustomerHomePage(),
          auth: authProvider,
          home: emptyProvider,
        ),
      );

      expect(find.text('Chưa có dữ liệu sân thể thao'), findsOneWidget);
      expect(find.text('Làm mới'), findsOneWidget);
    });
  });
}
