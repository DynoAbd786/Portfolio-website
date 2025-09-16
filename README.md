# Personal Portfolio Website

A modern, full-stack portfolio website built with Blazor WebAssembly frontend and ASP.NET Core Web API backend, containerized for cloud deployment.

## 🚀 Live Demo

Visit the live portfolio at: [https://dynoabd786.github.io](https://dynoabd786.github.io)

## 📋 Overview

This portfolio serves multiple purposes:
- **Professional Showcase**: Display projects for recruiters and employers
- **Full-Stack Learning**: Document C# development journey across frontend and backend
- **Cloud-Native Architecture**: Demonstrate modern containerized deployment practices
- **AI-Assisted Development**: Showcase development workflows using Claude Code

## 🛠️ Tech Stack

### Frontend
- **Framework**: Blazor WebAssembly
- **Runtime**: .NET 8
- **Language**: C# with nullable types enabled
- **Styling**: Tailwind CSS (via CDN)
- **Icons**: Font Awesome 6.4.0
- **Fonts**: Google Fonts (Roboto Mono, Source Code Pro)
- **Animations**: Vanilla Tilt.js for 3D card effects

### Backend
- **Framework**: ASP.NET Core Web API
- **Runtime**: .NET 8
- **Architecture**: RESTful API with CORS support
- **Features**: Static file serving, SPA fallback routing

### DevOps & Deployment
- **Containerization**: Docker multi-stage builds
- **Cloud Platform**: Render deployment ready
- **Development**: Docker Compose for local development
- **CI/CD**: GitHub Actions for GitHub Pages (frontend-only)

## 🏗️ Project Architecture

```
├── website/                     # Blazor WebAssembly Frontend
│   ├── Data/
│   │   ├── Project.cs          # Project model
│   │   └── ProjectService.cs   # API client service
│   ├── Pages/                  # Razor pages and components
│   ├── Shared/                 # Shared components
│   └── wwwroot/                # Static assets
├── website.api/                # ASP.NET Core Web API Backend
│   ├── Controllers/
│   │   └── ProjectsController.cs  # Projects API endpoints
│   ├── Models/
│   │   └── Project.cs          # Project data model
│   ├── Services/
│   │   └── ProjectService.cs   # Business logic layer
│   └── Program.cs              # API configuration
├── Dockerfile                  # Production container build
├── Dockerfile.dev             # Development container
├── docker-compose.yml         # Local development orchestration
├── render.yaml                # Render deployment config
└── .dockerignore              # Container build optimization
```

## 🎯 Key Features

### Modern Architecture
- **Full-Stack .NET**: Frontend and backend both in C#
- **Containerized Deployment**: Production-ready Docker containers
- **API-First Design**: RESTful backend with JSON responses
- **SPA Experience**: Client-side routing with API integration

### Backend API Endpoints
- `GET /api/projects` - Retrieve all projects
- `GET /api/projects/by-url?url={projectUrl}` - Get project by URL
- `GET /api/projects/categories/{category}` - Filter by category
- Health check endpoint for deployment monitoring

### Frontend Features
- **Dark Theme**: Professional dark color scheme with cyan accents
- **Responsive Design**: Mobile-first approach with Tailwind CSS
- **3D Effects**: Interactive tilt animations on project cards
- **API Integration**: Dynamic data loading with fallback support

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Docker (optional, for containerized development)
- Modern web browser

### Option 1: Docker Development (Recommended)

```bash
# Clone the repository
git clone https://github.com/DynoAbd786/website.git
cd website

# Run with Docker Compose
docker-compose up --build

# Access at: http://localhost:8080
```

### Option 2: Local Development Servers

**Terminal 1 - Start Backend API:**
```bash
cd website.api
dotnet run --urls "https://localhost:7233;http://localhost:7232"
```

**Terminal 2 - Start Frontend:**
```bash
cd website
dotnet run
```

- Frontend: http://localhost:5195 or https://localhost:7112
- API: https://localhost:7233 (with Swagger documentation)

### Option 3: Production Container

```bash
# Build and run production container
docker build -t portfolio-app .
docker run -p 8080:10000 portfolio-app

# Access at: http://localhost:8080
```

## 🐳 Docker Commands

### Development
```bash
# Run development environment
docker-compose up --build

# Run API only in development mode
docker-compose --profile dev up api-dev

# Stop all services
docker-compose down
```

### Production
```bash
# Build production image
docker build -t portfolio-app .

# Run production container
docker run -d -p 8080:10000 --name portfolio portfolio-app

# View logs
docker logs portfolio

# Stop and remove
docker stop portfolio && docker rm portfolio
```

## ☁️ Cloud Deployment

### Render Deployment
This project is configured for one-click deployment to Render:

1. Connect your GitHub repository to Render
2. The `render.yaml` file automatically configures:
   - Docker-based deployment
   - Health checks on `/api/projects`
   - Environment variables for production
   - Port 10000 configuration

### GitHub Pages (Frontend Only)
The existing GitHub Actions workflow deploys the frontend to GitHub Pages:
- Automatic deployment on main branch pushes
- Blazor WebAssembly static files only
- Custom domain configuration

## 📚 Learning Journey

This project demonstrates mastery of:

### Full-Stack .NET Development
- **Blazor WebAssembly**: Client-side C# web development
- **ASP.NET Core Web API**: RESTful backend development
- **Dependency Injection**: Both frontend and backend DI patterns
- **Async Programming**: Task-based asynchronous patterns throughout

### Cloud-Native Architecture
- **Containerization**: Multi-stage Docker builds
- **Microservices Patterns**: API-first design
- **Configuration Management**: Environment-based settings
- **Health Monitoring**: Application health endpoints

### Modern DevOps Practices
- **Infrastructure as Code**: Docker and Render configurations
- **Container Orchestration**: Docker Compose for development
- **Cloud Deployment**: Production-ready container deployment
- **Monitoring**: Application health and logging

### API Design Principles
- **RESTful Endpoints**: Standard HTTP methods and status codes
- **CORS Configuration**: Cross-origin resource sharing
- **Error Handling**: Proper exception management
- **Documentation**: Swagger/OpenAPI integration

## 🔧 Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: Server binding URLs
- Development/Production settings in `appsettings.json`

### CORS Configuration
- Development: Allows localhost origins
- Production: Configured for same-origin requests
- Supports credentials and all HTTP methods

### Static File Serving
- API serves both backend endpoints and frontend assets
- Fallback routing for SPA client-side navigation
- Production-optimized static file caching

## 🤝 AI Development Partnership

This project showcases advanced AI-assisted development:

### Claude Code Integration
- **Full-Stack Architecture**: End-to-end system design guidance
- **Containerization**: Docker configuration and best practices
- **API Development**: RESTful endpoint design and implementation
- **Cloud Deployment**: Production deployment configuration

### Modern Development Workflow
1. **System Architecture**: AI-assisted architectural decisions
2. **Backend Development**: API design and implementation guidance
3. **Frontend Integration**: Client-server communication patterns
4. **DevOps Setup**: Container and deployment configuration
5. **Testing & Validation**: End-to-end testing strategies

## 📄 API Documentation

### Projects Endpoints

#### Get All Projects
```http
GET /api/projects
```
Returns array of all projects with full details.

#### Get Project by URL
```http
GET /api/projects/by-url?url=/projects/taskflow
```
Returns specific project matching the URL path.

#### Get Projects by Category
```http
GET /api/projects/categories/Professional
```
Returns filtered projects by category (Personal, Professional, Academic).

### Response Format
```json
{
  "title": "Project Title",
  "description": "Detailed description...",
  "imageUrl": "https://...",
  "projectUrl": "/projects/...",
  "tags": ["Tag1", "Tag2"],
  "category": "Professional"
}
```

## 📦 Deployment Options

### 1. Render (Recommended)
- Automatic Docker deployment
- Environment variable management
- Health monitoring
- Custom domain support

### 2. Any Container Platform
- Docker Hub registry
- Kubernetes deployment
- Cloud Run, ECS, or similar

### 3. Traditional Hosting
- IIS deployment
- Linux/Nginx reverse proxy
- Static file serving configuration

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

## 🙋‍♂️ Contact

**Muhammad Kashif-Khan**
- Portfolio: [dynoabd786.github.io](https://dynoabd786.github.io)
- GitHub: [@DynoAbd786](https://github.com/DynoAbd786)
- LinkedIn: [dynoabd786](https://linkedin.com/in/dynoabd786)
- Email: Abdua786@outlook.com

---

*Built with 💙 using Blazor WebAssembly, ASP.NET Core, Docker, and Claude Code assistance*