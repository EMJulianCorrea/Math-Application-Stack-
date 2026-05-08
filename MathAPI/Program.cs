using MathAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using  System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MathDbContext>(options =>
                options.UseSqlServer(Environment.GetEnvironmentVariable("Math_DB")));

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("JWT key missing in appsettings.json");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            ),

        NameClaimType = ClaimTypes.NameIdentifier
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Add this
    app.UseSwaggerUI(); // Add this
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();
