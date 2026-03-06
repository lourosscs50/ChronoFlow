using ChronoFlow.Modules.Identity.Application;
using ChronoFlow.Modules.Identity.Contracts;
using ChronoFlow.Modules.Identity.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ChronoFlow.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChronoFlow.Api.Security;
using ChronoFlow.Api.Endpoints;
using ChronoFlow.Modules.Events.Application;
using ChronoFlow.Modules.Events.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI (Open Application Programming Interface)
builder.Services.AddOpenApi();

// JWT (JSON Web Token) configuration
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
var jwtKey = builder.Configuration["Jwt:Key"]!;

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Identity module services
var eventsConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseNpgsql(eventsConnectionString));
builder.Services.AddScoped<IEventRepository, EfEventRepository>();
builder.Services.AddScoped<IngestEventHandler>();
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(eventsConnectionString));
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ChronoFlow.Api.Security.ICurrentUser, ChronoFlow.Api.Security.HttpContextCurrentUser>();


var app = builder.Build();
app.MapEventsEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// IMPORTANT: auth middleware must be before protected endpoints
app.UseAuthentication(); // authentication = who are you?
app.UseAuthorization(); // authorization = are you allowed? 

// Identity endpoints
app.MapPost("/auth/register", async (RegisterRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        var result = await auth.RegisterAsync(new RegisterUserCommand(req.Email, req.Password), ct);
        return Results.Ok(new RegisterResponse(result.UserId, result.Email));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
})
.AllowAnonymous();

app.MapPost("/auth/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        var result = await auth.LoginAsync(new LoginUserCommand(req.Email, req.Password), ct);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, result.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, result.Email),
            new Claim("uid", result.UserId.ToString())
        };

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(new { token = tokenString });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);
    }
})
.AllowAnonymous();

app.MapGet("/me", (ICurrentUser currentUser) =>
{
    return Results.Ok(new { userId = currentUser.UserId, email = currentUser.Email });
})
.RequireAuthorization();

app.Run();

public partial class Program { }