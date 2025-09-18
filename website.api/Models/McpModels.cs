using System.Text.Json.Serialization;

namespace website.api.Models;

public class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "muhammad-portfolio-mcp";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new();
}

public class McpCapabilities
{
    [JsonPropertyName("resources")]
    public bool Resources { get; set; } = true;

    [JsonPropertyName("tools")]
    public bool Tools { get; set; } = true;

    [JsonPropertyName("prompts")]
    public bool Prompts { get; set; } = false;
}

public class McpResource
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

public class McpResourceContent
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "application/json";

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };
}

public class McpToolCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}

public class McpToolResult
{
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    public bool IsError { get; set; } = false;
}

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}