# tasktracker-web (React + Vite)

A minimal internal dashboard to exercise the TaskTracker API.

## Prerequisites
- Node.js (LTS)

## Setup
- Install deps:
  - `npm install`
- Create .env.local and set API base URL (defaults to https://localhost:7148):
  - `VITE_API_BASE_URL=https://localhost:7148`
- Start dev server:
  - `npm run dev`
- Open http://localhost:5173

## Notes
- The API must be running and accessible from the browser.
- If calling HTTPS locally, trust the dev cert once:
  - `dotnet dev-certs https --trust`
- If needed, enable CORS in the API (Development only) for http://localhost:5173
