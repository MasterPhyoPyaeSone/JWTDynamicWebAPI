using JWTDynamicRBACWebAPI.Database.AppDbContextModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace JWTDynamicRBACWebAPI.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Login ဝင်ရန် ဖောင် (DTO)
        public class LoginDto
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            // ၁။ User ကို Database တွင် စစ်ဆေးခြင်း (Role နှင့် ၎င်း၏ Permission များကိုပါ တစ်ခါတည်း ဆွဲထုတ်မည်)
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.Permissions) // 💡 EF Core စနစ်သစ်ကြောင့် တိုက်ရိုက်ခေါ်၍ရသည်
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);

            if (user == null) return Unauthorized("Invalid username or password.");

            // ၂။ JWT တွင် ထည့်သွင်းမည့် အချက်အလက်များ (Claims) တည်ဆောက်ခြင်း
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            // ၃။ User လုပ်ခွင့်ရှိသော Permission များကို Claim အဖြစ် ပေါင်းထည့်ခြင်း
            foreach (var permission in user.Role.Permissions)
            {
                claims.Add(new Claim("Permission", permission.PermissionName));
            }

            // ၄။ JWT Token ဖန်တီးခြင်း
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2), // ၂ နာရီခံမည်
                signingCredentials: creds
            );

            return Ok(new 
            { 
                Token = new JwtSecurityTokenHandler().WriteToken(token) 
            });
        }
    }
}