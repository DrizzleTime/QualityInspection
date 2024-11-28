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
    public async Task<IActionResult> GetAllItems([FromBody] GetItemsRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var query = context.Items.AsQueryable();

        // 如果传入了 RegionId，则过滤出该区域的所有项
        if (request.RegionId.HasValue)
        {
            query = query.Where(i => i.RegionId == request.RegionId.Value);
        }

        // 只选择未删除的项
        query = query.Where(i => !i.DeleteFlag);

        // 获取总数
        var totalCount = await query.CountAsync();

        // 分页查询
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
                ScoreLevelName = i.ScoreLevel != null ? i.ScoreLevel.Name : null
            })
            .ToListAsync();

        // 构造分页数据
        var pagedData = new PagedData<ItemDto>(items, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<ItemDto>>.Success(pagedData, "获取检查条目列表成功"));
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

public class GetItemsRequest : PagedRequest
{
    public int? RegionId { get; set; } // 可选的区域ID，用来筛选当前区域下的检查项
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