using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HORIZON1.Data;
using HORIZON1.Models;
using HORIZON1.Repository;
using System.Net;
using System.Net.Sockets;
using HORIZON1.Services;

// ФУНКЦІЯ ПОШУКУ ПОРТУ: Допомагає знайти вільний порт (наприклад, 5000), 
// щоб програма могла запуститися, навіть якщо інша програма вже зайняла порт.
static int FindAvailablePort(int start = 5000, int end = 5050)
{
    for (var port = start; port <= end; port++)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start(); // Пробуємо "зайняти" порт
            listener.Stop();
            return port; // Якщо успішно — повертаємо цей номер
        }
        catch (SocketException) // Якщо зайнято — йдемо далі
        {
            continue;
        }
    }
    throw new InvalidOperationException($"No available ports found in range {start}-{end}.");
}

var builder = WebApplication.CreateBuilder(args);

// НАЛАШТУВАННЯ ПОРТУ: Визначаємо, на якій адресі буде працювати наш сервер
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

// CORS: Дозволяє фронтенду робити запити до бекенду з різних джерел
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JSON: Налаштування, щоб сервер не "зависав" при обробці циклічних посилань у даних
builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// БАЗА ДАНИХ: Підключаємо SQLite через рядок підключення з appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// IDENTITY: Налаштування системи користувачів (пароль мін. 6 символів, цифра обов'язкова)
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>();

// DEPENDENCY INJECTION (Впровадження залежностей):
// Реєструємо наші класи, щоб їх можна було використовувати в контролерах
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<HORIZON1.Factory.ReminderFactory>();

// SWAGGER: Інструмент для тестування API (документація методів)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ФОНОВА СЛУЖБА: Реєструємо сервіс нагадувань, щоб він працював паралельно
builder.Services.AddHostedService<ReminderBackgroundService>();

var app = builder.Build();

// MIDDLEWARE (Конвеєр обробки запитів):
app.UseDefaultFiles(); // Дозволяє відкривати index.html за замовчуванням
app.UseStaticFiles();  // Дозволяє серверу віддавати CSS, JS та картинки

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// ПОРЯДОК ВАЖЛИВИЙ: Спочатку перевіряємо КТО користувач, потім ЩО йому дозволено
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers(); // Підключаємо маршрутизацію до наших контролерів

app.Run(); // ЗАПУСК СЕРВЕРА