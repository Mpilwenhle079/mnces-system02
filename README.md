# Mnce Shisanyama Ordering System

ASP.NET Core ordering system with a customer ordering page, kitchen board, admin dashboard, SQLite storage, and SignalR order updates.

## Run locally

```powershell
dotnet run --project .\src\MnceShisanyama.Api\MnceShisanyama.Api.csproj --launch-profile http
```

Open `http://localhost:5080/` for customer ordering, or `/kitchen.html` for the kitchen board.

## Database schema recovery

The application uses `EnsureCreated` rather than EF migrations. On startup, `DbSeeder` checks the SQLite schema for the current payment, support-call, customer-email, and order-discount fields. If an older schema is detected, the database is deleted, recreated, and reseeded automatically.

This recovery prevents stale local databases from causing HTTP 500 errors after model changes, but it also removes existing local data. Export any data that must be retained before changing the model.