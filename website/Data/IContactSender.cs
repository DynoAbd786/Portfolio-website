namespace website.Data
{
    public record ContactMessage(string Name, string Email, string? Company, string? Role, string Subject, string Message);

    // Sends a message from the contact form. The form is handled on the server, so the host
    // supplies an implementation that emails it directly.
    public interface IContactSender
    {
        Task<bool> SendAsync(ContactMessage message);
    }
}
