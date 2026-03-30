using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HORIZON1.Data;
using HORIZON1.Models;
using HORIZON1.Repository;

var builder = WebApplication.CreateBuilder(args);

// 1. ДОДАЄМО НАЛАШТУВАННЯ CORS (дозволяємо фронтенду робити запити)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<HORIZON1.Factory.ReminderFactory>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. ПОРЯДОК МАЄ ЗНАЧЕННЯ: спочатку статика, потім CORS, потім контролери
app.UseDefaultFiles(); // Дозволяє відкривати index.html за замовчуванням
app.UseStaticFiles();  // Дозволяє читати папку wwwroot (твої HTML/CSS/JS)

app.UseSwagger();
app.UseSwaggerUI();

// ПРИМУСОВО вмикаємо CORS
app.UseCors("AllowAll");

// app.UseHttpsRedirection(); // Можеш тимчасово закоментувати, якщо тестуєш на http
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();