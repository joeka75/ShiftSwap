using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSwap.Data;
using ShiftSwap.Dtos;
using ShiftSwap.DTOs;
using ShiftSwap.Models;
using ShiftSwap.Services;

namespace ShiftSwap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtTokenService _tokenService;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(AppDbContext db, JwtTokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid credentials.");

            var token = _tokenService.GenerateToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already in use.");

            var company = await _db.Companies.FirstOrDefaultAsync();
            if (company == null)
                return BadRequest("No company exists. Seed must run first.");

            var user = new User
            {
                CompanyId = company.Id,
                LocationId = dto.LocationId,
                FullName = dto.FullName,
                Email = dto.Email,
                Role = dto.Role,
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok("User registered.");
        }
    }
}
