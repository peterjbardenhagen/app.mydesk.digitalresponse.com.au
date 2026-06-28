using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MyDesk.Shared.Models;

/// <summary>
/// Multi-client platform configuration supporting Techlight, Digital Response, Carter Capner Law
/// </summary>
public class ClientPlatform
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";  // techlight, digital-response, carter-capner-law
    public string BrandingName { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string PrimaryColor { get; set; } = "#00c8c8";
    public bool EnableQuickBooksIntegration { get; set; }
    public bool EnableFrolloIntegration { get; set; }
    public bool EnableMYOBIntegration { get; set; }
    public bool EnableXeroIntegration { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClientPlatformSettings
{
    public int PlatformId { get; set; }
    public int CompanyId { get; set; }
    public string QuickBooksCompanyId { get; set; } = "";
    public string FrolloAccountId { get; set; } = "";
    public string FrolloInstitutionId { get; set; } = "";
    public string BankAccountId { get; set; } = "";
    public string BankAccountName { get; set; } = "";
    public string BankBsb { get; set; } = "";
}

public class EndOfMonthSummary
{
    public DateTime Month { get; set; }
    public int InvoiceCount { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal InvoiceGST { get; set; }
    public int ExpenseCount { get; set; }
    public decimal ExpenseTotal { get; set; }
    public int StatementCount { get; set; }
    public int ReconciledCount { get; set; }
    public bool IsBankReconciled => StatementCount > 0 && ReconciledCount == StatementCount;
}