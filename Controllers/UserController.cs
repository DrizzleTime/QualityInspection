using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
{
    [Authorize(Roles = "Administrator,LeadInspector")]
    [HttpPost("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers([FromBody] GetAllUsersRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // 查询用户的总数（如果提供了角色ID，则只查询该角色的用户数量）
        var query = context.Users.AsQueryable();
        if (request.RoleId.HasValue)
        {
            query = query.Where(u => u.RoleId == request.RoleId.Value);
        }

        var totalCount = await query.CountAsync();

        // 分页查询
        var users = await query
            .Include(u => u.Role)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                RoleName = u.Role.Name,
                Email = u.Email,
                Telephone = u.Telephone,
                Date = u.Date
            }).ToListAsync();

        var pagedData = new PagedData<UserDto>(users, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<UserDto>>.Success(pagedData, "获取用户列表成功"));
    }

    [Authorize(Roles = "Administrator,LeadInspector")]
    [HttpPost("GetUserById")]
    public async Task<IActionResult> GetUserById([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users
            .Include(u => u.Role)
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                RoleName = u.Role.Name,
                Email = u.Email,
                Telephone = u.Telephone,
                Date = u.Date
            }).FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(ApiResponse<string>.Fail("用户未找到"));
        }

        return Ok(ApiResponse<UserDto>.Success(user, "获取用户信息成功"));
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("CreateUser")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(ApiResponse<string>.Fail("用户名已存在"));
        }

        var newUser = new User
        {
            Username = request.Username,
            Password = request.Password,
            RoleId = request.RoleId,
            Email = request.Email,
            Telephone = request.Telephone,
            Date = DateTime.UtcNow
        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("用户创建成功"));
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("UpdateUser")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.Id);

        if (user == null)
        {
            return NotFound(ApiResponse<string>.Fail("用户未找到"));
        }

        user.Username = request.Username;
        user.Password = request.Password;
        user.RoleId = request.RoleId;
        user.Email = request.Email;
        user.Telephone = request.Telephone;

        context.Users.Update(user);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("用户信息更新成功"));
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("DeleteUser")]
    public async Task<IActionResult> DeleteUser([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(ApiResponse<string>.Fail("用户未找到"));
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("用户删除成功"));
    }
}

public class GetAllUsersRequest : PagedRequest
{
    public int? RoleId { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public DateTime Date { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int RoleId { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
}

public class UpdateUserRequest
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int RoleId { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
}