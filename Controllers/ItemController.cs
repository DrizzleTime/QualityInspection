using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class ItemController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
{
    [HttpPost("GetAllItems")]
    public async Task<IActionResult> GetAllItems([FromBody] PagedRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var totalCount = await context.Items.CountAsync(i => !i.DeleteFlag);
        var items = await context.Items
            .Where(i => !i.DeleteFlag)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new ItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Score = i.Score,
                RegionId = i.RegionId,
                RegionName = i.Region.Name,
                ScoreLevelId = i.ScoreLevelId,
                ScoreLevelName = i.ScoreLevel != null ? i.ScoreLevel.Name : null
            }).ToListAsync();

        var pagedData = new PagedData<ItemDto>(items, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<ItemDto>>.Success(pagedData, "获取项目列表成功"));
    }

    [HttpPost("GetItemById")]
    public async Task<IActionResult> GetItemById([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var item = await context.Items
            .Where(i => i.Id == id && !i.DeleteFlag)
            .Select(i => new ItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Score = i.Score,
                RegionId = i.RegionId,
                RegionName = i.Region.Name,
                ScoreLevelId = i.ScoreLevelId,
                ScoreLevelName = i.ScoreLevel != null ? i.ScoreLevel.Name : null
            }).FirstOrDefaultAsync();

        if (item == null)
        {
            return NotFound(ApiResponse<string>.Fail("项目未找到"));
        }

        return Ok(ApiResponse<ItemDto>.Success(item, "获取项目信息成功"));
    }

    [HttpPost("CreateItem")]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var newItem = new Item
        {
            Name = request.Name,
            Description = request.Description,
            Score = request.Score,
            RegionId = request.RegionId,
            ScoreLevelId = request.ScoreLevelId
        };

        context.Items.Add(newItem);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("项目创建成功"));
    }

    [HttpPost("UpdateItem")]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateItemRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.Id && !i.DeleteFlag);

        if (item == null)
        {
            return NotFound(ApiResponse<string>.Fail("项目未找到"));
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.Score = request.Score;
        item.RegionId = request.RegionId;
        item.ScoreLevelId = request.ScoreLevelId;

        context.Items.Update(item);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("项目更新成功"));
    }

    [HttpPost("DeleteItem")]
    public async Task<IActionResult> DeleteItem([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound(ApiResponse<string>.Fail("项目未找到"));
        }

        item.DeleteFlag = true;
        context.Items.Update(item);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("项目删除成功"));
    }
}

public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Score { get; set; }
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;
    public int? ScoreLevelId { get; set; }
    public string? ScoreLevelName { get; set; }
}

public class CreateItemRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Score { get; set; }
    public int RegionId { get; set; }
    public int? ScoreLevelId { get; set; }
}

public class UpdateItemRequest : CreateItemRequest
{
    public int Id { get; set; }
}