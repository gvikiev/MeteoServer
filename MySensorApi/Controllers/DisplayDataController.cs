using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.DTO;
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

        // ✅ GET /api/DisplayData/{username}/latest/DTO
        [HttpGet("{username}/{roomName}/latest/DTO")]
        public async Task<ActionResult<SensorDataDto>> GetLatestSensorDataByUserAndRoom(string username, string roomName)
        {
            var latest = await _context.SensorData
                .Where(s => s.Username == username && s.RoomName == roomName)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync();

            if (latest == null)
                return NotFound($"Немає даних для {username} / {roomName}");

            return Ok(new SensorDataDto
            {
                RoomName = latest.RoomName,
                TemperatureDht = latest.TemperatureDht,
                HumidityDht = latest.HumidityDht,
                GasDetected = latest.GasDetected,
                Pressure = latest.Pressure,
                Altitude = latest.Altitude,
                Timestamp = latest.Timestamp
            });
        }


        [HttpGet("{username}/{roomName}/recommendations")]
        public async Task<ActionResult<ComfortRecommendation>> GetLatestRecommendationByUserAndRoom(string username, string roomName)
        {
            var latest = await _context.ComfortRecommendations
                .Where(r => r.Username == username && r.RoomName == roomName)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            if (latest == null)
                return NotFound($"Немає рекомендацій для {username} / {roomName}");

            return Ok(latest);
        }


        // ✅ GET /api/DisplayData/{username}/rooms
        [HttpGet("{username}/rooms")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllRoomNamesByUsername(string username)
        {
            var rooms = await _context.SensorData
                .Where(s => s.Username == username)
                .Select(s => s.RoomName)
                .Distinct()
                .ToListAsync();

            return Ok(rooms);
        }
    }
}
