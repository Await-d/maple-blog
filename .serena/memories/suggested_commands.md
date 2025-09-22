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
npm install                       # Install dependencies
npm run dev                       # Start dev server (http://localhost:3000)
npm run build                     # Production build
npm run typecheck                 # TypeScript checking
npm run lint                      # ESLint
npm test                          # Vitest tests
```

## Docker Commands
```bash
docker-compose up -d              # Start development environment
docker-compose -f docker-compose.prod.yml up -d  # Production
docker-compose down               # Stop all services
```

## Testing Commands
- **Frontend**: `npm test` (from frontend/ directory)
- **Backend**: `dotnet test` (from backend/ directory)
- **Single test**: `dotnet test --filter TestName` or `npm test -- --grep "test name"`