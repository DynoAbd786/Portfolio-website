using System.Collections.Concurrent;

namespace website.api.Services;

public enum AccessStatus
{
    Pending,
    Approved, // Clicked in Discord (optional step if we want to track it)
    Ready     // System is ready (n8n finished)
}

public interface IAccessStateService
{
    string CreateRequest(string email);
    AccessStatus GetStatus(string id);
    void SetStatus(string id, AccessStatus status);
}

public class AccessStateService : IAccessStateService
{
    // ID -> Status
    private readonly ConcurrentDictionary<string, AccessStatus> _requests = new();
    
    // ID -> Email (for logging/debug)
    private readonly ConcurrentDictionary<string, string> _emails = new();

    public string CreateRequest(string email)
    {
        var id = Guid.NewGuid().ToString();
        _requests[id] = AccessStatus.Pending;
        _emails[id] = email;
        return id;
    }

    public AccessStatus GetStatus(string id)
    {
        if (_requests.TryGetValue(id, out var status))
        {
            return status;
        }
        return AccessStatus.Pending; // Default to pending if not found (or expired)
    }

    public void SetStatus(string id, AccessStatus status)
    {
        if (_requests.ContainsKey(id))
        {
            _requests[id] = status;
        }
    }
}
