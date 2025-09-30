# Repository Guidelines

## Project Structure & Module Organization
- `backend/`: ASP.NET Core API (`src/MapleBlog.API`) plus domain, application, and infrastructure projects.
- `frontend/`: Public site (Vite + React + TS). Tests under `src/**/__tests__` and `e2e/`.
- `frontend/admin/`: Admin console (Vite + React + TS). Tests under `src/**/__tests__` and `e2e/`.
- `docs/`, `docker/`, `specs/`, `data/`: Documentation, container config, specs, and local data.

## Build, Test, and Development Commands
- Backend dev: `dotnet run --project backend/src/MapleBlog.API`
- Backend build: `dotnet build backend/MapleBlog.sln`
- Frontend dev: `cd frontend && npm ci && npm run dev`
- Admin dev: `cd frontend/admin && npm ci && npm run dev`
- Frontend build: `npm run build` (run in `frontend/` or `frontend/admin/`)
- Lint/format: `npm run lint` | `npm run lint:fix`
- Unit tests: `npm run test` | coverage `npm run test:coverage`
- Docker (optional): `docker-compose up -d` | prod `-f docker-compose.prod.yml`

## Coding Style & Naming Conventions
- TypeScript/React: 2‑space indent; components `PascalCase`; variables/functions `camelCase`; hooks prefixed `use*`; test files `*.test.ts(x)`.
- ESLint enforced (`frontend/.eslintrc.cjs`, `frontend/admin/.eslintrc.json`). Fix with `npm run lint:fix`.
- C#: 4‑space indent; classes/enums `PascalCase`; interfaces `I*`; private fields `_camelCase`; async methods `*Async`.

## Testing Guidelines
- Unit/UI: Vitest + Testing Library. Place near code in `__tests__` or alongside as `*.test.tsx`.
- E2E: Playwright configs present (`frontend/**/playwright.config.ts`); run if installed: `npx playwright test`.
- Aim for meaningful coverage on new/changed code; include critical paths and reducers/stores.

## Commit & Pull Request Guidelines
- Use Conventional Commits: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`. Example: `feat(api): add comment moderation endpoints`.
- One change per PR; keep diffs focused. Include description, linked issues, and screenshots/GIFs for UI changes.
- Before opening PR: run `dotnet build`, `npm run lint`, and `npm run test` in affected packages; update docs when behavior changes.

## Security & Configuration Tips
- Copy envs from templates: root `.env.template` → `.env`; also `frontend/.env.development` or `frontend/admin/.env.development` as needed. Do not commit secrets.
- Default dev ports: API `http://localhost:5000`, web `http://localhost:3000`. Keep CORS and auth settings aligned with active environment.

