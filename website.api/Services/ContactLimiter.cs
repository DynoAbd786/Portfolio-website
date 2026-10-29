namespace website.api.Services;

// How often contact messages may go out through MCP (AI agents and the site's assistant both use it):
// a few per visitor an hour, and a ceiling for everyone together, so the public endpoint can't flood
// the inbox. The web contact form posts on its own path.
public class ContactLimiter
{
    public const int PerVisitorPerHour = 5;
    public const int AllPerHour = 30;

    private static readonly TimeSpan Window = TimeSpan.FromHours(1);
    private readonly Dictionary<string, Queue<DateTime>> _byVisitor = new();
    private readonly Queue<DateTime> _all = new();
    private readonly object _lock = new();

    // Counts a send for this visitor if both limits allow it
    public bool TryTake(string visitor)
    {
        var now = DateTime.UtcNow;
        lock (_lock)
        {
            Prune(_all, now);
            foreach (var key in _byVisitor.Keys.ToList())
                if (Prune(_byVisitor[key], now).Count == 0) _byVisitor.Remove(key);

            var mine = _byVisitor.TryGetValue(visitor, out var q) ? q : null;
            if (_all.Count >= AllPerHour || mine?.Count >= PerVisitorPerHour) return false;

            if (mine is null) _byVisitor[visitor] = mine = new Queue<DateTime>();
            mine.Enqueue(now);
            _all.Enqueue(now);
            return true;
        }
    }

    private static Queue<DateTime> Prune(Queue<DateTime> q, DateTime now)
    {
        while (q.Count > 0 && now - q.Peek() > Window) q.Dequeue();
        return q;
    }
}
