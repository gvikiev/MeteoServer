using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MySensorApi.Models;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SensorDataController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data)
        {
            _context.SensorData.Add(data);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(data.RoomName))
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC GenerateComfortRecommendations @Room = {data.RoomName}");
            }

            Console.WriteLine("Процедура викликана...");
            return Ok(new { message = "Дані збережено!" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IEnumerable<SensorData>> GetSensorData()
        {
            return await _context.SensorData.ToListAsync();
        }

        [Authorize]
        [HttpGet("secure-test")]
        public IActionResult SecureTest()
        {
            var username = User.Identity?.Name;
            return Ok($"🔒 Привіт, {username}. Доступ дозволено.");
        }
    }
}
