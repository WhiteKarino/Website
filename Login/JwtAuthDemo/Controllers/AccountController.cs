using JwtAuthDemo.Data;
using JwtAuthDemo.Models;
using JwtAuthDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace JwtAuthDemo.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AccountController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Username == username);
            if (userExists) return BadRequest("User already exists.");

            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.PasswordHash != HashPassword(password))
                return Unauthorized("Invalid credentials.");

            var token = _jwtService.GenerateToken(user.Username);

            HttpContext.Session.SetString("AuthToken", token);
            HttpContext.Session.SetString("Username", user.Username);

            // Store RememberMe flag in session
            if (rememberMe)
            {
                HttpContext.Session.SetInt32("RememberMe", 1);
                HttpContext.Session.SetString("SessionTimeout", "7Days");
            }
            else
            {
                HttpContext.Session.SetInt32("RememberMe", 0);
                HttpContext.Session.SetString("SessionTimeout", "30Minutes");
            }

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public IActionResult Logout()
        {
            // Clear the session (token and username)
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
