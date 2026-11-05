# Snip – URL Shortener

A full-stack URL shortener built with **C# / ASP.NET Core 10** (backend) and **React + TypeScript** (frontend), containerised with Docker.

---

## Architecture

```
frontend/      React + TypeScript (Vite)
backend/
  UrlShortener.Api/      ASP.NET Core 10 Web API + SQLite (EF Core)
  UrlShortener.Tests/    xUnit unit + integration tests
docker-compose.yml       Orchestrates both services
```

The frontend proxies API calls through Nginx in production, so there are no CORS concerns once containerised. In development, Vite's dev server proxies to the local API.

---
## Getting started

### Prerequisites

- [Docker](https://www.docker.com/) (for the one-command route), **or**
- [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js](https://nodejs.org/) (LTS) to run locally

### Run with Docker

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| UI | http://localhost:3000 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |

The SQLite database is stored in `./data`, so links survive restarts.

### Run locally

```bash
# API (http://localhost:8080)
cd backend/UrlShortener.Api
dotnet run

# UI (http://localhost:3000) — in a second terminal
cd frontend
npm install
npm run dev
```

The Vite dev server proxies API calls to `http://localhost:8080`, so no extra configuration is needed.

### Run the tests

```bash
dotnet test backend/UrlShortener.sln   # backend: unit + integration
cd frontend && npm test                # frontend: component + service tests
```

---

## API reference

The API matches the provided `openapi.yaml` contract exactly.

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/shorten` | Create a shortened URL |
| `GET` | `/urls` | List all shortened URLs |
| `GET` | `/{alias}` | Redirect to the full URL |
| `DELETE` | `/{alias}` | Delete a shortened URL |

### POST /shorten

```json
// Request
{
  "fullUrl": "https://example.com/very/long/path",
  "customAlias": "my-alias"   // optional
}

// Response 201
{
  "alias": "my-alias",
  "fullUrl": "https://example.com/very/long/path",
  "shortUrl": "http://localhost:8080/my-alias",
  "createdAt": "2026-11-05T09:30:00Z"
}
```

Returns `400` if the URL is invalid, the alias is malformed, or the alias is already taken.

### GET /urls

| Query param | Values | Description |
|-------------|--------|-------------|
| `pageIndex` | `1`–`10` | Page number (1-based) |
| `pageSize` | `1`–`100` | Items per page |
| `sortOrder` | `0` = newest first, `1` = oldest first | Order by `createdAt` |

```json
// GET /urls?pageIndex=1&pageSize=10&sortOrder=0
// Response 200
{
  "items": [
    {
      "alias": "my-alias",
      "fullUrl": "https://example.com/very/long/path",
      "shortUrl": "http://localhost:8080/my-alias",
      "createdAt": "2026-11-05T09:30:00Z"
    }
  ],
  "pageIndex": 1,
  "pageSize": 10,
  "totalCount": 42
}
```

A page past the end returns an empty `items` array. `400` for out-of-range parameters.

### GET /{alias}

Returns `302 Redirect` to the full URL, or `404` if the alias doesn't exist.

### DELETE /{alias}

Returns `204 No Content` on success, `404` if alias not found.

---

## Design decisions & assumptions

**Persistence** — SQLite via EF Core. Zero-dependency, file-based, and entirely sufficient for this exercise. The database file path is configurable via `DatabasePath` in `appsettings.json` or an environment variable, and is volume-mounted in Docker so data survives container restarts.

**Alias validation** — Aliases must be 2–64 characters, containing only letters, numbers, and hyphens. This mirrors typical URL shortener conventions and avoids characters that are awkward in URLs.

**Random alias generation** — When no custom alias is supplied, the protocol is stripped from the full URL and the remainder is slugified, truncated to 64 characters. Generation is deterministic, so shortening the same URL twice produces the same alias and the second request is rejected with a `400` (duplicate alias) instead of creating a second entry. Supplying a `customAlias` is the way to have more than one short link for the same URL.

**Pagination** — `GET /urls` is paged server-side so the list stays fast as it grows.
- `pageIndex` is **1-based** in the API (`1`–`10`) and `pageSize` is `1`–`100`; out-of-range values return `400`. Note that the `pageIndex` cap means a client using a small page size can only reach `pageSize × 10` rows.
- `pageSize` has been set to a constant `5` in the frontend due to time constraints, but a page size dropdown could be implemented to overcome this.
- The frontend uses `react-paginate`, which is **0-based**. The conversion happens in one place (`services/api.ts`), so the rest of the UI works in 0-based pages.
- The response is an envelope: `items`, `pageIndex`, `pageSize` and `totalCount`. The UI derives the page count from `totalCount / pageSize`.
- Requesting a page beyond the last one returns an empty `items` array with the correct `totalCount`, not an error.

**Sorting** — `sortOrder` selects the ordering by `createdAt`: `0` = `CreatedByDesc` (newest first, the default) and `1` = `CreatedByAsc` (oldest first). Unknown values are rejected with `400`. The frontend mirrors these values in a `SortDirection` constant, and changing the sort resets to the first page, because page 4 of one ordering has no meaningful relationship to page 4 of another.

**Timestamps** — `createdAt` is stored and returned in UTC and is produced through an injectable `IDateTimeHelper`, so tests can control the clock and assert on sort order deterministically. The UI renders it in the browser's local time zone.

**Error handling** — A global exception middleware catches unhandled exceptions and returns a clean JSON `{ "error": "..." }` envelope. Domain errors (duplicate alias, invalid input) are surfaced as typed exceptions from the service layer, translated to 400s in the controller.

**Testing** — The backend has both unit tests (service layer, in-memory EF) and integration tests (`WebApplicationFactory` + in-memory EF, full HTTP stack). The frontend has component tests (React Testing Library) and service layer tests (mocked fetch).

**Frontend** — Intentionally lightweight: a custom hook (`useUrlShortener`) owns all state and API interaction; components are purely presentational. No external state management library is needed at this scale.

**CORS** — Allowed origins are configured in `appsettings.json`. In the Docker Compose setup, the Nginx reverse proxy means the frontend and API appear on the same origin to the browser, so CORS is only relevant in local development.

