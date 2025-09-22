# Maple Blog Project Overview

## Purpose
Maple Blog is a modern AI-driven blog system built with React 19 and .NET 10. It's designed for AI-related content but includes all traditional blog features. Uses clean architecture with front-end/back-end separation.

## Technology Stack
- **Frontend**: React 19 + TypeScript + Vite + Tailwind CSS + Zustand + TanStack Query
- **Backend**: .NET 10 + ASP.NET Core Web API + Entity Framework Core 
- **Database**: SQLite (dev) / PostgreSQL (prod) / SQL Server / MySQL / Oracle
- **Authentication**: JWT Bearer tokens
- **Testing**: xUnit (.NET), Vitest (React)
- **Caching**: Redis (optional for dev)

## Key Architecture
- Clean Architecture with Domain/Application/Infrastructure/API layers
- Frontend uses feature-based structure with pages/components/stores/services
- Entity Framework Code First with migrations
- RESTful API with Swagger documentation