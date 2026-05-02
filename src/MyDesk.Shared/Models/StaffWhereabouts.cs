using System;

namespace MyDesk.Shared.Models;

public class StaffWhereabouts
{
    public int WhereaboutId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime WeekStartDate { get; set; }
    public int DayOfWeek { get; set; } // 1=Monday, 2=Tuesday, ..., 5=Friday
    public int TimeSlot { get; set; } // 0=8am, 1=9am, ..., 10=6pm
    public string Status { get; set; } = "Available";
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}
