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
    options.Password.RequireNonAlphanumeric = false; // Спростимо для тестів
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// --- ЦЕЙ БЛОК ВИПРАВЛЯЄ 404 НА /Account/Login ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login.html"; // Перенаправляти на твій файл у wwwroot
    options.AccessDeniedPath = "/login.html";
    options.Events.OnRedirectToLogin = context =>
    {
        // Якщо це запит до API, не робимо редирект, а повертаємо 401
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
// ------------------------------------------------

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<HORIZON1.Factory.ReminderFactory>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<ReminderBackgroundService>();

var app = builder.Build();

// 5. Конфігурація Middleware
app.UseDefaultFiles(); // Дозволяє завантажувати index.html автоматично
app.UseStaticFiles();  // Дозволяє доступ до файлів у wwwroot

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Horizon API V1");
});

app.UseCors("AllowAll");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

// 6. Додатковий маршрут для SPA (якщо сторінку не знайдено в API, вантажимо index.html)
app.MapFallbackToFile("index.html");

app.Run();