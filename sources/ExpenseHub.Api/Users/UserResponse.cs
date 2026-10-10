namespace ExpenseHub.Api.Users;

/// <summary>
/// Represents a user returned by the administration endpoints.
/// </summary>
public sealed class UserResponse
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the roles currently assigned to the user.
    /// </summary>
    public string[] Roles { get; set; } = [];
}
