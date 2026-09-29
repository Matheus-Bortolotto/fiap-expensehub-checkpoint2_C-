using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Represents a successful authentication response.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// Gets or sets the bearer token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration time in UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the roles associated with the authenticated user.
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
