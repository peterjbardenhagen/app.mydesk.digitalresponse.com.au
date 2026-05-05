using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// In-memory RFQ (Request For Quote) service ported from legacy MyDesk.
/// TODO: persist to database
/// </summary>
public class RfqService
{
    private readonly ILogger<RfqService> _logger;
    private static readonly object _lock = new();
    private static readonly List<Rfq> _store = SeedData();
    private static int _responseSeq = 100;

    public RfqService(ILogger<RfqService> logger)
    {
        _logger = logger;
    }

    public Task<List<Rfq>> GetAllAsync()
    {
        lock (_lock)
        {
            // return a shallow copy ordered by created date desc
            return Task.FromResult(_store
                .OrderByDescending(r => r.CreatedDate)
                .ToList());
        }
    }

    public Task<Rfq?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.FirstOrDefault(r => r.Id == id));
        }
    }

    public Task<Rfq> CreateAsync(Rfq rfq)
    {
        lock (_lock)
        {
            rfq.Id = _store.Count == 0 ? 1 : _store.Max(r => r.Id) + 1;
            if (string.IsNullOrWhiteSpace(rfq.RfqNumber))
                rfq.RfqNumber = $"RFQ-{DateTime.Now:yyyyMM}-{rfq.Id:D4}";
            rfq.CreatedDate = DateTime.Now;
            _store.Add(rfq);
            _logger.LogInformation("RFQ {RfqNumber} created", rfq.RfqNumber);
            return Task.FromResult(rfq);
        }
    }

    public Task<bool> UpdateAsync(Rfq rfq)
    {
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(r => r.Id == rfq.Id);
            if (existing == null) return Task.FromResult(false);
            existing.Title = rfq.Title;
            existing.Description = rfq.Description;
            existing.RequiredByDate = rfq.RequiredByDate;
            existing.Status = rfq.Status;
            existing.Suppliers = rfq.Suppliers;
            existing.OwnerUserCode = rfq.OwnerUserCode;
            existing.OwnerName = rfq.OwnerName;
            existing.Qid = rfq.Qid;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(int id)
    {
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(r => r.Id == id);
            if (existing == null) return Task.FromResult(false);
            _store.Remove(existing);
            _logger.LogInformation("RFQ {Id} deleted", id);
            return Task.FromResult(true);
        }
    }

    public Task<RfqResponse> AddSupplierResponseAsync(int rfqId, RfqResponse response)
    {
        lock (_lock)
        {
            var rfq = _store.FirstOrDefault(r => r.Id == rfqId)
                ?? throw new InvalidOperationException($"RFQ {rfqId} not found");
            response.Id = System.Threading.Interlocked.Increment(ref _responseSeq);
            response.RfqId = rfqId;
            response.ReceivedDate = DateTime.Now;
            rfq.Responses.Add(response);
            if (rfq.Status == RfqStatus.Sent || rfq.Status == RfqStatus.Draft)
                rfq.Status = RfqStatus.Responded;
            return Task.FromResult(response);
        }
    }

    public Task<bool> SelectWinnerAsync(int rfqId, int responseId)
    {
        lock (_lock)
        {
            var rfq = _store.FirstOrDefault(r => r.Id == rfqId);
            if (rfq == null) return Task.FromResult(false);
            var winner = rfq.Responses.FirstOrDefault(r => r.Id == responseId);
            if (winner == null) return Task.FromResult(false);
            rfq.WinningResponseId = responseId;
            rfq.Status = RfqStatus.Awarded;
            _logger.LogInformation("RFQ {Id} awarded to response {ResponseId}", rfqId, responseId);
            return Task.FromResult(true);
        }
    }

    public Task<Rfq> GenerateFromQuoteAsync(int qid, string title, string? description = null)
    {
        var rfq = new Rfq
        {
            Title = title,
            Description = description,
            Qid = qid,
            Status = RfqStatus.Draft
        };
        return CreateAsync(rfq);
    }

    private static List<Rfq> SeedData()
    {
        return new List<Rfq>
        {
            new()
            {
                Id = 1,
                RfqNumber = "RFQ-202604-0001",
                Title = "Variable Message Signs - 4 units",
                Description = "Required for VicRoads M1 upgrade project. 4 x VMS trailers with arrow boards.",
                CreatedDate = DateTime.Now.AddDays(-12),
                RequiredByDate = DateTime.Today.AddDays(20),
                Status = RfqStatus.Responded,
                Suppliers = new List<int> { 101, 102, 103 },
                OwnerName = "Sales Team",
                Responses = new List<RfqResponse>
                {
                    new() { Id = 101, RfqId = 1, SupplierId = 101, SupplierName = "Acme Signs Co", QuotedPrice = 48500m, LeadTimeDays = 10, Notes = "In stock", ReceivedDate = DateTime.Now.AddDays(-9) },
                    new() { Id = 102, RfqId = 1, SupplierId = 102, SupplierName = "Highway Hire Pty Ltd", QuotedPrice = 51200m, LeadTimeDays = 14, Notes = "Includes delivery", ReceivedDate = DateTime.Now.AddDays(-8) },
                    new() { Id = 103, RfqId = 1, SupplierId = 103, SupplierName = "TrafficTech", QuotedPrice = 47900m, LeadTimeDays = 21, Notes = "Best price, longer lead time", ReceivedDate = DateTime.Now.AddDays(-6) },
                }
            },
            new()
            {
                Id = 2,
                RfqNumber = "RFQ-202604-0002",
                Title = "LED Bollards - 200 units",
                Description = "Solar LED bollards for council pedestrian crossing rollout.",
                CreatedDate = DateTime.Now.AddDays(-5),
                RequiredByDate = DateTime.Today.AddDays(45),
                Status = RfqStatus.Sent,
                Suppliers = new List<int> { 201, 202 },
                OwnerName = "Procurement",
                Responses = new List<RfqResponse>()
            },
            new()
            {
                Id = 3,
                RfqNumber = "RFQ-202604-0003",
                Title = "Crash cushions - TMA grade",
                Description = "Truck mounted attenuators for hire fleet expansion.",
                CreatedDate = DateTime.Now.AddDays(-30),
                RequiredByDate = DateTime.Today.AddDays(-5),
                Status = RfqStatus.Awarded,
                WinningResponseId = 200,
                Suppliers = new List<int> { 101, 301 },
                OwnerName = "Operations",
                Responses = new List<RfqResponse>
                {
                    new() { Id = 199, RfqId = 3, SupplierId = 101, SupplierName = "Acme Signs Co", QuotedPrice = 178000m, LeadTimeDays = 30, ReceivedDate = DateTime.Now.AddDays(-22) },
                    new() { Id = 200, RfqId = 3, SupplierId = 301, SupplierName = "BarrierWorks", QuotedPrice = 162500m, LeadTimeDays = 28, Notes = "Selected — best price + warranty", ReceivedDate = DateTime.Now.AddDays(-20) },
                }
            }
        };
    }
}
