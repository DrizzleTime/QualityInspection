using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator, Manager, Supervisor")]
    public class BatchController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
    {
        [HttpPost("GetAllBatches")]
        public async Task<IActionResult> GetAllBatches([FromBody] PagedRequest request)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var totalCount = await context.Batches
                .Where(b => !b.DeleteFlag)
                .CountAsync();

            var batches = await context.Batches
                .Where(b => !b.DeleteFlag)
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
                    HospitalName = b.Hospital.Name,
                    CategoryNames = b.BatchCategories.Select(bc => bc.Category.Name).ToList()
                }).ToListAsync();

            var pagedData = new PagedData<BatchDto>(batches, request.PageNumber, request.PageSize, totalCount);

            return Ok(ApiResponse<PagedData<BatchDto>>.Success(pagedData, "获取批次列表成功"));
        }

        // 获取某个批次的详细信息
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
                    HospitalName = b.Hospital.Name, // 假设 Batch 和 Hospital 之间有关联
                    CategoryNames = b.BatchCategories.Select(bc => bc.Category.Name).ToList() // 获取批次关联的类别名称
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
                Name = request.Name,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = request.Status,
                SummarizeProblem = request.SummarizeProblem,
                SummarizeHighlight = request.SummarizeHighlight,
                SummarizeNeedImprove = request.SummarizeNeedImprove,
                Note = request.Note,
                SummarizePersonId = request.SummarizePersonId,
                HospitalId = request.HospitalId
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

            var batch = await context.Batches.FirstOrDefaultAsync(b => b.Id == request.Id && !b.DeleteFlag);

            if (batch == null)
            {
                return NotFound(ApiResponse<string>.Fail("批次未找到"));
            }

            batch.Name = request.Name;
            batch.StartTime = request.StartTime;
            batch.EndTime = request.EndTime;
            batch.Status = request.Status;
            batch.SummarizeProblem = request.SummarizeProblem;
            batch.SummarizeHighlight = request.SummarizeHighlight;
            batch.SummarizeNeedImprove = request.SummarizeNeedImprove;
            batch.Note = request.Note;
            batch.SummarizePersonId = request.SummarizePersonId;
            batch.HospitalId = request.HospitalId;

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
        public int SummarizePersonId { get; set; }
        public string HospitalName { get; set; } = null!;

        // 新增属性：批次关联的类别信息
        public List<string> CategoryNames { get; set; } = new List<string>();
    }


    // 请求参数类
    public class CreateBatchRequest
    {
        public string Name { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Status { get; set; } = 1;
        public string? SummarizeProblem { get; set; }
        public string? SummarizeHighlight { get; set; }
        public string? SummarizeNeedImprove { get; set; }
        public string? Note { get; set; }
        public int SummarizePersonId { get; set; }
        public int HospitalId { get; set; }

        public List<int> CategoryIds { get; set; } = new List<int>();
    }


    public class UpdateBatchRequest : CreateBatchRequest
    {
        public int Id { get; set; }
    }
}