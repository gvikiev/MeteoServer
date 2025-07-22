using Microsoft.AspNetCore.Authorization;
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

        // Get latest sensor data for a specific room
        [Authorize]
        [HttpGet("{roomName}")]
        public async Task<ActionResult<SensorData>> GetLastSensorDataPerRoom(string roomName)
        {
            var sensorData = await _context.SensorData
                .Where(s => s.RoomName == roomName)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync();

            if (sensorData == null)
            {
                return NotFound($"No sensor data found for room: {roomName}");
            }

            return Ok(sensorData);
        }

        // Alternative: Get latest data for ALL rooms (more efficient single query)
        [Authorize]
        [HttpGet("all/latest-efficient")]
        public async Task<IEnumerable<SensorData>> GetLatestSensorDataAllRoomsEfficient()
        {
            var latestPerRoom = await _context.SensorData
                .GroupBy(s => s.RoomName)
                .Select(g => g
                    .OrderByDescending(s => s.Timestamp)
                    .First())
                .ToListAsync();

            return latestPerRoom;
        }

        // Get latest sensor data for a specific room as DTO
        [Authorize]
        [HttpGet("{roomName}/latest/DTO")]
        public async Task<ActionResult<SensorDataDto>> GetLatestSensorData(string roomName)
        {
            var latestData = await _context.SensorData
                .Where(s => s.RoomName == roomName)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync();

            if (latestData == null)
            {
                return NotFound($"No sensor data found for room: {roomName}");
            }

            // Project to DTO
            var dto = new SensorDataDto
            {
                RoomName = latestData.RoomName,
                TemperatureDht = latestData.TemperatureDht,
                HumidityDht = latestData.HumidityDht,
                GasDetected = latestData.GasDetected,
                Pressure = latestData.Pressure,
                Altitude = latestData.Altitude,
                Timestamp = latestData.Timestamp
            };

            return Ok(dto);
        }

        // Get latest recommendations for a specific room
        [Authorize]
        [HttpGet("{roomName}/recommendations")]
        public async Task<ActionResult<ComfortRecommendation>> GetLatestRecommendations(string roomName)
        {
            var recommendation = await _context.ComfortRecommendations
                .Where(r => r.RoomName == roomName)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            if (recommendation == null)
            {
                return NotFound($"No recommendations found for room: {roomName}");
            }

            return Ok(recommendation);
        }

        // Alternative: Get latest recommendations for ALL rooms (if needed)
        [Authorize]
        [HttpGet("all/recommendations")]
        public async Task<IEnumerable<ComfortRecommendation>> GetLatestRecommendationsAllRooms()
        {
            // Get distinct room names first
            var roomNames = await _context.ComfortRecommendations
                .Where(r => r.RoomName != null)
                .Select(r => r.RoomName)
                .Distinct()
                .ToListAsync();

            var latestRecommendations = new List<ComfortRecommendation>();

            // Get latest recommendation for each room
            foreach (var roomName in roomNames)
            {
                var latest = await _context.ComfortRecommendations
                    .Where(r => r.RoomName == roomName)
                    .OrderByDescending(r => r.Timestamp)
                    .FirstOrDefaultAsync();

                if (latest != null)
                {
                    latestRecommendations.Add(latest);
                }
            }

            return latestRecommendations;
        }
    }
}