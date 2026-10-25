# Blazor Web App: website.api renders the pages and serves the API;
# the website project is its WebAssembly client for interactive components.

# Stage 1: build and publish (publishing the host also publishes the client)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first so the package layer is cached. Publish restores again once the
# sources are in: the Blazor framework scripts only resolve when .razor files exist
COPY website/*.csproj ./website/
COPY website.api/*.csproj ./website.api/
RUN dotnet restore ./website.api/website.api.csproj

COPY website/ ./website/
COPY website.api/ ./website.api/

# The build downloads the Tailwind CLI into .tools/ and compiles the CSS
RUN dotnet publish ./website.api/website.api.csproj -c Release -o /app

# Stage 2: runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Render routes to port 10000
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "website.api.dll"]
