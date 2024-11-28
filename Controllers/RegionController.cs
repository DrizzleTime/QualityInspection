using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class RegionController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
{
    [HttpPost("GetAllRegions")]
    public async Task<IActionResult> GetAllRegions([FromBody] GetRegionsRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var query = context.Regions.AsQueryable();

        // 1. 如果传入了 CategoryId，则过滤出属于该类别的区域
        if (request.CategoryId.HasValue)
        {
            query = query.Where(r => r.CategoryId == request.CategoryId.Value);
        }

        // 2. 只选择未删除的区域
        query = query.Where(r => !r.DeleteFlag);

        // 获取总数
        var totalCount = await query.CountAsync();

        // 分页查询
        var regions = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RegionDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name
            })
            .ToListAsync();

        // 构造分页数据
        var pagedData = new PagedData<RegionDto>(regions, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<RegionDto>>.Success(pagedData, "获取区域列表成功"));
    }


    [HttpPost("GetRegionById")]
    public async Task<IActionResult> GetRegionById([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var region = await context.Regions
            .Where(r => r.Id == id && !r.DeleteFlag)
            .Select(r => new RegionDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name
            }).FirstOrDefaultAsync();

        if (region == null)
        {
            return NotFound(ApiResponse<string>.Fail("区域未找到"));
        }

        return Ok(ApiResponse<RegionDto>.Success(region, "获取区域信息成功"));
    }

    [HttpPost("CreateRegion")]
    public async Task<IActionResult> CreateRegion([FromBody] CreateRegionRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var newRegion = new Region
        {
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId
        };

        context.Regions.Add(newRegion);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("区域创建成功"));
    }

    [HttpPost("UpdateRegion")]
    public async Task<IActionResult> UpdateRegion([FromBody] UpdateRegionRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var region = await context.Regions.FirstOrDefaultAsync(r => r.Id == request.Id && !r.DeleteFlag);

        if (region == null)
        {
            return NotFound(ApiResponse<string>.Fail("区域未找到"));
        }

        region.Name = request.Name;
        region.Description = request.Description;
        region.CategoryId = request.CategoryId;

        context.Regions.Update(region);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("区域更新成功"));
    }

    [HttpPost("DeleteRegion")]
    public async Task<IActionResult> DeleteRegion([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var region = await context.Regions.FirstOrDefaultAsync(r => r.Id == id);

        if (region == null)
        {
            return NotFound(ApiResponse<string>.Fail("区域未找到"));
        }

        region.DeleteFlag = true;
        context.Regions.Update(region);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("区域删除成功"));
    }
}

public class RegionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
}

public class GetRegionsRequest : PagedRequest
{
    public int? CategoryId { get; set; } // 可选的类别ID，用来筛选当前类别下的区域
}

public class CreateRegionRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateRegionRequest : CreateRegionRequest
{
    public int Id { get; set; }
}