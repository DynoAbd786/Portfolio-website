using System.Text.RegularExpressions;

namespace website.Data
{
    // What counts as an email address on the contact form, MCP and the assistant: name@domain.tld,
    // where the domain is letters, digits and hyphens and the ending is at least two letters (or an
    // xn-- one). [EmailAddress] only checks for an @, so typos like "ada@gmail" got through and the
    // reply bounced. Whether the domain takes mail is checked on the server (IMailDomains).
    public static class Emails
    {
        private static readonly Regex Shape = new(
            @"^[^@\s]+@(?:[\p{L}\p{N}-]+\.)+(?:\p{L}{2,}|xn--[a-z0-9-]+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool LooksRight(string? email) => email is not null && Shape.IsMatch(email.Trim());

        // Everything after the @, for an address that LooksRight
        public static string Domain(string email) => email.Trim()[(email.Trim().LastIndexOf('@') + 1)..];
    }
}
