﻿using Microsoft.AspNetCore.Authorization;
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
    [AllowAnonymous]
    [HttpPost("GetAllCategories")]
    public async Task<IActionResult> GetAllCategories([FromBody] GetCategoriesRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // 1. 根据传入的 BatchId 筛选相关类别
        var query = context.Categories.AsQueryable();

        // 如果传入了 BatchId，则通过 BatchCategory 关联表来过滤
        if (request.BatchId.HasValue)
        {
            query = query.Where(c => c.BatchCategories.Any(bc => bc.BatchId == request.BatchId.Value));
        }

        // 只选择未删除的类别
        query = query.Where(c => !c.DeleteFlag);

        // 获取总数
        var totalCount = await query.CountAsync();

        // 分页查询
        var categories = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();

        // 构造分页数据
        var pagedData = new PagedData<CategoryDto>(categories, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<CategoryDto>>.Success(pagedData, "获取类别列表成功"));
    }

    [AllowAnonymous]
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
public class GetCategoriesRequest : PagedRequest
{
    public int? BatchId { get; set; }  // 可选的批次ID，用来筛选当前批次下的类别
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