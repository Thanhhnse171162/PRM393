import 'package:flutter/foundation.dart';

import '../../../../shared/models/user_model.dart';

/// Placeholder auth state. Real login/API integration comes with
/// feature/api-auth; for now roles can be simulated for navigation work.
class AuthProvider extends ChangeNotifier {
  UserModel? _user;

  UserModel? get user => _user;

  /// `null` means guest.
  UserRole? get role => _user?.role;

  bool get isAuthenticated => _user != null;

  /// DEV ONLY: simulate a signed-in user.
  void signInAs(UserRole role) {
    _user = UserModel(
      id: 'dev-${role.name}',
      fullName: 'Demo ${role.name}',
      email: '${role.name}@courtgo.local',
      role: role,
    );
    notifyListeners();
  }

  void signOut() {
    _user = null;
    notifyListeners();
  }
}
