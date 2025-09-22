using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Distributed;
using MapleBlog.Domain.Entities;
using MapleBlog.Application.Interfaces;
using System.Text.Json;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// JWT service implementation with token management and blacklist support
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly IDistributedCache _cache;
    private readonly RSA _rsa;
    private readonly RsaSecurityKey _publicKey;
    private readonly RsaSecurityKey _privateKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _accessTokenLifetime;
    private readonly TimeSpan _refreshTokenLifetime;

    public JwtService(
        IConfiguration configuration,
        ILogger<JwtService> logger,
        IDistributedCache cache)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        // Initialize RSA keys
        _rsa = RSA.Create(2048);
        LoadOrGenerateRSAKeys();

        _publicKey = new RsaSecurityKey(_rsa);
        _privateKey = new RsaSecurityKey(_rsa);

        // Load configuration
        _issuer = _configuration["Jwt:Issuer"] ?? "MapleBlog";
        _audience = _configuration["Jwt:Audience"] ?? "MapleBlog";

        var accessTokenLifetimeMinutes = _configuration.GetValue<int>("Jwt:AccessTokenLifetimeMinutes");
        _accessTokenLifetime = TimeSpan.FromMinutes(accessTokenLifetimeMinutes > 0 ? accessTokenLifetimeMinutes : 15);

        var refreshTokenLifetimeDays = _configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays");
        _refreshTokenLifetime = TimeSpan.FromDays(refreshTokenLifetimeDays > 0 ? refreshTokenLifetimeDays : 7);

        _logger.LogInformation("JWT Service initialized with {AccessTokenLifetime} access token lifetime and {RefreshTokenLifetime} refresh token lifetime",
            _accessTokenLifetime, _refreshTokenLifetime);
    }

    public string GenerateAccessToken(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var tokenId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var accessTokenExpiry = now.Add(_accessTokenLifetime);

        // Create claims
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("display_name", user.DisplayName),
            new("email_verified", user.EmailConfirmed.ToString().ToLower()),
            new("is_active", user.IsActive.ToString().ToLower())
        };

        // Create access token
        var accessToken = CreateJwtToken(claims, accessTokenExpiry);
        var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

        _logger.LogDebug("Generated JWT access token for user {UserId} ({UserName}) with token ID {TokenId}",
            user.Id, user.UserName, tokenId);

        return accessTokenString;
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = CreateTokenValidationParameters();

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    public bool ValidateToken(string token)
    {
        return GetPrincipalFromToken(token) != null;
    }

    public DateTime GetTokenExpiration(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return DateTime.MinValue;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get token expiration");
            return DateTime.MinValue;
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        var principal = GetPrincipalFromToken(token);
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetUserNameFromToken(string token)
    {
        var principal = GetPrincipalFromToken(token);
        return principal?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public string? GetRoleFromToken(string token)
    {
        var principal = GetPrincipalFromToken(token);
        return principal?.FindFirst(ClaimTypes.Role)?.Value;
    }

    public bool IsTokenExpiringSoon(string token, int minutesThreshold = 5)
    {
        var expiration = GetTokenExpiration(token);
        if (expiration == DateTime.MinValue)
            return true;

        return expiration <= DateTime.UtcNow.AddMinutes(minutesThreshold);
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenId = ExtractTokenId(token);
        if (!string.IsNullOrEmpty(tokenId))
        {
            var expiration = GetTokenExpiration(token);
            await BlacklistTokenAsync(tokenId, cancellationToken);
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenId = ExtractTokenId(token);
        if (string.IsNullOrEmpty(tokenId))
            return false;

        var key = $"blacklist:token:{tokenId}";
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return !string.IsNullOrEmpty(value);
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.Add(_accessTokenLifetime);

        // Store refresh token
        var refreshTokenData = new
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime)
        };

        var key = $"refresh_token:{refreshToken}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _refreshTokenLifetime
        };

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(refreshTokenData), options, cancellationToken);

        _logger.LogDebug("Generated token pair for user {UserId}", user.Id);

        return (accessToken, refreshToken, expiresAt);
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        try
        {
            var key = $"refresh_token:{refreshToken}";
            var data = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(data))
                return null;

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;

            if (root.TryGetProperty("UserId", out var userIdElement) &&
                root.TryGetProperty("ExpiresAt", out var expiresAtElement))
            {
                var userId = Guid.Parse(userIdElement.GetString()!);
                var expiresAt = DateTime.Parse(expiresAtElement.GetString()!);

                if (expiresAt <= DateTime.UtcNow)
                    return null;

                // Remove old refresh token
                await _cache.RemoveAsync(key, cancellationToken);

                // Note: In a complete implementation, we would need to fetch the user
                // from the repository here to generate new tokens. For now, return null
                // as this requires user repository injection.
                _logger.LogInformation("Refresh token validated for user {UserId} but user repository not available", userId);
                return null;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return null;
        }
    }

    public async Task BlacklistTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenId = ExtractTokenId(token);
        if (string.IsNullOrEmpty(tokenId))
            return;

        var expiration = GetTokenExpiration(token);
        var key = $"blacklist:token:{tokenId}";
        var ttl = expiration - DateTime.UtcNow;

        if (ttl > TimeSpan.Zero)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            await _cache.SetStringAsync(key, "blacklisted", options, cancellationToken);
            _logger.LogDebug("Blacklisted token {TokenId} until {ExpirationTime}", tokenId, expiration);
        }
    }

    public async Task BlacklistAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Store user token revocation timestamp
        var key = $"user:token_revoked_at:{userId}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // Set with a TTL longer than the longest token lifetime
        var ttl = _refreshTokenLifetime.Add(TimeSpan.FromDays(1));
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        await _cache.SetStringAsync(key, timestamp, options, cancellationToken);

        _logger.LogInformation("Revoked all tokens for user {UserId}", userId);
    }

    #region Private Helper Methods

    private string? ExtractTokenId(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract token ID from JWT");
            return null;
        }
    }

    private JwtSecurityToken CreateJwtToken(IEnumerable<Claim> claims, DateTime expiry)
    {
        var signingCredentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);

        return new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiry,
            signingCredentials: signingCredentials);
    }

    private TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _publicKey,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }

    private void LoadOrGenerateRSAKeys()
    {
        // In production, RSA keys should be loaded from secure storage (Azure Key Vault, etc.)
        // For development, we can generate them dynamically
        var rsaKeyConfig = _configuration["Jwt:RSAKey"];

        if (!string.IsNullOrEmpty(rsaKeyConfig))
        {
            try
            {
                // Load from configuration (base64 encoded XML)
                var keyBytes = Convert.FromBase64String(rsaKeyConfig);
                var keyXml = Encoding.UTF8.GetString(keyBytes);
                _rsa.FromXmlString(keyXml);
                _logger.LogInformation("Loaded RSA key from configuration");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load RSA key from configuration, generating new key");
                GenerateNewRSAKey();
            }
        }
        else
        {
            GenerateNewRSAKey();
        }
    }

    private void GenerateNewRSAKey()
    {
        // Generate new RSA key pair
        _logger.LogInformation("Generated new RSA key pair for JWT signing");

        // In development, log the key for reuse (don't do this in production!)
#if DEBUG
        var keyXml = _rsa.ToXmlString(true);
        var keyBytes = Encoding.UTF8.GetBytes(keyXml);
        var keyBase64 = Convert.ToBase64String(keyBytes);
        _logger.LogDebug("Generated RSA Key (for development only): {RSAKey}", keyBase64);
#endif
    }

    /// <summary>
    /// Validates token asynchronously (async version of ValidateToken)
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var principal = await ValidateTokenAndGetPrincipalAsync(token, cancellationToken);
        return principal != null;
    }

    /// <summary>
    /// Validates token and returns the ClaimsPrincipal if valid
    /// </summary>
    public async Task<ClaimsPrincipal?> ValidateTokenAndGetPrincipalAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            // Check if token is blacklisted
            var isRevoked = await IsTokenRevokedAsync(token, cancellationToken);
            if (isRevoked)
            {
                _logger.LogDebug("Token validation failed: token is revoked");
                return null;
            }

            // Validate token structure and signature
            var principal = GetPrincipalFromToken(token);
            if (principal == null)
            {
                _logger.LogDebug("Token validation failed: invalid token structure or signature");
                return null;
            }

            // Check if user tokens were globally revoked
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var userRevocationKey = $"user:token_revoked_at:{userId}";
                var revocationTimestamp = await _cache.GetStringAsync(userRevocationKey, cancellationToken);

                if (!string.IsNullOrEmpty(revocationTimestamp))
                {
                    var issuedAtClaim = principal.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
                    if (long.TryParse(issuedAtClaim, out var issuedAt) &&
                        long.TryParse(revocationTimestamp, out var revokedAt) &&
                        issuedAt < revokedAt)
                    {
                        _logger.LogDebug("Token validation failed: token issued before user token revocation");
                        return null;
                    }
                }
            }

            _logger.LogDebug("Token validation successful");
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during async token validation");
            return null;
        }
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rsa?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}