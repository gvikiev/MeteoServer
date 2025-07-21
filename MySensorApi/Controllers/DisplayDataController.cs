using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Models;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisplayDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DisplayDataController(AppDbContext context)
        {
            _context = context;
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
