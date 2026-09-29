namespace ExpenseHub.Api.Identity;

internal static class ApplicationRoles
{
    internal const string Admin = "Admin";
    internal const string Employee = "Employee";
    internal const string Approver = "Approver";
    internal const string Finance = "Finance";
    internal const string Auditor = "Auditor";

    internal static readonly string[] All =
    [
        Admin,
        Employee,
        Approver,
        Finance,
        Auditor
    ];
}
