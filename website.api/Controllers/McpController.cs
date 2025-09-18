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
        _logger.LogInformation("=== MCP REQUEST START (NO MODEL BINDING) ===");
        _logger.LogInformation("HTTP Method: {HttpMethod}", HttpContext.Request.Method);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("Content-Type: {ContentType}", HttpContext.Request.ContentType);
        _logger.LogInformation("User-Agent: {UserAgent}", HttpContext.Request.Headers.UserAgent);

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

    [HttpGet("")]
    public async Task<IActionResult> GetMcpRoot()
    {
        _logger.LogInformation("=== MCP ROOT GET REQUEST - INITIALIZING ===");
        _logger.LogInformation("HTTP Method: {HttpMethod}", HttpContext.Request.Method);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("User-Agent: {UserAgent}", HttpContext.Request.Headers.UserAgent);

        _logger.LogInformation("Creating default initialize request for GET");

        // Create a default "initialize" request and process it through the MCP service
        var initRequest = new McpRequest
        {
            Id = "http-get-init",
            Method = "initialize",
            JsonRpc = "2.0"
        };

        _logger.LogInformation("Processing initialize request through MCP service");

        // Use the existing McpService to handle the initialize request
        var response = await _mcpService.HandleRequestAsync(initRequest);

        _logger.LogInformation("Returning MCP initialize response for GET request");

        // Return the full McpResponse directly - this is what Claude expects
        return Ok(response);
    }

    [HttpGet("info")]
    public IActionResult GetServerInfo()
    {
        _logger.LogInformation("=== MCP INFO REQUEST ===");
        _logger.LogInformation("HTTP Method: {HttpMethod}", HttpContext.Request.Method);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("User-Agent: {UserAgent}", HttpContext.Request.Headers.UserAgent);

        var serverInfo = new McpServerInfo
        {
            Name = "muhammad-portfolio-mcp",
            Version = "1.0.0",
            Capabilities = new McpCapabilities
            {
                Resources = true,
                Tools = true,
                Prompts = false
            }
        };

        _logger.LogInformation("Returning server info: {@ServerInfo}", serverInfo);
        return Ok(serverInfo);
    }

    [HttpGet("debug/tools")]
    public async Task<IActionResult> GetToolsDebug()
    {
        _logger.LogInformation("=== DEBUG TOOLS REQUEST ===");

        // Create a mock tools/list request to test our response
        var mockRequest = new McpRequest
        {
            Id = "debug-tools",
            Method = "tools/list",
            JsonRpc = "2.0"
        };

        var response = await _mcpService.HandleRequestAsync(mockRequest);

        _logger.LogInformation("Debug tools response: {Response}",
            JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));

        return Ok(response);
    }

    [HttpOptions("")]
    public IActionResult HandlePreflight()
    {
        _logger.LogInformation("=== MCP OPTIONS REQUEST (CORS Preflight) ===");
        _logger.LogInformation("HTTP Method: {HttpMethod}", HttpContext.Request.Method);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("Origin: {Origin}", HttpContext.Request.Headers.Origin);
        _logger.LogInformation("Access-Control-Request-Method: {RequestMethod}",
            HttpContext.Request.Headers["Access-Control-Request-Method"]);
        _logger.LogInformation("Access-Control-Request-Headers: {RequestHeaders}",
            HttpContext.Request.Headers["Access-Control-Request-Headers"]);

        _logger.LogInformation("Returning OK for preflight request");
        return Ok();
    }
}