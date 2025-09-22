# Task Completion Checklist

When completing any development task, ALWAYS run these commands:

## Frontend Tasks
```bash
cd frontend
npm run typecheck                 # TypeScript type checking
npm run lint                      # ESLint validation
npm test                          # Run tests
```

## Backend Tasks  
```bash
cd backend
dotnet build                      # Build validation
dotnet test                       # Run all tests
```

## Database Changes
```bash
cd backend/src/MapleBlog.API
dotnet ef migrations add <MigrationName>  # If schema changed
dotnet ef database update         # Apply migrations
```

## Quality Rules
- **NEVER** commit with failing tests
- **NEVER** commit with TypeScript errors
- **NEVER** commit with lint errors
- **ALWAYS** test locally before committing
- **DELETE** test files after completion (per project rules)
- **COMPLETE** functional implementation - no mocks or placeholders