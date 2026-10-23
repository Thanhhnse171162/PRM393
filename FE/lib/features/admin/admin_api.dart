import 'package:flutter/foundation.dart';

import '../../core/network/api_client.dart';
import '../../core/network/network_result.dart';
import '../../shared/models/customer_models.dart' hide Json;
import 'shared/admin_models.dart';

class AdminApi extends ChangeNotifier {
  AdminApi(this.client);

  final ApiClient client;
  int revision = 0;

  void notifyChanged() {
    revision++;
    notifyListeners();
  }

  Future<T> _value<T>(Future<NetworkResult<T>> result) async =>
      (await result).when(success: (v) => v, failure: (e) => throw e);

  Future<Json> get(String path, {Json? query}) =>
      _value(client.get<Json>(path, query: query));

  Future<dynamic> getRaw(String path, {Json? query}) =>
      _value(client.get<dynamic>(path, query: query));

  Future<Json> post(String path, Json body) async {
    final result = await _value(client.post<Json>(path, body: body));
    notifyChanged();
    return result;
  }

  Future<Json> put(String path, Json body) async {
    final result = await _value(client.put<Json>(path, body: body));
    notifyChanged();
    return result;
  }

  Future<Json> patch(String path, [Json body = const {}]) async {
    final result = await _value(client.patch<Json>(path, body: body));
    notifyChanged();
    return result;
  }

  Future<void> delete(String path) async {
    (await client.delete<dynamic>(path)).when(
      success: (_) {},
      failure: (e) => throw e,
    );
    notifyChanged();
  }

  // ==========================================
  // Dashboard
  // ==========================================

  Future<AdminDashboardData> getDashboard({
    String? date,
    String? centerId,
  }) async {
    final res = await get(
      '/api/admin/dashboard',
      query: {
        if (date != null && date.isNotEmpty) 'date': date,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
      },
    );
    return AdminDashboardData.fromJson(res);
  }

  // ==========================================
  // Staff Management
  // ==========================================

  Future<PageResult<AdminStaffItem>> getStaffList({
    String? centerId,
    bool? isActive,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await get(
      '/api/admin/staff',
      query: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
        if (isActive != null) 'isActive': isActive,
        if (search != null && search.trim().isNotEmpty)
          'search': search.trim(),
      },
    );
    return PageResult.fromJson(res, AdminStaffItem.fromJson);
  }

  Future<AdminStaffDetail> getStaffDetail(String staffId) async {
    final res = await get('/api/admin/staff/$staffId');
    return AdminStaffDetail.fromJson(res);
  }

  Future<AdminStaffDetail> createStaff({
    required String fullName,
    String? email,
    required String phoneNumber,
    required String password,
    required String sportCenterId,
  }) async {
    final res = await post('/api/admin/staff', {
      'fullName': fullName.trim(),
      if (email != null && email.trim().isNotEmpty) 'email': email.trim(),
      'phoneNumber': phoneNumber.trim(),
      'password': password,
      'sportCenterId': sportCenterId,
    });
    return AdminStaffDetail.fromJson(res);
  }

  Future<AdminStaffDetail> updateStaffProfile(
    String staffId, {
    required String fullName,
    String? email,
    required String phoneNumber,
  }) async {
    final res = await put('/api/admin/staff/$staffId', {
      'fullName': fullName.trim(),
      if (email != null && email.trim().isNotEmpty) 'email': email.trim(),
      'phoneNumber': phoneNumber.trim(),
    });
    return AdminStaffDetail.fromJson(res);
  }

  Future<AdminStaffDetail> updateStaffStatus(
    String staffId,
    bool isActive,
  ) async {
    final res = await patch(
      '/api/admin/staff/$staffId/status',
      {'isActive': isActive},
    );
    return AdminStaffDetail.fromJson(res);
  }

  Future<AdminStaffDetail> reassignStaff(
    String staffId,
    String sportCenterId,
  ) async {
    final res = await put(
      '/api/admin/staff/$staffId/assignment',
      {'sportCenterId': sportCenterId},
    );
    return AdminStaffDetail.fromJson(res);
  }

  // ==========================================
  // Sport Centers
  // ==========================================

  Future<PageResult<AdminSportCenterItem>> getCenters({
    String? status,
    String? city,
    String? district,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await get(
      '/api/admin/sport-centers',
      query: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (status != null && status.isNotEmpty) 'status': status,
        if (city != null && city.isNotEmpty) 'city': city,
        if (district != null && district.isNotEmpty) 'district': district,
        if (search != null && search.trim().isNotEmpty)
          'search': search.trim(),
      },
    );
    return PageResult.fromJson(res, AdminSportCenterItem.fromJson);
  }

  Future<AdminSportCenterDetail> getCenter(String centerId) async {
    final res = await get('/api/admin/sport-centers/$centerId');
    return AdminSportCenterDetail.fromJson(res);
  }

  Future<AdminSportCenterDetail> createCenter(Json body) async {
    final res = await post('/api/admin/sport-centers', body);
    return AdminSportCenterDetail.fromJson(res);
  }

  Future<AdminSportCenterDetail> updateCenter(String centerId, Json body) async {
    final res = await put('/api/admin/sport-centers/$centerId', body);
    return AdminSportCenterDetail.fromJson(res);
  }

  Future<AdminSportCenterDetail> updateCenterStatus(
    String centerId,
    String status,
  ) async {
    final res = await patch(
      '/api/admin/sport-centers/$centerId/status',
      {'status': status},
    );
    return AdminSportCenterDetail.fromJson(res);
  }

  // ==========================================
  // Sports
  // ==========================================

  Future<PageResult<AdminSportItem>> getSports({
    bool? isActive,
    String? search,
    int page = 1,
    int pageSize = 50,
  }) async {
    final res = await get(
      '/api/admin/sports',
      query: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (isActive != null) 'isActive': isActive,
        if (search != null && search.trim().isNotEmpty)
          'search': search.trim(),
      },
    );
    return PageResult.fromJson(res, AdminSportItem.fromJson);
  }

  Future<AdminSportItem> getSport(String sportId) async {
    final res = await get('/api/admin/sports/$sportId');
    return AdminSportItem.fromJson(res);
  }

  Future<AdminSportItem> createSport(Json body) async {
    final res = await post('/api/admin/sports', body);
    return AdminSportItem.fromJson(res);
  }

  Future<AdminSportItem> updateSport(String sportId, Json body) async {
    final res = await put('/api/admin/sports/$sportId', body);
    return AdminSportItem.fromJson(res);
  }

  Future<AdminSportItem> updateSportStatus(
    String sportId,
    bool isActive,
  ) async {
    final res = await patch(
      '/api/admin/sports/$sportId/status',
      {'isActive': isActive},
    );
    return AdminSportItem.fromJson(res);
  }

  // ==========================================
  // Courts
  // ==========================================

  Future<PageResult<AdminCourtItem>> getCourts({
    String? centerId,
    String? sportId,
    String? status,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await get(
      '/api/admin/courts',
      query: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
        if (sportId != null && sportId.isNotEmpty) 'sportId': sportId,
        if (status != null && status.isNotEmpty) 'status': status,
        if (search != null && search.trim().isNotEmpty)
          'search': search.trim(),
      },
    );
    return PageResult.fromJson(res, AdminCourtItem.fromJson);
  }

  Future<AdminCourtDetail> getCourt(String courtId) async {
    final res = await get('/api/admin/courts/$courtId');
    return AdminCourtDetail.fromJson(res);
  }

  Future<AdminCourtDetail> createCourt(Json body) async {
    final res = await post('/api/admin/courts', body);
    return AdminCourtDetail.fromJson(res);
  }

  Future<AdminCourtDetail> updateCourt(String courtId, Json body) async {
    final res = await put('/api/admin/courts/$courtId', body);
    return AdminCourtDetail.fromJson(res);
  }

  Future<AdminCourtDetail> updateCourtStatus(
    String courtId,
    String status,
  ) async {
    final res = await patch(
      '/api/admin/courts/$courtId/status',
      {'status': status},
    );
    return AdminCourtDetail.fromJson(res);
  }

  // ==========================================
  // Operating Hours & Exceptions
  // ==========================================

  Future<AdminCenterOperatingHoursData> getOperatingHours(
    String centerId,
  ) async {
    final res = await get(
      '/api/admin/sport-centers/$centerId/operating-hours',
    );
    return AdminCenterOperatingHoursData.fromJson(res);
  }

  Future<AdminCenterOperatingHoursData> updateOperatingHours(
    String centerId,
    List<AdminOperatingHourItem> days,
  ) async {
    final res = await put(
      '/api/admin/sport-centers/$centerId/operating-hours',
      {'days': days.map((d) => d.toJson()).toList()},
    );
    return AdminCenterOperatingHoursData.fromJson(res);
  }

  Future<List<AdminOperatingHourExceptionItem>> getOperatingHourExceptions(
    String centerId, {
    String? from,
    String? to,
  }) async {
    final raw = await getRaw(
      '/api/admin/sport-centers/$centerId/operating-hour-exceptions',
      query: {
        if (from != null && from.isNotEmpty) 'from': from,
        if (to != null && to.isNotEmpty) 'to': to,
      },
    );
    return jsonList(raw)
        .map(AdminOperatingHourExceptionItem.fromJson)
        .toList();
  }

  Future<AdminOperatingHourExceptionItem> createOperatingHourException(
    String centerId,
    Json body,
  ) async {
    final res = await post(
      '/api/admin/sport-centers/$centerId/operating-hour-exceptions',
      body,
    );
    return AdminOperatingHourExceptionItem.fromJson(res);
  }

  Future<AdminOperatingHourExceptionItem> updateOperatingHourException(
    String centerId,
    String exceptionId,
    Json body,
  ) async {
    final res = await put(
      '/api/admin/sport-centers/$centerId/operating-hour-exceptions/$exceptionId',
      body,
    );
    return AdminOperatingHourExceptionItem.fromJson(res);
  }

  Future<void> deleteOperatingHourException(
    String centerId,
    String exceptionId,
  ) async {
    await delete(
      '/api/admin/sport-centers/$centerId/operating-hour-exceptions/$exceptionId',
    );
  }

  // ==========================================
  // Price Rules
  // ==========================================

  Future<List<AdminPriceRuleItem>> getPriceRules(
    String courtId, {
    String? dayOfWeek,
    bool? isActive,
  }) async {
    final raw = await getRaw(
      '/api/admin/courts/$courtId/price-rules',
      query: {
        if (dayOfWeek != null && dayOfWeek.isNotEmpty) 'dayOfWeek': dayOfWeek,
        if (isActive != null) 'isActive': isActive,
      },
    );
    return jsonList(raw).map(AdminPriceRuleItem.fromJson).toList();
  }

  Future<AdminPriceRuleItem> getPriceRule(
    String courtId,
    String priceRuleId,
  ) async {
    final res = await get(
      '/api/admin/courts/$courtId/price-rules/$priceRuleId',
    );
    return AdminPriceRuleItem.fromJson(res);
  }

  Future<AdminPriceRuleItem> createPriceRule(
    String courtId,
    Json body,
  ) async {
    final res = await post(
      '/api/admin/courts/$courtId/price-rules',
      body,
    );
    return AdminPriceRuleItem.fromJson(res);
  }

  Future<AdminPriceRuleItem> updatePriceRule(
    String courtId,
    String priceRuleId,
    Json body,
  ) async {
    final res = await put(
      '/api/admin/courts/$courtId/price-rules/$priceRuleId',
      body,
    );
    return AdminPriceRuleItem.fromJson(res);
  }

  Future<AdminPriceRuleItem> updatePriceRuleStatus(
    String courtId,
    String priceRuleId,
    bool isActive,
  ) async {
    final res = await patch(
      '/api/admin/courts/$courtId/price-rules/$priceRuleId/status',
      {'isActive': isActive},
    );
    return AdminPriceRuleItem.fromJson(res);
  }

  // ==========================================
  // System Settings
  // ==========================================

  Future<SystemSettingsData> getSystemSettings() async {
    final res = await get('/api/admin/system-settings');
    return SystemSettingsData.fromJson(res);
  }

  Future<SystemSettingsData> updateSystemSettings(Json body) async {
    final res = await put('/api/admin/system-settings', body);
    return SystemSettingsData.fromJson(res);
  }

  // ==========================================
  // Cancellation Policies
  // ==========================================

  Future<List<AdminCancellationPolicySummary>>
      getCancellationPolicies() async {
    final raw = await getRaw('/api/admin/cancellation-policies');
    return jsonList(raw)
        .map(AdminCancellationPolicySummary.fromJson)
        .toList();
  }

  Future<AdminCancellationPolicyDetail> getCancellationPolicy(
    String policyId,
  ) async {
    final res = await get('/api/admin/cancellation-policies/$policyId');
    return AdminCancellationPolicyDetail.fromJson(res);
  }

  Future<AdminCancellationPolicyDetail> createCancellationPolicy(
    Json body,
  ) async {
    final res = await post('/api/admin/cancellation-policies', body);
    return AdminCancellationPolicyDetail.fromJson(res);
  }

  Future<AdminCancellationPolicyDetail> activateCancellationPolicy(
    String policyId,
  ) async {
    final res =
        await post('/api/admin/cancellation-policies/$policyId/activate', {});
    return AdminCancellationPolicyDetail.fromJson(res);
  }

  // ==========================================
  // Bookings
  // ==========================================

  Future<PageResult<AdminBookingItem>> getBookings({
    String? centerId,
    String? sportId,
    String? courtId,
    String? status,
    String? paymentStatus,
    String? dateFrom,
    String? dateTo,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await get(
      '/api/admin/bookings',
      query: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
        if (sportId != null && sportId.isNotEmpty) 'sportId': sportId,
        if (courtId != null && courtId.isNotEmpty) 'courtId': courtId,
        if (status != null && status.isNotEmpty) 'status': status,
        if (paymentStatus != null && paymentStatus.isNotEmpty)
          'paymentStatus': paymentStatus,
        if (dateFrom != null && dateFrom.isNotEmpty) 'dateFrom': dateFrom,
        if (dateTo != null && dateTo.isNotEmpty) 'dateTo': dateTo,
        if (search != null && search.trim().isNotEmpty)
          'search': search.trim(),
      },
    );
    return PageResult.fromJson(res, AdminBookingItem.fromJson);
  }

  Future<AdminBookingDetail> getBooking(String bookingId) async {
    final res = await get('/api/admin/bookings/$bookingId');
    return AdminBookingDetail.fromJson(res);
  }

  Future<Json> cancelBooking(
    String bookingId, {
    required String reason,
    double? refundAmount,
  }) async {
    return post('/api/admin/bookings/$bookingId/cancel', {
      'reason': reason.trim(),
      if (refundAmount != null) 'refundAmount': refundAmount,
    });
  }

  // ==========================================
  // Cancellation Requests
  // ==========================================

  Future<PageResult<AdminCancellationRequestSummary>>
      getCancellationRequests({
    String? status,
    String? centerId,
    String? dateFrom,
    String? dateTo,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await get(
      '/api/admin/cancellation-requests',
      query: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (status != null && status.isNotEmpty) 'status': status,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
        if (dateFrom != null && dateFrom.isNotEmpty) 'dateFrom': dateFrom,
        if (dateTo != null && dateTo.isNotEmpty) 'dateTo': dateTo,
        if (search != null && search.trim().isNotEmpty)
          'search': search.trim(),
      },
    );
    return PageResult.fromJson(
        res, AdminCancellationRequestSummary.fromJson);
  }

  Future<AdminCancellationRequestDetail> getCancellationRequest(
    String requestId,
  ) async {
    final res = await get('/api/admin/cancellation-requests/$requestId');
    return AdminCancellationRequestDetail.fromJson(res);
  }

  Future<Json> decideCancellationRequest(
    String requestId, {
    required String decision,
    double? approvedRefundAmount,
    String? reason,
  }) async {
    return post(
      '/api/admin/cancellation-requests/$requestId/decision',
      {
        'decision': decision,
        if (approvedRefundAmount != null)
          'approvedRefundAmount': approvedRefundAmount,
        if (reason != null && reason.trim().isNotEmpty)
          'reason': reason.trim(),
      },
    );
  }

  // ==========================================
  // Reports
  // ==========================================

  Future<AdminReportSummaryData> getSummaryReport({
    required String dateFrom,
    required String dateTo,
    String? centerId,
  }) async {
    final res = await get(
      '/api/admin/reports/summary',
      query: {
        'dateFrom': dateFrom,
        'dateTo': dateTo,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
      },
    );
    return AdminReportSummaryData.fromJson(res);
  }

  Future<AdminRevenueReportData> getRevenueReport({
    required String dateFrom,
    required String dateTo,
    String? centerId,
    String groupBy = 'day',
  }) async {
    final res = await get(
      '/api/admin/reports/revenue',
      query: {
        'dateFrom': dateFrom,
        'dateTo': dateTo,
        'groupBy': groupBy,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
      },
    );
    return AdminRevenueReportData.fromJson(res);
  }

  Future<AdminBookingReportData> getBookingReport({
    required String dateFrom,
    required String dateTo,
    String? centerId,
  }) async {
    final res = await get(
      '/api/admin/reports/bookings',
      query: {
        'dateFrom': dateFrom,
        'dateTo': dateTo,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
      },
    );
    return AdminBookingReportData.fromJson(res);
  }

  Future<AdminOccupancyReportData> getOccupancyReport({
    required String dateFrom,
    required String dateTo,
    String? centerId,
  }) async {
    final res = await get(
      '/api/admin/reports/occupancy',
      query: {
        'dateFrom': dateFrom,
        'dateTo': dateTo,
        if (centerId != null && centerId.isNotEmpty) 'centerId': centerId,
      },
    );
    return AdminOccupancyReportData.fromJson(res);
  }
}
