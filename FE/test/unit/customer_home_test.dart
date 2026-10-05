import 'package:courtgo_mobile/features/customer/home/data/models/banner_item_model.dart';
import 'package:courtgo_mobile/features/customer/home/data/models/nearby_center_item_model.dart';
import 'package:courtgo_mobile/features/customer/home/data/models/recommended_court_model.dart';
import 'package:courtgo_mobile/features/customer/home/data/services/customer_home_service.dart';
import 'package:courtgo_mobile/features/customer/home/presentation/providers/customer_home_provider.dart';
import 'package:courtgo_mobile/shared/models/sport_model.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('CustomerHomeService Tests', () {
    late CustomerHomeService service;

    setUp(() {
      service = CustomerHomeService();
    });

    test('getBanners returns non-empty list of banners', () async {
      final banners = await service.getBanners();
      expect(banners, isA<List<BannerItemModel>>());
      expect(banners, isNotEmpty);
      expect(banners.first.tag, contains('GIỜ VÀNG'));
    });

    test('getSports returns predefined sport categories', () async {
      final sports = await service.getSports();
      expect(sports, isA<List<SportModel>>());
      expect(sports.length, greaterThanOrEqualTo(4));
      expect(sports.any((SportModel s) => s.name == 'Cầu lông'), isTrue);
      expect(sports.any((SportModel s) => s.name == 'Pickleball'), isTrue);
    });

    test('getNearbyCenters returns center items matching UI reference', () async {
      final centers = await service.getNearbyCenters();
      expect(centers, isA<List<NearbyCenterItemModel>>());
      expect(centers, isNotEmpty);
      expect(centers.first.name, 'CourtGo Sports Arena');
      expect(centers.first.isOpen, isTrue);
    });

    test('getRecommendedCourts returns slot items', () async {
      final courts = await service.getRecommendedCourts();
      expect(courts, isA<List<RecommendedCourtModel>>());
      expect(courts.length, greaterThanOrEqualTo(3));
      expect(courts.any((c) => c.codeBadge == 'A1'), isTrue);
      expect(courts.any((c) => c.codeBadge == 'P1'), isTrue);
    });
  });

  group('CustomerHomeProvider State Tests', () {
    late CustomerHomeService service;
    late CustomerHomeProvider provider;

    setUp(() {
      service = CustomerHomeService();
      provider = CustomerHomeProvider(service: service);
    });

    test('loadHomeData populates all sections successfully', () async {
      await provider.loadHomeData();

      expect(provider.isLoading, isFalse);
      expect(provider.errorMessage, isNull);
      expect(provider.isEmpty, isFalse);
      expect(provider.banners, isNotEmpty);
      expect(provider.sports, isNotEmpty);
      expect(provider.nearbyCenters, isNotEmpty);
      expect(provider.recommendedCourts, isNotEmpty);
    });

    test('filter by search query filters nearby centers correctly', () async {
      await provider.loadHomeData();

      provider.setSearchQuery('GreenField');
      expect(provider.filteredCenters.length, 1);
      expect(provider.filteredCenters.first.name, contains('GreenField'));

      provider.setSearchQuery('Unknown NonExistent');
      expect(provider.filteredCenters, isEmpty);

      provider.setSearchQuery('');
      expect(provider.filteredCenters.length, provider.nearbyCenters.length);
    });

    test('filter by sport filters nearby centers', () async {
      await provider.loadHomeData();

      provider.setSelectedSport('Bóng đá');
      expect(provider.selectedSport, 'Bóng đá');
      expect(
        provider.filteredCenters.every((NearbyCenterItemModel c) => c.sportsTag.contains('Bóng đá')),
        isTrue,
      );
    });

    test('setQuickDate and setQuickTime update quick booking fields', () {
      provider.setQuickDate('Ngày mai');
      provider.setQuickTime('20:00');

      expect(provider.quickDate, 'Ngày mai');
      expect(provider.quickTime, '20:00');
    });

    test('setSelectedArea updates location', () {
      provider.setSelectedArea('Quận 1, TP. Hồ Chí Minh');
      expect(provider.selectedArea, 'Quận 1, TP. Hồ Chí Minh');
    });
  });
}
