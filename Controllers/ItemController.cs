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
    [AllowAnonymous]
    [HttpPost("GetAllItems")]
    public async Task<IActionResult> GetAllItems([FromBody] GetItemsRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var query = context.Items.AsQueryable();

        if (request.RegionId.HasValue)
        {
            query = query.Where(i => i.RegionId == request.RegionId.Value);
        }

        query = query.Where(i => !i.DeleteFlag);

        var totalCount = await query.CountAsync();

        var items = await query
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
                ScoreLevelName = i.ScoreLevel != null ? i.ScoreLevel.Name : null,
                IsScored = request.BatchId.HasValue
                    ? context.Scores.Any(s => s.BatchId == request.BatchId.Value && s.ItemId == i.Id)
                    : null,
                ScoreValue = request.BatchId.HasValue
                    ? context.Scores
                        .Where(s => s.BatchId == request.BatchId.Value && s.ItemId == i.Id)
                        .Select(s => s.ScoreValue)
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync();

        var pagedData = new PagedData<ItemDto>(items, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<ItemDto>>.Success(pagedData, "获取检查条目列表成功"));
    }


    [AllowAnonymous]
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
                ScoreLevelName = i.ScoreLevel != null ? i.ScoreLevel.Name : null,
                IsScored = context.Scores.Any(s => s.ItemId == i.Id),
                ScoreValue = context.Scores
                    .Where(s => s.ItemId == i.Id)
                    .Select(s => s.ScoreValue)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return NotFound(ApiResponse<string>.Fail("检查条目未找到"));
        }

        return Ok(ApiResponse<ItemDto>.Success(item, "获取检查条目信息成功"));
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

        return Ok(ApiResponse<string>.Success("检查条目创建成功"));
    }

    [HttpPost("UpdateItem")]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateItemRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.Id && !i.DeleteFlag);

        if (item == null)
        {
            return NotFound(ApiResponse<string>.Fail("检查条目未找到"));
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.Score = request.Score;
        item.RegionId = request.RegionId;
        item.ScoreLevelId = request.ScoreLevelId;

        context.Items.Update(item);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("检查条目更新成功"));
    }

    [HttpPost("DeleteItem")]
    public async Task<IActionResult> DeleteItem([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound(ApiResponse<string>.Fail("检查条目未找到"));
        }

        item.DeleteFlag = true;
        context.Items.Update(item);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("检查条目删除成功"));
    }

    [HttpPost("BatchDeleteItemsByRegionId")]
    public async Task<IActionResult> BatchDeleteItemsByRegionId([FromBody] int regionId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var items = await context.Items
            .Where(i => i.RegionId == regionId && !i.DeleteFlag)
            .ToListAsync();

        if (!items.Any())
        {
            return NotFound(ApiResponse<string>.Fail("未找到属于该区域的检查项"));
        }

        foreach (var item in items)
        {
            item.DeleteFlag = true;
        }

        context.Items.UpdateRange(items);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("批量删除检查项成功"));
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
    public bool? IsScored { get; set; }
    public int? ScoreValue { get; set; } // 新增属性
}

public class GetItemsRequest : PagedRequest
{
    public int? RegionId { get; set; }
    public int? BatchId { get; set; }
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