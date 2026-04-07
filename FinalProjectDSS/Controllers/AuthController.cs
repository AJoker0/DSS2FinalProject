using FinalProjectDSS.Data;
using FinalProjectDSS.Models;
using FinalProjectDSS.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace FinalProjectDSS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Dependencies: database context and configuration
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Constructor: injects dependencies
        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST /api/auth/register
        // Registers a new user
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // Check if email is already used
            if (_context.Users.Any(u => u.Email == request.Email))
                return Conflict(new { message = "Email already in use" });

            // Create new user entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                DisplayName = request.DisplayName,
                CreatedAt = DateTime.UtcNow
            };

            // Save user to database
            _context.Users.Add(user);
            _context.SaveChanges();

            // Return created user info (without password)
            return Created("", new AuthUserResponse { Id = user.Id, Email = user.Email, DisplayName = user.DisplayName });
        }

        // POST /api/auth/login
        // Authenticates a user and returns a JWT token
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Find user by email and verify password
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password" });

            // Prepare JWT claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            // Create signing credentials using secret key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create JWT token
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            // Return token and user info
            return Ok(new LoginResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                TokenType = "Bearer",
                ExpiresInSeconds = 3600,
                User = new AuthUserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName
                }
            });
        }
    }
}