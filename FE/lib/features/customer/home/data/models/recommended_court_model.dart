/// Model for quick court booking recommendations in the "Gợi ý cho bạn" section.
class RecommendedCourtModel {
  const RecommendedCourtModel({
    required this.id,
    required this.courtName,
    required this.centerName,
    required this.codeBadge,
    required this.availabilityText,
    required this.pricePerHour,
    required this.imageUrl,
    this.isAvailable = true,
  });

  final String id;
  final String courtName;
  final String centerName;
  final String codeBadge;
  final String availabilityText;
  final double pricePerHour;
  final String imageUrl;
  final bool isAvailable;

  factory RecommendedCourtModel.fromJson(Map<String, dynamic> json) {
    return RecommendedCourtModel(
      id: json['id'] as String,
      courtName: json['courtName'] as String? ?? '',
      centerName: json['centerName'] as String? ?? '',
      codeBadge: json['codeBadge'] as String? ?? '',
      availabilityText: json['availabilityText'] as String? ?? '',
      pricePerHour: (json['pricePerHour'] as num?)?.toDouble() ?? 0.0,
      imageUrl: json['imageUrl'] as String? ?? '',
      isAvailable: json['isAvailable'] as bool? ?? true,
    );
  }
}
