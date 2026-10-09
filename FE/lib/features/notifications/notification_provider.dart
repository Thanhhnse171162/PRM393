import 'package:flutter/foundation.dart';

import '../../core/api/customer_api.dart';

class NotificationProvider extends ChangeNotifier {
  NotificationProvider(this.api) {
    api.addListener(refresh);
  }
  final CustomerApi api;
  int unreadCount = 0;
  String? userId;
  bool _disposed = false;
  void setUser(String? id) {
    if (userId == id) return;
    userId = id;
    unreadCount = 0;
    if (id != null) refresh();
  }

  Future<void> refresh() async {
    final id = userId;
    if (id == null) return;
    try {
      final count = await api.unread();
      if (!_disposed && id == userId) {
        unreadCount = count;
        notifyListeners();
      }
    } catch (_) {
      /* List screen owns visible errors. A failed badge read never blocks navigation. */
    }
  }

  @override
  void dispose() {
    api.removeListener(refresh);
    _disposed = true;
    super.dispose();
  }
}
