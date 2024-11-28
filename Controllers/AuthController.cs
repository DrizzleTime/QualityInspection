using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IDbContextFactory<MyDbContext> contextFactory, IConfiguration configuration)
    : ControllerBase
{
    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

        if (user == null)
        {
            return Unauthorized(ApiResponse<string>.Fail("用户名或密码错误"));
        }

        var token = CreateToken(user);
        return Ok(ApiResponse<object>.Success(new
        {
            token,
            user = new
            {
                user.Id,
                user.Username,
                Role = user.Role.Name
            }
        }, "登录成功"));
    }


    [Authorize]
    [HttpPost("GetUserInfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return NotFound(ApiResponse<string>.Fail("用户未找到"));
        }

        var userInfo = new
        {
            user.Id,
            user.Username,
            Role = user.Role.Name
        };

        return Ok(ApiResponse<object>.Success(userInfo, "用户信息获取成功"));
    }

    private string CreateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ??
                                         throw new InvalidOperationException("JWT 密钥未配置。"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.Name, user.Username ?? throw new InvalidOperationException("用户名不能为空。")),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("UserId", user.Id.ToString())
            ]),
            Expires = DateTime.UtcNow.AddDays(365),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}