import '../../../core/constants/api_constants.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/network_result.dart';

class VnPayService {
  const VnPayService(this._api);

  final ApiClient _api;

  Future<NetworkResult<Map<String, dynamic>>> create(String bookingId) =>
      _api.post<Map<String, dynamic>>(
        '${ApiConstants.payments}/vnpay/create',
        body: {'bookingId': bookingId},
      );

  Future<NetworkResult<Map<String, dynamic>>> status(String paymentId) =>
      _api.get<Map<String, dynamic>>('${ApiConstants.payments}/$paymentId');
}
