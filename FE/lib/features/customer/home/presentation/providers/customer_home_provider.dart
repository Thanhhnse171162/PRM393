import 'package:flutter/foundation.dart';

import '../../../../../shared/models/sport_model.dart';
import '../../data/models/banner_item_model.dart';
import '../../data/models/nearby_center_item_model.dart';
import '../../data/models/recommended_court_model.dart';
import '../../data/services/customer_home_service.dart';

/// Provider for managing state and business interactions of the Customer Home screen.
class CustomerHomeProvider extends ChangeNotifier {
  CustomerHomeProvider({CustomerHomeService? service})
      : _service = service ?? CustomerHomeService();

  final CustomerHomeService _service;

  bool _isLoading = false;
  String? _errorMessage;

  List<BannerItemModel> _banners = [];
  List<SportModel> _sports = [];
  List<NearbyCenterItemModel> _nearbyCenters = [];
  List<RecommendedCourtModel> _recommendedCourts = [];

  String _selectedArea = 'Quận 7, TP. Hồ Chí Minh';
  String? _selectedSportId;
  String _selectedSportName = 'Cầu lông';
  String _searchQuery = '';

  // Quick booking card state
  String _quickSport = 'Cầu lông';
  String _quickDate = 'Hôm nay';
  String _quickTime = '19:00';

  // Getters
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  bool get hasError => _errorMessage != null;
  bool get isEmpty =>
      !_isLoading &&
      !hasError &&
      _banners.isEmpty &&
      _nearbyCenters.isEmpty &&
      _recommendedCourts.isEmpty;

  List<BannerItemModel> get banners => _banners;
  List<SportModel> get sports => _sports;
  List<NearbyCenterItemModel> get nearbyCenters => _nearbyCenters;
  List<RecommendedCourtModel> get recommendedCourts => _recommendedCourts;

  String get selectedArea => _selectedArea;
  String? get selectedSportId => _selectedSportId;
  String get selectedSport => _selectedSportName;
  String get searchQuery => _searchQuery;

  String get quickSport => _quickSport;
  String get quickDate => _quickDate;
  String get quickTime => _quickTime;

  /// Filtered nearby centers according to search query and selected sport.
  List<NearbyCenterItemModel> get filteredCenters {
    return _nearbyCenters.where((center) {
      final matchesSearch = _searchQuery.isEmpty ||
          center.name.toLowerCase().contains(_searchQuery.toLowerCase()) ||
          center.address.toLowerCase().contains(_searchQuery.toLowerCase()) ||
          center.sportsTag.toLowerCase().contains(_searchQuery.toLowerCase());

      final matchesSport = _selectedSportName.isEmpty ||
          center.sportsTag
              .toLowerCase()
              .contains(_selectedSportName.toLowerCase());

      return matchesSearch && matchesSport;
    }).toList();
  }

  /// Initial load or reload of all data for the home screen.
  Future<void> loadData({bool silent = false}) async {
    if (!silent) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final results = await Future.wait([
        _service.getBanners(),
        _service.getSports(),
        _service.getNearbyCenters(area: _selectedArea),
        _service.getRecommendedCourts(),
      ]);

      _banners = results[0] as List<BannerItemModel>;
      _sports = results[1] as List<SportModel>;
      _nearbyCenters = results[2] as List<NearbyCenterItemModel>;
      _recommendedCourts = results[3] as List<RecommendedCourtModel>;

      _isLoading = false;
      _errorMessage = null;
      notifyListeners();
    } catch (e) {
      _isLoading = false;
      _errorMessage = 'Không thể tải dữ liệu trang chủ. Vui lòng thử lại.';
      notifyListeners();
    }
  }

  /// Alias for loadData()
  Future<void> loadHomeData() => loadData();

  void setArea(String area) {
    if (_selectedArea == area) return;
    _selectedArea = area;
    notifyListeners();
    loadData(silent: true);
  }

  void setSelectedArea(String area) => setArea(area);

  void selectSport(String? sportId) {
    if (_selectedSportId == sportId) {
      _selectedSportId = null;
    } else {
      _selectedSportId = sportId;
    }
    notifyListeners();
  }

  void setSelectedSport(String sportName) {
    _selectedSportName = sportName;
    _quickSport = sportName;
    notifyListeners();
  }

  void setSearchQuery(String query) {
    _searchQuery = query.trim();
    notifyListeners();
  }

  void setQuickDate(String date) {
    _quickDate = date;
    notifyListeners();
  }

  void setQuickTime(String time) {
    _quickTime = time;
    notifyListeners();
  }

  void updateQuickBooking({
    String? sport,
    String? date,
    String? time,
  }) {
    if (sport != null) {
      _quickSport = sport;
      _selectedSportName = sport;
    }
    if (date != null) _quickDate = date;
    if (time != null) _quickTime = time;
    notifyListeners();
  }
}
