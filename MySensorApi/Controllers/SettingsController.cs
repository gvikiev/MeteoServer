using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/Settings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Setting>>> GetSettings()
        {
            return await _context.Settings.ToListAsync();
        }

        // GET: /api/Settings/{name}
        [HttpGet("{name}")]
        public async Task<ActionResult<Setting>> GetSettingByName(string name)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Name == name);
            if (setting == null)
                return NotFound($"Налаштування '{name}' не знайдено");

            return Ok(setting);
        }

        // PUT: /api/Settings
        [HttpPut]
        public async Task<IActionResult> EditSetting([FromBody] Setting updated)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Id == updated.Id);
            if (setting == null)
                return NotFound("Налаштування не знайдено");

            setting.Value = updated.Value;
            await _context.SaveChangesAsync();

            return Ok("Налаштування оновлено");
        }
    }
}
