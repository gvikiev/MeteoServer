using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
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

        [HttpGet]
        public async Task<IEnumerable<SensorData>> GetLastSensorDataPerRoom()
        {
            return await _context.SensorData
                .GroupBy(s => s.RoomName)
                .Select(g => g.OrderByDescending(s => s.Timestamp).FirstOrDefault()!)
                .ToListAsync();
        }


        [HttpGet("latest")]
        public async Task<IEnumerable<SensorDataDto>> GetLatestSensorData()
        {
            var latestPerRoom = await _context.SensorData
                .GroupBy(s => s.RoomName)
                .Select(g => g.OrderByDescending(s => s.Timestamp).FirstOrDefault()!)
                .ToListAsync();

            // Проєкція у DTO — тільки після того, як витягнули з БД
            return latestPerRoom.Select(s => new SensorDataDto
            {
                RoomName = s.RoomName,
                TemperatureDht = s.TemperatureDht,
                HumidityDht = s.HumidityDht,
                GasDetected = s.GasDetected,
                Pressure = s.Pressure,
                Altitude = s.Altitude,
                Timestamp = s.Timestamp
            });
        }

        [HttpGet("recommendations")]
        public async Task<IEnumerable<ComfortRecommendation>> GetLatestRecommendations()
        {
            return await _context.ComfortRecommendations
                .GroupBy(r => r.RoomName)
                .Select(g => g.OrderByDescending(r => r.Timestamp).FirstOrDefault()!)
                .ToListAsync();
        }
    }
}
