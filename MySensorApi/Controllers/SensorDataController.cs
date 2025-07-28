using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.DTO;
using MySensorApi.Models;
using System.Security.Claims;

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

        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data)
        {
            _context.SensorData.Add(data);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Дані збережено!" });
        }

        //[Authorize]
        [HttpGet]
        public async Task<IEnumerable<SensorData>> GetSensorData()
        {
            return await _context.SensorData.ToListAsync();
        }

        [HttpGet("room-info")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoomInfo()
        {
            var rooms = await _context.Rooms.ToListAsync();
            var result = new List<object>();

            foreach (var room in rooms)
            {
                var latestSensor = await _context.SensorData
                    .Where(s => s.RoomName == room.Name)
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                result.Add(new
                {
                    room.Id,
                    room.Name,
                    room.ImageName,
                    Temperature = latestSensor?.TemperatureDht ?? latestSensor?.TemperatureBme,
                    Humidity = latestSensor?.HumidityDht ?? latestSensor?.HumidityBme,
                    Timestamp = latestSensor?.Timestamp
                });
            }

            return Ok(result);
        }


        [HttpPost("room")]
        public async Task<IActionResult> CreateRoom([FromBody] RoomRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.ImageName))
                return BadRequest("Некоректні дані");

            var room = new Room
            {
                Name = dto.Name,
                ImageName = dto.ImageName
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Кімната створена!", room.Id });
        }
    }
}
