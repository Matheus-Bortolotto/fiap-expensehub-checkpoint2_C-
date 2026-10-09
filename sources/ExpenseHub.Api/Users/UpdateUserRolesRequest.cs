namespace ExpenseHub.Api.Users;

/// <summary>
/// Represents the roles that should be assigned to a user.
/// </summary>
public sealed class UpdateUserRolesRequest
{
    /// <summary>
    /// Gets or sets the complete list of roles assigned to the user.
    /// </summary>
    public string[] Roles { get; set; } = [];
}
