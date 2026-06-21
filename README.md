# CloudGuard AI

Projekt full-stack me **React** (frontend) dhe **.NET** (backend API).

## Struktura

```
cloudguard-ai/
├── backend/          # ASP.NET Core Web API (.NET 10)
└── frontend/         # React + TypeScript + Vite
```

## Si ta nisësh

Hap **dy terminale**:

### 1. Backend (.NET API)

```bash
cd backend
dotnet run
```

API do të jetë në: `http://localhost:8080`

Endpoints:
- `GET /api/status` — kontrollon nëse API është online
- `GET /api/weatherforecast` — demo endpoint me të dhëna

### 2. Frontend (React)

```bash
cd frontend
npm install
npm run dev
```

Aplikacioni do të hapet në: `http://localhost:5173`

## Lidhja React ↔ .NET

1. **Vite proxy** — kërkesat nga React te `/api/*` ridrejtohen te backend (`frontend/vite.config.ts`)
2. **CORS** — backend lejon origin-in `http://localhost:5173` (`backend/Program.cs`)
3. **API client** — React thërret API-në nga `frontend/src/api/client.ts`

Shembull në React:

```ts
import { fetchStatus } from './api/client'

const status = await fetchStatus()
console.log(status.message) // "CloudGuard API është online!"
```

## Zhvillim

- Shto controller të ri në `backend/Controllers/`
- Shto funksione API në `frontend/src/api/client.ts`
- Thirri API-në nga komponentët React me `fetch` ose client-in ekzistues
