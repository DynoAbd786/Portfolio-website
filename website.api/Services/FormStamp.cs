using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace website.api.Services;

// When the contact form was shown, signed with the app's Data Protection keys (like the antiforgery
// token) so it can't be made up. Nobody writes a message in a few seconds; a bot posting straight
// after loading the page does.
public class FormStamp
{
    public static readonly TimeSpan MinWritingTime = TimeSpan.FromSeconds(3);

    private readonly IDataProtector _protector;
    private readonly TimeProvider _time;

    public FormStamp(IDataProtectionProvider protection, TimeProvider time)
    {
        _protector = protection.CreateProtector("website.contact-form.stamp");
        _time = time;
    }

    public string Issue() => _protector.Protect(_time.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));

    // Why a stamp doesn't pass, or null if it does
    public string? Problem(string? stamp)
    {
        if (string.IsNullOrEmpty(stamp)) return "no-stamp";
        string shown;
        try { shown = _protector.Unprotect(stamp); }
        catch (Exception e) when (e is CryptographicException or FormatException) { return "bad-stamp"; }
        if (!long.TryParse(shown, NumberStyles.None, CultureInfo.InvariantCulture, out var ms)) return "bad-stamp";
        return _time.GetUtcNow() - DateTimeOffset.FromUnixTimeMilliseconds(ms) < MinWritingTime ? "too-fast" : null;
    }
}
