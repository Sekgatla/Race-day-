using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace RaceDay.Api.Security;

public sealed record SessionIdentity(int UserId, string Role, DateTimeOffset ExpiresAt);

public interface ISessionStore
{
    (string Token, DateTimeOffset ExpiresAt) Create(int userId, string role);
    bool TryGet(string token, out SessionIdentity? identity);
    bool Revoke(string token);
}

public sealed class InMemorySessionStore : ISessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, SessionIdentity> _sessions = new();

    public (string Token, DateTimeOffset ExpiresAt) Create(int userId, string role)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        _sessions[Hash(token)] = new SessionIdentity(userId, role, expiresAt);
        return (token, expiresAt);
    }

    public bool TryGet(string token, out SessionIdentity? identity)
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var key = Hash(token);
        if (!_sessions.TryGetValue(key, out var session))
        {
            return false;
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(key, out _);
            return false;
        }

        identity = session;
        return true;
    }

    public bool Revoke(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return _sessions.TryRemove(Hash(token), out _);
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
