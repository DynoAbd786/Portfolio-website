using System.Net;

namespace website.api.Services;

// Who is asking, as far as the server can tell, for the contact limits. The live site runs on Render
// behind Cloudflare: the connection (and the X-Forwarded-For entry ASP.NET Core reads) is Cloudflare's
// edge server, which many visitors share, and Cloudflare puts the visitor's own address in
// CF-Connecting-IP. Without that header (local runs, tests) it's the connection's address. A made-up
// header only gets someone their own allowance; ContactLimiter's cap for everyone still holds.
public static class Visitor
{
    public static string Ip(HttpContext? http)
    {
        var header = http?.Request.Headers["CF-Connecting-IP"].ToString();
        if (IPAddress.TryParse(header, out var ip)) return ip.ToString();
        return http?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
