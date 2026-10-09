import 'package:flutter/foundation.dart';

import '../../core/api/customer_api.dart';
import '../../core/network/api_exception.dart';
import '../../core/utils/customer_format.dart';
import '../../shared/models/customer_models.dart';

class AvailabilityProvider extends ChangeNotifier {
  AvailabilityProvider(this.api, this.courtId, this.date);
  final CustomerApi api;
  final String courtId;
  DateTime date;
  Availability? data;
  List<Slot> selected = [];
  bool loading = false, holding = false, disposed = false;
  String? error;
  int generation = 0;
  double get estimatedTotal => selected.fold(0, (sum, s) => sum + s.price);
  Future<void> load([DateTime? day]) async {
    if (day != null) date = day;
    final current = ++generation;
    loading = true;
    selected = [];
    error = null;
    notifyListeners();
    try {
      final next = await api.availability(courtId, CustomerFormat.day(date));
      if (!disposed && current == generation) data = next;
    } on ApiException catch (e) {
      if (!disposed && current == generation) {
        data = null;
        error = e.message;
      }
    } catch (_) {
      if (!disposed && current == generation) {
        data = null;
        error = 'Chưa tải được giờ trống.';
      }
    } finally {
      if (!disposed && current == generation) {
        loading = false;
        notifyListeners();
      }
    }
  }

  String? toggle(Slot slot) {
    if (!slot.available || holding) return null;
    final exists = selected.indexWhere((s) => s.start == slot.start);
    if (exists >= 0) {
      if (exists != 0 && exists != selected.length - 1) {
        return 'Chỉ chọn các khung giờ liên tiếp. Hãy bỏ giờ ở đầu hoặc cuối.';
      }
      selected.removeAt(exists);
    } else {
      if (selected.isNotEmpty &&
          DateTime.parse(slot.start) != DateTime.parse(selected.last.end) &&
          DateTime.parse(slot.end) != DateTime.parse(selected.first.start)) {
        return 'Vui lòng chọn giờ liền trước hoặc liền sau các giờ đang chọn.';
      }
      selected.add(slot);
      selected.sort(
        (a, b) => DateTime.parse(a.start).compareTo(DateTime.parse(b.start)),
      );
    }
    error = null;
    notifyListeners();
    return null;
  }

  Future<Booking?> hold() async {
    if (holding || selected.isEmpty) return null;
    holding = true;
    error = null;
    notifyListeners();
    try {
      return await api.hold(courtId, List.of(selected));
    } on ApiException catch (e) {
      if (e.isConflict) {
        await load();
        error = 'Khung giờ vừa được người khác chọn. Vui lòng chọn lại.';
      } else {
        error = e.message;
      }
      return null;
    } catch (_) {
      error =
          'Chưa xác nhận được giữ sân. Kiểm tra lịch đặt trước khi thử lại.';
      return null;
    } finally {
      if (!disposed) {
        holding = false;
        notifyListeners();
      }
    }
  }

  @override
  void dispose() {
    disposed = true;
    super.dispose();
  }
}
