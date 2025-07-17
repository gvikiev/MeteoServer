using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MySensorApi.Data; // заміни на свій простір імен, якщо інший

var builder = WebApplication.CreateBuilder(args);

//// Налаштування Kestrel для HTTP (порт 80)
//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(80); // HTTP порт
//});

// Додаємо сервіси
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

//Check if DB exist and run migration
if(app.Environment.IsDevelopment())
{
    await using (var serviceScope = app.Services.CreateAsyncScope())
    await using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>())
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
}

// Middleware
app.UseSwagger();

app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();
