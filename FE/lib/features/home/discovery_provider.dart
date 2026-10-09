import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';

import '../../core/api/customer_api.dart';
import '../../core/network/api_exception.dart';
import '../../core/storage/local_storage_service.dart';
import '../../shared/models/customer_models.dart';
import '../../shared/models/sport_model.dart';

class DiscoveryProvider extends ChangeNotifier {
  DiscoveryProvider(this.api, this.storage) : area = storage.selectedArea ?? '';
  final CustomerApi api;
  final LocalStorageService storage;
  List<CenterInfo> centers = [];
  List<SportModel> sports = [];
  String area, search = '';
  String? sportId, error;
  bool loading = false, loaded = false, disposed = false;
  Position? position;
  DateTime? quickDate;
  int? quickHour;
  int _generation = 0;
  Timer? _debounce;
  Future<void> load() async {
    final generation = ++_generation;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final values = await Future.wait<dynamic>([
        sports.isEmpty ? api.sports() : Future.value(sports),
        api.centers(sportId: sportId, district: area, search: search),
      ]);
      if (disposed || generation != _generation) return;
      sports = values[0] as List<SportModel>;
      centers = values[1] as List<CenterInfo>;
      if (position != null) {
        centers.sort(
          (a, b) => (distance(a) ?? double.infinity).compareTo(
            distance(b) ?? double.infinity,
          ),
        );
      }
      loaded = true;
    } on ApiException catch (e) {
      if (!disposed && generation == _generation) error = e.message;
    } catch (_) {
      if (!disposed && generation == _generation) {
        error = 'Chưa tải được các chi nhánh. Vui lòng thử lại.';
      }
    } finally {
      if (!disposed && generation == _generation) {
        loading = false;
        notifyListeners();
      }
    }
  }

  void setSearch(String value) {
    search = value.trim();
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 350), load);
  }

  void selectSport(String? value) {
    sportId = value;
    load();
  }

  Future<void> selectArea(String value) async {
    area = value;
    position = null;
    await storage.saveSelectedArea(value);
    await load();
  }

  void setQuick(DateTime date, int hour) {
    quickDate = date;
    quickHour = hour;
    notifyListeners();
  }

  double? distance(CenterInfo center) {
    if (position == null ||
        center.latitude == null ||
        center.longitude == null) {
      return null;
    }
    return Geolocator.distanceBetween(
          position!.latitude,
          position!.longitude,
          center.latitude!,
          center.longitude!,
        ) /
        1000;
  }

  Future<void> locate() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      throw const ApiException(
        message: 'Hãy bật dịch vụ vị trí trên thiết bị.',
      );
    }
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      throw const ApiException(
        message: 'Chưa có quyền vị trí. Bạn có thể chọn khu vực bên dưới.',
      );
    }
    position = await Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.medium,
        timeLimit: Duration(seconds: 15),
      ),
    );
    area = '';
    await storage.saveSelectedArea('');
    await load();
  }

  @override
  void dispose() {
    disposed = true;
    _debounce?.cancel();
    super.dispose();
  }
}
