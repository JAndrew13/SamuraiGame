using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server;
using Server.Models;
using Server.Services;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


var settings = new Settings();
builder.Configuration.Bind("Settings", settings);
builder.Services.AddSingleton(settings);

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
builder.Services.AddDbContext<GameDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
Console.WriteLine($"Azure Env Var Connection String: {envConnectionString}");

var connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
Console.WriteLine($"Azure SQL Connection String: {connectionString}");

var configConnectionString = builder.Configuration.GetConnectionString("Database");
Console.WriteLine($"Config File Connection String: {configConnectionString}");

var finalConnectionString = envConnectionString ?? configConnectionString;
Console.WriteLine($"Final Connection String Used: {finalConnectionString}");



builder.Services
    .AddControllers()
    .AddNewtonsoftJson(o => {
        o.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    }); // This helps with unity serializer, 

builder.Services.AddScoped<IHeroService, PlayerService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { 
    o.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.BearerKey)),
        ValidateIssuerSigningKey = true, // Is the key valid and trusted?
        ValidateAudience = false, // Where is the client coming from, is it valid?
        ValidateIssuer = false, // Who is sending the token
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
