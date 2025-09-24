# Multi-stage Dockerfile for Blazor WebAssembly + ASP.NET Core API

# Stage 1: Build the API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /source

# Copy API project files
COPY website.api/*.csproj ./website.api/
RUN dotnet restore ./website.api/website.api.csproj

# Copy API source code
COPY website.api/ ./website.api/

# Build and publish API
RUN dotnet publish ./website.api/website.api.csproj -c Release -o /app/api

# Stage 2: Build the Blazor WebAssembly app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS wasm-build
WORKDIR /source

# Copy frontend project files
COPY website/*.csproj ./website/
RUN dotnet restore ./website/website.csproj

# Copy frontend source code
COPY website/ ./website/

# Build and publish Blazor WASM with all framework files
RUN dotnet publish ./website/website.csproj -c Release -o /app/published

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy API from build stage
COPY --from=api-build /app/api .

# Copy Blazor WASM files to wwwroot (published output structure)
COPY --from=wasm-build /app/published/wwwroot ./wwwroot

# Ensure proper permissions for all files
RUN chmod -R 644 ./wwwroot/*

# Expose port (Render uses port 10000)
EXPOSE 10000

# Configure ASP.NET Core to serve static files
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

# Update API Program.cs will be needed to serve static files
ENTRYPOINT ["dotnet", "website.api.dll"]