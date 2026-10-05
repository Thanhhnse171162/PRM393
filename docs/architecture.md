# Architecture

```
Flutter (FE)
   ↓ HTTP / JSON (Dio, JWT Bearer)
ASP.NET Core Web API   (BE/src/CourtGo.Api)
   ↓
Application            (use cases, DTOs, interfaces, validators)
   ↓
Domain                 (entities, enums, pure business rules)
   ↑ implemented by
Infrastructure / EF Core (DbContext, configurations, JWT, hashing)
   ↓
SQL Server
```

## Backend dependency rule

| Project | Depends on |
|---------|-----------|
| CourtGo.Domain | nothing |
| CourtGo.Application | Domain |
| CourtGo.Infrastructure | Application, Domain |
| CourtGo.Api | Application, Infrastructure |

The Application layer defines interfaces (e.g. `IJwtTokenService`, `IPasswordHasher`); Infrastructure implements them; Api wires everything via DI. Modular monolith: one deployable API, features separated by folders (Auth, Bookings, Payments, ...).

## Frontend structure

```
lib/core      config, constants, theme, routing, network (Dio), storage, utils, shared widgets
lib/shared    models/providers/widgets shared by several roles
lib/features  auth, location, customer/*, staff/*, admin/*
```
State management: **Provider only**. Routing: **go_router** with a bottom-navigation shell per role.

## Role architecture

ONE Flutter app serves all roles.

```
Splash → (location) → Customer/Guest area
Login  → role from JWT → CUSTOMER /customer/home
                       → STAFF    /staff/home
                       → ADMIN    /admin/home
```
- `RouteGuard` (FE) redirects users away from areas of other roles (UX only).
- Backend enforces authorization with `[Authorize(Roles = ...)]` (real security).
- Staff are assigned to ONE branch via `StaffAssignment`; staff endpoints must filter by the assigned branch.
- Guests may browse; creating a booking requires authentication.

## Errors

RFC 7807 problem responses: 400 validation, 401 unauthorized, 403 forbidden, 404 not found, 409 conflict (slot taken), 500 server error. Implemented in `ExceptionHandlingMiddleware`.

## Configuration & secrets

`appsettings.json` has empty placeholders. Real values come from user-secrets, `appsettings.Development.json` (git-ignored) or environment variables.
