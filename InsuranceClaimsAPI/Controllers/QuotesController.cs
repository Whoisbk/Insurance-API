using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteService _quoteService;

        public QuotesController(IQuoteService quoteService)
        {
            _quoteService = quoteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var quotes = await _quoteService.GetAllAsync();
            return Ok(quotes);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var quote = await _quoteService.GetByIdAsync(id);
            if (quote == null)
            {
                return NotFound(new { message = "Quote not found" });
            }
            return Ok(quote);
        }

        [HttpGet("claim/{claimId:int}")]
        public async Task<IActionResult> GetByClaim(int claimId)
        {
            var quotes = await _quoteService.GetByClaimAsync(claimId);
            return Ok(quotes);
        }

        [HttpGet("provider/{userId}")]
        public async Task<IActionResult> GetByProviderFirebaseId(string userId)
        {
            var quotes = await _quoteService.GetByProviderFirebaseIdAsync(userId);
            return Ok(quotes);
        }

        [HttpPost("/api/add/quotes")]
        // [Authorize(Policy = "RequireProviderRole")]
        public async Task<IActionResult> Submit([FromBody] Quote quote)
        {
            var created = await _quoteService.SubmitAsync(quote);
            return Ok(created);
        }

        public class SetStatusRequest { public QuoteStatus Status { get; set; } public string? ReasonOrNotes { get; set; } }

        [HttpPut("{id:int}/status")]
        // [Authorize(Policy = "RequireInsurerRole")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetStatusRequest request)
        {
            await _quoteService.SetStatusAsync(id, request.Status, request.ReasonOrNotes);
            return NoContent();
        }

        [HttpPut("/api/quotes/update/status/{quoteId:int}")]
        // [Authorize(Roles = "Insurer,Admin")]
        public async Task<IActionResult> UpdateQuoteStatus(int quoteId, [FromBody] SetStatusRequest request)
        {
            var quote = await _quoteService.GetByIdAsync(quoteId);
            if (quote == null)
            {
                return NotFound(new { message = "Quote not found" });
            }

            await _quoteService.SetStatusAsync(quoteId, request.Status, request.ReasonOrNotes);
            return Ok(new { message = "Quote status updated successfully", quoteId, status = request.Status });
        }
    }
}


