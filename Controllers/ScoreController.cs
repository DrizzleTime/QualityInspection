﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, ProjectManager, Inspector, LeadInspector")]
public class ScoreController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("GetAllScores")]
    public async Task<IActionResult> GetAllScores([FromBody] GetScoresRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // 构建查询条件
        var query = context.Scores.AsQueryable();

        // 根据批次ID筛选
        if (request.BatchId.HasValue)
        {
            query = query.Where(s => s.BatchId == request.BatchId.Value);
        }

        // 根据区域ID筛选（Item 通过 Region 关联）
        if (request.RegionId.HasValue)
        {
            query = query.Where(s => s.Item.RegionId == request.RegionId.Value);
        }

        // 根据类别ID筛选（通过 Item 的 Region 关联到 Category）
        if (request.CategoryId.HasValue)
        {
            query = query.Where(s => s.Item.Region.CategoryId == request.CategoryId.Value);
        }

        // 只选择未删除的评分记录
        query = query.Where(s => !s.DeleteFlag);

        // 获取总数
        var totalCount = await query.CountAsync();

        // 分页查询
        var scores = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ScoreDto
            {
                Id = s.Id,
                BatchId = s.BatchId,
                ItemId = s.ItemId,
                ScoreValue = s.ScoreValue,
                Comment = s.Comment,
                Date = s.Date,
                UserId = s.UserId,
                UserName = s.User.Username
            }).ToListAsync();

        // 构造分页数据
        var pagedData = new PagedData<ScoreDto>(scores, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<ScoreDto>>.Success(pagedData, "获取打分列表成功"));
    }

    // 获取某个打分记录（根据ID）
    [AllowAnonymous]
    [HttpPost("GetScoreById")]
    public async Task<IActionResult> GetScoreById([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var score = await context.Scores
            .Where(s => s.Id == id && !s.DeleteFlag)
            .Select(s => new ScoreDto
            {
                Id = s.Id,
                BatchId = s.BatchId,
                ItemId = s.ItemId,
                ScoreValue = s.ScoreValue,
                Comment = s.Comment,
                Date = s.Date,
                UserId = s.UserId,
                UserName = s.User.Username
            }).FirstOrDefaultAsync();

        if (score == null)
        {
            return NotFound(ApiResponse<string>.Fail("打分记录未找到"));
        }

        return Ok(ApiResponse<ScoreDto>.Success(score, "获取打分信息成功"));
    }

    // 创建打分记录
    [HttpPost("CreateScore")]
    public async Task<IActionResult> CreateScore([FromBody] CreateScoreRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // 检查当前批次下是否存在该 ItemId
        var batchExists = await context.BatchCategories
            .AnyAsync(bc => bc.BatchId == request.BatchId &&
                            bc.Category.Regions.Any(r => r.Items.Any(i => i.Id == request.ItemId)));

        if (!batchExists)
        {
            return BadRequest(ApiResponse<string>.Fail("当前批次下不存在该ItemId"));
        }

        // 检查打分是否超过该 Item 的最大分数
        var item = await context.Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId);

        if (item == null)
        {
            return NotFound(ApiResponse<string>.Fail("指定的Item未找到"));
        }

        if (request.ScoreValue > item.Score)
        {
            return BadRequest(ApiResponse<string>.Fail($"评分值不能大于该Item的最大分数 {item.Score}"));
        }

        // 创建打分记录
        var newScore = new Score
        {
            BatchId = request.BatchId,
            ItemId = request.ItemId,
            ScoreValue = request.ScoreValue,
            Comment = request.Comment,
            UserId = request.UserId,
            Date = DateTime.UtcNow
        };

        context.Scores.Add(newScore);

        // 更新批次状态为 1
        var batch = await context.Batches.FirstOrDefaultAsync(b => b.Id == request.BatchId);
        if (batch == null)
        {
            return NotFound(ApiResponse<string>.Fail("指定的批次未找到"));
        }

        if (batch.Status == 0) // 如果状态为0，则更新为1
        {
            batch.Status = 1;
        }

        await context.SaveChangesAsync();

        // 检查是否所有评分完成
        await UpdateBatchStatusIfScoresComplete(context, batch);

        return Ok(ApiResponse<string>.Success("打分记录创建成功"));
    }


    // 更新打分记录
    [HttpPost("UpdateScore")]
    public async Task<IActionResult> UpdateScore([FromBody] UpdateScoreRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var score = await context.Scores
            .FirstOrDefaultAsync(s => s.Id == request.Id && !s.DeleteFlag);

        if (score == null)
        {
            return NotFound(ApiResponse<string>.Fail("打分记录未找到"));
        }

        score.ScoreValue = request.ScoreValue;
        score.Comment = request.Comment;

        context.Scores.Update(score);

        // 获取批次并检查状态
        var batch = await context.Batches.FirstOrDefaultAsync(b => b.Id == score.BatchId);
        if (batch == null)
        {
            return NotFound(ApiResponse<string>.Fail("关联的批次未找到"));
        }

        // 检查是否所有评分完成
        await UpdateBatchStatusIfScoresComplete(context, batch);

        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("打分记录更新成功"));
    }


    // 删除打分记录（软删除）
    [HttpPost("DeleteScore")]
    public async Task<IActionResult> DeleteScore([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var score = await context.Scores
            .FirstOrDefaultAsync(s => s.Id == id);

        if (score == null)
        {
            return NotFound(ApiResponse<string>.Fail("打分记录未找到"));
        }

        score.DeleteFlag = true;
        context.Scores.Update(score);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("打分记录删除成功"));
    }

    private async Task UpdateBatchStatusIfScoresComplete(MyDbContext context, Batch batch)
    {
        // 获取批次关联的所有Item ID
        var batchItemIds = await context.BatchCategories
            .Where(bc => bc.BatchId == batch.Id)
            .SelectMany(bc => bc.Category.Regions.SelectMany(r => r.Items.Select(i => i.Id)))
            .ToListAsync();

        // 获取已评分的Item ID
        var scoredItemIds = await context.Scores
            .Where(s => s.BatchId == batch.Id && !s.DeleteFlag)
            .Select(s => s.ItemId)
            .Distinct()
            .ToListAsync();

        // 如果所有Item都已评分，则更新批次状态为2
        if (batchItemIds.All(id => scoredItemIds.Contains(id)))
        {
            batch.Status = 2;
        }
    }
}

public class ScoreDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int ItemId { get; set; }
    public int ScoreValue { get; set; }
    public string? Comment { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = null!; // 打分人员用户名
}

public class GetScoresRequest : PagedRequest
{
    public int? BatchId { get; set; } // 可选的批次ID
    public int? CategoryId { get; set; } // 可选的类别ID
    public int? RegionId { get; set; } // 可选的区域ID
}

public class CreateScoreRequest
{
    public int BatchId { get; set; }
    public int ItemId { get; set; }
    public int ScoreValue { get; set; }
    public string? Comment { get; set; }
    public int UserId { get; set; }
}

public class UpdateScoreRequest : CreateScoreRequest
{
    public int Id { get; set; }
}