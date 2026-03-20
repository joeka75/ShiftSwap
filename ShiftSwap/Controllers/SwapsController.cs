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
    public class SwapsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogger _audit;

        public SwapsController(AppDbContext db, IAuditLogger audit)
        {
            _db = db;
            _audit = audit;
        }

        // Nyitott csereajánlatok (paging)
        [HttpGet("open")]
        public async Task<ActionResult<PagedResult<SwapRequestDto>>> GetOpenSwapRequests(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "created",
            [FromQuery] string? sortDir = "desc")
        {
            var query = _db.ShiftSwapRequests
                .Include(r => r.Shift).ThenInclude(s => s.Location)
                .Include(r => r.FromUser)
                .Include(r => r.ToUser)
                .Where(r => r.Status == SwapRequestStatus.Open);

            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy?.ToLower()) switch
            {
                "date" => desc
                    ? query.OrderByDescending(r => r.Shift.ShiftDate)
                    : query.OrderBy(r => r.Shift.ShiftDate),
                "from" => desc
                    ? query.OrderByDescending(r => r.FromUser.FullName)
                    : query.OrderBy(r => r.FromUser.FullName),
                _ => desc
                    ? query.OrderByDescending(r => r.CreatedAt)
                    : query.OrderBy(r => r.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<SwapRequestDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items.Select(r => new SwapRequestDto
                {
                    Id = r.Id,
                    ShiftId = r.ShiftId,
                    ShiftInfo = $"{r.Shift.ShiftDate:yyyy-MM-dd} {r.Shift.StartDateTime:HH:mm}-{r.Shift.EndDateTime:HH:mm} @ {r.Shift.Location.Name}",
                    FromUserName = r.FromUser.FullName,
                    ToUserName = r.ToUser?.FullName,
                    Status = r.Status.ToString()
                })
            };

            return Ok(result);
        }

        // Saját csereajánlataim (én kértem vagy én vállaltam)
        [HttpGet("my")]
        public async Task<ActionResult<PagedResult<SwapRequestDto>>> GetMySwaps(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user.");

            var query = _db.ShiftSwapRequests
                .Include(r => r.Shift).ThenInclude(s => s.Location)
                .Include(r => r.FromUser)
                .Include(r => r.ToUser)
                .Where(r => r.FromUserId == userId || r.ToUserId == userId)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<SwapRequestDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items.Select(r => new SwapRequestDto
                {
                    Id = r.Id,
                    ShiftId = r.ShiftId,
                    ShiftInfo = $"{r.Shift.ShiftDate:yyyy-MM-dd} {r.Shift.StartDateTime:HH:mm}-{r.Shift.EndDateTime:HH:mm} @ {r.Shift.Location.Name}",
                    FromUserName = r.FromUser.FullName,
                    ToUserName = r.ToUser?.FullName,
                    Status = r.Status.ToString()
                })
            };

            return Ok(result);
        }

        // Manager: pending approval
        [Authorize(Roles = "Manager,Admin")]
        [HttpGet("pending-approval")]
        public async Task<ActionResult<PagedResult<SwapRequestDto>>> GetPendingApproval(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var companyIdClaim = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized("Invalid company.");

            var query = _db.ShiftSwapRequests
                .Include(r => r.Shift).ThenInclude(s => s.Location)
                .Include(r => r.FromUser)
                .Include(r => r.ToUser)
                .Where(r => r.Status == SwapRequestStatus.Accepted &&
                            r.Shift.Location.CompanyId == companyId)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<SwapRequestDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items.Select(r => new SwapRequestDto
                {
                    Id = r.Id,
                    ShiftId = r.ShiftId,
                    ShiftInfo = $"{r.Shift.ShiftDate:yyyy-MM-dd} {r.Shift.StartDateTime:HH:mm}-{r.Shift.EndDateTime:HH:mm} @ {r.Shift.Location.Name}",
                    FromUserName = r.FromUser.FullName,
                    ToUserName = r.ToUser?.FullName,
                    Status = r.Status.ToString()
                })
            };

            return Ok(result);
        }

        // Csere létrehozása (saját műszakra)
        [HttpPost]
        public async Task<ActionResult<SwapRequestDto>> CreateSwapRequest([FromBody] CreateSwapRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user.");

            var shift = await _db.Shifts
                .Include(s => s.Location)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == dto.ShiftId);

            if (shift == null)
                return NotFound("Shift not found.");

            if (shift.UserId != userId)
                return BadRequest("You do not own this shift.");

            if (shift.Status != ShiftStatus.Assigned && shift.Status != ShiftStatus.PendingSwap)
                return BadRequest("Shift is not eligible for swapping.");

            var fromUser = await _db.Users.FindAsync(userId);
            if (fromUser == null || !fromUser.IsActive)
                return BadRequest("User not found or inactive.");

            var request = new ShiftSwapRequest
            {
                ShiftId = shift.Id,
                FromUserId = userId,
                ToUserId = null,
                Status = SwapRequestStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            shift.Status = ShiftStatus.PendingSwap;

            _db.ShiftSwapRequests.Add(request);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(userId, "SwapCreated", "ShiftSwapRequest", request.Id, $"ShiftId={shift.Id}");

            var result = new SwapRequestDto
            {
                Id = request.Id,
                ShiftId = request.ShiftId,
                ShiftInfo = $"{shift.ShiftDate:yyyy-MM-dd} {shift.StartDateTime:HH:mm}-{shift.EndDateTime:HH:mm} @ {shift.Location.Name}",
                FromUserName = fromUser.FullName,
                ToUserName = null,
                Status = request.Status.ToString()
            };

            return CreatedAtAction(nameof(GetOpenSwapRequests), new { id = request.Id }, result);
        }

        // Dolgozó: vállalja a cserét
        [HttpPost("{id}/accept")]
        public async Task<ActionResult> AcceptSwap(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user.");

            var request = await _db.ShiftSwapRequests
                .Include(r => r.Shift)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("Swap request not found.");

            if (request.Status != SwapRequestStatus.Open)
                return BadRequest("Swap request is not open.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return BadRequest("User not found or inactive.");

            if (request.ToUserId.HasValue && request.ToUserId.Value != userId)
                return BadRequest("This request is not assigned to this user.");

            if (request.FromUserId == userId)
                return BadRequest("Nem veheted át a saját felajánlott műszakodat.");

            request.Status = SwapRequestStatus.Accepted;
            request.ToUserId = userId;
            request.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(userId, "SwapAccepted", "ShiftSwapRequest", request.Id, null);

            return Ok("Swap request accepted. Waiting for manager approval.");
        }

        // Manager: jóváhagyja
        [Authorize(Roles = "Manager,Admin")]
        [HttpPost("{id}/approve")]
        public async Task<ActionResult> ApproveSwap(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var managerId))
                return Unauthorized("Invalid user.");

            var request = await _db.ShiftSwapRequests
                .Include(r => r.Shift)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("Swap request not found.");

            if (request.Status != SwapRequestStatus.Accepted)
                return BadRequest("Swap request must be accepted before approval.");

            if (!request.ToUserId.HasValue)
                return BadRequest("No target user to assign the shift to.");

            var shift = request.Shift;
            shift.UserId = request.ToUserId;
            shift.Status = ShiftStatus.Swapped;

            request.Status = SwapRequestStatus.ApprovedByManager;
            request.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(managerId, "SwapApproved", "ShiftSwapRequest", request.Id, null);

            return Ok("Swap approved and shift reassigned.");
        }

        // Reject
        [HttpPost("{id}/reject")]
        public async Task<ActionResult> RejectSwap(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var request = await _db.ShiftSwapRequests
                .Include(r => r.Shift)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("Swap request not found.");

            if (request.Status != SwapRequestStatus.Open &&
                request.Status != SwapRequestStatus.Accepted)
                return BadRequest("This swap request cannot be rejected in its current state.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return BadRequest("User not found or inactive.");

            bool isTargetWorker =
                request.ToUserId.HasValue && request.ToUserId.Value == userId;

            bool isManagerLike =
                role == UserRole.Manager.ToString() || role == UserRole.Admin.ToString();

            if (!isTargetWorker && !isManagerLike)
                return BadRequest("User is not allowed to reject this swap request.");

            request.Status = SwapRequestStatus.Rejected;
            request.UpdatedAt = DateTime.UtcNow;

            if (request.Shift.Status == ShiftStatus.PendingSwap)
            {
                request.Shift.Status = ShiftStatus.Assigned;
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(userId, "SwapRejected", "ShiftSwapRequest", request.Id, null);

            return Ok("Swap request rejected.");
        }

        // Cancel
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelSwap(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var request = await _db.ShiftSwapRequests
                .Include(r => r.Shift)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("Swap request not found.");

            if (request.Status == SwapRequestStatus.ApprovedByManager ||
                request.Status == SwapRequestStatus.Cancelled)
                return BadRequest("This swap request cannot be cancelled.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return BadRequest("User not found or inactive.");

            bool isOwner = request.FromUserId == userId;
            bool isManagerLike =
                role == UserRole.Manager.ToString() || role == UserRole.Admin.ToString();

            if (!isOwner && !isManagerLike)
                return BadRequest("User is not allowed to cancel this swap request.");

            request.Status = SwapRequestStatus.Cancelled;
            request.UpdatedAt = DateTime.UtcNow;

            if (request.Shift.Status == ShiftStatus.PendingSwap)
            {
                request.Shift.Status = ShiftStatus.Assigned;
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(userId, "SwapCancelled", "ShiftSwapRequest", request.Id, null);

            return Ok("Swap request cancelled.");
        }
    }
}
