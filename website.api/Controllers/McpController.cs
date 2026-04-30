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
    private readonly IConfiguration _configuration;

    public McpController(IMcpService mcpService, ILogger<McpController> logger, IConfiguration configuration)
    {
        _mcpService = mcpService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> HandleRootGet()
    {
        // If the client wants SSE, delegate to the SSE handler
        if (Request.Headers["Accept"].ToString().Contains("text/event-stream"))
        {
            return await HandleSseConnection();
        }

        // Otherwise, provide basic discovery info
        return Ok(new 
        { 
            status = "MCP Server Operational", 
            sse_endpoint = "/api/mcp/sse",
            message_endpoint = "/api/mcp/message"
        });
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
    
    [HttpGet("sse")]
    [HttpPost("sse")] // Support POST for initialization handshake if needed
    public async Task<IActionResult> HandleSseConnection()
    {
        if (Request.Method == "POST")
        {
            // Just treat it as a message or a ping if it's hitting the SSE URL with POST
            return await HandleMcpRequest();
        }

        // SECURITY CHECK (Hybrid Model)
        // If BRIDGE_API_KEY is allowed to be missing (public), connection is "Guest"
        // If BRIDGE_API_KEY is present, we check it.
        // - Match = "Bridge" role (can receive chat requests)
        // - Mismatch = "Guest" role (can only use tools, cannot be a bridge)
        
        bool isBridge = false;
        var apiKey = _configuration["BRIDGE_API_KEY"];

        if (!string.IsNullOrEmpty(apiKey))
        {
            if (Request.Headers.TryGetValue("X-Bridge-Key", out var providedKey) && providedKey == apiKey)
            {
                isBridge = true;
                _logger.LogInformation("Authenticated Bridge connection accepted.");
            }
            else
            {
                _logger.LogInformation("Unauthenticated connection accepted as Guest (Read-Only/Tools).");
            }
        }
        else
        {
             // If no key configured on server, everything is "Guest" to be safe, 
             // or "Bridge" if you want totally open system. defaulting to Guest for safety.
             _logger.LogWarning("No BRIDGE_API_KEY configured. Connection accepted as Guest.");
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // For Nginx/proxies

        // Disable response buffering for the server
        var bufferingFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        if (bufferingFeature != null)
        {
            bufferingFeature.DisableBuffering();
        }

        // Flush headers immediately to establish connection
        await Response.Body.FlushAsync();

        var sessionId = Guid.NewGuid().ToString();
        
        // Register connection with the Singleton Service
        _mcpService.RegisterConnection(sessionId, Response, isBridge);

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
            
            // Manual event sending (since we are responding on this thread)
            await Response.WriteAsync($"event: endpoint\n");
            await Response.WriteAsync($"data: {endpointUri}\n\n");
            await Response.Body.FlushAsync();

            // Keep connection open with heartbeats
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(15000); // 15s heartbeat
                await Response.WriteAsync($"event: ping\n");
                await Response.WriteAsync($"data: keepalive\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE connection error");
        }
        finally
        {
            _mcpService.RemoveConnection(sessionId);
            _logger.LogInformation($"Client disconnected. SessionId: {sessionId}");
        }

        return new EmptyResult();
    }

    [HttpPost("message")]
    public async Task<IActionResult> HandleSseMessage([FromQuery] string sessionId)
    {
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
                // SPECIAL HANDLER: Bridge Responses (Chat)
                if (request.Method == "notifications/chat_response" && request.Params != null)
                {
                    string? callbackId = null;
                    string? content = null;
                    string? model = "unknown";
                    List<OllamaToolCall>? toolCalls = null;
                    bool toolsDisabled = false;

                    if (request.Params is JsonElement paramsElem)
                    {
                        if (paramsElem.TryGetProperty("callback_id", out var idElem))
                        {
                            callbackId = idElem.ToString();
                        }
                        
                        if (paramsElem.TryGetProperty("response", out var respElem))
                        {
                            content = respElem.ToString();
                        }

                        if (paramsElem.TryGetProperty("model", out var modelElem))
                        {
                            model = modelElem.ToString();
                        }

                        if (paramsElem.TryGetProperty("tool_calls", out var toolCallsElem) && toolCallsElem.ValueKind == JsonValueKind.Array)
                        {
                            toolCalls = JsonSerializer.Deserialize<List<OllamaToolCall>>(toolCallsElem.GetRawText());
                        }

                        if (paramsElem.TryGetProperty("tools_disabled", out var disabledElem))
                        {
                            toolsDisabled = disabledElem.GetBoolean();
                        }
                    }

                    if (!string.IsNullOrEmpty(callbackId)) 
                    {
                         _mcpService.HandleChatResponse(callbackId, content ?? "", model, toolCalls, toolsDisabled);
                         return Ok(new { status = "accepted", method = request.Method });
                    }
                }

                // SPECIAL HANDLER: Bridge Errors
                if (request.Method == "notifications/chat_error" && request.Params != null)
                {
                    string? callbackId = null;
                    string? error = null;

                    if (request.Params is JsonElement paramsElem)
                    {
                        if (paramsElem.TryGetProperty("callback_id", out var idElem)) callbackId = idElem.ToString();
                        if (paramsElem.TryGetProperty("error", out var errElem)) error = errElem.ToString();
                    }

                    if (!string.IsNullOrEmpty(callbackId))
                    {
                        _mcpService.HandleChatError(callbackId, error ?? "Unknown bridge error");
                        return Ok(new { status = "accepted", method = request.Method });
                    }
                }

                // SPECIAL HANDLER: Bridge Responses (Models)
                if (request.Method == "notifications/models_response" && request.Params != null)
                {
                    string? callbackId = null;
                    List<string>? models = null;

                    if (request.Params is JsonElement paramsElem)
                    {
                        if (paramsElem.TryGetProperty("callback_id", out var idElem))
                        {
                            callbackId = idElem.ToString();
                        }
                        
                        if (paramsElem.TryGetProperty("models", out var modelsElem) && modelsElem.ValueKind == JsonValueKind.Array)
                        {
                            models = JsonSerializer.Deserialize<List<string>>(modelsElem.GetRawText());
                        }
                    }

                    if (!string.IsNullOrEmpty(callbackId) && models != null) 
                    {
                         _mcpService.HandleModelsResponse(callbackId, models);
                         return Ok(new { status = "accepted", method = request.Method });
                    }
                }

                // Normal Handler
                var response = await _mcpService.HandleRequestAsync(request);
                
                // Note: We don't need to manually send the response back via SSE here anymore
                // because standard Clients (like prompts) usually poll or expect an immediate HTTP return if using POST.
                // However, the JSON-RPC spec over SSE implies we might push the response via the connection if checking ID.
                // For simplicity and compatibility with most MCP clients, if we receive a POST, we return the result in the HTTP Body.
                // BUT, if it was an Async notification, we return Accepted.
                
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SSE message");
            return BadRequest(ex.Message);
        }

        return BadRequest("Invalid request");
    }

    [HttpGet("status/bridge")]
    public IActionResult GetBridgeStatus()
    {
        return Ok(new { isConnected = _mcpService.IsBridgeConnected() });
    }
}