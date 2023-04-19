using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")] // POST: api/account/register
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO regDTO)
        {
            if (await UserExists(regDTO.username)) return BadRequest("User has been taken!");

            using var hmac = new HMACSHA512(); // using => it will be disposed automatically 

            var user = new AppUser
            {
                UserName = regDTO.username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(regDTO.password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            var user = await _context.Users
                .Include(u => u.Photos)
                .SingleOrDefaultAsync(
                    u => u.UserName == loginDTO.Username
                );

            if (user == null) return Unauthorized("invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt); // using will make it to be disposed automatically
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("invalid password");
            }

            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain)?.Url
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username.ToLower());
        }
    }
}