/// Data model representing a promotional/hero banner.
class BannerItemModel {
  const BannerItemModel({
    required this.id,
    required this.tag,
    required this.title,
    required this.subtitle,
    required this.buttonText,
    required this.imageUrl,
    this.targetUrl,
  });

  final String id;
  final String tag;
  final String title;
  final String subtitle;
  final String buttonText;
  final String imageUrl;
  final String? targetUrl;

  factory BannerItemModel.fromJson(Map<String, dynamic> json) {
    return BannerItemModel(
      id: json['id'] as String,
      tag: json['tag'] as String? ?? 'ƯU ĐÃI',
      title: json['title'] as String? ?? '',
      subtitle: json['subtitle'] as String? ?? '',
      buttonText: json['buttonText'] as String? ?? 'Xem ngay',
      imageUrl: json['imageUrl'] as String? ?? '',
      targetUrl: json['targetUrl'] as String?,
    );
  }
}
