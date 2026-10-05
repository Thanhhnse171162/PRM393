/// Runtime environments supported by the app.
enum Environment {
  dev,
  staging,
  prod;

  static Environment fromName(String name) {
    return Environment.values.firstWhere(
      (e) => e.name == name,
      orElse: () => Environment.dev,
    );
  }
}
