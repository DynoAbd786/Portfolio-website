namespace website.Data
{
    public record ContactMessage(string Name, string Email, string? Company, string? Role, string Subject, string Message);

    // The bot checks that travel with the form: a field people never see, so only bots fill it in
    // (Trap), and when the server showed the form (Stamp, from StampForm)
    public record FormCheck(string? Trap, string? Stamp);

    public enum ContactResult
    {
        Sent,
        Dropped, // looked like a bot: nothing is sent, but the form says it was, so a bot learns nothing
        Limited, // too many messages from this visitor, or from everyone, in the last hour
        Undeliverable, // the address's domain has no mail server, so it's almost certainly a typo
        Failed,
    }

    // Sends a message from the contact form. The form is handled on the server, so the host
    // supplies an implementation that checks it and emails it directly.
    public interface IContactSender
    {
        string StampForm();
        Task<ContactResult> SendAsync(ContactMessage message, FormCheck check);
    }
}
