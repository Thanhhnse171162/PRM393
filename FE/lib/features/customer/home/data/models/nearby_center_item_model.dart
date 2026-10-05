/// UI and API model for a sports center card displayed on the Home screen.
class NearbyCenterItemModel {
  const NearbyCenterItemModel({
    required this.id,
    required this.name,
    required this.address,
    required this.imageUrl,
    required this.rating,
    required this.distanceKm,
    required this.statusText,
    required this.sportsTag,
    required this.priceFrom,
    this.isOpen = true,
  });

  final String id;
  final String name;
  final String address;
  final String imageUrl;
  final double rating;
  final double distanceKm;
  final String statusText;
  final String sportsTag;
  final double priceFrom;
  final bool isOpen;

  factory NearbyCenterItemModel.fromJson(Map<String, dynamic> json) {
    return NearbyCenterItemModel(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      address: json['address'] as String? ?? '',
      imageUrl: json['imageUrl'] as String? ?? '',
      rating: (json['rating'] as num?)?.toDouble() ?? 5.0,
      distanceKm: (json['distanceKm'] as num?)?.toDouble() ?? 1.0,
      statusText: json['statusText'] as String? ?? 'Mở cửa',
      sportsTag: json['sportsTag'] as String? ?? '',
      priceFrom: (json['priceFrom'] as num?)?.toDouble() ?? 0.0,
      isOpen: json['isOpen'] as bool? ?? true,
    );
  }
}
