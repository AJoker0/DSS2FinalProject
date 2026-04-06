using System;
using FinalProjectDSS.Data;
using FinalProjectDSS.Models;
using FinalProjectDSS.DTOs;
using BCrypt.Net;
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
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // check if this email is already exist in database
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return Conflict(new { message = "Email already in use." });
            }
            // hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            //create a new user
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash
            };
            // save the user to database
            _context.Users.Add(newUser);
            _context.SaveChanges();

            // (AuthUserResponse) format the response
            var response = new AuthUserResponse
            {
                Id = newUser.Id,
                Email = newUser.Email,
                DisplayName = request.DisplayName
            };
            // return 201 created
            return Created("", response);
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // find user by email
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);



            // check if user exist and if the password is correct
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // return 401 unauthorized if the credentials are invalid
                return Unauthorized(new { message = "Invalid email or password." });
            }
            // if credentials are valid, generate a JWT token
            var token = GenerateJwtToken(user);

            // form the response (LoginResponse)
            var response = new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresInSeconds = 3600,
                User = new AuthUserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    DisplayName = ""
                }
            };
            // return 200 OK
            return Ok(response);

        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // The token keeps the user’s ID inside
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(3600), //The validity of the token from the specification
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
