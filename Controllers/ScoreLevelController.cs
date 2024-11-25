using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class ScoreLevelController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
{
    [HttpPost("GetAllScoreLevels")]
    public async Task<IActionResult> GetAllScoreLevels([FromBody] PagedRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var totalCount = await context.ScoreLevels.CountAsync(s => !s.DeleteFlag);
        var scoreLevels = await context.ScoreLevels
            .Where(s => !s.DeleteFlag)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ScoreLevelDto
            {
                Id = s.Id,
                Name = s.Name,
                Score = s.Score,
                UpperBound = s.UpperBound,
                LowerBound = s.LowerBound
            }).ToListAsync();

        var pagedData = new PagedData<ScoreLevelDto>(scoreLevels, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<ScoreLevelDto>>.Success(pagedData, "获取评分等级列表成功"));
    }

    [HttpPost("GetScoreLevelById")]
    public async Task<IActionResult> GetScoreLevelById([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var scoreLevel = await context.ScoreLevels
            .Where(s => s.Id == id && !s.DeleteFlag)
            .Select(s => new ScoreLevelDto
            {
                Id = s.Id,
                Name = s.Name,
                Score = s.Score,
                UpperBound = s.UpperBound,
                LowerBound = s.LowerBound
            }).FirstOrDefaultAsync();

        if (scoreLevel == null)
        {
            return NotFound(ApiResponse<string>.Fail("评分等级未找到"));
        }

        return Ok(ApiResponse<ScoreLevelDto>.Success(scoreLevel, "获取评分等级信息成功"));
    }

    [HttpPost("CreateScoreLevel")]
    public async Task<IActionResult> CreateScoreLevel([FromBody] CreateScoreLevelRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var newScoreLevel = new ScoreLevel
        {
            Name = request.Name,
            Score = request.Score,
            UpperBound = request.UpperBound,
            LowerBound = request.LowerBound
        };

        context.ScoreLevels.Add(newScoreLevel);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("评分等级创建成功"));
    }

    [HttpPost("UpdateScoreLevel")]
    public async Task<IActionResult> UpdateScoreLevel([FromBody] UpdateScoreLevelRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var scoreLevel = await context.ScoreLevels.FirstOrDefaultAsync(s => s.Id == request.Id && !s.DeleteFlag);

        if (scoreLevel == null)
        {
            return NotFound(ApiResponse<string>.Fail("评分等级未找到"));
        }

        scoreLevel.Name = request.Name;
        scoreLevel.Score = request.Score;
        scoreLevel.UpperBound = request.UpperBound;
        scoreLevel.LowerBound = request.LowerBound;

        context.ScoreLevels.Update(scoreLevel);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("评分等级更新成功"));
    }

    [HttpPost("DeleteScoreLevel")]
    public async Task<IActionResult> DeleteScoreLevel([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var scoreLevel = await context.ScoreLevels.FirstOrDefaultAsync(s => s.Id == id);

        if (scoreLevel == null)
        {
            return NotFound(ApiResponse<string>.Fail("评分等级未找到"));
        }

        scoreLevel.DeleteFlag = true;
        context.ScoreLevels.Update(scoreLevel);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("评分等级删除成功"));
    }
}

public class ScoreLevelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Score { get; set; }
    public int UpperBound { get; set; }
    public int LowerBound { get; set; }
}

public class CreateScoreLevelRequest
{
    public string Name { get; set; } = null!;
    public int Score { get; set; }
    public int UpperBound { get; set; }
    public int LowerBound { get; set; }
}

public class UpdateScoreLevelRequest : CreateScoreLevelRequest
{
    public int Id { get; set; }
}
