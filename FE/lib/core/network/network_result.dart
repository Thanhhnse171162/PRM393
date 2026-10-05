import 'api_exception.dart';

/// Simple result wrapper so repositories never throw to the UI.
sealed class NetworkResult<T> {
  const NetworkResult();

  R when<R>({
    required R Function(T data) success,
    required R Function(ApiException error) failure,
  }) {
    final self = this;
    return switch (self) {
      NetworkSuccess<T>() => success(self.data),
      NetworkFailure<T>() => failure(self.error),
    };
  }
}

class NetworkSuccess<T> extends NetworkResult<T> {
  const NetworkSuccess(this.data);
  final T data;
}

class NetworkFailure<T> extends NetworkResult<T> {
  const NetworkFailure(this.error);
  final ApiException error;
}
