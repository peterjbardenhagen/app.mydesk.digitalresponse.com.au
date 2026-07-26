using MyDesk.Browser.Models;
using MyDesk.Browser.Services;
using System.IO;
using Xunit;

namespace MyDesk.Browser.Tests;

public class SupportTicketServiceTests : IDisposable
{
    private readonly SupportTicketService _service = new();

    public void Dispose()
    {
        // Clean up test data to avoid cross-test contamination
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyDesk", "Browser", "SupportTickets");
        if (Directory.Exists(appData))
            Directory.Delete(appData, true);
    }

    [Fact]
    public void Submit_CreatesTicketWithSubmittedStatus()
    {
        var ticket = _service.Submit(
            "Cannot login", "Login page returns 500 error",
            "High", "Technical", "alice@techlight.com.au");

        Assert.NotNull(ticket);
        Assert.Equal("Submitted", ticket.Status);
        Assert.Equal("Cannot login", ticket.Subject);
        Assert.Equal("High", ticket.Priority);
    }

    [Fact]
    public void Submit_AddsToTicketsList()
    {
        var beforeCount = _service.Tickets.Count;
        _service.Submit("Issue 1", "Desc", "Low", "Billing", "user@test.com");
        Assert.Equal(beforeCount + 1, _service.Tickets.Count);
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        var ticket = _service.Submit("Bug", "Details", "Medium", "Technical", "dev@test.com");
        _service.UpdateStatus(ticket.Id, "In Progress");
        var updated = _service.Tickets.First(t => t.Id == ticket.Id);
        Assert.Equal("In Progress", updated.Status);
    }

    [Fact]
    public void UpdateStatus_UnknownId_DoesNotAddNewTickets()
    {
        var beforeCount = _service.Tickets.Count;
        _service.UpdateStatus("nonexistent-id", "Resolved");
        Assert.Equal(beforeCount, _service.Tickets.Count);
    }

    [Fact]
    public void TechlightSupportTicket_CreatesCorrectly()
    {
        var ticket = _service.Submit(
            "Techlight onboarding issue",
            "Cannot access Techlight portal after SSO update",
            "High", "Technical", "admin@techlight.com.au");

        Assert.Contains("Techlight", ticket.Subject);
        Assert.Equal("admin@techlight.com.au", ticket.SubmittedBy);
    }

    [Fact]
    public void MultipleTickets_AllPersisted()
    {
        var beforeCount = _service.Tickets.Count;
        _service.Submit("Issue A", "Desc A", "Low", "General", "a@test.com");
        _service.Submit("Issue B", "Desc B", "High", "Billing", "b@test.com");
        _service.Submit("Issue C", "Desc C", "Medium", "Technical", "c@test.com");
        Assert.Equal(beforeCount + 3, _service.Tickets.Count);
    }
}
