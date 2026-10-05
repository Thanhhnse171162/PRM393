# Database setup

No migration and no real database are included yet. To create one locally (after configuring your connection string, see root README):

```bash
dotnet tool install --global dotnet-ef        # once
cd BE
dotnet ef migrations add InitialCreate -p src/CourtGo.Infrastructure -s src/CourtGo.Api -o Data/Migrations
dotnet ef database update -p src/CourtGo.Infrastructure -s src/CourtGo.Api
```

Rules:
- Each teammate uses a **local** database (e.g. `CourtGoDb`).
- Commit migrations; never commit connection strings or `.mdf` files.
- Pull `develop` and run `dotnet ef database update` after teammates add migrations.

See [database-design.md](database-design.md).
