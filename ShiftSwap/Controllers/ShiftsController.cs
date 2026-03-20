using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSwap.Data;
using ShiftSwap.Dtos;
using ShiftSwap.DTOs;
using ShiftSwap.Models;
using ShiftSwap.Services;
using System.Security.Claims;

namespace ShiftSwap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShiftsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogger _audit;

        public ShiftsController(AppDbContext db, IAuditLogger audit)
        {
            _db = db;
            _audit = audit;
        }

        // Saját műszakok (paging + sorting)
        [HttpGet("my")]
        public async Task<ActionResult<PagedResult<ShiftDto>>> GetMyShifts(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "start",
            [FromQuery] string? sortDir = "asc")
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user.");

            var query = _db.Shifts
                .Include(s => s.Location)
                .Where(s => s.UserId == userId &&
                            s.ShiftDate >= from.Date &&
                            s.ShiftDate <= to.Date);

            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy?.ToLower()) switch
            {
                "date" => desc ? query.OrderByDescending(s => s.ShiftDate)
                               : query.OrderBy(s => s.ShiftDate),
                "status" => desc ? query.OrderByDescending(s => s.Status)
                                 : query.OrderBy(s => s.Status),
                _ => desc ? query.OrderByDescending(s => s.StartDateTime)
                          : query.OrderBy(s => s.StartDateTime)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<ShiftDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items.Select(s => new ShiftDto
                {
                    Id = s.Id,
                    ShiftDate = s.ShiftDate,
                    StartDateTime = s.StartDateTime,
                    EndDateTime = s.EndDateTime,
                    Status = s.Status.ToString(),
                    LocationName = s.Location.Name
                })
            };

            return Ok(result);
        }

        // Telephely műszakjai managernek
        [Authorize(Roles = "Manager,Admin")]
        [HttpGet("location")]
        public async Task<ActionResult<PagedResult<ShiftDto>>> GetLocationShifts(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int? locationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var companyIdClaim = User.FindFirst("companyId")?.Value;
            var locationIdClaim = User.FindFirst("locationId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized("Invalid company.");

            int? userLocationId = int.TryParse(locationIdClaim, out var loc) ? loc : (int?)null;

            int targetLocationId;
            if (locationId.HasValue)
            {
                targetLocationId = locationId.Value;
            }
            else if (userLocationId.HasValue)
            {
                targetLocationId = userLocationId.Value;
            }
            else
            {
                return BadRequest("No location specified for manager without default location.");
            }

            var query = _db.Shifts
                .Include(s => s.Location)
                .Include(s => s.User)
                .Where(s => s.LocationId == targetLocationId &&
                            s.ShiftDate >= from.Date &&
                            s.ShiftDate <= to.Date)
                .OrderBy(s => s.StartDateTime);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<ShiftDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items.Select(s => new ShiftDto
                {
                    Id = s.Id,
                    ShiftDate = s.ShiftDate,
                    StartDateTime = s.StartDateTime,
                    EndDateTime = s.EndDateTime,
                    Status = s.Status.ToString(),
                    LocationName = s.Location.Name
                })
            };

            return Ok(result);
        }

        // Új műszak
        [Authorize(Roles = "Manager,Admin")]
        [HttpPost]
        public async Task<ActionResult<ShiftDto>> CreateShift([FromBody] CreateShiftDto dto)
        {
            var companyIdClaim = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized("Invalid company.");

            var location = await _db.Locations
                .FirstOrDefaultAsync(l => l.Id == dto.LocationId && l.CompanyId == companyId);

            if (location == null)
                return BadRequest("Invalid location.");

            if (dto.EndDateTime <= dto.StartDateTime)
                return BadRequest("EndDateTime must be after StartDateTime.");

            User? user = null;
            if (dto.UserId.HasValue)
            {
                user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == dto.UserId.Value && u.CompanyId == companyId);
                if (user == null)
                    return BadRequest("Invalid user.");
            }

            var shift = new Shift
            {
                LocationId = dto.LocationId,
                UserId = dto.UserId,
                ShiftDate = dto.StartDateTime.Date,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                Status = ShiftStatus.Assigned
            };

            _db.Shifts.Add(shift);
            await _db.SaveChangesAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? currentUserId = int.TryParse(userIdClaim, out var uid) ? uid : (int?)null;
            await _audit.LogAsync(currentUserId, "ShiftCreated", "Shift", shift.Id, $"LocationId={shift.LocationId}");

            var result = new ShiftDto
            {
                Id = shift.Id,
                ShiftDate = shift.ShiftDate,
                StartDateTime = shift.StartDateTime,
                EndDateTime = shift.EndDateTime,
                Status = shift.Status.ToString(),
                LocationName = location.Name
            };

            return CreatedAtAction(nameof(GetLocationShifts),
                new { from = shift.ShiftDate, to = shift.ShiftDate, locationId = shift.LocationId }, result);
        }

        // Műszak módosítás
        [Authorize(Roles = "Manager,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateShift(int id, [FromBody] UpdateShiftDto dto)
        {
            var companyIdClaim = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized("Invalid company.");

            var shift = await _db.Shifts
                .IgnoreQueryFilters()
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id && s.Location.CompanyId == companyId);

            if (shift == null)
                return NotFound("Shift not found.");

            if (dto.UserId.HasValue)
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == dto.UserId.Value && u.CompanyId == companyId);
                if (user == null)
                    return BadRequest("Invalid user.");
                shift.UserId = dto.UserId;
            }

            if (dto.StartDateTime.HasValue)
            {
                shift.StartDateTime = dto.StartDateTime.Value;
                shift.ShiftDate = dto.StartDateTime.Value.Date;
            }

            if (dto.EndDateTime.HasValue)
            {
                shift.EndDateTime = dto.EndDateTime.Value;
            }

            if (dto.Status.HasValue)
            {
                shift.Status = dto.Status.Value;
            }

            await _db.SaveChangesAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? currentUserId = int.TryParse(userIdClaim, out var uid) ? uid : (int?)null;
            await _audit.LogAsync(currentUserId, "ShiftUpdated", "Shift", shift.Id, null);

            return Ok("Shift updated.");
        }

        // Soft delete
        [Authorize(Roles = "Manager,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteShift(int id)
        {
            var companyIdClaim = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized("Invalid company.");

            var shift = await _db.Shifts
                .IgnoreQueryFilters()
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id && s.Location.CompanyId == companyId);

            if (shift == null)
                return NotFound("Shift not found.");

            if (shift.IsDeleted)
                return BadRequest("Shift is already deleted.");

            shift.IsDeleted = true;

            var swapRequests = await _db.ShiftSwapRequests
                .Where(r => r.ShiftId == id &&
                            (r.Status == SwapRequestStatus.Open || r.Status == SwapRequestStatus.Accepted))
                .ToListAsync();

            foreach (var req in swapRequests)
            {
                req.Status = SwapRequestStatus.Cancelled;
                req.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? currentUserId = int.TryParse(userIdClaim, out var uid) ? uid : (int?)null;
            await _audit.LogAsync(currentUserId, "ShiftDeleted", "Shift", shift.Id, "Soft delete");

            return Ok("Shift deleted (soft delete).");
        }
    }
}
