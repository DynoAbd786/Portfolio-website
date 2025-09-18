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
        try
        {
            _logger.LogInformation("MCP request received: {Method}", request.Method);

            var response = await _mcpService.HandleRequestAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return Ok(new McpResponse
            {
                Id = request?.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal server error"
                }
            });
        }
    }

    [HttpGet("info")]
    public IActionResult GetServerInfo()
    {
        return Ok(new McpServerInfo
        {
            Name = "muhammad-portfolio-mcp",
            Version = "1.0.0",
            Capabilities = new McpCapabilities
            {
                Resources = true,
                Tools = true,
                Prompts = false
            }
        });
    }

    [HttpOptions("")]
    public IActionResult HandlePreflight()
    {
        return Ok();
    }
}