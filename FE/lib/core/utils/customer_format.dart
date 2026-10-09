class CustomerFormat {
  static String day(DateTime date) =>
      '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
  // Display the offset-local time in the API payload, never the emulator's timezone.
  static String time(String iso) =>
      iso.length >= 16 ? iso.substring(11, 16) : iso;
  static String date(String iso) {
    if (iso.length < 10) return iso;
    return '${iso.substring(8, 10)}/${iso.substring(5, 7)}/${iso.substring(0, 4)}';
  }

  static String interval(String start, String end) =>
      '${time(start)} – ${time(end)}';
}
