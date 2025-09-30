# Essential Development Commands

## Backend Commands (.NET)
```bash
cd backend/src/MapleBlog.API
dotnet restore                    # Install dependencies  
dotnet run                        # Start dev server (http://localhost:5000)
dotnet build                      # Build project
dotnet test                       # Run all tests (from solution root: cd backend && dotnet test)
dotnet ef database update         # Apply migrations
dotnet ef migrations add <Name>   # Create new migration
```

## Frontend Commands (React)
```bash
cd frontend
pnpm install                       # Install dependencies
pnpm run dev                       # Start dev server (http://localhost:3000)
pnpm run build                     # Production build
pnpm run typecheck                 # TypeScript checking
pnpm run lint                      # ESLint
pnpm test                          # Vitest tests
```

## Docker Commands
```bash
docker-compose up -d              # Start development environment
docker-compose -f docker-compose.prod.yml up -d  # Production
docker-compose down               # Stop all services
```

## Testing Commands
- **Frontend**: `pnpm test` (from frontend/ directory)
- **Backend**: `dotnet test` (from backend/ directory)
- **Single test**: `dotnet test --filter TestName` or `pnpm test -- --grep "test name"`