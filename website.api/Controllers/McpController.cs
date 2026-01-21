using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using website.api.Models;
using website.api.Services;

namespace website.api.Controllers;

[ApiController]
[Route("api/mcp")]
public class McpController : ControllerBase
{
    private readonly IMcpService _mcpService;
    private readonly ILogger<McpController> _logger;

    public McpController(IMcpService mcpService, ILogger<McpController> logger)
    {
        _mcpService = mcpService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleMcpRequest()
    {
        // Add cache-busting headers for MCP clients
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        _logger.LogInformation("=== MCP REQUEST START (NO MODEL BINDING) ===");
        _logger.LogInformation("HTTP Method: {HttpMethod}", HttpContext.Request.Method);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("Content-Type: {ContentType}", HttpContext.Request.ContentType);
        _logger.LogInformation("User-Agent: {UserAgent}", HttpContext.Request.Headers.UserAgent.ToString());

        // Log all request headers
        _logger.LogInformation("=== REQUEST HEADERS ===");
        foreach (var header in HttpContext.Request.Headers)
        {
            _logger.LogInformation("Header {Name}: {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
        }

        // Log OAuth context if available
        if (HttpContext.Items.ContainsKey("oauth_client_id"))
        {
            _logger.LogInformation("=== OAUTH CONTEXT ===");
            _logger.LogInformation("OAuth Client ID: {ClientId}", HttpContext.Items["oauth_client_id"]);
            _logger.LogInformation("OAuth Scope: {Scope}", HttpContext.Items["oauth_scope"]);
            _logger.LogInformation("OAuth Resource: {Resource}", HttpContext.Items["oauth_resource"]);
        }

        try
        {
            // Read raw body
            HttpContext.Request.EnableBuffering();
            using var reader = new StreamReader(HttpContext.Request.Body);
            var body = await reader.ReadToEndAsync();
            HttpContext.Request.Body.Position = 0;

            _logger.LogInformation("Raw request body: {Body}", body);

            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Request body is empty");
                return BadRequest("Request body is required");
            }

            // Try to parse JSON
            McpRequest? request = null;
            try
            {
                request = JsonSerializer.Deserialize<McpRequest>(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("Successfully parsed request: {@Request}", request);
            }
            catch (Exception parseEx)
            {
                _logger.LogError(parseEx, "Failed to parse JSON: {Message}", parseEx.Message);
                return BadRequest($"Invalid JSON: {parseEx.Message}");
            }

            if (request == null)
            {
                _logger.LogWarning("Parsed request is null");
                return BadRequest("Invalid request format");
            }

            _logger.LogInformation("MCP Request Details: ID={Id}, Method={Method}, JsonRpc={JsonRpc}",
                request.Id, request.Method, request.JsonRpc);

            _logger.LogInformation("Calling MCP service...");
            var response = await _mcpService.HandleRequestAsync(request);

            _logger.LogInformation("MCP service returned response for ID: {Id}", response.Id);
            _logger.LogInformation("=== MCP REQUEST SUCCESS ===");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== MCP REQUEST ERROR === Exception occurred: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);

            var errorResponse = new McpResponse
            {
                Id = null,
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal server error"
                }
            };

            return Ok(errorResponse);
        }
    }

    // --- SSE IMPLEMENTATION FOR STANDARD MCP CLIENTS ---
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SseClient> _connectedClients = new();

    [HttpGet("sse")]
    public async Task HandleSseConnection()
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // For Nginx/proxies

        // Flush headers immediately to establish connection
        await Response.Body.FlushAsync();

        var sessionId = Guid.NewGuid().ToString();
        var client = new SseClient(Response);
        _connectedClients.TryAdd(sessionId, client);

        _logger.LogInformation($"Client connected via SSE. SessionId: {sessionId}");

        try
        {
            // Calculate the correct endpoint for POST messages
            // Use X-Forwarded-Proto/Host if available (e.g. from Render load balancer)
            var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
            var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.ToString();
            
            // Fallback to simple construction if headers missing (local dev)
            var endpointUri = $"{scheme}://{host}/api/mcp/message?sessionId={sessionId}";
            
            _logger.LogInformation($"Sending endpoint: {endpointUri}");
            await client.SendEventAsync("endpoint", endpointUri);

            // Keep connection open with heartbeats
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(15000); // 15s heartbeat
                await client.SendEventAsync("ping", "keepalive");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE connection error");
        }
        finally
        {
            _connectedClients.TryRemove(sessionId, out _);
            _logger.LogInformation($"Client disconnected. SessionId: {sessionId}");
        }
    }

    [HttpPost("message")]
    public async Task<IActionResult> HandleSseMessage([FromQuery] string sessionId)
    {
        if (!_connectedClients.TryGetValue(sessionId, out var client))
        {
            return NotFound("Session not found");
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        
        try 
        {
            var request = JsonSerializer.Deserialize<McpRequest>(body, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            if (request != null)
            {
                // Handle the request via our service
                var response = await _mcpService.HandleRequestAsync(request);
                
                // Send the response back via SSE
                await client.SendEventAsync("message", JsonSerializer.Serialize(response, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }));
                
                return Accepted();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SSE message");
            return BadRequest(ex.Message);
        }

        return BadRequest("Invalid request");
    }

    private class SseClient
    {
        private readonly HttpResponse _response;
        public SseClient(HttpResponse response) => _response = response;

        public async Task SendEventAsync(string eventType, string data)
        {
            try 
            {
                await _response.WriteAsync($"event: {eventType}\n");
                await _response.WriteAsync($"data: {data}\n\n");
                await _response.Body.FlushAsync();
            }
            catch (Exception)
            {
                // Ignore write errors as they likely mean client disconnected
            }
        }
    }
}