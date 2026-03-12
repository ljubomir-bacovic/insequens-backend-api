using Microsoft.AspNetCore.Mvc;
using Insequens.Domain.Data;
using Microsoft.AspNetCore.Identity;
using Insequens.Infrastructure.Data.Models;

namespace Insequens.Api.Controllers
{
    [ApiController]
    [Route("warmup")]
    public class WarmupController : ControllerBase
    {
        private readonly InsequensContext _context;

        public WarmupController(InsequensContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            // Light DB query to initialize EF Core, DB connection, etc.

            var user = _context.Users.FirstOrDefault();
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            var dummy = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "fakepassword");
            return Ok("Warmed up.");
        }
    }
}
