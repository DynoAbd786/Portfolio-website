# Personal Portfolio Website

A modern, responsive portfolio website built with Blazor WebAssembly and .NET 8 to showcase projects and document my C# learning journey.

## 🚀 Live Demo

Visit the live portfolio at: [https://dynoabd786.github.io](https://dynoabd786.github.io)

## 📋 Overview

This portfolio serves multiple purposes:
- **Professional Showcase**: Display projects for recruiters and employers
- **Learning Documentation**: Track my C# and .NET development journey
- **AI-Assisted Development Study**: Demonstrate modern development workflows using Claude Code

## 🛠️ Tech Stack

- **Framework**: Blazor WebAssembly
- **Runtime**: .NET 8
- **Language**: C# with nullable types enabled
- **Styling**: Tailwind CSS (via CDN)
- **Icons**: Font Awesome 6.4.0
- **Fonts**: Google Fonts (Roboto Mono, Source Code Pro)
- **Animations**: Vanilla Tilt.js for 3D card effects
- **Development Assistant**: Claude Code AI

## 🏗️ Project Structure

```
website/
├── Data/                          # Data models and services
│   ├── Project.cs                # Project model definition
│   └── ProjectService.cs         # Project data service
├── Pages/                         # Razor pages
│   ├── Index.razor               # Homepage
│   ├── Projects.razor            # Projects listing
│   ├── About.razor               # About page
│   ├── TaskFlowProject.razor     # TaskFlow project details
│   └── AIResearchProject.razor   # Portfolio project details
├── Shared/                        # Shared components
│   ├── MainLayout.razor          # Main layout wrapper
│   └── ProjectCard.razor         # Reusable project card component
├── wwwroot/                       # Static assets
│   ├── css/app.css               # Custom styles
│   ├── js/vanilla-tilt.min.js    # 3D tilt library
│   └── index.html                # App entry point
├── App.razor                      # App component with routing
├── Program.cs                     # Application entry point
└── website.csproj                # Project configuration
```

## 🎯 Key Features

### Modern UI/UX
- **Dark Theme**: Professional dark color scheme with cyan accents
- **Responsive Design**: Mobile-first approach with Tailwind CSS
- **3D Effects**: Interactive tilt animations on project cards
- **Smooth Animations**: Hover effects and transitions throughout

### Technical Architecture
- **Client-Side Rendering**: Full WebAssembly deployment for optimal performance
- **Component-Based**: Modular Razor component architecture
- **Service Pattern**: Dependency injection with scoped services
- **Type Safety**: C# nullable reference types enabled

### Content Management
- **Dynamic Projects**: Service-based project data with rich descriptions
- **Categorized Sections**: Personal projects vs professional experience
- **Detailed Project Pages**: Comprehensive project breakdowns
- **SEO Friendly**: Proper page titles and meta descriptions

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Modern web browser

### Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/DynoAbd786/website.git
   cd website
   ```

2. **Navigate to project directory**
   ```bash
   cd website
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Run the development server**
   ```bash
   dotnet run
   ```

5. **Open in browser**
   - HTTP: http://localhost:5195
   - HTTPS: https://localhost:7112

### Development Commands

```bash
# Watch mode (auto-reload on changes)
dotnet watch

# Build for production
dotnet build -c Release

# Publish for deployment
dotnet publish -c Release
```

## 📚 Learning Journey

This project represents my exploration of:

### C# & .NET Concepts
- **Modern C# Features**: Records, nullable types, pattern matching
- **Blazor WebAssembly**: Client-side C# web development
- **Component Lifecycle**: Razor component patterns and lifecycle methods
- **Dependency Injection**: Built-in DI container usage
- **Async Programming**: Task-based asynchronous patterns

### Web Development
- **SPA Architecture**: Single-page application patterns
- **Responsive Design**: Mobile-first CSS frameworks
- **Performance Optimization**: Client-side rendering strategies
- **Modern Tooling**: .NET CLI, hot reload, debugging

### AI-Assisted Development
- **Claude Code Integration**: Leveraging AI for learning acceleration
- **Prompt Engineering**: Effective AI collaboration techniques
- **Modern Workflows**: AI-augmented development practices

## 🎨 Design Philosophy

### Visual Design
- **Dark Theme**: Reduces eye strain, professional appearance
- **Minimalist Layout**: Clean, focused content presentation
- **Consistent Typography**: Carefully selected font pairings
- **Strategic Color Use**: Cyan accents for key interactive elements

### User Experience
- **Fast Loading**: Client-side rendering with optimized assets
- **Mobile Responsive**: Touch-friendly design for all devices
- **Intuitive Navigation**: Clear information architecture
- **Accessibility**: Semantic HTML and ARIA labels

## 🔧 Configuration

### Launch Profiles
The application supports multiple launch profiles:
- **HTTP Profile**: Development server on port 5195
- **HTTPS Profile**: Secure development server on port 7112
- **IIS Express**: Integration with Visual Studio

### Environment Variables
- Development settings in `Properties/launchSettings.json`
- Production configuration via deployment environment

## 📦 Deployment

### GitHub Pages Deployment
The site is automatically deployed to GitHub Pages using GitHub Actions:

1. **Build Process**: .NET publish creates optimized WebAssembly output
2. **Asset Optimization**: CSS/JS minification and compression
3. **Custom Domain**: Configured for `dynoabd786.github.io`
4. **Automatic Updates**: Deployments trigger on main branch pushes

### Manual Deployment
```bash
# Build for production
dotnet publish -c Release -o dist

# Deploy dist/wwwroot contents to web server
```

## 🤝 AI Development Partnership

This project showcases modern AI-assisted development:

### Claude Code Integration
- **Architecture Guidance**: System design and best practices
- **Code Generation**: Component scaffolding and boilerplate
- **Learning Acceleration**: Real-time explanations and mentorship
- **Quality Assurance**: Code review and optimization suggestions

### Development Workflow
1. **Concept Discussion**: Project planning with AI assistance
2. **Iterative Development**: Feature implementation with AI guidance
3. **Code Review**: AI-powered quality checks and improvements
4. **Documentation**: AI-assisted README and code documentation

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

## 🙋‍♂️ Contact

**Muhammad Kashif-Khan**
- Portfolio: [dynoabd786.github.io](https://dynoabd786.github.io)
- GitHub: [@DynoAbd786](https://github.com/DynoAbd786)
- LinkedIn: [dynoabd786](https://linkedin.com/in/dynoabd786)
- Email: Abdua786@outlook.com

---

*Built with 💙 using Blazor WebAssembly and Claude Code assistance*