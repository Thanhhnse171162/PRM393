enum UserRole {
  customer,
  staff,
  admin;

  static UserRole fromString(String? value) {
    return UserRole.values.firstWhere(
      (r) => r.name.toLowerCase() == (value ?? '').toLowerCase(),
      orElse: () => UserRole.customer,
    );
  }
}

class UserModel {
  const UserModel({
    required this.id,
    required this.fullName,
    required this.email,
    required this.role,
    this.phoneNumber,
  });

  final String id;
  final String fullName;
  final String email;
  final String? phoneNumber;
  final UserRole role;

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] as String,
      fullName: json['fullName'] as String? ?? '',
      email: json['email'] as String? ?? '',
      phoneNumber: json['phoneNumber'] as String?,
      role: UserRole.fromString(json['role'] as String?),
    );
  }
}
