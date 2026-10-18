# NoteApp

A secure notes app with an ASP.NET Core API and a React (Vite + TypeScript) frontend.

## Features

- User registration, login, logout, and `/api/auth/me`
- JWT access tokens with refresh token rotation
- Notes: create, read, update, soft-delete, restore, pin
- Search, tag filter, pinned filter, and pagination
- Per-user tags with assign on create/update
- Request validation and CORS for the React app
- Swagger / OpenAPI with Bearer auth
- SQL Server via EF Core migrations
- Tests for API (`NoteApp.Api.Tests`) and web (`NoteApp.Web`)

## Tech Stack

| Layer | Technology |
| --- | --- |
| API | ASP.NET Core (.NET 10), EF Core, SQL Server, JWT |
| Frontend | React, Vite, TypeScript, React Router |
| Docs | Swashbuckle (Swagger) |

## Project Structure

```
NoteApp/
├── NoteApp.Api/           # ASP.NET Core Web API
├── NoteApp.Api.Tests/     # API integration tests
└── NoteApp.Web/           # React frontend
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or Docker)
- Node.js 20+
- `dotnet ef` tool (`dotnet tool install -g dotnet-ef`)

## Getting Started

### 1. Clone

```bash
git clone https://github.com/YOUR_USERNAME/NoteApp.git
cd NoteApp
```

### 2. Configure the API

Update `NoteApp.Api/appsettings.json` (or User Secrets / environment variables):

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
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "https://localhost:5173",
      "http://127.0.0.1:5173"
    ]
  }
}
```

> **Important:** Never commit real secrets.

### 3. Apply migrations

```bash
dotnet ef database update --project NoteApp.Api/NoteApp.Api.csproj
```

### 4. Run the API

```bash
dotnet run --project NoteApp.Api/NoteApp.Api.csproj --launch-profile http
```

| | URL |
| --- | --- |
| API base | `http://localhost:5198` |
| Swagger | `http://localhost:5198/swagger` |

### 5. Run the React app

```bash
cd NoteApp.Web
```

Create `.env` from the example (PowerShell):

```powershell
Copy-Item .env.example .env
```

Or on macOS/Linux:

```bash
cp .env.example .env
```

`.env` contents:

```env
VITE_API_BASE_URL=http://localhost:5198
```

Install and start:

```bash
npm install
npm run dev
```

Open **http://localhost:5173**

Other frontend commands:

```bash
npm run build      # production build → dist/
npm run preview    # preview production build
npm test           # Vitest unit tests
```

Keep the API running while using the UI. The SPA stores JWT tokens in `localStorage` and refreshes them automatically on `401`.

## API Endpoints

### Auth — `/api/auth`

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | No | Register and return tokens |
| `POST` | `/api/auth/login` | No | Login and return tokens |
| `POST` | `/api/auth/refresh` | No | Rotate refresh token → new token pair |
| `POST` | `/api/auth/logout` | No | Revoke a refresh token |
| `GET` | `/api/auth/me` | Bearer | Current user profile |

### Notes — `/api/notes` (Bearer required)

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/api/notes` | List active notes (paged) |
| `GET` | `/api/notes/trash` | List soft-deleted notes |
| `GET` | `/api/notes/{id}` | Get one active note |
| `POST` | `/api/notes` | Create a note |
| `PUT` | `/api/notes/{id}` | Update a note |
| `DELETE` | `/api/notes/{id}` | Soft-delete (move to trash) |
| `POST` | `/api/notes/{id}/restore` | Restore from trash |
| `PATCH` | `/api/notes/{id}/pin` | Toggle pin |

#### Query params for `GET /api/notes` and `GET /api/notes/trash`

| Param | Type | Default | Description |
| --- | --- | --- | --- |
| `q` | string | — | Search in title and content |
| `tag` | string | — | Filter by tag name |
| `pinned` | bool | — | Filter pinned notes (`true` / `false`) |
| `page` | int | `1` | Page number |
| `pageSize` | int | `20` | Page size (max `100`) |

List response shape:

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

### Tags — `/api/tags` (Bearer required)

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/api/tags` | List current user's tags |
| `POST` | `/api/tags` | Create a tag |
| `DELETE` | `/api/tags/{id}` | Delete a tag |

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

### Refresh / Logout

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<refresh_token>"
}
```

```http
POST /api/auth/logout
Content-Type: application/json

{
  "refreshToken": "<refresh_token>"
}
```

### Current user

```http
GET /api/auth/me
Authorization: Bearer <access_token>
```

### Create a note (with tags)

```http
POST /api/notes
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "title": "Shopping list",
  "content": "Milk, eggs, bread",
  "isPinned": false,
  "tagIds": [1, 2]
}
```

### Search notes

```http
GET /api/notes?q=milk&tag=home&pinned=true&page=1&pageSize=10
Authorization: Bearer <access_token>
```

### Create a tag

```http
POST /api/tags
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "name": "work"
}
```

## Running tests

```bash
# API
dotnet test NoteApp.slnx

# Frontend
cd NoteApp.Web
npm test
```

## Security Notes

- Passwords are hashed; plain-text passwords are never stored
- Access tokens are short-lived; refresh tokens can be revoked via `/api/auth/logout`
- DTOs are validated (required fields, email format, length limits)
- CORS allows the React dev origin (`http://localhost:5173`)
- Notes and tags are scoped per user
- Prefer User Secrets / environment variables for JWT key and DB password in real deployments

## License

This project is available for personal and educational use.
