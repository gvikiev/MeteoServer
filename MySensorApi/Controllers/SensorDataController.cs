using Microsoft.AspNetCore.Mvc;
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

            await _context.Database.ExecuteSqlRawAsync("EXEC GenerateComfortRecommendations");
            Console.WriteLine("Процедура викликана...");

            return Ok(new { message = "Дані збережено!" });
        }

        [HttpGet]
        public async Task<IEnumerable<SensorData>> Get()
        {
            return await _context.SensorData.ToListAsync();
        }
    }
}
