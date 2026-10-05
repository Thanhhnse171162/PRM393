class SportCenterModel {
  const SportCenterModel({
    required this.id,
    required this.name,
    required this.address,
    this.imageUrl,
  });

  final String id;
  final String name;
  final String address;
  final String? imageUrl;

  factory SportCenterModel.fromJson(Map<String, dynamic> json) {
    return SportCenterModel(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      address: json['address'] as String? ?? '',
      imageUrl: json['imageUrl'] as String?,
    );
  }
}
