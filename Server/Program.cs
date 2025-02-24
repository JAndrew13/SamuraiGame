using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server;
using Server.Models;
using Server.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration sources in the correct order
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())  // Ensure correct base path
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // Environment variables take priority

// Load environment variable for BearerKey (overrides appsettings.json if set)
var bearerKey = builder.Configuration["BEARER_KEY"];
if (!string.IsNullOrEmpty(bearerKey))
{
    Console.WriteLine("Using BEARER_KEY from environment variables.");
}
else
{
    Console.WriteLine("Using BearerKey from appsettings.json (BEARER_KEY not set).");
}

// Load settings from appsettings.json
var settings = new Settings();
builder.Configuration.Bind("Settings", settings);

// Override settings.BearerKey with the Azure environment variable if available
if (!string.IsNullOrEmpty(bearerKey))
{
    settings.BearerKey = bearerKey;
}

builder.Services.AddSingleton(settings);

// Add Swagger and API Docs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Game Server API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Please enter Bearer token below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// Add services to the container.
builder.Services.AddDbContext<GameDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddControllers()
    .AddNewtonsoftJson(o => {
        o.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    }); // This helps with unity serializer, 

// Register Services
builder.Services.AddScoped<IHeroService, PlayerService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.BearerKey)),
            ValidateIssuerSigningKey = true, // Is the key valid and trusted?
            ValidateAudience = false, // Where is the client coming from, is it valid?
            ValidateIssuer = false, // Who is sending the token?
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

// These run every time an endpoint is hit
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
