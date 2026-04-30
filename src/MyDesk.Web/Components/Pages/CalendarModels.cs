namespace MyDesk.Web.Components.Pages;

public class CalendarDay
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public List<CalendarEvent> Events { get; set; } = new();
}

public class CalendarEvent
{
    public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string? Link { get; set; }
    public string Type { get; set; } = "";
}
