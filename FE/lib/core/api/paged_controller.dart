import 'package:flutter/foundation.dart';

import '../network/api_exception.dart';
import '../../shared/models/customer_models.dart';

class PagedController<T> extends ChangeNotifier {
  PagedController(this.fetch);
  final Future<PageResult<T>> Function(int) fetch;
  final List<T> items = [];
  bool loading = false, hasMore = true, _disposed = false;
  String? error;
  int _page = 0, _generation = 0;
  Future<void> load({bool reset = false}) async {
    if (loading && !reset) return;
    final generation = reset ? ++_generation : _generation;
    if (reset) {
      _page = 0;
      items.clear();
      hasMore = true;
    }
    if (!hasMore) return;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final result = await fetch(_page + 1);
      if (_disposed || generation != _generation) return;
      items.addAll(result.items);
      _page = result.page;
      hasMore = _page < result.totalPages;
    } on ApiException catch (e) {
      if (!_disposed && generation == _generation) error = e.message;
    } catch (_) {
      if (!_disposed && generation == _generation) {
        error = 'Dữ liệu chưa sẵn sàng. Vui lòng thử lại.';
      }
    } finally {
      if (!_disposed && generation == _generation) {
        loading = false;
        notifyListeners();
      }
    }
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
