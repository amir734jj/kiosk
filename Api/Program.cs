using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Api.Data;
using Api.Data.Entities;
using Api.Extensions;
using Api.Interfaces;
using Api.Middleware;
using Api.Services;
using Api.Utilities;
using EfCoreRepository.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Filter.ByExcluding(x =>
        x.Properties.TryGetValue("RequestPath", out var requestPath) &&
        requestPath.ToString().Contains("/api/health"))
    .Enrich.WithProperty("Application", "kiosk-api")
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {user} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/api-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var portConfig = builder.Configuration.GetValue<string>("PORT");
    var port = !string.IsNullOrEmpty(portConfig) && int.TryParse(portConfig, out var p) ? p : 5000;
    serverOptions.ListenAnyIP(port);
});

if (builder.Environment.IsDevelopment())
{
    var sqlitePath = builder.Configuration.GetConnectionString("Default") ?? "Data Source=kiosk.db";
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(sqlitePath));
}
else
{
    var connectionString = ConnectionStringUtility.ConnectionStringUrlToPgResource(
        builder.Configuration.GetValue<string>("DATABASE_URL")!);
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));
}

builder.Services
    .AddIdentity<AppUser, AppRole>(opt =>
    {
        opt.Password.RequiredLength = 6;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("login", w =>
    {
        w.Window = TimeSpan.FromMinutes(1);
        w.PermitLimit = 10;
        w.QueueLimit = 0;
        w.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

builder.Services.AddEfRepository<AppDbContext>(x =>
{
    x.Profile(Assembly.GetAssembly(typeof(AppDbContext)));
});

builder.Services.Scan(scan => scan
    .FromAssemblies(Assembly.Load("Api"))
    .AddClasses()
    .UsingRegistrationStrategy(Scrutor.RegistrationStrategy.Skip)
    .AsMatchingInterface()
    .WithScopedLifetime());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .WithOrigins(
            builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
    {
    }
    else
    {
        // await db.Database.MigrateAsync();
        
        await db.Database.EnsureCreatedAsync();
    }
}

app.UseCors();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=3600";
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<ActiveUserMiddleware>();
app.UseAuthorization();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var user = httpContext.User.Identity is { IsAuthenticated: true }
            ? httpContext.User.TryGetUsername() ?? "authenticated"
            : "anonymous";

        diagnosticContext.Set("user", user);
    };
});

app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();

app.MapFallback("api/{**rest}", async context =>
{
    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
    await context.Response.WriteAsync(
        $"Failed to find the endpoint for {context.Request.Method}:{context.Request.GetDisplayUrl()}");
});

if (app.Environment.IsDevelopment())
{
    app.MapFallback(() => Results.Text("Kiosk API server is running."));
}
else
{
    app.MapFallbackToFile("index.html");
}

await app.RunAsync();

