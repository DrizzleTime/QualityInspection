using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityInspection.Request;
using QualityInspection.Result;

namespace QualityInspection.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class HospitalController(IDbContextFactory<MyDbContext> contextFactory) : ControllerBase
{
    [HttpPost("GetAllHospitals")]
    public async Task<IActionResult> GetAllHospitals([FromBody] PagedRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var totalCount = await context.Hospitals.CountAsync(h => !h.DeleteFlag);
        var hospitals = await context.Hospitals
            .Where(h => !h.DeleteFlag)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(h => new HospitalDto
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address
            }).ToListAsync();

        var pagedData = new PagedData<HospitalDto>(hospitals, request.PageNumber, request.PageSize, totalCount);

        return Ok(ApiResponse<PagedData<HospitalDto>>.Success(pagedData, "获取医院列表成功"));
    }

    [HttpPost("GetHospitalById")]
    public async Task<IActionResult> GetHospitalById([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var hospital = await context.Hospitals
            .Where(h => h.Id == id && !h.DeleteFlag)
            .Select(h => new HospitalDto
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address
            }).FirstOrDefaultAsync();

        if (hospital == null)
        {
            return NotFound(ApiResponse<string>.Fail("医院未找到"));
        }

        return Ok(ApiResponse<HospitalDto>.Success(hospital, "获取医院信息成功"));
    }

    [HttpPost("CreateHospital")]
    public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var newHospital = new Hospital
        {
            Name = request.Name,
            Address = request.Address
        };

        context.Hospitals.Add(newHospital);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("医院创建成功"));
    }

    [HttpPost("UpdateHospital")]
    public async Task<IActionResult> UpdateHospital([FromBody] UpdateHospitalRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var hospital = await context.Hospitals.FirstOrDefaultAsync(h => h.Id == request.Id && !h.DeleteFlag);

        if (hospital == null)
        {
            return NotFound(ApiResponse<string>.Fail("医院未找到"));
        }

        hospital.Name = request.Name;
        hospital.Address = request.Address;

        context.Hospitals.Update(hospital);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("医院更新成功"));
    }

    [HttpPost("DeleteHospital")]
    public async Task<IActionResult> DeleteHospital([FromBody] int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var hospital = await context.Hospitals.FirstOrDefaultAsync(h => h.Id == id);

        if (hospital == null)
        {
            return NotFound(ApiResponse<string>.Fail("医院未找到"));
        }

        hospital.DeleteFlag = true;
        context.Hospitals.Update(hospital);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Success("医院删除成功"));
    }
}

public class HospitalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}

public class CreateHospitalRequest
{
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
}

public class UpdateHospitalRequest : CreateHospitalRequest
{
    public int Id { get; set; }
}