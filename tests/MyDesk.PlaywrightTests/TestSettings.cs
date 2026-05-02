namespace MyDesk.PlaywrightTests;

public class TestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5236";
    public TestUserSettings TestUser { get; set; } = new();
    public bool Headless { get; set; } = false;
    public int SlowMo { get; set; } = 100;
    public int Timeout { get; set; } = 30000;
}

public class TestUserSettings
{
    public string Username { get; set; } = "peter bardenhagen";
    public string Password { get; set; } = "fairmont";
}
