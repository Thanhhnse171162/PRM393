typedef Json = Map<String, dynamic>;
List<Json> jsonList(dynamic value) => (value as List? ?? [])
    .map((e) => Map<String, dynamic>.from(e as Map))
    .toList();
double amount(dynamic value) => (value as num?)?.toDouble() ?? 0;

class CenterInfo {
  CenterInfo.fromJson(Json j)
    : id = j['id'],
      name = j['name'],
      district = j['district'] ?? '',
      city = j['city'] ?? '',
      address = [
        j['addressLine'],
        j['ward'],
        j['district'],
        j['city'],
      ].whereType<String>().where((e) => e.isNotEmpty).join(', '),
      image = j['coverImageUrl'],
      sports = List<String>.from(j['sports'] ?? []),
      priceFrom = (j['priceFrom'] as num?)?.toDouble(),
      latitude = (j['latitude'] as num?)?.toDouble(),
      longitude = (j['longitude'] as num?)?.toDouble(),
      hours = jsonList(j['operatingHours']),
      courts = jsonList(j['courts']).map(Court.fromJson).toList(),
      phone = j['phoneNumber'],
      timeZoneId = j['timeZoneId'];
  final String id, name, address, district, city;
  final String? image, phone, timeZoneId;
  final List<String> sports;
  final double? priceFrom, latitude, longitude;
  final List<Json> hours;
  final List<Court> courts;
}

class Court {
  Court.fromJson(Json j)
    : id = j['id'],
      centerId = j['sportCenterId'],
      centerName = j['sportCenterName'],
      sportId = j['sportId'],
      sportName = j['sportName'],
      name = j['name'],
      code = j['code'],
      image = j['coverImageUrl'],
      surface = j['surfaceType'],
      description = j['description'],
      price = amount(j['basePricePerHour']),
      status = j['status'];
  final String id, centerId, centerName, sportId, sportName, name, code, status;
  final String? image, surface, description;
  final double price;
}

class Slot {
  Slot.fromJson(Json j)
    : start = j['startAt'],
      end = j['endAt'],
      price = amount(j['price']),
      status = j['status'] ?? 'Unknown';
  final String start, end, status;
  final double price;
  bool get available => status == 'Available';
}

class Availability {
  Availability.fromJson(Json j)
    : court = Map<String, dynamic>.from(j['court']),
      date = j['date'],
      closed = j['isClosed'],
      slots = jsonList(j['slots']).map(Slot.fromJson).toList();
  final Json court;
  final String date;
  final bool closed;
  final List<Slot> slots;
}

class Booking {
  Booking.fromJson(Json j)
    : id = j['bookingId'],
      code = j['bookingCode'],
      status = j['bookingStatus'],
      paymentStatus = j['paymentStatus'],
      center = j['centerName'] ?? (j['center'] as Map?)?['name'] ?? '',
      court = j['courtName'] ?? (j['court'] as Map?)?['name'] ?? '',
      courtId = (j['court'] as Map?)?['id'],
      sport = j['sportName'] ?? (j['sport'] as Map?)?['name'] ?? '',
      start = j['startAt'],
      end = j['endAt'],
      duration = j['durationMinutes'],
      total = amount(
        j['totalAmount'] ?? (j['payment'] as Map?)?['totalAmount'],
      ),
      deposit = amount(
        j['depositAmount'] ?? (j['payment'] as Map?)?['depositAmount'],
      ),
      paid = amount(j['paidAmount'] ?? (j['payment'] as Map?)?['paidAmount']),
      remaining = amount(
        j['remainingAmount'] ?? (j['payment'] as Map?)?['remainingAmount'],
      ),
      hasQr = j['hasQr'] ?? false,
      expires = j['holdExpiresAt'],
      slots = jsonList(j['slots']).map(Slot.fromJson).toList();
  final String id,
      code,
      status,
      paymentStatus,
      center,
      court,
      sport,
      start,
      end;
  final String? expires, courtId;
  final int duration;
  final double total, deposit, paid, remaining;
  final bool hasQr;
  final List<Slot> slots;
  bool get canCancel => status == 'Confirmed';
  bool get completed => status == 'Completed';
  bool get awaitingPayment => status == 'PendingPayment';
}

class PageResult<T> {
  const PageResult(this.items, this.page, this.totalPages);
  factory PageResult.fromJson(Json j, T Function(Json) decode) => PageResult(
    jsonList(j['items']).map(decode).toList(),
    j['pageNumber'],
    j['totalPages'],
  );
  final List<T> items;
  final int page, totalPages;
}

class CustomerNotification {
  CustomerNotification.fromJson(Json j)
    : id = j['notificationId'],
      type = j['type'],
      title = j['title'],
      message = j['message'],
      read = j['isRead'],
      createdAt = j['createdAt'],
      referenceType = j['referenceType'],
      referenceId = j['referenceId'];
  final String id, type, title, message, createdAt;
  final String? referenceType, referenceId;
  final bool read;
}
