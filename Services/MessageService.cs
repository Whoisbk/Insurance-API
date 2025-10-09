using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Hubs;
using InsuranceClaimsAPI.Models.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public interface IMessageService
    {
        Task<Message> SendAsync(Message message);
        Task<IReadOnlyList<Message>> GetByClaimAsync(int claimId);
    }

    public class MessageService : IMessageService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IHubContext<MessageHub> _hubContext;

        public MessageService(InsuranceClaimsContext context, IHubContext<MessageHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<Message> SendAsync(Message message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            message.Status = MessageStatus.Sent;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"Claim_{message.ClaimId}").SendAsync("ReceiveMessage", new
            {
                ClaimId = message.ClaimId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                MessageId = message.Id,
                Content = message.Content,
                Type = message.Type.ToString(),
                Timestamp = DateTime.UtcNow
            });

            return message;
        }

        public async Task<IReadOnlyList<Message>> GetByClaimAsync(int claimId)
        {
            return await _context.Messages
                .Where(m => m.ClaimId == claimId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}


