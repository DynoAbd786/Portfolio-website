using System.Globalization;
using DnsClient;

namespace website.api.Services;

// Can a reply reach someone at this domain? Only if it has a mail server (an MX record in DNS). The
// usual typo domains (gmial.com, outlok.com, hotmial.com) are parked websites with no mail server,
// and some say outright that they take no mail (a "null MX", like example.com). Mail can in theory
// fall back to a domain's website address (RFC 5321), but real mail domains publish MX records, and
// counting that would let those typos through.
public interface IMailDomains
{
    Task<bool> AcceptsMailAsync(string domain);
}

// Asks the system's DNS. If DNS itself fails or is slow, the address is let through: a hiccup here
// must never turn a real person away.
public sealed class MailDomains : IMailDomains
{
    // the form waits for this, so it doesn't wait long
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(3);

    private readonly ILookupClient _dns;
    private readonly ILogger<MailDomains> _logger;

    public MailDomains(ILookupClient dns, ILogger<MailDomains> logger)
    {
        _dns = dns;
        _logger = logger;
    }

    public async Task<bool> AcceptsMailAsync(string domain)
    {
        string name;
        try { name = new IdnMapping().GetAscii(domain.Trim()); } // bücher.de is xn--bcher-kva.de in DNS
        catch (ArgumentException) { return false; } // not a name DNS could hold

        using var patience = new CancellationTokenSource(Patience);
        try
        {
            var answer = await _dns.QueryAsync(name, QueryType.MX, QueryClass.IN, patience.Token);
            if (answer.Header.ResponseCode == DnsHeaderResponseCode.NotExistentDomain) return false;
            if (answer.HasError) return Unsure(domain, answer.ErrorMessage);
            // a null MX is one record pointing at "." (RFC 7505), which isn't a server
            return answer.Answers.MxRecords().Any(mx => mx.Exchange.Value.TrimEnd('.').Length > 0);
        }
        catch (Exception ex)
        {
            return Unsure(domain, ex.Message);
        }
    }

    private bool Unsure(string domain, string why)
    {
        _logger.LogWarning("Couldn't check whether {Domain} takes mail, so letting it through: {Why}", domain, why);
        return true;
    }
}
