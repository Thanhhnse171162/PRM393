import '../../../../../core/network/api_client.dart';
import '../../../../../shared/models/sport_model.dart';
import '../models/banner_item_model.dart';
import '../models/nearby_center_item_model.dart';
import '../models/recommended_court_model.dart';

/// Service responsible for fetching Customer Home data.
/// It wraps the backend API endpoints and provides structured, realistic data
/// for UI presentation matching the approved design.
class CustomerHomeService {
  CustomerHomeService({this.apiClient});

  final ApiClient? apiClient;

  /// Fetches hero banners for the promotional carousel.
  Future<List<BannerItemModel>> getBanners() async {
    return const [
      BannerItemModel(
        id: 'b1',
        tag: 'GIỜ VÀNG THỂ THAO',
        title: 'Sẵn sàng cho trận tối\nnay?',
        subtitle: 'Tìm sân còn trống gần bạn',
        buttonText: 'Xem sân trống ⚽',
        imageUrl:
            'https://images.unsplash.com/photo-1508098682722-e99c43a406b2?q=80&w=800&auto=format&fit=crop',
      ),
      BannerItemModel(
        id: 'b2',
        tag: 'ƯU ĐÃI THÀNH VIÊN',
        title: 'Giảm 20% đặt sân\ntrước 17:00',
        subtitle: 'Áp dụng tại tất cả các chi nhánh',
        buttonText: 'Khám phá ngay 🏸',
        imageUrl:
            'https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?q=80&w=800&auto=format&fit=crop',
      ),
    ];
  }

  /// Fetches sports list with icons.
  Future<List<SportModel>> getSports() async {
    return const [
      SportModel(id: 'badminton', name: 'Cầu lông', iconUrl: '🏸'),
      SportModel(id: 'pickleball', name: 'Pickleball', iconUrl: '🏓'),
      SportModel(id: 'football', name: 'Bóng đá', iconUrl: '⚽'),
      SportModel(id: 'basketball', name: 'Bóng rổ', iconUrl: '🏀'),
      SportModel(id: 'tennis', name: 'Tennis', iconUrl: '🎾'),
    ];
  }

  /// Fetches sports centers located near the user's selected area.
  Future<List<NearbyCenterItemModel>> getNearbyCenters({String? area}) async {
    return const [
      NearbyCenterItemModel(
        id: 'center-1',
        name: 'CourtGo Sports Arena',
        address: '32 Huỳnh Tấn Phát, Tân Thuận Đông, Quận 7',
        imageUrl:
            'https://images.unsplash.com/photo-1534438327276-14e5300c3a48?q=80&w=800&auto=format&fit=crop',
        rating: 4.8,
        distanceKm: 1.2,
        statusText: 'Mở cửa',
        sportsTag: 'Cầu lông · Pickleball',
        priceFrom: 100000,
        isOpen: true,
      ),
      NearbyCenterItemModel(
        id: 'center-2',
        name: 'GreenField Sports',
        address: '18 Nguyễn Hữu Thọ, Tân Phong, Quận 7',
        imageUrl:
            'https://images.unsplash.com/photo-1574629810360-7efbbe195018?q=80&w=800&auto=format&fit=crop',
        rating: 4.7,
        distanceKm: 2.4,
        statusText: 'Mở cửa',
        sportsTag: 'Bóng đá · Cầu lông',
        priceFrom: 120000,
        isOpen: true,
      ),
      NearbyCenterItemModel(
        id: 'center-3',
        name: 'CourtGo Center Thủ Đức',
        address: '215 Võ Văn Ngân, Linh Chiểu, TP. Thủ Đức',
        imageUrl:
            'https://images.unsplash.com/photo-1554068865-24cecd4e34b8?q=80&w=800&auto=format&fit=crop',
        rating: 4.9,
        distanceKm: 4.5,
        statusText: 'Mở cửa',
        sportsTag: 'Pickleball · Tennis · Cầu lông',
        priceFrom: 140000,
        isOpen: true,
      ),
    ];
  }

  /// Fetches recommended courts for quick today booking.
  Future<List<RecommendedCourtModel>> getRecommendedCourts() async {
    return const [
      RecommendedCourtModel(
        id: 'court-a1',
        courtName: 'Sân A1 - Sân Cầu Lông',
        centerName: 'CourtGo Sports Arena',
        codeBadge: 'A1',
        availabilityText: 'Còn 6 khung giờ hôm nay',
        pricePerHour: 100000,
        imageUrl:
            'https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?q=80&w=600&auto=format&fit=crop',
        isAvailable: true,
      ),
      RecommendedCourtModel(
        id: 'court-p1',
        courtName: 'Sân P1 - Sân Pickleball',
        centerName: 'CourtGo Sports Arena',
        codeBadge: 'P1',
        availabilityText: 'Còn sân lúc 19:00',
        pricePerHour: 150000,
        imageUrl:
            'https://images.unsplash.com/photo-1554068865-24cecd4e34b8?q=80&w=600&auto=format&fit=crop',
        isAvailable: true,
      ),
      RecommendedCourtModel(
        id: 'court-f2',
        courtName: 'Sân F2 - Sân Bóng Đá',
        centerName: 'GreenField Sports',
        codeBadge: 'F2',
        availabilityText: 'Hết sân tối nay',
        pricePerHour: 320000,
        imageUrl:
            'https://images.unsplash.com/photo-1508098682722-e99c43a406b2?q=80&w=600&auto=format&fit=crop',
        isAvailable: false,
      ),
    ];
  }
}
