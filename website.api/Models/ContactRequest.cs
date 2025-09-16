using System.ComponentModel.DataAnnotations;

namespace website.api.Models;

public class ContactRequest
{
    [Required]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string? Company { get; set; }
    public string? Role { get; set; }

    [Required]
    public string Subject { get; set; } = "";

    [Required]
    public string Message { get; set; } = "";
}