using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Tests declarative validation for public authentication and administration inputs.
/// </summary>
[TestClass]
public sealed class RequestValidationTests
{
    /// <summary>
    /// Verifies that a login without credentials is rejected as invalid input.
    /// </summary>
    [TestMethod]
    public void LoginRequest_WhenCredentialsAreMissing_IsInvalid()
    {
        LoginRequest request = new();

        bool isValid = TryValidate(request);

        Assert.IsFalse(isValid);
    }

    /// <summary>
    /// Verifies that a role-update request requires the roles collection.
    /// </summary>
    [TestMethod]
    public void UpdateUserRolesRequest_WhenRolesAreMissing_IsInvalid()
    {
        UpdateUserRolesRequest request = new()
        {
            Roles = null!
        };

        bool isValid = TryValidate(request);

        Assert.IsFalse(isValid);
    }

    private static bool TryValidate(object request)
    {
        List<ValidationResult> results = [];

        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);
    }
}
