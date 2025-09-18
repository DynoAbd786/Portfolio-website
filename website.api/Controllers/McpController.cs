using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("")]
    public async Task<IActionResult> HandleMcpRequest([FromBody] McpRequest request)
    {
        _logger.LogInformation("=== MCP REQUEST START ===");
        _logger.LogInformation("HTTP Method: {HttpMethod}", HttpContext.Request.Method);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("Content-Type: {ContentType}", HttpContext.Request.ContentType);
        _logger.LogInformation("User-Agent: {UserAgent}", HttpContext.Request.Headers.UserAgent);
        _logger.LogInformation("Request Headers: {@Headers}", HttpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

        try
        {
            if (request == null)
            {
                _logger.LogWarning("Request body is null");
                return BadRequest("Request body is required");
            }

            _logger.LogInformation("MCP Request Details: ID={Id}, Method={Method}, JsonRpc={JsonRpc}",
                request.Id, request.Method, request.JsonRpc);

            if (request.Params != null)
            {
                _logger.LogInformation("Request Params: {@Params}", request.Params);
            }

            _logger.LogInformation("Calling MCP service...");
            var response = await _mcpService.HandleRequestAsync(request);

            _logger.LogInformation("MCP service returned response for ID: {Id}", response.Id);
            _logger.LogInformation("Response has error: {HasError}", response.Error != null);

            _logger.LogInformation("=== MCP REQUEST SUCCESS ===");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== MCP REQUEST ERROR === Exception occurred: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);

            var errorResponse = new McpResponse
            {
                Id = request?.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal server error"
                }
            };

            _logger.LogInformation("Returning error response with ID: {Id}", errorResponse.Id);
            return Ok(errorResponse);
        }
    }

    [HttpGet("")]
    public IActionResult GetMcpRoot()
    {
        _logger.LogInformation("=== MCP ROOT GET REQUEST ===");
        _logger.LogInformation("HTTP Method: {HttpMethod}", HttpContext.Request.Method);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("User-Agent: {UserAgent}", HttpContext.Request.Headers.UserAgent);

        _logger.LogInformation("Redirecting GET /api/mcp to server info");

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

        return Ok(new
        {
            message = "Muhammad Portfolio MCP Server",
            serverInfo,
            endpoints = new
            {
                info = "/api/mcp/info",
                mcp = "/api/mcp (POST for MCP requests)"
            }
        });
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