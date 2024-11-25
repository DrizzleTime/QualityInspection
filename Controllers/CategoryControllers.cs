using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class CategoryController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
{
    [HttpPost("GetAllCategories")]
    public async Task<IActionResult> GetAllCategories([FromBody] PagedRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var totalCount = await context.Categories.CountAsync(c => !c.DeleteFlag);
        var categories = await context.Categories
            .Where(c => !c.DeleteFlag)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).ToListAsync();

        var pagedData = new PagedData<CategoryDto>(categories, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<CategoryDto>>.Success(pagedData, "获取类别列表成功"));
    }

    [HttpPost("GetCategoryById")]
    public async Task<IActionResult> GetCategoryById([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var category = await context.Categories
            .Where(c => c.Id == id && !c.DeleteFlag)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(ApiResponse<string>.Fail("类别未找到"));
        }

        return Ok(ApiResponse<CategoryDto>.Success(category, "获取类别信息成功"));
    }

    [HttpPost("CreateCategory")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var newCategory = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        context.Categories.Add(newCategory);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("类别创建成功"));
    }

    [HttpPost("UpdateCategory")]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && !c.DeleteFlag);

        if (category == null)
        {
            return NotFound(ApiResponse<string>.Fail("类别未找到"));
        }

        category.Name = request.Name;
        category.Description = request.Description;

        context.Categories.Update(category);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("类别更新成功"));
    }

    [HttpPost("DeleteCategory")]
    public async Task<IActionResult> DeleteCategory([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(ApiResponse<string>.Fail("类别未找到"));
        }

        category.DeleteFlag = true;
        context.Categories.Update(category);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("类别删除成功"));
    }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest
{
    public int Id { get; set; }
}