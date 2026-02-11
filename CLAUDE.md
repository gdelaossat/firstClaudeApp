# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build              # Build the project
dotnet run                # Run the application (default: https://localhost:5001)
dotnet watch              # Run with hot reload
```

## Architecture

Blazor Server app (.NET 10) with server-side rendering and interactive components.

- `Components/` - Razor components (.razor files)
  - `Layout/` - Main layout and navigation
  - `Pages/` - Routable page components
- `Program.cs` - Application entry point and service configuration
- `wwwroot/` - Static assets (CSS, JS, images)
