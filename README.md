# CourtGo

**CourtGo – Sport Court Booking**: a mobile application for booking sports courts across multiple CourtGo branches (sport centers).

- **Course:** PRM393 – Mobile Application Development
- **Sports:** Cầu lông, Pickleball, Bóng đá, Bóng rổ, Tennis
- **Business model:** ONE business (CourtGo) with many branches; each branch has many courts.

## Technology stack

| Layer | Technology |
|-------|-----------|
| Mobile (FE) | Flutter / Dart, Material 3, Provider, Dio, go_router, flutter_secure_storage, shared_preferences |
| API (BE) | C# · ASP.NET Core Web API (.NET 8), Entity Framework Core, JWT Bearer, Swagger |
| Database | SQL Server |
| CI | GitHub Actions |

## Architecture

```
Flutter app  ──HTTP/JSON──▶  ASP.NET Core Web API
                              ├─ Application   (use cases, DTOs, interfaces)
                              ├─ Domain        (entities, enums, business rules)
                              └─ Infrastructure (EF Core, JWT, hashing) ──▶ SQL Server
```

Modular monolith, Clean-Architecture-inspired (no microservices). Details: [docs/architecture.md](docs/architecture.md).

## Folder structure

```
PRM393_Project/
├── BE/                     ASP.NET Core backend
│   ├── CourtGo.sln
│   ├── src/  CourtGo.Api · CourtGo.Application · CourtGo.Domain · CourtGo.Infrastructure
│   └── tests/ CourtGo.UnitTests · CourtGo.IntegrationTests
├── FE/                     THE Flutter project (one app: Customer + Staff + Admin)
│   ├── lib/core · lib/shared · lib/features/{auth,location,customer,staff,admin}
│   ├── test/unit · test/widget
│   └── assets/
├── docs/                   SRS, architecture, business rules, API contract, DB, UI flows, test plan
├── .github/                CI workflows + PR template
├── .gitignore  .editorconfig  README.md
```

## User roles

| Role | Summary |
|------|---------|
| Guest | Browse centers/courts/availability without login. Login required to book. |
| **CUSTOMER** | Search, choose area, filter by sport, select 1-hour slots, book, pay deposit, history, QR, notifications. |
| **STAFF** | Belongs to ONE branch. Dashboard, today's bookings, QR check-in, collect remaining payment, court status, walk-in booking. Not publicly registered. |
| **ADMIN** | Manage branches, courts, sports, staff accounts/assignments, prices, opening hours, all bookings, refunds, reports, configuration. |

After login the app routes to `/customer/home`, `/staff/home` or `/admin/home`.

## Requirements

- Flutter SDK (stable, Dart ≥ 3.13) – developed with Flutter 3.47.x
- .NET SDK 8 or newer (the projects target `net8.0`; the .NET 8 runtime is required to run/test)
- SQL Server (LocalDB, Express, Developer or Docker)
- Android Studio / Xcode, Git

## Setup

### Flutter
```bash
cd FE
flutter pub get
flutter doctor
```

### Backend
```bash
cd BE
dotnet restore
dotnet build
```

### Configure SQL Server and secrets

Never commit real credentials. Pick ONE option:

**Option A – user-secrets (recommended)**
```bash
cd BE/src/CourtGo.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=CourtGoDb;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<at-least-32-random-characters>"
```

**Option B – local settings file (git-ignored)**
Copy `BE/src/CourtGo.Api/appsettings.Development.example.json` to `appsettings.Development.json` and fill your own values.

**Option C – environment variables**
`ConnectionStrings__DefaultConnection`, `Jwt__Key`.

Connection string examples (each teammate uses their own):
- SQL login: `Server=<server>;Database=CourtGoDb;User Id=<user>;Password=<password>;TrustServerCertificate=True`
- Windows auth: `Server=<server>;Database=CourtGoDb;Trusted_Connection=True;TrustServerCertificate=True`

> No database migration exists yet. The first backend feature will add the initial EF Core migration (see [docs/database/README.md](docs/database/README.md)).

## Run

**Backend** (listens on `http://0.0.0.0:5080`, Swagger at `/swagger`):
```bash
cd BE/src/CourtGo.Api
dotnet run
```

**Flutter**:
```bash
cd FE
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
```

### How Flutter connects to the API

The base URL is read from `--dart-define=API_BASE_URL=...` (see `FE/lib/core/config/app_config.dart`).

| Target | Base URL |
|--------|----------|
| Android emulator | `http://10.0.2.2:5080` (default) |
| iOS simulator | `http://localhost:5080` |
| Physical device | `http://<YOUR_PC_LAN_IP>:5080` (same Wi-Fi; allow port 5080 in the firewall) |

## Tests

```bash
cd FE && flutter analyze && flutter test
cd BE && dotnet test
```

## Git workflow

Branches: `main` (stable/release) · `develop` (integration) · `feature/*` · `fix/*`.

Examples: `feature/customer-home`, `feature/customer-booking`, `feature/staff-dashboard`, `feature/staff-checkin`, `feature/admin-management`, `feature/api-auth`, `feature/api-booking`, `fix/booking-conflict`.

**Do NOT develop directly on `main`.**

```
develop → feature/xxx → commit → push → pull request → merge into develop → test → merge release into main
```

## Team workflow

1. `git checkout develop && git pull`
2. `git checkout -b feature/<name>`
3. Small commits (`feat:`, `fix:`, `docs:`, `chore:`, `test:`).
4. Push and open a PR into `develop`; CI (Flutter + Backend) must be green; at least one teammate reviews.
5. Agree on API changes in [docs/api-contract.md](docs/api-contract.md) BEFORE implementing FE/BE.
6. Never commit secrets, `bin/`, `obj/`, `build/`.

## Troubleshooting

- **`flutter test` fails with weird `listener.dart` syntax errors:** your folder path contains an apostrophe (`'`), e.g. `PRM393_Project'`. Rename the folder, or map a drive letter (`subst X: "D:\PRM393_Project'"`) and run from `X:\FE`.
- **App can't reach API on emulator:** use `10.0.2.2`, not `localhost`; confirm the API is running on port 5080.
- **Physical device can't reach API:** use your PC LAN IP and open the firewall port; the API binds to `0.0.0.0`.
- **401 on every request:** `Jwt:Key` not configured or token expired.
- **`SqlException` / login failed:** check your connection string and that SQL Server accepts connections.
- **`Jwt:Key is missing` error:** set it via user-secrets or `Jwt__Key`.
- **.NET runtime not found when testing:** install the .NET 8 runtime.
