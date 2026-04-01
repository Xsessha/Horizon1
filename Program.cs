using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HORIZON1.Data;
using HORIZON1.Models;
using HORIZON1.Repository;
using System.Net;
using System.Net.Sockets;

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

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

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();