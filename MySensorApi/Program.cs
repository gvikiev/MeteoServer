using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Models;
using MySensorApi.Data; // заміни на свій простір імен, якщо інший

var builder = WebApplication.CreateBuilder(args);

// Додаємо сервіси
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() // або вкажи IP мобільного
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

//Check if DB exist and run migration
if(app.Environment.IsDevelopment())
{
    await using (var serviceScope = app.Services.CreateAsyncScope())
    await using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>())
    {
        await dbContext.Database.MigrateAsync();
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Middleware
app.UseSwagger();

app.UseSwaggerUI();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();


