# CloudGuard AI

Projekt full-stack me **React** (frontend) dhe **.NET** (backend API).

## Struktura

```
cloudguard-ai/
├── backend/
│   ├── Controllers/       # HTTP layer — vetëm request/response
│   ├── Services/          # Business logic
│   ├── Repositories/      # Data access (EF Core queries)
│   ├── DTOs/              # Response models
│   ├── Models/            # Database entities
│   ├── Data/              # DbContext
│   ├── Mappings/          # Entity → DTO
│   └── Migrations/        # EF Core migrations
└── frontend/              # React + TypeScript + Vite
```

## Si ta nisësh

Hap **dy terminale**:

### 1. Backend (.NET API)

**Me auto-reload (rekomandohet gjatë zhvillimit)** — kur ruan një skedar `.cs`, serveri riniset vetë:

```bash
cd backend
dotnet watch run --launch-profile http
```

Ose:

```bash
./backend/dev.sh
```

Në Cursor/VS Code: `Terminal` → `Run Task` → **backend: watch**

**Pa auto-reload** (nisje e thjeshtë):

```bash
cd backend
dotnet run
```

API do të jetë në: `http://localhost:8080`

**Swagger UI** (vetëm në Development):

```
http://localhost:8080/swagger
```

Endpoints:
- `GET /api/status` — kontrollon nëse API është online
- `POST /api/terraform/upload` — upload `.tf` ose `.zip` (me module), parse dhe ruaj shërbimet
- `GET /api/terraform/uploads` — lista e upload-eve
- `GET /api/terraform/uploads/{id}` — detaje upload + shërbimet e zbuluara
- `GET /api/services` — të gjitha shërbimet nga databaza
- `GET /api/services/{id}` — një shërbim
- `GET /api/services/by-upload/{uploadId}` — shërbimet nga një upload
- `GET/POST /api/metrics` — metrikat (CPU, memory, latency, error rate)
- `GET/POST /api/anomalies` — anomalitë e AI
- `GET/POST /api/incidents` — incidentet (+ `GET /active`, `PATCH /resolve`)
- `GET/POST /api/recovery-actions` — veprimet e self-healing (+ `PATCH /execute`)
- `GET/POST /api/resources` — resurset e zbuluara nga Terraform (+ `GET /by-source`)

Shembull upload me curl:

```bash
# Një skedar .tf
curl -X POST http://localhost:8080/api/terraform/upload \
  -F "file=@backend/samples/main.tf"

# Projekti i plotë me module (.zip)
cd backend/samples && zip -r ../samples.zip . -i "*.tf" && cd ..
curl -X POST http://localhost:8080/api/terraform/upload \
  -F "file=@backend/samples.zip"
```

Çdo shërbim kthen metadata Terraform:
- `sourceKind` — resource / module / data
- `rawResourceType` — p.sh. aws_db_instance
- `sourceFile` — skedari ku u gjet
- `moduleSource` — source i module-it (p.sh. ./modules/auth)
- `parentModule` — moduli prind (p.sh. authentication)

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
