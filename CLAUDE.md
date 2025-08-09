# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a personal portfolio website built with Blazor WebAssembly (.NET 8). The project showcases projects and provides an about page, running entirely in the browser as a client-side single-page application.

## Architecture

The application follows a standard Blazor WebAssembly structure:

- **Entry Point**: `website/Program.cs` - Sets up the WebAssembly host, registers services (HttpClient and ProjectService)
- **Routing**: `website/App.razor` - Handles client-side routing with MainLayout as the default layout
- **Data Layer**: 
  - `website/Data/Project.cs` - Model for project information (Title, Description, ImageUrl, ProjectUrl, Tags)
  - `website/Data/ProjectService.cs` - Service that provides hardcoded project data asynchronously
- **Pages**: Located in `website/Pages/`
  - `Index.razor` & `Index.razor.cs` - Homepage with code-behind pattern
  - `Projects.razor` - Project showcase page
  - `About.razor` - About page
- **Components**: 
  - `website/Shared/MainLayout.razor` - Main layout wrapper
  - `website/Shared/ProjectCard.razor` - Reusable component for displaying project information
- **Static Assets**: `website/wwwroot/` contains CSS, JavaScript (vanilla-tilt.min.js), and images

## Development Commands

Since this is a .NET project, use standard .NET CLI commands:

```bash
# Navigate to the project directory
cd website

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run in development mode (starts dev server)
dotnet run

# Watch mode for development (auto-reload on changes)
dotnet watch

# Publish for deployment
dotnet publish -c Release
```

## Development Server

The application runs on:
- HTTP: http://localhost:5195
- HTTPS: https://localhost:7112

Launch settings are configured in `website/Properties/launchSettings.json` with profiles for HTTP, HTTPS, and IIS Express.

## Key Patterns

- **Service Registration**: Services are registered in Program.cs using `builder.Services.AddScoped()`
- **Async Data**: ProjectService uses async patterns with `Task<List<Project>>`
- **Component Structure**: Uses both Razor components (.razor) and code-behind files (.razor.cs)
- **Styling**: Bootstrap CSS framework is included for responsive design
- **Client-Side Rendering**: Fully client-side application with no server-side rendering