# Muhammad's Portfolio MCP Server

This API exposes a Model Context Protocol (MCP) server that allows AI agents to interact with Muhammad Kashif-Khan's portfolio data.

## How it Works

The MCP server provides:
- **Resources**: Access to portfolio data (projects, profile, contact info)
- **Tools**: Interactive capabilities (submit contact forms, search projects)

## Available Resources

- `portfolio://profile` - Professional profile and bio information
- `portfolio://contact` - Contact information and response times
- `portfolio://projects/all` - Complete portfolio of all projects
- `portfolio://projects/{project-id}` - Individual project details

## Available Tools

### submit_contact
Submit a contact form message to Muhammad
```json
{
  "name": "Your Name",
  "email": "your@email.com",
  "subject": "Job Opportunity",
  "message": "Your message here"
}
```

### search_projects
Search projects by category, technology, or keyword
```json
{
  "category": "Professional", // Personal, Professional, Academic
  "technology": "Python",
  "keyword": "medical"
}
```

## Client Setup

### For Claude Desktop
Add to your `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "muhammad-portfolio": {
      "transport": "http",
      "url": "https://mcp.mkkai.dev",
      "description": "Muhammad's Portfolio MCP Server"
    }
  }
}
```

### For Custom MCP Clients
Use the HTTP transport to connect to: `https://mcp.mkkai.dev`

## Example Interactions

**User**: "What are Muhammad's Python projects?"
**AI**: *Queries search_projects tool* → Returns TaskFlow and pNanoLocz projects

**User**: "Send him a message about a senior developer role"
**AI**: *Uses submit_contact tool* → Submits contact form automatically

**User**: "Tell me about his professional experience"
**AI**: *Accesses portfolio://profile resource* → Returns education, skills, background

## Deployment

1. Deploy your ASP.NET Core API to your hosting platform
2. Ensure the `/mcp` endpoints are accessible via HTTPS
3. Share your MCP server URL with AI users
4. Users configure their AI clients to connect to your server

## Security

- CORS enabled for MCP endpoints
- Contact form submissions use the same email validation as the website
- No authentication required (portfolio is public information)
- Rate limiting should be implemented in production

## Testing

Test the server info endpoint:
```bash
curl https://mcp.mkkai.dev/info
```

Test a resource read:
```bash
curl -X POST https://mcp.mkkai.dev \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "resources/read",
    "params": {"uri": "portfolio://profile"}
  }'
```