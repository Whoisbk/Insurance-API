using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("claim/{claimId:int}")]
        public async Task<IActionResult> GetByClaim(int claimId)
        {
            var list = await _messageService.GetByClaimAsync(claimId);
            return Ok(list);
        }

        public class SendMessageRequest
        {
            public int ClaimId { get; set; }
            public int? ReceiverId { get; set; }
            public string Content { get; set; } = string.Empty;
            public MessageType Type { get; set; } = MessageType.Text;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            var senderIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            var senderId = int.Parse(senderIdStr);

            var message = new Message
            {
                ClaimId = request.ClaimId,
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                Type = request.Type
            };

            var created = await _messageService.SendAsync(message);
            return Ok(created);
        }
    }
}


