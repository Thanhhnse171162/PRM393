class CourtModel {
  const CourtModel({
    required this.id,
    required this.sportCenterId,
    required this.sportId,
    required this.name,
    required this.status,
    this.pricePerHour = 0,
  });

  final String id;
  final String sportCenterId;
  final String sportId;
  final String name;

  /// Matches backend CourtStatus (Active, TemporarilyBlocked, ...).
  final String status;
  final double pricePerHour;

  factory CourtModel.fromJson(Map<String, dynamic> json) {
    return CourtModel(
      id: json['id'] as String,
      sportCenterId: json['sportCenterId'] as String,
      sportId: json['sportId'] as String,
      name: json['name'] as String? ?? '',
      status: json['status'] as String? ?? 'Active',
      pricePerHour: (json['pricePerHour'] as num?)?.toDouble() ?? 0,
    );
  }
}
