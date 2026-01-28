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
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

    public string CreateRequest(string email)
    {
        var id = Guid.NewGuid().ToString();
        _requests[id] = new AccessRequestInfo 
        { 
            Status = AccessStatus.Pending, 
            Email = email,
            LastAccessedAt = DateTime.UtcNow 
        };
        return id;
    }

    public AccessStatus GetStatus(string id)
    {
        if (_requests.TryGetValue(id, out var info))
        {
            if (info.Status == AccessStatus.Ready && (DateTime.UtcNow - info.LastAccessedAt) > SessionTimeout)
            {
                _requests.TryRemove(id, out _);
                return AccessStatus.Pending; // Treat as expired/not found
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
        }
    }

    public void RefreshAccess(string id)
    {
        if (_requests.TryGetValue(id, out var info))
        {
            info.LastAccessedAt = DateTime.UtcNow;
        }
    }
}
