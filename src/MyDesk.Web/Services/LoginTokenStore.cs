using System.Collections.Concurrent;

namespace MyDesk.Web.Services;

/// <summary>
/// Short-lived one-time-token store for Blazor Server login.
/// 
/// Problem: Blazor Server components run over a SignalR WebSocket, so the HTTP
/// response has already been committed by the time a component method runs.
/// You cannot set cookies (SignInAsync) from inside a Blazor circuit.
///
/// Solution: The Blazor Login component validates credentials, stores a
/// 30-second one-time token here, then navigates (forceLoad) to GET /auth/signin?token=xxx.
/// That normal HTTP endpoint consumes the token, calls SignInAsync, and redirects to /.
/// </summary>
public class LoginTokenStore
{
    private record Entry(int UserId, string UserCode, bool RememberMe, DateTime Expiry);

    private readonly ConcurrentDictionary<string, Entry> _tokens = new();

    /// <summary>Creates a one-time token valid for 30 seconds.</summary>
    public string CreateToken(int userId, string userCode, bool rememberMe)
    {
        Purge();
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new Entry(userId, userCode, rememberMe, DateTime.UtcNow.AddSeconds(30));
        return token;
    }

    /// <summary>Consumes (removes) a token. Returns null if expired or not found.</summary>
    public (int UserId, string UserCode, bool RememberMe)? ConsumeToken(string token)
    {
        if (_tokens.TryRemove(token, out var entry) && entry.Expiry > DateTime.UtcNow)
            return (entry.UserId, entry.UserCode, entry.RememberMe);
        return null;
    }

    private void Purge()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _tokens)
            if (kv.Value.Expiry <= now)
                _tokens.TryRemove(kv.Key, out _);
    }
}
