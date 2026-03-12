using Microsoft.AspNetCore.Mvc;
using Journal.Domain.Data;
using Microsoft.AspNetCore.Identity;
using Journal.Infrastructure.Data.Models;

namespace Journal.Api.Controllers
{
    [ApiController]
    [Route("warmup")]
    public class WarmupController : ControllerBase
    {
        private readonly JournalContext _context;

        public WarmupController(JournalContext context)
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
