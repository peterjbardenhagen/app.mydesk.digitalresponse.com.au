namespace MyDesk.PlaywrightTests;

public class TestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5236";
    public TestUserSettings TestUser { get; set; } = new();
    /// <summary>
    /// Which tenant slug to pick on the /login/select-tenant page when the test
    /// user has multiple tenants. Defaults to "demo" so tests target the isolated
    /// Demo MyDesk tenant (seed data) rather than production Techlight/DR data.
    /// Override in appsettings.json TestSettings.TenantSlug.
    /// </summary>
    public string TenantSlug { get; set; } = "demo";
    public bool Headless { get; set; } = false;
    public int SlowMo { get; set; } = 100;
    public int Timeout { get; set; } = 30000;
}

public class TestUserSettings
{
    public string Username { get; set; } = "peter bardenhagen";
    public string Password { get; set; } = "fairmont";
}
