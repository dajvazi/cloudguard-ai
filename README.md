# CloudGuard AI

Full-stack AIOps platform for cloud infrastructure monitoring, anomaly detection, and automated self-healing.

**Stack:** React + TypeScript (frontend) · ASP.NET Core + PostgreSQL (backend)

## Features

- **Terraform ingestion** — upload `.tf` or `.zip` files to discover infrastructure
- **Service monitoring** — track cloud services, resources, and health status
- **Anomaly detection** — background engine analyzes metrics using statistical thresholds (σ deviation)
- **AI analysis** — OpenAI-powered root cause analysis with rule-based fallback
- **Self-healing pipeline** — anomaly → incident → recovery action → healthy state in one call
- **Operations dashboard** — real-time overview of services, incidents, and AI analysis

## Project Structure

```
cloudguard-ai/
├── backend/
│   ├── Controllers/       # HTTP layer
│   ├── Services/        # Business logic
│   │   ├── AI/          # Anomaly engine, AI analysis, self-healing orchestrator
│   │   └── Terraform/   # Parser, upload, archive extraction
│   ├── Repositories/    # Data access (EF Core)
│   ├── DTOs/            # Response models
│   ├── Models/          # Database entities
│   ├── Data/            # DbContext
│   ├── Mappings/        # Entity → DTO
│   └── Migrations/      # EF Core migrations
├── frontend/
│   ├── src/pages/       # Dashboard, Services, Incidents, AI Analysis, Recovery
│   ├── src/components/  # Sidebar, StatCard, StatusBadge, TerraformUploadDialog
│   └── src/api/         # API client
└── samples/             # Sample Terraform files
```

## Getting Started

Open **two terminals**:

### 1. Database

Create the PostgreSQL database:

```sql
CREATE DATABASE cloudguard;
```

Connection string is in `backend/appsettings.Development.json`.

### 2. Backend (.NET API)

**Environment variables** — copy the example and add your OpenAI key:

```bash
cp backend/.env.example backend/.env
# Edit backend/.env and set OpenAI__ApiKey
```

**Run with auto-reload** (recommended):

```bash
cd backend
dotnet watch run --launch-profile http
```

Or:

```bash
./backend/dev.sh
```

API: `http://localhost:8080`  
Swagger UI: `http://localhost:8080/swagger`

### 3. Frontend (React)

```bash
cd frontend
npm install
npm run dev
```

App: `http://localhost:5173`

## API Endpoints

| Area | Endpoints |
|------|-----------|
| Status | `GET /api/status` |
| Terraform | `POST /api/terraform/upload`, `GET /api/terraform/uploads` |
| Services | `GET /api/services`, `GET /api/services/{id}` |
| Metrics | `GET/POST /api/metrics` |
| Anomalies | `GET/POST /api/anomalies` |
| Incidents | `GET /api/incidents`, `GET /api/incidents/active`, `PATCH /resolve` |
| Recovery | `GET/POST /api/recovery-actions`, `PATCH /execute` |
| Resources | `GET/POST /api/resources` |
| Self-Healing | `POST /api/self-healing/trigger/{serviceId}`, `POST /api/self-healing/trigger/anomaly/{anomalyId}` |

### Terraform Upload

```bash
# Single .tf file
curl -X POST http://localhost:8080/api/terraform/upload \
  -F "file=@samples/cloudguard-infra.tf"

# Full project with modules (.zip)
cd backend/samples && zip -r ../samples.zip . -i "*.tf" && cd ..
curl -X POST http://localhost:8080/api/terraform/upload \
  -F "file=@backend/samples.zip"
```

### Self-Healing

```bash
# Trigger by service ID
curl -X POST http://localhost:8080/api/self-healing/trigger/52

# Trigger by anomaly ID
curl -X POST http://localhost:8080/api/self-healing/trigger/anomaly/17
```

## Self-Healing Pipeline

```
Metrics Collection → AI Analysis → Anomaly Detection → Incident Creation → Recovery Engine → Service Restart → Healthy State
```

1. **AnomalyDetectionEngine** (BackgroundService) — scans metrics every 30s, detects σ deviations
2. **AiAnalysisService** — calls OpenAI for root cause + recommended action (falls back to rules)
3. **SelfHealingOrchestrator** — chains anomaly → incident → recovery → healthy in one request

## Configuration

| File | Purpose |
|------|---------|
| `backend/appsettings.Development.json` | DB connection string, logging |
| `backend/.env` | OpenAI API key (gitignored) |
| `backend/.env.example` | Template for environment variables |
| `frontend/vite.config.ts` | Vite proxy to backend on port 8080 |

**OpenAI** (optional — without a key, rule-based analysis is used):

```env
# backend/.env
OpenAI__ApiKey=sk-your-key-here
OpenAI__Model=gpt-4o-mini
```

## Architecture

```
Controller → Service → Repository → DbContext → PostgreSQL
```

- **Controllers** — HTTP only, no business logic
- **Services** — business rules, orchestration
- **Repositories** — EF Core queries, data access
- **DTOs** — API response models, separated from entities

## Frontend Pages

| Route | Page |
|-------|------|
| `/` | Dashboard — stats, service health, incidents, AI analysis |
| `/services` | Cloud services grid + Terraform upload dialog |
| `/resources` | Discovered infrastructure resources |
| `/anomalies` | AI anomaly detection results |
| `/incidents` | Incident table with auto-heal buttons |
| `/recovery` | Self-healing pipeline visualization + history |

## Development

- Add controllers in `backend/Controllers/`
- Register services in `backend/Extensions/ServiceCollectionExtensions.cs`
- Add API functions in `frontend/src/api/client.ts`
- Migrations run automatically on backend startup
