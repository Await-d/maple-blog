// This file shows the authentication-related updates needed for Program.cs
// Merge these changes with the existing Program.cs file

// Add these additional service registrations after the existing service registrations:

// Additional Authentication Services (add to existing Program.cs)
/*
// Register new authentication services
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Configure authentication service settings
builder.Services.Configure<EmailVerificationSettings>(builder.Configuration.GetSection("EmailVerificationSettings"));
builder.Services.Configure<PasswordResetSettings>(builder.Configuration.GetSection("PasswordResetSettings"));
builder.Services.Configure<UserProfileSettings>(builder.Configuration.GetSection("UserProfileSettings"));

// Rate limiting configuration
var rateLimitingSettings = builder.Configuration.GetSection("RateLimitingSettings").Get<RateLimitingSettings>()
    ?? new RateLimitingSettings();
builder.Services.AddSingleton(rateLimitingSettings);

// Add authentication services using our extension
builder.Services.AddAuthenticationServices(builder.Configuration);

// Update JWT authentication to use our enhanced version
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add authorization policies
builder.Services.AddAuthorizationPolicies();

// Add CORS policies
builder.Services.AddCorsPolicies(builder.Configuration);

// Add rate limiting
builder.Services.AddRateLimiting(builder.Configuration);

// Add Swagger documentation
builder.Services.AddSwaggerDocumentation();

// Add security headers
builder.Services.AddSecurityHeaders();

// Add health checks
builder.Services.AddHealthChecks(builder.Configuration);

// Add the middleware in the correct order (add after app.UseCors):
app.UseRateLimiting();
app.UseJwtAuthentication();

// Make sure authentication comes before authorization
app.UseAuthentication();
app.UseAuthorization();
*/

// Required appsettings.json configuration sections:
/*
{
  "JwtSettings": {
    "Issuer": "MapleBlog",
    "Audience": "MapleBlog.Users",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "PrivateKey": "", // Base64 encoded RSA private key
    "PublicKey": ""   // Base64 encoded RSA public key
  },
  "EmailVerificationSettings": {
    "TokenExpirationHours": 24,
    "ResendCooldownMinutes": 5,
    "MaxResendAttempts": 3
  },
  "PasswordResetSettings": {
    "TokenExpirationHours": 1,
    "RequestCooldownMinutes": 15,
    "BcryptWorkFactor": 12,
    "MinPasswordLength": 8,
    "MinPasswordComplexity": 3
  },
  "UserProfileSettings": {
    "MaxAvatarSizeBytes": 5242880,
    "BcryptWorkFactor": 12,
    "MinPasswordLength": 8,
    "MinPasswordComplexity": 3
  },
  "RateLimitingSettings": {
    "AuthEndpoints": {
      "MaxRequests": 5,
      "WindowSizeSeconds": 300
    },
    "EmailEndpoints": {
      "MaxRequests": 3,
      "WindowSizeSeconds": 3600
    },
    "ApiEndpoints": {
      "MaxRequests": 100,
      "WindowSizeSeconds": 60
    }
  },
  "CorsSettings": {
    "AllowAnyOrigin": false,
    "AllowedOrigins": ["http://localhost:3000", "https://localhost:3000"],
    "AllowAnyMethod": false,
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    "AllowAnyHeader": false,
    "AllowedHeaders": ["Authorization", "Content-Type", "Accept"],
    "AllowCredentials": true,
    "PreflightMaxAgeMinutes": 10
  }
}
*/