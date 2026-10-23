
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Data;
using RaceDay.Models;

var builder = WebApplication.CreateBuilder(args);

// START: CONTROLLERS AND JSON CONFIGURATION
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// END: CONTROLLERS AND JSON CONFIGURATION

// START: DATABASE CONFIGURATION
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
// END: DATABASE CONFIGURATION

// START: PASSWORD HASHING
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
// END: PASSWORD HASHING

// START: JWT AUTHENTICATION
var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT Key is missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "The JWT signing key must contain at least 32 UTF-8 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
// END: JWT AUTHENTICATION

// START: AUTHORIZATION
builder.Services.AddAuthorization();
// END: AUTHORIZATION

var app = builder.Build();

// START: SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// END: SWAGGER

// START: HTTP PIPELINE
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// END: HTTP PIPELINE

app.Run();
