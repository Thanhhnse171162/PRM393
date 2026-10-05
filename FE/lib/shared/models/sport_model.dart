class SportModel {
  const SportModel({required this.id, required this.name, this.iconUrl});

  final String id;
  final String name;
  final String? iconUrl;

  factory SportModel.fromJson(Map<String, dynamic> json) {
    return SportModel(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      iconUrl: json['iconUrl'] as String?,
    );
  }
}
