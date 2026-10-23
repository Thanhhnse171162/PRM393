import '../../../shared/models/customer_models.dart' show Json;
export '../../../shared/models/customer_models.dart' show Json;

// ==========================================
// Dashboard
// ==========================================

class AdminDashboardData {
  const AdminDashboardData({
    required this.date,
    required this.timeZoneId,
    this.centerId,
    required this.totalBookings,
    required this.pendingBookings,
    required this.confirmedBookings,
    required this.checkedInBookings,
    required this.inProgressBookings,
    required this.completedBookings,
    required this.cancelledBookings,
    required this.noShowBookings,
    required this.todayRevenue,
    required this.refundAmount,
    required this.activeCenters,
    required this.activeCourts,
    required this.occupancyRate,
    required this.upcomingBookings,
    required this.pendingCancellationRequests,
  });

  final String date;
  final String timeZoneId;
  final String? centerId;
  final int totalBookings;
  final int pendingBookings;
  final int confirmedBookings;
  final int checkedInBookings;
  final int inProgressBookings;
  final int completedBookings;
  final int cancelledBookings;
  final int noShowBookings;
  final double todayRevenue;
  final double refundAmount;
  final int activeCenters;
  final int activeCourts;
  final double occupancyRate;
  final int upcomingBookings;
  final int pendingCancellationRequests;

  factory AdminDashboardData.fromJson(Json json) {
    return AdminDashboardData(
      date: json['date'] as String? ?? '',
      timeZoneId: json['timeZoneId'] as String? ?? 'SE Asia Standard Time',
      centerId: json['centerId'] as String?,
      totalBookings: (json['totalBookings'] as num?)?.toInt() ?? 0,
      pendingBookings: (json['pendingBookings'] as num?)?.toInt() ?? 0,
      confirmedBookings: (json['confirmedBookings'] as num?)?.toInt() ?? 0,
      checkedInBookings: (json['checkedInBookings'] as num?)?.toInt() ?? 0,
      inProgressBookings: (json['inProgressBookings'] as num?)?.toInt() ?? 0,
      completedBookings: (json['completedBookings'] as num?)?.toInt() ?? 0,
      cancelledBookings: (json['cancelledBookings'] as num?)?.toInt() ?? 0,
      noShowBookings: (json['noShowBookings'] as num?)?.toInt() ?? 0,
      todayRevenue: (json['todayRevenue'] as num?)?.toDouble() ?? 0.0,
      refundAmount: (json['refundAmount'] as num?)?.toDouble() ?? 0.0,
      activeCenters: (json['activeCenters'] as num?)?.toInt() ?? 0,
      activeCourts: (json['activeCourts'] as num?)?.toInt() ?? 0,
      occupancyRate: (json['occupancyRate'] as num?)?.toDouble() ?? 0.0,
      upcomingBookings: (json['upcomingBookings'] as num?)?.toInt() ?? 0,
      pendingCancellationRequests:
          (json['pendingCancellationRequests'] as num?)?.toInt() ?? 0,
    );
  }
}

// ==========================================
// Staff Management
// ==========================================

class AssignedCenterInfo {
  const AssignedCenterInfo({
    required this.centerId,
    required this.centerName,
  });

  final String centerId;
  final String centerName;

  factory AssignedCenterInfo.fromJson(Json json) {
    return AssignedCenterInfo(
      centerId: json['centerId'] as String? ?? '',
      centerName: json['centerName'] as String? ?? '',
    );
  }
}

class StaffAssignmentHistoryItem {
  const StaffAssignmentHistoryItem({
    required this.assignmentId,
    required this.centerId,
    required this.centerName,
    required this.assignedAt,
    required this.isActive,
  });

  final String assignmentId;
  final String centerId;
  final String centerName;
  final String assignedAt;
  final bool isActive;

  factory StaffAssignmentHistoryItem.fromJson(Json json) {
    return StaffAssignmentHistoryItem(
      assignmentId: json['assignmentId'] as String? ?? '',
      centerId: json['centerId'] as String? ?? '',
      centerName: json['centerName'] as String? ?? '',
      assignedAt: json['assignedAt'] as String? ?? '',
      isActive: json['isActive'] as bool? ?? false,
    );
  }
}

class AdminStaffItem {
  const AdminStaffItem({
    required this.staffId,
    required this.fullName,
    this.email,
    required this.phoneNumber,
    required this.isActive,
    this.assignedCenter,
    required this.createdAt,
  });

  final String staffId;
  final String fullName;
  final String? email;
  final String phoneNumber;
  final bool isActive;
  final AssignedCenterInfo? assignedCenter;
  final String createdAt;
  String get id => staffId;

  factory AdminStaffItem.fromJson(Json json) {
    return AdminStaffItem(
      staffId: json['staffId'] as String? ?? '',
      fullName: json['fullName'] as String? ?? '',
      email: json['email'] as String?,
      phoneNumber: json['phoneNumber'] as String? ?? '',
      isActive: json['isActive'] as bool? ?? true,
      assignedCenter: json['assignedCenter'] != null
          ? AssignedCenterInfo.fromJson(json['assignedCenter'] as Json)
          : null,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminStaffDetail {
  const AdminStaffDetail({
    required this.staffId,
    required this.fullName,
    this.email,
    required this.phoneNumber,
    required this.isActive,
    this.assignedCenter,
    required this.assignmentHistory,
    required this.createdAt,
  });

  final String staffId;
  final String fullName;
  final String? email;
  final String phoneNumber;
  final bool isActive;
  final AssignedCenterInfo? assignedCenter;
  final List<StaffAssignmentHistoryItem> assignmentHistory;
  final String createdAt;
  String get id => staffId;

  factory AdminStaffDetail.fromJson(Json json) {
    return AdminStaffDetail(
      staffId: json['staffId'] as String? ?? '',
      fullName: json['fullName'] as String? ?? '',
      email: json['email'] as String?,
      phoneNumber: json['phoneNumber'] as String? ?? '',
      isActive: json['isActive'] as bool? ?? true,
      assignedCenter: json['assignedCenter'] != null
          ? AssignedCenterInfo.fromJson(json['assignedCenter'] as Json)
          : null,
      assignmentHistory: (json['assignmentHistory'] as List<dynamic>? ?? [])
          .map((e) => StaffAssignmentHistoryItem.fromJson(e as Json))
          .toList(),
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

// ==========================================
// Sport Centers
// ==========================================

class AdminSportCenterItem {
  const AdminSportCenterItem({
    required this.centerId,
    required this.name,
    required this.address,
    required this.city,
    required this.district,
    this.phone,
    this.latitude,
    this.longitude,
    required this.timeZoneId,
    required this.status,
    required this.courtCount,
    required this.activeCourtCount,
    required this.createdAt,
  });

  final String centerId;
  final String name;
  final String address;
  final String city;
  final String district;
  final String? phone;
  final double? latitude;
  final double? longitude;
  final String timeZoneId;
  final String status;
  final int courtCount;
  final int activeCourtCount;
  final String createdAt;

  bool get isActive => status.toLowerCase() == 'active';
  String get id => centerId;

  factory AdminSportCenterItem.fromJson(Json json) {
    return AdminSportCenterItem(
      centerId: json['centerId'] as String? ?? '',
      name: json['name'] as String? ?? '',
      address: json['address'] as String? ?? '',
      city: json['city'] as String? ?? '',
      district: json['district'] as String? ?? '',
      phone: json['phone'] as String?,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      timeZoneId: json['timeZoneId'] as String? ?? 'SE Asia Standard Time',
      status: json['status'] as String? ?? 'Active',
      courtCount: (json['courtCount'] as num?)?.toInt() ?? 0,
      activeCourtCount: (json['activeCourtCount'] as num?)?.toInt() ?? 0,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminSportCenterDetail {
  const AdminSportCenterDetail({
    required this.centerId,
    required this.name,
    required this.addressLine,
    this.ward,
    required this.district,
    required this.city,
    this.latitude,
    this.longitude,
    required this.timeZoneId,
    this.phoneNumber,
    this.coverImageUrl,
    required this.status,
    required this.totalCourts,
    required this.activeCourts,
    required this.assignedActiveStaffCount,
    required this.createdAt,
    this.updatedAt,
  });

  final String centerId;
  final String name;
  final String addressLine;
  final String? ward;
  final String district;
  final String city;
  final double? latitude;
  final double? longitude;
  final String timeZoneId;
  final String? phoneNumber;
  final String? coverImageUrl;
  final String status;
  final int totalCourts;
  final int activeCourts;
  final int assignedActiveStaffCount;
  final String createdAt;
  final String? updatedAt;

  bool get isActive => status.toLowerCase() == 'active';
  String get id => centerId;

  factory AdminSportCenterDetail.fromJson(Json json) {
    return AdminSportCenterDetail(
      centerId: json['centerId'] as String? ?? '',
      name: json['name'] as String? ?? '',
      addressLine: json['addressLine'] as String? ?? '',
      ward: json['ward'] as String?,
      district: json['district'] as String? ?? '',
      city: json['city'] as String? ?? '',
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      timeZoneId: json['timeZoneId'] as String? ?? 'SE Asia Standard Time',
      phoneNumber: json['phoneNumber'] as String?,
      coverImageUrl: json['coverImageUrl'] as String?,
      status: json['status'] as String? ?? 'Active',
      totalCourts: (json['totalCourts'] as num?)?.toInt() ?? 0,
      activeCourts: (json['activeCourts'] as num?)?.toInt() ?? 0,
      assignedActiveStaffCount:
          (json['assignedActiveStaffCount'] as num?)?.toInt() ?? 0,
      createdAt: json['createdAt'] as String? ?? '',
      updatedAt: json['updatedAt'] as String?,
    );
  }
}

// ==========================================
// Sports
// ==========================================

class AdminSportItem {
  const AdminSportItem({
    required this.id,
    required this.code,
    required this.name,
    this.iconUrl,
    required this.displayOrder,
    required this.isActive,
    required this.courtCount,
    required this.createdAt,
  });

  final String id;
  final String code;
  final String name;
  final String? iconUrl;
  final int displayOrder;
  final bool isActive;
  final int courtCount;
  final String createdAt;

  factory AdminSportItem.fromJson(Json json) {
    return AdminSportItem(
      id: json['id'] as String? ?? '',
      code: json['code'] as String? ?? '',
      name: json['name'] as String? ?? '',
      iconUrl: json['iconUrl'] as String?,
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
      isActive: json['isActive'] as bool? ?? true,
      courtCount: (json['courtCount'] as num?)?.toInt() ?? 0,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

// ==========================================
// Courts
// ==========================================

class AdminCourtItem {
  const AdminCourtItem({
    required this.courtId,
    required this.sportCenterId,
    required this.sportCenterName,
    required this.sportId,
    required this.sportName,
    required this.code,
    required this.name,
    this.surfaceType,
    required this.basePricePerHour,
    required this.status,
    this.coverImageUrl,
    required this.createdAt,
  });

  final String courtId;
  final String sportCenterId;
  final String sportCenterName;
  final String sportId;
  final String sportName;
  final String code;
  final String name;
  final String? surfaceType;
  final double basePricePerHour;
  final String status;
  final String? coverImageUrl;
  final String createdAt;

  factory AdminCourtItem.fromJson(Json json) {
    return AdminCourtItem(
      courtId: json['courtId'] as String? ?? '',
      sportCenterId: json['sportCenterId'] as String? ?? '',
      sportCenterName: json['sportCenterName'] as String? ?? '',
      sportId: json['sportId'] as String? ?? '',
      sportName: json['sportName'] as String? ?? '',
      code: json['code'] as String? ?? '',
      name: json['name'] as String? ?? '',
      surfaceType: json['surfaceType'] as String?,
      basePricePerHour: (json['basePricePerHour'] as num?)?.toDouble() ?? 0.0,
      status: json['status'] as String? ?? 'Active',
      coverImageUrl: json['coverImageUrl'] as String?,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminCourtDetail {
  const AdminCourtDetail({
    required this.courtId,
    required this.sportCenterId,
    required this.sportCenterName,
    required this.sportId,
    required this.sportName,
    required this.code,
    required this.name,
    this.surfaceType,
    this.description,
    this.coverImageUrl,
    required this.basePricePerHour,
    required this.status,
    required this.activeBookingCount,
    required this.createdAt,
    this.updatedAt,
  });

  final String courtId;
  final String sportCenterId;
  final String sportCenterName;
  final String sportId;
  final String sportName;
  final String code;
  final String name;
  final String? surfaceType;
  final String? description;
  final String? coverImageUrl;
  final double basePricePerHour;
  final String status;
  final int activeBookingCount;
  final String createdAt;
  final String? updatedAt;

  factory AdminCourtDetail.fromJson(Json json) {
    return AdminCourtDetail(
      courtId: json['courtId'] as String? ?? '',
      sportCenterId: json['sportCenterId'] as String? ?? '',
      sportCenterName: json['sportCenterName'] as String? ?? '',
      sportId: json['sportId'] as String? ?? '',
      sportName: json['sportName'] as String? ?? '',
      code: json['code'] as String? ?? '',
      name: json['name'] as String? ?? '',
      surfaceType: json['surfaceType'] as String?,
      description: json['description'] as String?,
      coverImageUrl: json['coverImageUrl'] as String?,
      basePricePerHour: (json['basePricePerHour'] as num?)?.toDouble() ?? 0.0,
      status: json['status'] as String? ?? 'Active',
      activeBookingCount: (json['activeBookingCount'] as num?)?.toInt() ?? 0,
      createdAt: json['createdAt'] as String? ?? '',
      updatedAt: json['updatedAt'] as String?,
    );
  }
}

// ==========================================
// Operating Hours & Exceptions
// ==========================================

class AdminOperatingHourItem {
  const AdminOperatingHourItem({
    required this.dayOfWeek,
    required this.isClosed,
    this.openTime,
    this.closeTime,
  });

  final String dayOfWeek;
  final bool isClosed;
  final String? openTime;
  final String? closeTime;

  factory AdminOperatingHourItem.fromJson(Json json) {
    return AdminOperatingHourItem(
      dayOfWeek: json['dayOfWeek'] as String? ?? 'Monday',
      isClosed: json['isClosed'] as bool? ?? false,
      openTime: json['openTime'] as String?,
      closeTime: json['closeTime'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
        'dayOfWeek': dayOfWeek,
        'isClosed': isClosed,
        'openTime': isClosed ? null : openTime,
        'closeTime': isClosed ? null : closeTime,
      };
}

class AdminCenterOperatingHoursData {
  const AdminCenterOperatingHoursData({
    required this.centerId,
    required this.timeZoneId,
    required this.days,
  });

  final String centerId;
  final String timeZoneId;
  final List<AdminOperatingHourItem> days;

  factory AdminCenterOperatingHoursData.fromJson(Json json) {
    return AdminCenterOperatingHoursData(
      centerId: json['centerId'] as String? ?? '',
      timeZoneId: json['timeZoneId'] as String? ?? 'SE Asia Standard Time',
      days: (json['days'] as List<dynamic>? ?? [])
          .map((e) => AdminOperatingHourItem.fromJson(e as Json))
          .toList(),
    );
  }
}

class AdminOperatingHourExceptionItem {
  const AdminOperatingHourExceptionItem({
    required this.exceptionId,
    required this.sportCenterId,
    required this.date,
    required this.isClosed,
    this.openTime,
    this.closeTime,
    this.reason,
  });

  final String exceptionId;
  final String sportCenterId;
  final String date;
  final bool isClosed;
  final String? openTime;
  final String? closeTime;
  final String? reason;

  factory AdminOperatingHourExceptionItem.fromJson(Json json) {
    return AdminOperatingHourExceptionItem(
      exceptionId: json['exceptionId'] as String? ?? '',
      sportCenterId: json['sportCenterId'] as String? ?? '',
      date: json['date'] as String? ?? '',
      isClosed: json['isClosed'] as bool? ?? false,
      openTime: json['openTime'] as String?,
      closeTime: json['closeTime'] as String?,
      reason: json['reason'] as String?,
    );
  }
}

// ==========================================
// Price Rules
// ==========================================

class AdminPriceRuleItem {
  const AdminPriceRuleItem({
    required this.id,
    required this.courtId,
    required this.dayOfWeek,
    required this.startTime,
    required this.endTime,
    required this.pricePerHour,
    this.effectiveFrom,
    this.effectiveTo,
    required this.isActive,
    required this.createdAt,
  });

  final String id;
  final String courtId;
  final String dayOfWeek;
  final String startTime;
  final String endTime;
  final double pricePerHour;
  final String? effectiveFrom;
  final String? effectiveTo;
  final bool isActive;
  final String createdAt;

  factory AdminPriceRuleItem.fromJson(Json json) {
    return AdminPriceRuleItem(
      id: json['id'] as String? ?? '',
      courtId: json['courtId'] as String? ?? '',
      dayOfWeek: json['dayOfWeek'] as String? ?? 'Monday',
      startTime: json['startTime'] as String? ?? '00:00:00',
      endTime: json['endTime'] as String? ?? '23:59:59',
      pricePerHour: (json['pricePerHour'] as num?)?.toDouble() ?? 0.0,
      effectiveFrom: json['effectiveFrom'] as String?,
      effectiveTo: json['effectiveTo'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

// ==========================================
// System Settings
// ==========================================

class SystemSettingsData {
  const SystemSettingsData({
    required this.holdDurationMinutes,
    required this.minBookingLeadMinutes,
    required this.defaultDepositPercent,
    required this.allowOutstandingCheckIn,
    required this.updatedAt,
    this.updatedByUserId,
  });

  final int holdDurationMinutes;
  final int minBookingLeadMinutes;
  final double defaultDepositPercent;
  final bool allowOutstandingCheckIn;
  final String updatedAt;
  final String? updatedByUserId;

  factory SystemSettingsData.fromJson(Json json) {
    return SystemSettingsData(
      holdDurationMinutes:
          (json['holdDurationMinutes'] as num?)?.toInt() ?? 10,
      minBookingLeadMinutes:
          (json['minBookingLeadMinutes'] as num?)?.toInt() ?? 30,
      defaultDepositPercent:
          (json['defaultDepositPercent'] as num?)?.toDouble() ?? 30.0,
      allowOutstandingCheckIn:
          json['allowOutstandingCheckIn'] as bool? ?? false,
      updatedAt: json['updatedAt'] as String? ?? '',
      updatedByUserId: json['updatedByUserId'] as String?,
    );
  }
}

// ==========================================
// Cancellation Policies
// ==========================================

class AdminCancellationPolicyRule {
  const AdminCancellationPolicyRule({
    required this.id,
    required this.minHoursBeforeStart,
    this.maxHoursBeforeStart,
    required this.refundPercent,
  });

  final String id;
  final int minHoursBeforeStart;
  final int? maxHoursBeforeStart;
  final double refundPercent;

  factory AdminCancellationPolicyRule.fromJson(Json json) {
    return AdminCancellationPolicyRule(
      id: json['id'] as String? ?? '',
      minHoursBeforeStart:
          (json['minHoursBeforeStart'] as num?)?.toInt() ?? 0,
      maxHoursBeforeStart: (json['maxHoursBeforeStart'] as num?)?.toInt(),
      refundPercent: (json['refundPercent'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class AdminCancellationPolicySummary {
  const AdminCancellationPolicySummary({
    required this.id,
    required this.name,
    required this.version,
    required this.effectiveFrom,
    this.effectiveTo,
    required this.isActive,
    required this.ruleCount,
    required this.createdAt,
  });

  final String id;
  final String name;
  final int version;
  final String effectiveFrom;
  final String? effectiveTo;
  final bool isActive;
  final int ruleCount;
  final String createdAt;

  factory AdminCancellationPolicySummary.fromJson(Json json) {
    return AdminCancellationPolicySummary(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      version: (json['version'] as num?)?.toInt() ?? 1,
      effectiveFrom: json['effectiveFrom'] as String? ?? '',
      effectiveTo: json['effectiveTo'] as String?,
      isActive: json['isActive'] as bool? ?? false,
      ruleCount: (json['ruleCount'] as num?)?.toInt() ?? 0,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminCancellationPolicyDetail {
  const AdminCancellationPolicyDetail({
    required this.id,
    required this.name,
    required this.version,
    required this.effectiveFrom,
    this.effectiveTo,
    required this.isActive,
    required this.rules,
    required this.createdAt,
  });

  final String id;
  final String name;
  final int version;
  final String effectiveFrom;
  final String? effectiveTo;
  final bool isActive;
  final List<AdminCancellationPolicyRule> rules;
  final String createdAt;

  factory AdminCancellationPolicyDetail.fromJson(Json json) {
    return AdminCancellationPolicyDetail(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      version: (json['version'] as num?)?.toInt() ?? 1,
      effectiveFrom: json['effectiveFrom'] as String? ?? '',
      effectiveTo: json['effectiveTo'] as String?,
      isActive: json['isActive'] as bool? ?? false,
      rules: (json['rules'] as List<dynamic>? ?? [])
          .map((e) => AdminCancellationPolicyRule.fromJson(e as Json))
          .toList(),
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

// ==========================================
// Bookings
// ==========================================

class AdminBookingItem {
  const AdminBookingItem({
    required this.bookingId,
    required this.bookingCode,
    required this.customerName,
    required this.customerPhone,
    required this.sportCenterId,
    required this.sportCenterName,
    required this.courtId,
    required this.courtName,
    required this.sportId,
    required this.sportName,
    required this.startAt,
    required this.endAt,
    required this.totalAmount,
    required this.paidAmount,
    required this.remainingAmount,
    required this.bookingStatus,
    required this.paymentStatus,
    required this.source,
    required this.createdAt,
  });

  final String bookingId;
  final String bookingCode;
  final String customerName;
  final String customerPhone;
  final String sportCenterId;
  final String sportCenterName;
  final String courtId;
  final String courtName;
  final String sportId;
  final String sportName;
  final String startAt;
  final String endAt;
  final double totalAmount;
  final double paidAmount;
  final double remainingAmount;
  final String bookingStatus;
  final String paymentStatus;
  final String source;
  final String createdAt;

  factory AdminBookingItem.fromJson(Json json) {
    return AdminBookingItem(
      bookingId: json['bookingId'] as String? ?? '',
      bookingCode: json['bookingCode'] as String? ?? '',
      customerName: json['customerName'] as String? ?? '',
      customerPhone: json['customerPhone'] as String? ?? '',
      sportCenterId: json['sportCenterId'] as String? ?? '',
      sportCenterName: json['sportCenterName'] as String? ?? '',
      courtId: json['courtId'] as String? ?? '',
      courtName: json['courtName'] as String? ?? '',
      sportId: json['sportId'] as String? ?? '',
      sportName: json['sportName'] as String? ?? '',
      startAt: json['startAt'] as String? ?? '',
      endAt: json['endAt'] as String? ?? '',
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      paidAmount: (json['paidAmount'] as num?)?.toDouble() ?? 0.0,
      remainingAmount: (json['remainingAmount'] as num?)?.toDouble() ?? 0.0,
      bookingStatus: json['bookingStatus'] as String? ?? '',
      paymentStatus: json['paymentStatus'] as String? ?? '',
      source: json['source'] as String? ?? '',
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminCustomerSnapshot {
  const AdminCustomerSnapshot({
    this.customerUserId,
    required this.customerName,
    required this.customerPhone,
    this.customerEmail,
  });

  final String? customerUserId;
  final String customerName;
  final String customerPhone;
  final String? customerEmail;

  factory AdminCustomerSnapshot.fromJson(Json json) {
    return AdminCustomerSnapshot(
      customerUserId: json['customerUserId'] as String?,
      customerName: json['customerName'] as String? ?? '',
      customerPhone: json['customerPhone'] as String? ?? '',
      customerEmail: json['customerEmail'] as String?,
    );
  }
}

class AdminFacilitySnapshot {
  const AdminFacilitySnapshot({
    required this.sportCenterId,
    required this.sportCenterName,
    required this.courtId,
    required this.courtName,
    required this.sportId,
    required this.sportName,
  });

  final String sportCenterId;
  final String sportCenterName;
  final String courtId;
  final String courtName;
  final String sportId;
  final String sportName;

  factory AdminFacilitySnapshot.fromJson(Json json) {
    return AdminFacilitySnapshot(
      sportCenterId: json['sportCenterId'] as String? ?? '',
      sportCenterName: json['sportCenterName'] as String? ?? '',
      courtId: json['courtId'] as String? ?? '',
      courtName: json['courtName'] as String? ?? '',
      sportId: json['sportId'] as String? ?? '',
      sportName: json['sportName'] as String? ?? '',
    );
  }
}

class AdminBookingSlotItem {
  const AdminBookingSlotItem({
    required this.slotId,
    required this.startAt,
    required this.endAt,
    required this.unitPrice,
    required this.reservationState,
  });

  final String slotId;
  final String startAt;
  final String endAt;
  final double unitPrice;
  final String reservationState;

  factory AdminBookingSlotItem.fromJson(Json json) {
    return AdminBookingSlotItem(
      slotId: json['slotId'] as String? ?? '',
      startAt: json['startAt'] as String? ?? '',
      endAt: json['endAt'] as String? ?? '',
      unitPrice: (json['unitPrice'] as num?)?.toDouble() ?? 0.0,
      reservationState: json['reservationState'] as String? ?? '',
    );
  }
}

class AdminFinancialSummary {
  const AdminFinancialSummary({
    required this.totalAmount,
    required this.depositAmount,
    required this.paidAmount,
    required this.remainingAmount,
    required this.refundedAmount,
    required this.paymentStatus,
  });

  final double totalAmount;
  final double depositAmount;
  final double paidAmount;
  final double remainingAmount;
  final double refundedAmount;
  final String paymentStatus;

  factory AdminFinancialSummary.fromJson(Json json) {
    return AdminFinancialSummary(
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      depositAmount: (json['depositAmount'] as num?)?.toDouble() ?? 0.0,
      paidAmount: (json['paidAmount'] as num?)?.toDouble() ?? 0.0,
      remainingAmount: (json['remainingAmount'] as num?)?.toDouble() ?? 0.0,
      refundedAmount: (json['refundedAmount'] as num?)?.toDouble() ?? 0.0,
      paymentStatus: json['paymentStatus'] as String? ?? '',
    );
  }
}

class AdminPaymentEntry {
  const AdminPaymentEntry({
    required this.paymentId,
    required this.kind,
    required this.method,
    required this.amount,
    required this.transactionStatus,
    this.providerTransactionId,
    this.relatedPaymentId,
    this.paidAt,
    required this.createdAt,
  });

  final String paymentId;
  final String kind;
  final String method;
  final double amount;
  final String transactionStatus;
  final String? providerTransactionId;
  final String? relatedPaymentId;
  final String? paidAt;
  final String createdAt;

  factory AdminPaymentEntry.fromJson(Json json) {
    return AdminPaymentEntry(
      paymentId: json['paymentId'] as String? ?? '',
      kind: json['kind'] as String? ?? '',
      method: json['method'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0.0,
      transactionStatus: json['transactionStatus'] as String? ?? '',
      providerTransactionId: json['providerTransactionId'] as String?,
      relatedPaymentId: json['relatedPaymentId'] as String?,
      paidAt: json['paidAt'] as String?,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminBookingStatusHistory {
  const AdminBookingStatusHistory({
    required this.id,
    this.fromStatus,
    required this.toStatus,
    this.changedByUserId,
    this.reason,
    required this.createdAt,
  });

  final String id;
  final String? fromStatus;
  final String toStatus;
  final String? changedByUserId;
  final String? reason;
  final String createdAt;

  factory AdminBookingStatusHistory.fromJson(Json json) {
    return AdminBookingStatusHistory(
      id: json['id'] as String? ?? '',
      fromStatus: json['fromStatus'] as String?,
      toStatus: json['toStatus'] as String? ?? '',
      changedByUserId: json['changedByUserId'] as String?,
      reason: json['reason'] as String?,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminCheckInDetail {
  const AdminCheckInDetail({
    required this.id,
    required this.staffUserId,
    required this.checkedInAt,
    required this.outstandingPaymentOverride,
    this.overrideReason,
  });

  final String id;
  final String staffUserId;
  final String checkedInAt;
  final bool outstandingPaymentOverride;
  final String? overrideReason;

  factory AdminCheckInDetail.fromJson(Json json) {
    return AdminCheckInDetail(
      id: json['id'] as String? ?? '',
      staffUserId: json['staffUserId'] as String? ?? '',
      checkedInAt: json['checkedInAt'] as String? ?? '',
      outstandingPaymentOverride:
          json['outstandingPaymentOverride'] as bool? ?? false,
      overrideReason: json['overrideReason'] as String?,
    );
  }
}

class AdminCancellationRequestItem {
  const AdminCancellationRequestItem({
    required this.id,
    required this.status,
    required this.reason,
    required this.calculatedRefundAmount,
    this.approvedRefundAmount,
    required this.createdAt,
    this.processedAt,
  });

  final String id;
  final String status;
  final String reason;
  final double calculatedRefundAmount;
  final double? approvedRefundAmount;
  final String createdAt;
  final String? processedAt;

  factory AdminCancellationRequestItem.fromJson(Json json) {
    return AdminCancellationRequestItem(
      id: json['id'] as String? ?? '',
      status: json['status'] as String? ?? '',
      reason: json['reason'] as String? ?? '',
      calculatedRefundAmount:
          (json['calculatedRefundAmount'] as num?)?.toDouble() ?? 0.0,
      approvedRefundAmount:
          (json['approvedRefundAmount'] as num?)?.toDouble(),
      createdAt: json['createdAt'] as String? ?? '',
      processedAt: json['processedAt'] as String?,
    );
  }
}

class AdminBookingReviewItem {
  const AdminBookingReviewItem({
    required this.id,
    required this.rating,
    this.comment,
    required this.createdAt,
  });

  final String id;
  final int rating;
  final String? comment;
  final String createdAt;

  factory AdminBookingReviewItem.fromJson(Json json) {
    return AdminBookingReviewItem(
      id: json['id'] as String? ?? '',
      rating: (json['rating'] as num?)?.toInt() ?? 5,
      comment: json['comment'] as String?,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

class AdminBookingDetail {
  const AdminBookingDetail({
    required this.bookingId,
    required this.bookingCode,
    required this.source,
    required this.bookingStatus,
    required this.paymentStatus,
    required this.customer,
    required this.facility,
    required this.startAt,
    required this.endAt,
    required this.durationMinutes,
    required this.slots,
    required this.financialSummary,
    required this.payments,
    required this.statusHistories,
    this.checkIn,
    required this.cancellationRequests,
    this.review,
    required this.createdByUserId,
    required this.createdAt,
  });

  final String bookingId;
  final String bookingCode;
  final String source;
  final String bookingStatus;
  final String paymentStatus;
  final AdminCustomerSnapshot customer;
  final AdminFacilitySnapshot facility;
  final String startAt;
  final String endAt;
  final int durationMinutes;
  final List<AdminBookingSlotItem> slots;
  final AdminFinancialSummary financialSummary;
  final List<AdminPaymentEntry> payments;
  final List<AdminBookingStatusHistory> statusHistories;
  final AdminCheckInDetail? checkIn;
  final List<AdminCancellationRequestItem> cancellationRequests;
  final AdminBookingReviewItem? review;
  final String createdByUserId;
  final String createdAt;

  factory AdminBookingDetail.fromJson(Json json) {
    return AdminBookingDetail(
      bookingId: json['bookingId'] as String? ?? '',
      bookingCode: json['bookingCode'] as String? ?? '',
      source: json['source'] as String? ?? '',
      bookingStatus: json['bookingStatus'] as String? ?? '',
      paymentStatus: json['paymentStatus'] as String? ?? '',
      customer: AdminCustomerSnapshot.fromJson(
          json['customer'] as Json? ?? const {}),
      facility: AdminFacilitySnapshot.fromJson(
          json['facility'] as Json? ?? const {}),
      startAt: json['startAt'] as String? ?? '',
      endAt: json['endAt'] as String? ?? '',
      durationMinutes: (json['durationMinutes'] as num?)?.toInt() ?? 0,
      slots: (json['slots'] as List<dynamic>? ?? [])
          .map((e) => AdminBookingSlotItem.fromJson(e as Json))
          .toList(),
      financialSummary: AdminFinancialSummary.fromJson(
          json['financialSummary'] as Json? ?? const {}),
      payments: (json['payments'] as List<dynamic>? ?? [])
          .map((e) => AdminPaymentEntry.fromJson(e as Json))
          .toList(),
      statusHistories: (json['statusHistories'] as List<dynamic>? ?? [])
          .map((e) => AdminBookingStatusHistory.fromJson(e as Json))
          .toList(),
      checkIn: json['checkIn'] != null
          ? AdminCheckInDetail.fromJson(json['checkIn'] as Json)
          : null,
      cancellationRequests:
          (json['cancellationRequests'] as List<dynamic>? ?? [])
              .map((e) => AdminCancellationRequestItem.fromJson(e as Json))
              .toList(),
      review: json['review'] != null
          ? AdminBookingReviewItem.fromJson(json['review'] as Json)
          : null,
      createdByUserId: json['createdByUserId'] as String? ?? '',
      createdAt: json['createdAt'] as String? ?? '',
    );
  }
}

// ==========================================
// Cancellation Requests
// ==========================================

class AdminCancellationRequestSummary {
  const AdminCancellationRequestSummary({
    required this.requestId,
    required this.bookingId,
    required this.bookingCode,
    required this.status,
    required this.reason,
    required this.calculatedRefundAmount,
    this.approvedRefundAmount,
    required this.customerNameSnapshot,
    required this.customerPhoneSnapshot,
    required this.centerNameSnapshot,
    required this.courtNameSnapshot,
    required this.sportNameSnapshot,
    required this.startAt,
    required this.endAt,
    required this.createdAt,
    this.processedAt,
  });

  final String requestId;
  final String bookingId;
  final String bookingCode;
  final String status;
  final String reason;
  final double calculatedRefundAmount;
  final double? approvedRefundAmount;
  final String customerNameSnapshot;
  final String customerPhoneSnapshot;
  final String centerNameSnapshot;
  final String courtNameSnapshot;
  final String sportNameSnapshot;
  final String startAt;
  final String endAt;
  final String createdAt;
  final String? processedAt;

  factory AdminCancellationRequestSummary.fromJson(Json json) {
    return AdminCancellationRequestSummary(
      requestId: json['requestId'] as String? ?? '',
      bookingId: json['bookingId'] as String? ?? '',
      bookingCode: json['bookingCode'] as String? ?? '',
      status: json['status'] as String? ?? '',
      reason: json['reason'] as String? ?? '',
      calculatedRefundAmount:
          (json['calculatedRefundAmount'] as num?)?.toDouble() ?? 0.0,
      approvedRefundAmount:
          (json['approvedRefundAmount'] as num?)?.toDouble(),
      customerNameSnapshot: json['customerNameSnapshot'] as String? ?? '',
      customerPhoneSnapshot: json['customerPhoneSnapshot'] as String? ?? '',
      centerNameSnapshot: json['centerNameSnapshot'] as String? ?? '',
      courtNameSnapshot: json['courtNameSnapshot'] as String? ?? '',
      sportNameSnapshot: json['sportNameSnapshot'] as String? ?? '',
      startAt: json['startAt'] as String? ?? '',
      endAt: json['endAt'] as String? ?? '',
      createdAt: json['createdAt'] as String? ?? '',
      processedAt: json['processedAt'] as String?,
    );
  }
}

class AdminCancellationRequestDetail {
  const AdminCancellationRequestDetail({
    required this.requestId,
    required this.requestStatus,
    required this.reason,
    required this.createdAt,
    this.processedAt,
    this.processedByUserId,
    required this.bookingId,
    required this.bookingCode,
    required this.bookingStatus,
    required this.customer,
    required this.facility,
    required this.startAt,
    required this.endAt,
    required this.totalAmount,
    required this.paidAmount,
    required this.calculatedRefundAmount,
    this.approvedRefundAmount,
    this.policyName,
    this.matchedRefundPercent,
    required this.financialSummary,
  });

  final String requestId;
  final String requestStatus;
  final String reason;
  final String createdAt;
  final String? processedAt;
  final String? processedByUserId;
  final String bookingId;
  final String bookingCode;
  final String bookingStatus;
  final AdminCustomerSnapshot customer;
  final AdminFacilitySnapshot facility;
  final String startAt;
  final String endAt;
  final double totalAmount;
  final double paidAmount;
  final double calculatedRefundAmount;
  final double? approvedRefundAmount;
  final String? policyName;
  final double? matchedRefundPercent;
  final AdminFinancialSummary financialSummary;

  factory AdminCancellationRequestDetail.fromJson(Json json) {
    final pol = json['policy'] as Json?;
    final pay = json['paymentSummary'] as Json?;
    return AdminCancellationRequestDetail(
      requestId: json['requestId'] as String? ?? '',
      requestStatus: json['requestStatus'] as String? ?? '',
      reason: json['reason'] as String? ?? '',
      createdAt: json['createdAt'] as String? ?? '',
      processedAt: json['processedAt'] as String?,
      processedByUserId: json['processedByUserId'] as String?,
      bookingId: json['bookingId'] as String? ?? '',
      bookingCode: json['bookingCode'] as String? ?? '',
      bookingStatus: json['bookingStatus'] as String? ?? '',
      customer: AdminCustomerSnapshot.fromJson(
        {
          'customerName': (json['customer'] as Json?)?['name'] ?? '',
          'customerPhone': (json['customer'] as Json?)?['phone'] ?? '',
          'customerEmail': (json['customer'] as Json?)?['email'],
          'customerUserId': (json['customer'] as Json?)?['customerUserId'],
        },
      ),
      facility: AdminFacilitySnapshot.fromJson(
        {
          'sportCenterName':
              (json['facility'] as Json?)?['centerName'] ?? '',
          'courtName': (json['facility'] as Json?)?['courtName'] ?? '',
          'sportName': (json['facility'] as Json?)?['sportName'] ?? '',
          'sportCenterId': '',
          'courtId': '',
          'sportId': '',
        },
      ),
      startAt: json['startAt'] as String? ?? '',
      endAt: json['endAt'] as String? ?? '',
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      paidAmount: (json['paidAmount'] as num?)?.toDouble() ?? 0.0,
      calculatedRefundAmount:
          (json['calculatedRefundAmount'] as num?)?.toDouble() ?? 0.0,
      approvedRefundAmount:
          (json['approvedRefundAmount'] as num?)?.toDouble(),
      policyName: pol?['policyName'] as String?,
      matchedRefundPercent:
          (pol?['matchedRefundPercent'] as num?)?.toDouble(),
      financialSummary: AdminFinancialSummary(
        totalAmount: (pay?['totalAmount'] as num?)?.toDouble() ?? 0.0,
        depositAmount: 0,
        paidAmount: (pay?['paidAmount'] as num?)?.toDouble() ?? 0.0,
        remainingAmount: 0,
        refundedAmount: (pay?['refundedAmount'] as num?)?.toDouble() ?? 0.0,
        paymentStatus: pay?['paymentStatus'] as String? ?? '',
      ),
    );
  }
}

// ==========================================
// Reports
// ==========================================

class AdminReportSummaryData {
  const AdminReportSummaryData({
    required this.dateFrom,
    required this.dateTo,
    this.centerId,
    required this.totalBookings,
    required this.completedBookings,
    required this.cancelledBookings,
    required this.noShowBookings,
    required this.grossCollected,
    required this.refundAmount,
    required this.netRevenue,
    required this.averageBookingValue,
    required this.occupancyRate,
  });

  final String dateFrom;
  final String dateTo;
  final String? centerId;
  final int totalBookings;
  final int completedBookings;
  final int cancelledBookings;
  final int noShowBookings;
  final double grossCollected;
  final double refundAmount;
  final double netRevenue;
  final double averageBookingValue;
  final double occupancyRate;

  factory AdminReportSummaryData.fromJson(Json json) {
    return AdminReportSummaryData(
      dateFrom: json['dateFrom'] as String? ?? '',
      dateTo: json['dateTo'] as String? ?? '',
      centerId: json['centerId'] as String?,
      totalBookings: (json['totalBookings'] as num?)?.toInt() ?? 0,
      completedBookings: (json['completedBookings'] as num?)?.toInt() ?? 0,
      cancelledBookings: (json['cancelledBookings'] as num?)?.toInt() ?? 0,
      noShowBookings: (json['noShowBookings'] as num?)?.toInt() ?? 0,
      grossCollected: (json['grossCollected'] as num?)?.toDouble() ?? 0.0,
      refundAmount: (json['refundAmount'] as num?)?.toDouble() ?? 0.0,
      netRevenue: (json['netRevenue'] as num?)?.toDouble() ?? 0.0,
      averageBookingValue:
          (json['averageBookingValue'] as num?)?.toDouble() ?? 0.0,
      occupancyRate: (json['occupancyRate'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class AdminRevenueSeriesItem {
  const AdminRevenueSeriesItem({
    required this.key,
    required this.label,
    required this.grossCollected,
    required this.refundAmount,
    required this.netRevenue,
    required this.transactionCount,
  });

  final String key;
  final String label;
  final double grossCollected;
  final double refundAmount;
  final double netRevenue;
  final int transactionCount;

  factory AdminRevenueSeriesItem.fromJson(Json json) {
    return AdminRevenueSeriesItem(
      key: json['key'] as String? ?? '',
      label: json['label'] as String? ?? '',
      grossCollected: (json['grossCollected'] as num?)?.toDouble() ?? 0.0,
      refundAmount: (json['refundAmount'] as num?)?.toDouble() ?? 0.0,
      netRevenue: (json['netRevenue'] as num?)?.toDouble() ?? 0.0,
      transactionCount: (json['transactionCount'] as num?)?.toInt() ?? 0,
    );
  }
}

class AdminRevenueReportData {
  const AdminRevenueReportData({
    required this.dateFrom,
    required this.dateTo,
    this.centerId,
    required this.groupBy,
    required this.grossCollected,
    required this.refundAmount,
    required this.netRevenue,
    required this.items,
  });

  final String dateFrom;
  final String dateTo;
  final String? centerId;
  final String groupBy;
  final double grossCollected;
  final double refundAmount;
  final double netRevenue;
  final List<AdminRevenueSeriesItem> items;

  factory AdminRevenueReportData.fromJson(Json json) {
    return AdminRevenueReportData(
      dateFrom: json['dateFrom'] as String? ?? '',
      dateTo: json['dateTo'] as String? ?? '',
      centerId: json['centerId'] as String?,
      groupBy: json['groupBy'] as String? ?? 'day',
      grossCollected: (json['grossCollected'] as num?)?.toDouble() ?? 0.0,
      refundAmount: (json['refundAmount'] as num?)?.toDouble() ?? 0.0,
      netRevenue: (json['netRevenue'] as num?)?.toDouble() ?? 0.0,
      items: (json['items'] as List<dynamic>? ?? [])
          .map((e) => AdminRevenueSeriesItem.fromJson(e as Json))
          .toList(),
    );
  }
}

class AdminCategoryCountItem {
  const AdminCategoryCountItem({
    required this.key,
    required this.label,
    required this.count,
  });

  final String key;
  final String label;
  final int count;

  factory AdminCategoryCountItem.fromJson(Json json) {
    return AdminCategoryCountItem(
      key: json['key'] as String? ?? '',
      label: json['label'] as String? ?? '',
      count: (json['count'] as num?)?.toInt() ?? 0,
    );
  }
}

class AdminBookingTrendItem {
  const AdminBookingTrendItem({
    required this.date,
    required this.totalBookings,
    required this.cancelledBookings,
    required this.noShowBookings,
  });

  final String date;
  final int totalBookings;
  final int cancelledBookings;
  final int noShowBookings;

  factory AdminBookingTrendItem.fromJson(Json json) {
    return AdminBookingTrendItem(
      date: json['date'] as String? ?? '',
      totalBookings: (json['totalBookings'] as num?)?.toInt() ?? 0,
      cancelledBookings: (json['cancelledBookings'] as num?)?.toInt() ?? 0,
      noShowBookings: (json['noShowBookings'] as num?)?.toInt() ?? 0,
    );
  }
}

class AdminBookingReportData {
  const AdminBookingReportData({
    required this.dateFrom,
    required this.dateTo,
    this.centerId,
    required this.totalBookings,
    required this.cancelledCount,
    required this.noShowCount,
    required this.byStatus,
    required this.bySport,
    required this.byCenter,
    required this.trends,
  });

  final String dateFrom;
  final String dateTo;
  final String? centerId;
  final int totalBookings;
  final int cancelledCount;
  final int noShowCount;
  final List<AdminCategoryCountItem> byStatus;
  final List<AdminCategoryCountItem> bySport;
  final List<AdminCategoryCountItem> byCenter;
  final List<AdminBookingTrendItem> trends;

  factory AdminBookingReportData.fromJson(Json json) {
    return AdminBookingReportData(
      dateFrom: json['dateFrom'] as String? ?? '',
      dateTo: json['dateTo'] as String? ?? '',
      centerId: json['centerId'] as String?,
      totalBookings: (json['totalBookings'] as num?)?.toInt() ?? 0,
      cancelledCount: (json['cancelledCount'] as num?)?.toInt() ?? 0,
      noShowCount: (json['noShowCount'] as num?)?.toInt() ?? 0,
      byStatus: (json['byStatus'] as List<dynamic>? ?? [])
          .map((e) => AdminCategoryCountItem.fromJson(e as Json))
          .toList(),
      bySport: (json['bySport'] as List<dynamic>? ?? [])
          .map((e) => AdminCategoryCountItem.fromJson(e as Json))
          .toList(),
      byCenter: (json['byCenter'] as List<dynamic>? ?? [])
          .map((e) => AdminCategoryCountItem.fromJson(e as Json))
          .toList(),
      trends: (json['trends'] as List<dynamic>? ?? [])
          .map((e) => AdminBookingTrendItem.fromJson(e as Json))
          .toList(),
    );
  }
}

class AdminCourtOccupancyItem {
  const AdminCourtOccupancyItem({
    required this.courtId,
    required this.courtName,
    required this.sportName,
    required this.bookableMinutes,
    required this.occupiedMinutes,
    required this.occupancyRate,
  });

  final String courtId;
  final String courtName;
  final String sportName;
  final int bookableMinutes;
  final int occupiedMinutes;
  final double occupancyRate;

  factory AdminCourtOccupancyItem.fromJson(Json json) {
    return AdminCourtOccupancyItem(
      courtId: json['courtId'] as String? ?? '',
      courtName: json['courtName'] as String? ?? '',
      sportName: json['sportName'] as String? ?? '',
      bookableMinutes: (json['bookableMinutes'] as num?)?.toInt() ?? 0,
      occupiedMinutes: (json['occupiedMinutes'] as num?)?.toInt() ?? 0,
      occupancyRate: (json['occupancyRate'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class AdminDayOccupancyItem {
  const AdminDayOccupancyItem({
    required this.date,
    required this.bookableMinutes,
    required this.occupiedMinutes,
    required this.occupancyRate,
  });

  final String date;
  final int bookableMinutes;
  final int occupiedMinutes;
  final double occupancyRate;

  factory AdminDayOccupancyItem.fromJson(Json json) {
    return AdminDayOccupancyItem(
      date: json['date'] as String? ?? '',
      bookableMinutes: (json['bookableMinutes'] as num?)?.toInt() ?? 0,
      occupiedMinutes: (json['occupiedMinutes'] as num?)?.toInt() ?? 0,
      occupancyRate: (json['occupancyRate'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class AdminOccupancyReportData {
  const AdminOccupancyReportData({
    required this.dateFrom,
    required this.dateTo,
    this.centerId,
    required this.overallOccupancyRate,
    required this.totalBookableMinutes,
    required this.occupiedMinutes,
    required this.byCourt,
    required this.byDay,
  });

  final String dateFrom;
  final String dateTo;
  final String? centerId;
  final double overallOccupancyRate;
  final int totalBookableMinutes;
  final int occupiedMinutes;
  final List<AdminCourtOccupancyItem> byCourt;
  final List<AdminDayOccupancyItem> byDay;

  factory AdminOccupancyReportData.fromJson(Json json) {
    return AdminOccupancyReportData(
      dateFrom: json['dateFrom'] as String? ?? '',
      dateTo: json['dateTo'] as String? ?? '',
      centerId: json['centerId'] as String?,
      overallOccupancyRate:
          (json['overallOccupancyRate'] as num?)?.toDouble() ?? 0.0,
      totalBookableMinutes:
          (json['totalBookableMinutes'] as num?)?.toInt() ?? 0,
      occupiedMinutes: (json['occupiedMinutes'] as num?)?.toInt() ?? 0,
      byCourt: (json['byCourt'] as List<dynamic>? ?? [])
          .map((e) => AdminCourtOccupancyItem.fromJson(e as Json))
          .toList(),
      byDay: (json['byDay'] as List<dynamic>? ?? [])
          .map((e) => AdminDayOccupancyItem.fromJson(e as Json))
          .toList(),
    );
  }
}
