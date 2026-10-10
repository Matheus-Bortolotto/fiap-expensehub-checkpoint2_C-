using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Users;

/// <summary>
/// Represents the data required to register a new user.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
