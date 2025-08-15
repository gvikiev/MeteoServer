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

        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data)
        {
            data.CreatedAt = DateTime.UtcNow;

            _context.SensorData.Add(data);
            await _context.SaveChangesAsync();

            Console.WriteLine("Дані збережено без виклику процедури.");

            return Ok(new { message = "Дані збережено!", id = data.Id });
        }


        //[Authorize]
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
