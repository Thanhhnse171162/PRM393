class CurrencyUtils {
  const CurrencyUtils._();

  /// 240000 -> "240.000đ" (Vietnamese dong, dot as thousands separator).
  static String formatVnd(num amount) {
    final digits = amount.round().abs().toString();
    final buffer = StringBuffer();
    for (var i = 0; i < digits.length; i++) {
      if (i > 0 && (digits.length - i) % 3 == 0) buffer.write('.');
      buffer.write(digits[i]);
    }
    return '${amount < 0 ? '-' : ''}$buffer\u0111';
  }
}
