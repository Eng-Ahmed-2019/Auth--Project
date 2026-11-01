using System.Text;
using Microsoft.OpenApi;
using SecureCompany.Domain.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureCompany.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using SecureCompany.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using SecureCompany.Infrastructure.SeedData;
using SecureCompany.Application.Requirements;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SecureCompany.Application.Interfaces.Authentication;
using SecureCompany.Infrastructure.Services.Authentication;

var builder = WebApplication.CreateBuilder(args);
/*
using System.Security.Cryptography;

var key = Convert.ToBase64String(
    RandomNumberGenerator.GetBytes(64)
);
Console.WriteLine(key);
*/

builder.Services.AddControllers();

string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is missing.");
byte[] key = Encoding.UTF8.GetBytes(jwtKey);
string jwtIssuer = builder.Configuration["Jwt:Issuer"] ??
    throw new InvalidOperationException("Issuer is missing");
string jwtAudience = builder.Configuration["Jwt:Audience"] ??
    throw new InvalidOperationException("Audience is missing");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ClockSkew = TimeSpan.Zero,

            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewEmployees", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireClaim(
            "Permission",
            "Employees.View"
        );
    });
    options.AddPolicy("CanCreateEmployees", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireClaim(
            "Permission",
            "Employees.Create"
        );
    });
    options.AddPolicy("SameDepartment", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new DepartmentAccessRequirement());
    });
    options.AddPolicy("CanViewLeaveRequests", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new LeaveRequestAccessRequirement());
    });
    options.AddPolicy("CanApproveLeaveRequest", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.AddRequirements(
            new ApproveLeaveRequestRequirement()
        );
    });
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.User.RequireUniqueEmail = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();
builder.Services.AddScoped<IAuth, Auth>();
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<IAuthorizationHandler, DepartmentAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, LeaveRequestAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ApproveLeaveRequestHandler>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seeder = services.GetRequiredService<IdentitySeeder>();
        await seeder.SeedRoleAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();