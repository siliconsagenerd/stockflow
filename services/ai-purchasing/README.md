# StockFlow: KI-Einkaufs-Freigabe-Assistent

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Node](https://img.shields.io/badge/node-%3E%3D20-339933?logo=node.js&logoColor=white)](https://nodejs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Tailwind](https://img.shields.io/badge/Tailwind-4-06B6D4?logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![tests](https://img.shields.io/badge/tests-2%20Suites%20%C2%B7%20node%3Atest-2ea44f)](#tests)

[Deutsch](#deutsch) | [English](#english)

**Ein Freigabe-Assistent für offene Bestellanforderungen.** Der Service liest
offene Anfragen aus der StockFlow-Datenbank, wertet Regelsignale
(Bestandsdeckung, Menge gegen 90-Tage-Schnitt, Mehrfachanfragen,
Ablehnungsquote, Begründungslänge) aus, ergänzt optional ein LLM-Gutachten und
zeigt beides samt Datenlage dem Einkäufer. Freigaben selbst laufen weiterhin im
StockFlow-Backend; der Assistent schlägt nur vor.

```mermaid
flowchart LR
    subgraph Browser
        U[React SPA<br/>DE/EN]
    end
    subgraph Service
        E[Express API :8099]
        R[Regelwerk<br/>scoreRequest]
        L[LLM-Adapter<br/>optional]
        E --> R
        E --> L
    end
    subgraph Data
        D[(StockFlow<br/>PostgreSQL)]
    end
    U -->|HTTPS| E
    E -->|SQL read-only| D
```

<a name="deutsch"></a>

## Deutsch

### Was das Projekt abdeckt

| Anliegen | Umsetzung |
| --- | --- |
| Regelbasiertes Scoring | Signale mit Gewichtung und nachvollziehbarem Entscheidungsvorschlag (`src/recommender.ts`) |
| LLM-Ergänzung | Gemini/OpenAI mit zod-validierter JSON-Antwort, Fallback auf Regeln (`src/llm.ts`) |
| Datenlage | Konfidenzbalken aus Anzahl ausgewerteter Signale (`web/components/DecisionPanel.tsx`) |
| Zweisprachigkeit | Deutsch als Standard, EN-Umschalter, Sprachauswahl gemerkt (`web/i18n.ts`) |
| Ohne Backend testbar | Demo-Modus mit eingebauten Beispieldaten (`src/demo.ts`) |

### Features

- Regelwerk: Bestandsdeckung, Mengenverhältnis zum 90-Tage-Schnitt, weitere
  offene Anfragen desselben Produkts, historische Ablehnungsquote,
  Begründungslänge (`src/recommender.ts`)
- Optionales LLM-Gutachten (Gemini oder OpenAI); ohne Schlüssel, bei Timeout
  (8 s) oder ungültiger Antwort läuft der Service rein regelbasiert weiter
  (`src/llm.ts`)
- Warteschlange mit Entscheidungsfarbe (Freigeben/Prüfen/Ablehnen), Wert,
  Alter und Mehrfachmarkierung (`web/components/QueueList.tsx`)
- Detailpanel mit Begründung, Auffälligkeiten, Signalen und kopierbarem
  Empfehlungstext (`web/components/DecisionPanel.tsx`)
- Sprachumschalter mit `aria-label` und Tastaturbedienung
  (`web/components/LanguageToggle.tsx`)
- Leere, Lade- und Fehlerzustände in der Oberfläche
- Demo-Modus mit In-Memory-PGlite und vier Beispieldatensätzen

### Projektstruktur

```
services/ai-purchasing/
├── src/
│   ├── app.ts             Express-App, Routen, Fehlerbehandlung
│   ├── server.ts          Einstieg: Port, DB-Anbindung, LLM-Konfiguration
│   ├── recommender.ts     Regelsignale und Entscheidungsvorschlag
│   ├── llm.ts             Gemini/OpenAI-Adapter mit Validierung
│   ├── demo.ts            Demo-Datenbank (PGlite)
│   ├── db.ts              SQL gegen StockFlow (nur lesend)
│   └── types.ts           Gemeinsame Typen
├── web/
│   ├── App.tsx            Shell, Zustand, Sprachumschaltung
│   ├── api.ts             Fetch-Wrapper
│   ├── i18n.ts            DE/EN-Texte
│   └── components/        QueueList, DecisionPanel, Badges, LanguageToggle
├── test/                  node:test (api.test.ts, recommender.test.ts)
├── dist/                  Build-Ausgabe (api/, web/)
└── Dockerfile             Multi-Stage-Build, läuft als User node
```

### Quick start

Docker:

```sh
docker build -t stockflow-ai-purchasing services/ai-purchasing
docker run -p 8099:8099 -e DEMO_MODE=1 stockflow-ai-purchasing
# http://127.0.0.1:8099
```

Lokale Entwicklung:

```sh
cd services/ai-purchasing
npm install
npm run demo            # DEMO_MODE=1, http://127.0.0.1:8099
```

Getrennt mit Live-Reload:

```sh
npm run dev:api         # Express via tsx watch (Port 8099)
npm run dev:web         # Vite-Devserver (Port 5173)
```

### Konfiguration

| Variable | Default | Zweck |
| --- | --- | --- |
| `PORT` | `8099` | HTTP-Port |
| `DEMO_MODE` | – | `1` startet mit In-Memory-Beispieldaten statt PostgreSQL |
| `DATABASE_URL` | `postgres://stockflow:***@127.0.0.1:5432/stockflow` | StockFlow-Datenbank, nur lesend genutzt |
| `GEMINI_API_KEY` | – | aktiviert Gemini-Gutachten |
| `GEMINI_MODEL` | `gemini-2.0-flash` | Gemini-Modell |
| `OPENAI_API_KEY` | – | aktiviert OpenAI-Gutachten |
| `OPENAI_MODEL` | `gpt-4o-mini` | OpenAI-Modell |
| `LLM_TIMEOUT_MS` | `8000` | Abbruch des LLM-Aufrufs, dann reine Regeln |

API-Schlüssel ausschließlich über Umgebungsvariablen setzen, nie ins Repository.

### API

`GET /api/health` – 200, meldet verwendete Engine
(`rule-engine` oder Modellname)

```json
{ "status": "ok", "engine": "rule-engine" }
```

`GET /api/queue?workspaceId=…` – 200

```json
{
  "workspaceId": null,
  "generatedAt": "2026-11-09T10:00:00.000Z",
  "source": "rules",
  "model": "rule-engine",
  "items": [
    {
      "request": {
        "id": "1f9c", "productName": "Dichtungssatz Pumpe P3", "sku": "GSK-0310",
        "quantity": 2, "unit": "Set", "requester": "Anna Beispiel",
        "justification": "Pumpe undicht, Satz für Revision", "currentStock": 0
      },
      "recommendation": {
        "decision": "review", "score": 60, "confidence": 0.6,
        "rationale": ["…"], "risks": ["…"],
        "signals": {
          "stockCoverage": 0, "quantityRatio": 0.84, "historySamples": 3,
          "historyRejectionRate": 0.33, "justificationLength": 120
        }
      }
    }
  ]
}
```

`GET /api/requests/:id/recommendation` – 200, oder 404 wenn die Anforderung
nicht (mehr) offen ist.

`GET /api/workspaces` – 200, Liste der StockFlow-Arbeitsbereiche.

### Tests

```sh
npm test          # node:test: api.test.ts, recommender.test.ts
npm run typecheck # tsconfig.web.json + tsconfig.api.json
```

Beide Suiten laufen im Demo-Modus, es wird keine laufende Datenbank gebraucht.

### Bekannte Einschränkungen

- Kein Schreibpfad: der Assistent schlägt vor, entschieden wird in StockFlow.
- Demo-Daten sind statisches Beiwerk, keine Persistenz.
- Das LLM ist Ergänzung, nicht Pflicht: ohne Schlüssel läuft alles regelbasiert.
- Direkter SQL-Zugriff auf die StockFlow-Datenbank (nur lesend), keine eigene
  Rechteverwaltung des Service.
- Dieser Service hat kein eigenes Live-Deployment.

### Danksagung

Tests laufen über das node:test-Modul ohne zusätzliche Testabhängigkeit; die
Oberfläche nutzt Tailwind und React aus dem monorepo-weiten Tooling.

### Dokumentation

- Hauptprojekt StockFlow: `../README.md`
- Projektdokumentation: `../docs/`

---

<a name="english"></a>

## English

### What this project covers

| Concern | Implementation |
| --- | --- |
| Rule-based scoring | Weighted signals with an explainable decision proposal (`src/recommender.ts`) |
| LLM augmentation | Gemini/OpenAI with zod-validated JSON, rule fallback (`src/llm.ts`) |
| Data quality | Confidence bar derived from evaluated signal count (`web/components/DecisionPanel.tsx`) |
| Bilingual UI | German default, EN toggle, choice persisted (`web/i18n.ts`) |
| Runnable without backend | Demo mode with seeded sample data (`src/demo.ts`) |

### Features

- Rules: stock coverage, quantity against 90-day average, other open requests
  for the same product, historical rejection rate, justification length
  (`src/recommender.ts`)
- Optional LLM second opinion (Gemini or OpenAI); without a key, on timeout
  (8 s) or on an invalid answer the service stays purely rule-based
  (`src/llm.ts`)
- Queue rows with decision colour (approve/review/reject), score, age and
  duplicate marking (`web/components/QueueList.tsx`)
- Detail panel with rationale, risks, signals and a copyable recommendation
  (`web/components/DecisionPanel.tsx`)
- Language switch with `aria-label`, keyboard operable
  (`web/components/LanguageToggle.tsx`)
- Empty, loading and error states in the UI
- Demo mode backed by in-memory PGlite with four seeded requests

### Project structure

```
services/ai-purchasing/
├── src/
│   ├── app.ts             Express app, routes, error handling
│   ├── server.ts          Entry point: port, DB, LLM config
│   ├── recommender.ts     Rule signals and decision proposal
│   ├── llm.ts             Gemini/OpenAI adapter with validation
│   ├── demo.ts            Demo database (PGlite)
│   ├── db.ts              Read-only SQL against StockFlow
│   └── types.ts           Shared types
├── web/
│   ├── App.tsx            Shell, state, locale switch
│   ├── api.ts             Fetch wrapper
│   ├── i18n.ts            DE/EN strings
│   └── components/        QueueList, DecisionPanel, badges, LanguageToggle
├── test/                  node:test (api.test.ts, recommender.test.ts)
├── dist/                  Build output (api/, web/)
└── Dockerfile             Multi-stage build, runs as user node
```

### Quick start

Docker:

```sh
docker build -t stockflow-ai-purchasing services/ai-purchasing
docker run -p 8099:8099 -e DEMO_MODE=1 stockflow-ai-purchasing
# http://127.0.0.1:8099
```

Local development:

```sh
cd services/ai-purchasing
npm install
npm run demo            # DEMO_MODE=1, http://127.0.0.1:8099
```

Split with live reload:

```sh
npm run dev:api         # Express via tsx watch (port 8099)
npm run dev:web         # Vite dev server (port 5173)
```

### Configuration

| Variable | Default | Purpose |
| --- | --- | --- |
| `PORT` | `8099` | HTTP port |
| `DEMO_MODE` | – | set to `1` to serve seeded in-memory data instead of PostgreSQL |
| `DATABASE_URL` | `postgres://stockflow:***@127.0.0.1:5432/stockflow` | StockFlow database, read-only usage |
| `GEMINI_API_KEY` | – | enables the Gemini second opinion |
| `GEMINI_MODEL` | `gemini-2.0-flash` | Gemini model |
| `OPENAI_API_KEY` | – | enables the OpenAI second opinion |
| `OPENAI_MODEL` | `gpt-4o-mini` | OpenAI model |
| `LLM_TIMEOUT_MS` | `8000` | LLM call cutoff, falls back to rules |

Set API keys through environment variables only, never in the repository.

### API

`GET /api/health` – 200, reports the active engine (`rule-engine` or model
name)

```json
{ "status": "ok", "engine": "rule-engine" }
```

`GET /api/queue?workspaceId=…` – 200

```json
{
  "workspaceId": null,
  "generatedAt": "2026-11-09T10:00:00.000Z",
  "source": "rules",
  "model": "rule-engine",
  "items": [
    {
      "request": {
        "id": "1f9c", "productName": "Dichtungssatz Pumpe P3", "sku": "GSK-0310",
        "quantity": 2, "unit": "Set", "requester": "Anna Beispiel",
        "justification": "Pumpe undicht, Satz für Revision", "currentStock": 0
      },
      "recommendation": {
        "decision": "review", "score": 60, "confidence": 0.6,
        "rationale": ["…"], "risks": ["…"],
        "signals": {
          "stockCoverage": 0, "quantityRatio": 0.84, "historySamples": 3,
          "historyRejectionRate": 0.33, "justificationLength": 120
        }
      }
    }
  ]
}
```

`GET /api/requests/:id/recommendation` – 200, or 404 when the request is no
longer pending.

`GET /api/workspaces` – 200, list of StockFlow workspaces.

### Tests

```sh
npm test          # node:test: api.test.ts, recommender.test.ts
npm run typecheck # tsconfig.web.json + tsconfig.api.json
```

Both suites run in demo mode, no live database needed.

### Known limitations

- No write path: the assistant recommends, StockFlow decides.
- Demo data is a static seed, not persistence.
- The LLM is an addition, not a requirement: without a key everything runs on
  rules alone.
- Direct (read-only) SQL access to the StockFlow database; the service has no
  permission model of its own.
- This service has no live deployment of its own.

### Acknowledgements

Tests run on the node:test module with no extra test dependency; the UI uses
Tailwind and React from the monorepo-wide tooling.

### Documentation

- Main StockFlow project: `../README.md`
- Project documentation: `../docs/`

[Zur deutschen Fassung](#deutsch)
