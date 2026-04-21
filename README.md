# ShiftSwap

ShiftSwap is a simple ASP.NET Core Web API for managing employee shifts and shift swap requests. It supports user registration/login, shift creation, update, deletion, and swap request workflows (create, approve, reject, cancel, accept). 

## Features

- JWT-based authentication and role-based actions
- User registration and login
- CRUD operations for shifts
- Shift swap request lifecycle:
  - Create swap request
  - Approve swap request
  - Reject swap request
  - Cancel swap request
  - Accept swap request (finalize swap)
- Soft delete and audit metadata
- Pagination and filtering (implemented through DTOs)

## Project structure

- `Controllers/` - API controllers (`AuthController`, `ShiftsController`, `SwapsController`)
- `Data/` - EF Core context and DB initializer
- `DTOs/` - data transfer objects for API input and output
- `Models/` - domain entities (`User`, `Shift`, `ShiftSwapRequest`, etc.)
- `Services/` - services like JWT token generation and audit logging
- `Migrations/` - EF Core migrations

## Requirements

- .NET 8 SDK or later
- SQL Server (or another DB provider configured in `appsettings.json`)

## Setup

1. Clone repository:

```bash
git clone https://github.com/<your-org>/ShiftSwap.git
cd ShiftSwap
```

2. Update connection string in `appsettings.json` (or `appsettings.Development.json`):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ShiftSwapDb;Trusted_Connection=True;"
}
```

3. Run migrations and database update:

```bash
dotnet ef database update --project ShiftSwap\ShiftSwap.csproj
```

4. Run the app:

```bash
dotnet run --project ShiftSwap\ShiftSwap.csproj
```

## API endpoints

### Auth
- `POST /api/auth/register` - register new user
- `POST /api/auth/login` - login and receive JWT token

### Shifts
- `GET /api/shifts` - list shifts (supports filters)
- `GET /api/shifts/{id}` - get shift by ID
- `POST /api/shifts` - create shift
- `PUT /api/shifts/{id}` - update shift
- `DELETE /api/shifts/{id}` - delete shift

### Swap requests
- `POST /api/swaps` - create swap request
- `PUT /api/swaps/{id}/approve` - approve request
- `PUT /api/swaps/{id}/reject` - reject request
- `PUT /api/swaps/{id}/cancel` - cancel request
- `PUT /api/swaps/{id}/accept` - accept request and perform swap

## Testing

Use Postman/HTTP client with `Authorization: Bearer <token>` header after login. Example APIs may be preconfigured in `ShiftSwap.http`.

## Notes

- Token signing configuration is in `Services/JwtTokenService.cs` and `appsettings.json`.
- Audit logs persist in `AuditLog` entity (audit service is in `Services/AuditLogger.cs`).

## Contributing

Contributions are welcome via issues and pull requests. Follow standard GitHub workflow.
