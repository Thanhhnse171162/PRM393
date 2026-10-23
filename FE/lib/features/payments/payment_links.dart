String? paymentBookingFromLink(Uri uri) {
  if (uri.scheme != 'courtgo' ||
      uri.host != 'payment-result' ||
      (uri.path.isNotEmpty && uri.path != '/') ||
      uri.userInfo.isNotEmpty) {
    return null;
  }
  final ids = uri.queryParametersAll['bookingId'];
  if (ids == null ||
      ids.length != 1 ||
      !RegExp(
        r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
      ).hasMatch(ids.single)) {
    return null;
  }
  return ids.single;
}

bool trustedCheckoutUrl(Uri uri) =>
    uri.scheme == 'https' &&
    uri.userInfo.isEmpty &&
    (uri.host == 'momo.vn' ||
        uri.host.endsWith('.momo.vn') ||
        uri.host == 'sandbox.vnpayment.vn' ||
        uri.host == 'pay.vnpay.vn');
