using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.Hubs;
using MnceShisanyama.Api.Services;

var appRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var webRootPath = Path.Combine(appRoot, "wwwroot");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = appRoot,
    WebRootPath = webRootPath
});

// ---- Services ------------------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Serialize enums (OrderStatus, OrderChannel, StaffRole) as readable strings
        // in JSON instead of raw numbers, so the frontend never has to hardcode "0 = Pending".
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=mnce_shisanyama.db"));

builder.Services.AddSignalR();

builder.Services.AddSingleton<StaffAuthService>();
builder.Services.AddScoped<OrderNotifier>();
builder.Services.AddScoped<IPaymentGateway, DemoPaymentGateway>();
builder.Services.AddScoped<ISmsSender, DemoSmsSender>();

// Frontend is plain HTML/CSS/JS served from wwwroot on the same origin as the API,
// so no CORS policy is required for the bundled pages. This policy exists for teams
// that want to host the frontend separately (e.g. a CDN) and call this API remotely -
// tighten the origin list before you ship that setup.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ---- Seed the database on startup -----------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(db);
}

// ---- Middleware pipeline ---------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var staticFileProvider = new PhysicalFileProvider(webRootPath);

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = staticFileProvider,
    RequestPath = ""
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = staticFileProvider,
    RequestPath = ""
});

app.UseCors("Frontend");

// Staff-only endpoints are protected by the custom [StaffAuth] action filter
// (see Filters/StaffAuthFilter.cs), not ASP.NET Core's built-in policy auth,
// so there's no app.UseAuthentication()/UseAuthorization() call needed here.

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

app.Run();
