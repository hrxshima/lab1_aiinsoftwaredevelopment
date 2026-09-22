using System.Security.Claims;
using System.Text;
using AuthService.Data;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JWT secret is not configured.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirst("sub");

                var versionClaim = context.Principal?.FindFirst(JwtService.PasswordVersionClaim);

                if (userIdClaim != null && versionClaim != null
                    && int.TryParse(userIdClaim.Value, out var userId)
                    && int.TryParse(versionClaim.Value, out var tokenVersion))
                {
                    var dbContext = context.HttpContext.RequestServices
                        .GetRequiredService<AuthDbContext>();

                    var user = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user == null || user.PasswordVersion != tokenVersion)
                    {
                        context.Fail("Token has been invalidated due to password change.");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
