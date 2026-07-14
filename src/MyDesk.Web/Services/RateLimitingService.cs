using System.Collections.Concurrent;
using System.Diagnostics;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Rate limiting service for API protection against brute-force and DoS attacks.
/// Implements in-memory rate limiting with optional database-backed violation logging.
/// </summary>
public class RateLimitingService
{
    private readonly DatabaseService _db;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets;

    // Request tracking: Key = "identifier:endpoint", Value = list of request timestamps
    private readonly ConcurrentDictionary<string, Queue<long>> _requestHistory;

    public RateLimitingService(DatabaseService db, ILogger<RateLimitingService> logger)
    {
        _db = db;
        _logger = logger;
        _buckets = new();
        _requestHistory = new();
    }

    /// <summary>
    /// Check if a request should be rate limited
    /// </summary>
    public async Task<(bool Allowed, int Remaining, int RetryAfterSeconds)> CheckRateLimitAsync(
        string identifier,  // IP address or UserId
        string endpointPattern,
        string identifierType = "IP")  // 'IP' or 'USER'
    {
        var key = $"{identifier}:{endpointPattern}";
        var now = Stopwatch.GetTimestamp();

        // Get rate limiting rules from database
        var rulesResult = await _db.QueryAsync(
            @"SELECT RequestsPerWindow, WindowSizeSeconds, BackoffMultiplier, MaxBackoffSeconds
              FROM dbo.RateLimitingRules
              WHERE (EndpointPattern = @Pattern OR @Pattern LIKE REPLACE(EndpointPattern, '*', '%'))
                AND IsActive = 1
              ORDER BY EndpointPattern DESC",
            new() { ["Pattern"] = endpointPattern });

        if (rulesResult.Rows.Count == 0)
            return (true, -1, 0);  // No limit configured, allow request

        var rule = rulesResult.Rows[0];
        int requestsPerWindow = (int)rule["RequestsPerWindow"];
        int windowSizeSeconds = (int)rule["WindowSizeSeconds"];
        decimal? backoffMultiplier = rule["BackoffMultiplier"] != DBNull.Value ? (decimal?)rule["BackoffMultiplier"] : null;
        int? maxBackoffSeconds = rule["MaxBackoffSeconds"] != DBNull.Value ? (int?)rule["MaxBackoffSeconds"] : null;

        // Get request history for this identifier
        if (!_requestHistory.TryGetValue(key, out var requests))
        {
            requests = new Queue<long>();
            _requestHistory.TryAdd(key, requests);
        }

        // Remove old requests outside the window
        long windowStart = now - (windowSizeSeconds * Stopwatch.Frequency);
        int retryAfter = 0;
        bool limitExceeded = false;

        lock (requests)
        {
            while (requests.Count > 0 && requests.Peek() < windowStart)
                requests.Dequeue();

            if (requests.Count >= requestsPerWindow)
            {
                limitExceeded = true;
                retryAfter = (int)Math.Ceiling(maxBackoffSeconds.HasValue
                    ? Math.Min(windowSizeSeconds * Math.Pow(2, requests.Count - requestsPerWindow), maxBackoffSeconds.Value)
                    : windowSizeSeconds * Math.Pow(2, requests.Count - requestsPerWindow));

                _logger.LogWarning(
                    "Rate limit exceeded: {Identifier} on {Endpoint} ({Count} requests in {Window}s)",
                    identifier, endpointPattern, requests.Count, windowSizeSeconds);
            }
            else
            {
                // Request allowed, record it
                requests.Enqueue(now);
            }
        }

        // Log violation outside lock (cannot await inside lock)
        if (limitExceeded)
        {
            await LogViolationAsync(identifier, identifierType, endpointPattern, rulesResult.Rows[0]);
            return (false, 0, retryAfter);
        }

        int remaining = requestsPerWindow - requests.Count;
        return (true, remaining, 0);
    }

    /// <summary>
    /// Calculate backoff time based on violation count
    /// </summary>
    private int CalculateRetryAfter(int violationCount, decimal? backoffMultiplier, int? maxBackoff)
    {
        if (!backoffMultiplier.HasValue)
            return 60;  // Default 1 minute

        // Exponential backoff: baseTime * multiplier^violationCount
        decimal baseTime = 5;  // 5 seconds base
        decimal delaySeconds = baseTime * (decimal)Math.Pow((double)backoffMultiplier, violationCount - 1);

        int maxBackoffSeconds = maxBackoff ?? 3600;  // Default 1 hour max
        return Math.Min((int)delaySeconds, maxBackoffSeconds);
    }

    /// <summary>
    /// Log rate limit violation to database for security investigation
    /// </summary>
    private async Task LogViolationAsync(string identifier, string identifierType, string endpoint, DataRow rule)
    {
        try
        {
            string suspicionLevel = identifier.Contains(".") && identifier.Contains(":")
                ? "MEDIUM"  // IPv6
                : "LOW";    // IPv4

            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.RateLimitingViolations (RuleId, Identifier, IdentifierType, EndpointPattern, SuspicionLevel, IsAutoBlocked)
                  SELECT Id, @Identifier, @Type, @Endpoint, @Level, 0
                  FROM dbo.RateLimitingRules
                  WHERE EndpointPattern = @Endpoint AND IsActive = 1",
                new()
                {
                    ["Identifier"] = identifier,
                    ["Type"] = identifierType,
                    ["Endpoint"] = endpoint,
                    ["Level"] = suspicionLevel
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log rate limit violation for {Identifier}", identifier);
        }
    }

    /// <summary>
    /// Clean up old request buckets (called periodically to prevent memory leak)
    /// </summary>
    public void CleanupOldBuckets()
    {
        var now = Stopwatch.GetTimestamp();
        var expiredKeys = _requestHistory
            .Where(kvp =>
            {
                lock (kvp.Value)
                    return kvp.Value.Count == 0;
            })
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
            _requestHistory.TryRemove(key, out _);

        _logger.LogDebug("Cleaned up {Count} expired rate limit buckets", expiredKeys.Count);
    }

    /// <summary>
    /// Check if IP is auto-blocked for aggressive behavior
    /// </summary>
    public async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        var result = await _db.QueryAsync(
            @"SELECT COUNT(*) as cnt FROM dbo.RateLimitingViolations
              WHERE Identifier = @Ip AND BlockedUntil > GETUTCDATE() AND IsAutoBlocked = 1",
            new() { ["Ip"] = ipAddress });

        return (int)result.Rows[0]["cnt"] > 0;
    }
}

/// <summary>
/// Rate limit bucket for tracking request timing
/// </summary>
public class RateLimitBucket
{
    public string Identifier { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public Queue<DateTime> RequestTimes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
