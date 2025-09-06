using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySensorApi.DTO.User;
using System.Security.Claims;

namespace MySensorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected CurrentUserDTO CurrentUser
        {
            get
            {
                if (User?.Identity?.IsAuthenticated != true)
                    return new CurrentUserDTO
                    {
                        Id = 0,
                        Username = string.Empty,
                        Role = string.Empty
                    };

                return new CurrentUserDTO
                {
                    Id = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0,
                    Username = User.FindFirstValue(ClaimTypes.Name),
                    Role = User.FindFirstValue(ClaimTypes.Role)
                };
            }
        }
    }
}
