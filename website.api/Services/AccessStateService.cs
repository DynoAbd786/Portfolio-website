using System.Collections.Concurrent;

namespace website.api.Services;

public enum AccessStatus
{
    Pending,
    Approved,
    Ready
}


public class AccessRequestInfo
{
    public AccessStatus Status { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public string Email { get; set; } = "";
}

public interface IAccessStateService
{
    string CreateRequest(string email);
    AccessStatus GetStatus(string id);
    void SetStatus(string id, AccessStatus status);
    void RefreshAccess(string id);
}

public class AccessStateService : IAccessStateService
{
    private readonly ConcurrentDictionary<string, AccessRequestInfo> _requests = new();
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(60);
    private readonly ILogger<AccessStateService> _logger;

    public AccessStateService(ILogger<AccessStateService> logger)
    {
        _logger = logger;
        _logger.LogInformation("AccessStateService initialized. Session timeout: {Timeout}", SessionTimeout);
    }

    public string CreateRequest(string email)
    {
        var id = Guid.NewGuid().ToString();
        _requests[id] = new AccessRequestInfo 
        { 
            Status = AccessStatus.Pending, 
            Email = email,
            LastAccessedAt = DateTime.UtcNow 
        };
        _logger.LogInformation("Created access request {Id} for {Email}", id, email);
        return id;
    }

    public AccessStatus GetStatus(string id)
    {
        if (_requests.TryGetValue(id, out var info))
        {
            var age = DateTime.UtcNow - info.LastAccessedAt;
            if (info.Status == AccessStatus.Ready)
            {
                _logger.LogInformation("Session {Id} ({Email}) age: {AgeMinutes:F2} min. Limit: {Limit} min", 
                    id, info.Email, age.TotalMinutes, SessionTimeout.TotalMinutes);

                if (age > SessionTimeout)
                {
                    _logger.LogWarning("Session {Id} expired after {AgeMinutes:F2} min", id, age.TotalMinutes);
                    _requests.TryRemove(id, out _);
                    return AccessStatus.Pending;
                }
            }
            return info.Status;
        }
        return AccessStatus.Pending;
    }

    public void SetStatus(string id, AccessStatus status)
    {
        if (_requests.TryGetValue(id, out var info))
        {
            info.Status = status;
            info.LastAccessedAt = DateTime.UtcNow;
            _logger.LogInformation("Status updated to {Status} for session {Id}", status, id);
        }
    }

    public void RefreshAccess(string id)
    {
        if (_requests.TryGetValue(id, out var info))
        {
            info.LastAccessedAt = DateTime.UtcNow;
            _logger.LogInformation("Access refreshed for session {Id}", id);
        }
    }
}
