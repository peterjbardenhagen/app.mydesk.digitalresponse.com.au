namespace MyDesk.Shared.Services;

/// <summary>
/// Provides performance target values (company / team / user).
/// Implementation lives in the Web project so it can read its Config/targets.json.
/// </summary>
public interface ITargetsProvider
{
    decimal CompanyMonthlyTarget { get; }
    decimal CompanyQuarterlyTarget { get; }
    decimal CompanyYearlyTarget { get; }

    decimal DefaultUserMonthlyTarget { get; }
    decimal DefaultUserQuarterlyTarget { get; }
    decimal DefaultUserYearlyTarget { get; }

    decimal GetUserMonthlyTarget(string userCode);
    decimal GetUserQuarterlyTarget(string userCode);
    decimal GetUserYearlyTarget(string userCode);
}
