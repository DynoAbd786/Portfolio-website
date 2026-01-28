using System.Text;
using System.Text.Json;
using website.api.Models;

namespace website.api.Services;

public interface IDiscordService
{
    Task<string?> SendAccessRequestAsync(AccessRequest request);
}

public class DiscordService : IDiscordService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordService> _logger;
    private readonly IAccessStateService _accessState;

    public DiscordService(HttpClient httpClient, IConfiguration configuration, ILogger<DiscordService> logger, IAccessStateService accessState)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _accessState = accessState;
    }

    public async Task<string?> SendAccessRequestAsync(AccessRequest request)
    {
        var botToken = _configuration["DISCORD_BOT_TOKEN"];
        var channelId = _configuration["DISCORD_CHANNEL_ID"];
        var n8nUrl = _configuration["N8N_WEBHOOK_URL"];

        if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(channelId))
        {
            _logger.LogError("Discord Bot Token or Channel ID is not configured.");
            return null;
        }

        // Generate Request ID
        var requestId = _accessState.CreateRequest(request.Email);
        
        // Append ID to N8N URL
        var buttonUrl = !string.IsNullOrEmpty(n8nUrl) 
            ? $"{n8nUrl}?id={requestId}" 
            : "https://example.com/missing-n8n-url";

        Console.WriteLine($"[DiscordService Debug] Generated ID: {requestId}, Button URL: {buttonUrl}");

        try
        {
            var payload = new
            {
                content = $"@everyone 🚀 New Access Request from **{request.Name}**",
                embeds = new[]
                {
                    new
                    {
                        title = "Access Request Details",
                        color = 3447003, // Blue
                        fields = new[]
                        {
                            new { name = "Name", value = request.Name, inline = true },
                            new { name = "Email", value = request.Email, inline = true },
                            new { name = "Reason", value = request.Reason, inline = false }
                        },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                },
                components = new[]
                {
                    new
                    {
                        type = 1, // Action Row
                        components = new[]
                        {
                            new
                            {
                                type = 2, // Button
                                style = 5, // Link Button
                                label = "✅ Approve Access",
                                url = buttonUrl
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/v10/channels/{channelId}/messages");
            requestMessage.Headers.Add("Authorization", $"Bot {botToken}");
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Discord notification sent for {Email}", request.Email);
                return requestId;
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send Discord notification. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DiscordService Error] {ex}");
            _logger.LogError(ex, "Error sending Discord notification");
            return null;
        }
    }

}
