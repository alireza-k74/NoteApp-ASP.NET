# NoteApp API

A secure REST API for creating and managing personal notes. Built with ASP.NET Core, Entity Framework Core, and JWT authentication.

## Features

- User registration and login
- JWT access tokens with refresh token rotation
- Logout (refresh token revoke) and `/api/auth/me`
- Request validation on auth and note DTOs
- CORS for the React frontend (`localhost:5173`)
- Password hashing with ASP.NET Identity `PasswordHasher`
- Full CRUD for notes (create, read, update, delete)
- Notes are scoped per authenticated user
- Swagger / OpenAPI docs with Bearer auth support
- SQL Server persistence via EF Core migrations
- Automated integration tests (`NoteApp.Api.Tests`)

## Tech Stack

| Layer | Technology |
| --- | --- |
| Runtime | .NET 10 |
| Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Auth | JWT Bearer + Refresh Tokens |
| Docs | Swashbuckle (Swagger) |

## Project Structure

```
NoteApp/
├── NoteApp.Api/           # ASP.NET Core Web API
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Models/
│   ├── Services/
│   └── Migrations/
└── NoteApp.Api.Tests/     # Integration tests
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or Docker)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/YOUR_USERNAME/NoteApp.git
cd NoteApp
```

### 2. Configure settings

Update `NoteApp.Api/appsettings.json` (or use User Secrets / environment variables):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=NoteAppDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET",
    "Issuer": "NoteApp",
    "Audience": "NoteAppUsers",
    "AccessTokenMinutes": 30,
    "RefreshTokenDays": 7
  }
}
```

> **Important:** Never commit real secrets. Change the JWT key and database password before deploying.

### 3. Apply migrations

```bash
cd NoteApp.Api
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run
```

Swagger UI is available in Development at:

```
https://localhost:<port>/swagger
```

## API Endpoints

### Auth (`/api/auth`)

| Method | Endpoint | Description |
| --- | --- | --- |
| `POST` | `/api/auth/register` | Register a new user |
| `POST` | `/api/auth/login` | Login and receive tokens |
| `POST` | `/api/auth/refresh` | Exchange a refresh token for new tokens |
| `POST` | `/api/auth/logout` | Revoke a refresh token |
| `GET` | `/api/auth/me` | Get the current authenticated user |

### Notes (`/api/notes`) — requires Bearer token

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/api/notes` | List current user's notes |
| `GET` | `/api/notes/{id}` | Get a single note |
| `POST` | `/api/notes` | Create a note |
| `PUT` | `/api/notes/{id}` | Update a note |
| `DELETE` | `/api/notes/{id}` | Delete a note |

## Example Requests

### Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "alice",
  "email": "alice@example.com",
  "password": "StrongPassword123!"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "StrongPassword123!"
}
```

### Create a note

```http
POST /api/notes
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "title": "Shopping list",
  "content": "Milk, eggs, bread"
}
```

## Running tests

```bash
dotnet test NoteApp.slnx
```

## Security Notes

- Passwords are hashed; plain-text passwords are never stored
- Access tokens are short-lived; refresh tokens can be revoked via `/api/auth/logout`
- Request DTOs are validated (required fields, email format, length limits)
- CORS is configured for the React dev origin (`http://localhost:5173`)
- Each note is tied to a user — users cannot access another user's notes
- Prefer User Secrets / environment variables for JWT key and DB password in real deployments

## License

This project is available for personal and educational use.
