import 'package:flutter_test/flutter_test.dart';
import 'package:courtgo_mobile/core/routing/route_guard.dart';
import 'package:courtgo_mobile/core/routing/route_names.dart';
import 'package:courtgo_mobile/shared/models/user_model.dart';
import 'package:courtgo_mobile/features/admin/shared/admin_models.dart';

void main() {
  group('Admin Models & Deserialization', () {
    test('AdminDashboardData parses KPIs and revenue correctly', () {
      final json = {
        'date': '2026-10-09',
        'timeZoneId': 'SE Asia Standard Time',
        'centerId': null,
        'totalBookings': 42,
        'pendingBookings': 5,
        'confirmedBookings': 15,
        'checkedInBookings': 8,
        'inProgressBookings': 4,
        'completedBookings': 10,
        'cancelledBookings': 2,
        'noShowBookings': 1,
        'todayRevenue': 8500000.0,
        'refundAmount': 200000.0,
        'activeCenters': 3,
        'activeCourts': 18,
        'occupancyRate': 0.725,
        'upcomingBookings': 8,
        'pendingCancellationRequests': 2,
      };

      final data = AdminDashboardData.fromJson(json);

      expect(data.date, '2026-10-09');
      expect(data.totalBookings, 42);
      expect(data.todayRevenue, 8500000.0);
      expect(data.activeCenters, 3);
      expect(data.activeCourts, 18);
      expect(data.occupancyRate, 0.725);
    });

    test('AdminStaffDetail parses assignment history and preservation', () {
      final json = {
        'staffId': 'usr-staff-1',
        'fullName': 'Nguyễn Văn Nhân Viên',
        'email': 'staff@courtgo.vn',
        'phoneNumber': '0912345678',
        'isActive': true,
        'assignedCenter': {
          'centerId': 'ctr-1',
          'centerName': 'CourtGo Landmark 81',
          'assignedAt': '2026-01-15T08:00:00Z',
        },
        'assignmentHistory': [
          {
            'assignmentId': 'asg-1',
            'centerId': 'ctr-1',
            'centerName': 'CourtGo Landmark 81',
            'assignedAt': '2026-01-15T08:00:00Z',
            'isActive': true,
          },
          {
            'assignmentId': 'asg-0',
            'centerId': 'ctr-2',
            'centerName': 'CourtGo Thảo Điền',
            'assignedAt': '2025-06-01T08:00:00Z',
            'isActive': false,
          },
        ],
        'createdAt': '2025-06-01T08:00:00Z',
      };

      final staff = AdminStaffDetail.fromJson(json);

      expect(staff.staffId, 'usr-staff-1');
      expect(staff.id, 'usr-staff-1');
      expect(staff.fullName, 'Nguyễn Văn Nhân Viên');
      expect(staff.assignedCenter?.centerName, 'CourtGo Landmark 81');
      expect(staff.assignmentHistory.length, 2);
      expect(staff.assignmentHistory.first.isActive, true);
      expect(staff.assignmentHistory.last.isActive, false);
    });

    test('AdminSportCenterItem & Detail expose id and court counts', () {
      final itemJson = {
        'centerId': 'ctr-101',
        'name': 'CourtGo Sala',
        'address': '10 Mai Chí Thọ',
        'city': 'TP. Hồ Chí Minh',
        'district': 'Thành phố Thủ Đức',
        'timeZoneId': 'SE Asia Standard Time',
        'status': 'Active',
        'courtCount': 8,
        'activeCourtCount': 7,
        'createdAt': '2026-01-01T00:00:00Z',
      };

      final center = AdminSportCenterItem.fromJson(itemJson);

      expect(center.centerId, 'ctr-101');
      expect(center.id, 'ctr-101');
      expect(center.isActive, true);
      expect(center.courtCount, 8);
      expect(center.activeCourtCount, 7);
    });

    test('SystemSettingsData parses operational parameters accurately', () {
      final json = {
        'holdDurationMinutes': 10,
        'minBookingLeadMinutes': 30,
        'defaultDepositPercent': 50.0,
        'allowOutstandingCheckIn': true,
        'updatedAt': '2026-10-01T10:00:00Z',
      };

      final settings = SystemSettingsData.fromJson(json);

      expect(settings.holdDurationMinutes, 10);
      expect(settings.minBookingLeadMinutes, 30);
      expect(settings.defaultDepositPercent, 50.0);
      expect(settings.allowOutstandingCheckIn, true);
    });

    test('AdminOccupancyReportData parses and labels capacity correctly', () {
      final json = {
        'dateFrom': '2026-10-01',
        'dateTo': '2026-10-07',
        'overallOccupancyRate': 0.812,
        'totalBookableMinutes': 50400,
        'occupiedMinutes': 40924,
        'byCourt': [
          {
            'courtId': 'crt-1',
            'courtName': 'Sân Tennis Vip 1',
            'sportName': 'Tennis',
            'bookableMinutes': 5040,
            'occupiedMinutes': 4100,
            'occupancyRate': 0.813,
          }
        ],
        'byDay': [
          {
            'date': '2026-10-01',
            'bookableMinutes': 7200,
            'occupiedMinutes': 6000,
            'occupancyRate': 0.833,
          }
        ],
      };

      final occupancy = AdminOccupancyReportData.fromJson(json);

      expect(occupancy.overallOccupancyRate, 0.812);
      expect(occupancy.totalBookableMinutes, 50400);
      expect(occupancy.occupiedMinutes, 40924);
      expect(occupancy.byCourt.first.courtName, 'Sân Tennis Vip 1');
      expect(occupancy.byDay.first.occupancyRate, 0.833);
    });
  });

  group('RouteGuard Admin Routing Enforcement', () {
    test('Admin home route is /admin/home', () {
      expect(RouteGuard.homeFor(UserRole.admin), RouteNames.adminHome);
    });

    test('Admin user is permitted in /admin paths', () {
      final result = RouteGuard.redirect(
        location: '/admin/management',
        role: UserRole.admin,
      );
      expect(result, isNull);
    });

    test('Staff and Customer are blocked from /admin', () {
      final staffRedirect = RouteGuard.redirect(
        location: '/admin/management',
        role: UserRole.staff,
      );
      expect(staffRedirect, RouteNames.staffHome);

      final customerRedirect = RouteGuard.redirect(
        location: '/admin/management',
        role: UserRole.customer,
      );
      expect(customerRedirect, RouteNames.customerHome);

      final guestRedirect = RouteGuard.redirect(
        location: '/admin/management',
        role: null,
      );
      expect(guestRedirect, RouteNames.login);
    });

    test('Admin user visiting /customer is redirected to admin home', () {
      final redirect = RouteGuard.redirect(
        location: '/customer/explore',
        role: UserRole.admin,
      );
      expect(redirect, RouteNames.adminHome);
    });
  });
}
