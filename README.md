# FUNewsManagementSystem

News management system built with ASP.NET Core and a separate UI project.

## Summary

FUNewsManagementSystem helps editors manage categories, tags, and news articles, with user roles, auditing, and reporting to track content and activity.

## Key Features

- Role-based access control and authentication
- CRUD for news articles, categories, and tags
- Audit logging for critical actions
- Reporting and CSV export for articles
- Dashboard and basic analytics endpoints
- File upload support for article assets
- AI suggest content for articles

## Projects

- FUNewsManagementSystem: Web API, domain, data access, repositories
- UI: MVC/UI layer

## Requirements

- .NET SDK 8.0 or later
- SQL Server (or configured provider in appsettings)

## Setup

1. Restore packages:
   dotnet restore
2. Apply migrations:
   dotnet ef database update --project FUNewsManagementSystem/FUNewsManagementSystem.csproj
3. Run API:
   dotnet run --project FUNewsManagementSystem/FUNewsManagementSystem.csproj
4. Run UI:
   dotnet run --project UI/UI.csproj

## Configuration

- API settings: FUNewsManagementSystem/appsettings.json
- UI settings: UI/appsettings.json

## Scripts

SQL scripts are in the scripts/ folder.

## Reports

Generated reports are in the reports/ folder.
