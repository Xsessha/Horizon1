using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HORIZON1.Data;
using HORIZON1.Models;
using HORIZON1.Repository;
using System.Net;
using System.Net.Sockets;
using HORIZON1.Services;

static int FindAvailablePort(int start = 5000, int end = 5050)
{
    for (var port = start; port <= end; port++)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return port;
        }
        catch (SocketException)
        {
            continue;
        }
    }
    throw new InvalidOperationException($"No available ports found in range {start}-{end}.");
}

var builder = WebApplication.CreateBuilder(args);

// Налаштування порту
var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(envUrls))
{
    builder.WebHost.UseUrls(envUrls);
}
else
{
    var port = FindAvailablePort(5000, 5050);
    builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(port));
    Console.WriteLine($"[INFO] Using available port: {port}");
}

// 1. Налаштування CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. Налаштування контролерів (JSON)
builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// 3. База даних
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 4. Identity
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login.html";
    options.AccessDeniedPath = "/login.html";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

// --- СЕРВІСИ ---
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<HORIZON1.Factory.ReminderFactory>();

// ДОДАНО ДЛЯ БОТА: Реєструємо сервіс бота як Singleton
builder.Services.AddSingleton<TelegramBotService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<ReminderBackgroundService>();

var app = builder.Build();

// --- ЗАПУСК БОТА ПРИ СТАРТІ ---
// ДОДАНО ДЛЯ БОТА: Отримуємо сервіс і викликаємо метод Start()
using (var scope = app.Services.CreateScope())
{
    var botService = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
    botService.Start();
}
// ------------------------------

// 5. Конфігурація Middleware
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Horizon API V1");
});

app.UseCors("AllowAll");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();