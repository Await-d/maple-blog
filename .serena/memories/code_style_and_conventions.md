# Code Style and Conventions

## General Rules
- **NO mock data, temporary solutions, or simplified implementations**
- **Complete functional implementation required for all features**
- **NO test documentation files** - implement actual tests only
- **Auto-delete test files after completion**

## Frontend (React/TypeScript)
- Use functional components with hooks
- TypeScript strict mode enabled
- ESLint + Prettier for formatting
- PascalCase for components, camelCase for variables/functions
- kebab-case for file names
- Use Zustand for global state, TanStack Query for server state
- React Hook Form for forms

## Backend (.NET)
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Use async/await patterns consistently
- Dependency injection for all services
- Clean Architecture layering:
  - Domain: Entities, value objects, interfaces
  - Application: Services, DTOs, validation
  - Infrastructure: Data access, repositories
  - API: Controllers, middleware

## Database
- Entity Framework Code First
- Always create migrations for schema changes
- Use value objects where appropriate
- Repository pattern for data access