using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator, LeadInspector")]
    public class BatchController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("GetAllBatches")]
        public async Task<IActionResult> GetAllBatches([FromBody] PagedRequest request)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            // 获取当前用户的身份信息
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
            string role = roleClaim?.Value ?? string.Empty;

            // 创建查询
            var query = context.Batches.AsQueryable().Where(b => !b.DeleteFlag);

            // 如果用户是Inspector，则只获取InspectorId为当前用户的批次
            if (role == "Inspector" && userId.HasValue)
            {
                query = query.Where(b => b.InspectorId == userId);
            }

            // 获取总数量（用于分页）
            var totalCount = await query.CountAsync();

            // 分页数据
            var batches = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => new BatchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status,
                    SummarizeProblem = b.SummarizeProblem,
                    SummarizeHighlight = b.SummarizeHighlight,
                    SummarizeNeedImprove = b.SummarizeNeedImprove,
                    Note = b.Note,
                    HospitalId = b.HospitalId,
                    SummarizePersonId = b.SummarizePersonId,
                    InspectorId = b.InspectorId,
                    HospitalName = b.Hospital.Name,
                    CategoryNames = b.BatchCategories.Select(bc => bc.Category.Name).ToList()
                }).ToListAsync();

            var pagedData = new PagedData<BatchDto>(batches, request.PageNumber, request.PageSize, totalCount);

            return Ok(ApiResponse<PagedData<BatchDto>>.Success(pagedData, "获取批次列表成功"));
        }

        // 获取某个批次的详细信息
        [AllowAnonymous]
        [HttpPost("GetBatchById")]
        public async Task<IActionResult> GetBatchById([FromBody] int id)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var batch = await context.Batches
                .Where(b => b.Id == id && !b.DeleteFlag)
                .Select(b => new BatchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status,
                    SummarizeProblem = b.SummarizeProblem,
                    SummarizeHighlight = b.SummarizeHighlight,
                    SummarizeNeedImprove = b.SummarizeNeedImprove,
                    Note = b.Note,
                    HospitalId = b.HospitalId,
                    SummarizePersonId = b.SummarizePersonId,
                    InspectorId = b.InspectorId,
                    HospitalName = b.Hospital.Name,
                    CategoryNames = b.BatchCategories.Select(bc => bc.Category.Name).ToList()
                }).FirstOrDefaultAsync();

            if (batch == null)
            {
                return NotFound(ApiResponse<string>.Fail("批次未找到"));
            }

            return Ok(ApiResponse<BatchDto>.Success(batch, "获取批次信息成功"));
        }

        // 创建批次
        [HttpPost("CreateBatch")]
        public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest request)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            // 创建新的批次对象
            var newBatch = new Batch
            {
                Status = 0,
                Name = request.Name,
                Note = request.Note,
                HospitalId = request.HospitalId,
                InspectorId = request.InspectorId
            };

            // 将新批次添加到数据库
            context.Batches.Add(newBatch);
            await context.SaveChangesAsync(); // 保存批次，以便获取到生成的批次ID

            // 将批次与选中的类别关联
            foreach (var categoryId in request.CategoryIds)
            {
                var batchCategory = new BatchCategory
                {
                    BatchId = newBatch.Id,
                    CategoryId = categoryId
                };
                context.BatchCategories.Add(batchCategory);
            }

            // 保存类别关联
            await context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Success("批次创建成功"));
        }

        // 更新批次
        [HttpPost("UpdateBatch")]
        public async Task<IActionResult> UpdateBatch([FromBody] UpdateBatchRequest request)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var batch = await context.Batches.Include(b => b.BatchCategories)
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.DeleteFlag);

            if (batch == null)
            {
                return NotFound(ApiResponse<string>.Fail("批次未找到"));
            }

            // 更新批次的基本信息
            batch.Name = request.Name;
            batch.Status = request.Status ?? batch.Status;
            batch.SummarizeProblem = request.SummarizeProblem ?? batch.SummarizeProblem;
            batch.SummarizeHighlight = request.SummarizeHighlight ?? batch.SummarizeHighlight;
            batch.SummarizeNeedImprove = request.SummarizeNeedImprove ?? batch.SummarizeNeedImprove;
            batch.Note = request.Note;
            batch.SummarizePersonId = request.SummarizePersonId;
            batch.HospitalId = request.HospitalId;
            batch.InspectorId = request.InspectorId;

            // 更新批次的类别
            if (request.CategoryIds.Any())
            {
                // 删除现有的批次类别关联
                context.BatchCategories.RemoveRange(batch.BatchCategories);
                // 重新添加批次类别关联
                foreach (var categoryId in request.CategoryIds)
                {
                    var batchCategory = new BatchCategory
                    {
                        BatchId = batch.Id,
                        CategoryId = categoryId
                    };
                    context.BatchCategories.Add(batchCategory);
                }
            }

            context.Batches.Update(batch);
            await context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Success("批次更新成功"));
        }

        // 删除批次（软删除）
        [HttpPost("DeleteBatch")]
        public async Task<IActionResult> DeleteBatch([FromBody] int id)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var batch = await context.Batches.FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null)
            {
                return NotFound(ApiResponse<string>.Fail("批次未找到"));
            }

            batch.DeleteFlag = true;
            context.Batches.Update(batch);
            await context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Success("批次删除成功"));
        }

        // 完成批次
        [HttpPost("CompleteBatch")]
        public async Task<IActionResult> CompleteBatch([FromBody] CompleteBatchRequest request)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var batch = await context.Batches.FirstOrDefaultAsync(b => b.Id == request.Id && !b.DeleteFlag);

            if (batch == null)
            {
                return NotFound(ApiResponse<string>.Fail("批次未找到"));
            }

            if (batch.Status != 2)
            {
                return BadRequest(ApiResponse<string>.Fail("批次未完成评分，无法总结"));
            }

            // 更新批次信息
            batch.SummarizeProblem = request.SummarizeProblem;
            batch.SummarizeHighlight = request.SummarizeHighlight;
            batch.SummarizeNeedImprove = request.SummarizeNeedImprove;
            batch.EndTime = DateTime.UtcNow;
            batch.Note = request.Note;
            batch.Status = 3;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            batch.SummarizePersonId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
            context.Batches.Update(batch);
            await context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Success("批次完成成功"));
        }
    }


    // DTO类
    public class BatchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Status { get; set; }
        public string? SummarizeProblem { get; set; }
        public string? SummarizeHighlight { get; set; }
        public string? SummarizeNeedImprove { get; set; }
        public string? Note { get; set; }
        public int HospitalId { get; set; }
        public int? SummarizePersonId { get; set; }
        public int? InspectorId { get; set; }
        public string HospitalName { get; set; } = null!;
        public List<string> CategoryNames { get; set; } = new List<string>();
    }


    // 请求参数类
    public class CreateBatchRequest
    {
        public string Name { get; set; } = null!;
        public string? Note { get; set; }
        public int HospitalId { get; set; }
        public int? InspectorId { get; set; }
        public List<int> CategoryIds { get; set; } = new();
    }

    public class UpdateBatchRequest : CreateBatchRequest
    {
        public int Id { get; set; }
        public int? Status { get; set; } = 1;
        public string? SummarizeProblem { get; set; }
        public string? SummarizeHighlight { get; set; }
        public string? SummarizeNeedImprove { get; set; }
        public int? SummarizePersonId { get; set; }
    }

    public class CompleteBatchRequest
    {
        public int Id { get; set; }
        public string? SummarizeProblem { get; set; }
        public string? SummarizeHighlight { get; set; }
        public string? SummarizeNeedImprove { get; set; }
        public string? Note { get; set; }
    }
}