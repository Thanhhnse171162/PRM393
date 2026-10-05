class DateTimeUtils {
  const DateTimeUtils._();

  static String _two(int n) => n.toString().padLeft(2, '0');

  /// "17:00"
  static String formatTime(DateTime dt) => '${_two(dt.hour)}:${_two(dt.minute)}';

  /// "05/10/2026"
  static String formatDate(DateTime dt) =>
      '${_two(dt.day)}/${_two(dt.month)}/${dt.year}';

  /// "17:00 - 19:00" for a booking range.
  static String formatRange(DateTime start, DateTime end) =>
      '${formatTime(start)} - ${formatTime(end)}';

  /// Strips the time part.
  static DateTime dateOnly(DateTime dt) => DateTime(dt.year, dt.month, dt.day);
}
