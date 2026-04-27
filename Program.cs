using DiveIntoIVE.Configurations;
using DiveIntoIVE.Data;
using DiveIntoIVE.Middleware;
using DiveIntoIVE.Services.Implementations;
using DiveIntoIVE.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Run on port 5000 (used by nginx reverse proxy)
// builder.WebHost.UseUrls("http://0.0.0.0:5000");


// ---------------- DATABASE ----------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


// ---------------- CONFIGURATION BINDING ----------------
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));


// ---------------- JWT AUTHENTICATION ----------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services
.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});


// ---------------- CORS for React frontend ----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://iveph.com",
            "https://www.iveph.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});


// ---------------- SERVICES ----------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();


// ---------------- CONTROLLERS ----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// ---------------- SWAGGER + JWT ----------------
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
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
            Array.Empty<string>()
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("loginLimiter", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5; // 5 requests
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});
// ---------------- BUILD APP ----------------
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
app.UseMiddleware<ExceptionMiddleware>();
app.UseRateLimiter();

// ---------------- NGINX FORWARDED HEADERS ----------------
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});


// ---------------- CORS ----------------
app.UseCors("AllowReactApp");


// ---------------- SWAGGER ----------------
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}


// ---------------- AUTH ----------------
// app.UseHttpsRedirection(); // nginx handles HTTPS

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Allow WebGL texture loading from frontend origins (cross-origin image sampling).
        // Regular <img> tags can display without this, but Three.js texture uploads require CORS headers.
        context.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    }
});
app.UseAuthentication();
app.UseAuthorization();

// ---------------- ROUTES ----------------
app.MapControllers();


// ---------------- HEALTH CHECK ----------------
app.MapGet("/", () => "IVEPH API running");


app.Run();
