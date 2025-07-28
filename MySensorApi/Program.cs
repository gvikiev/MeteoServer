using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Models;
using MySensorApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

//Check run migration
if(app.Environment.IsDevelopment())
{
    await using (var serviceScope = app.Services.CreateAsyncScope())
    await using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>())
    {
        await dbContext.Database.MigrateAsync();
    }
}

// Middleware
app.UseSwagger();

app.UseSwaggerUI();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();


