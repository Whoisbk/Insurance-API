using AutoMapper;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Auth;

namespace InsuranceClaimsAPI.Configuration
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<RegisterRequestDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => UserStatus.Active));

            // Add more mappings as you create more DTOs
            // CreateMap<ClaimDto, Claim>();
            // CreateMap<Claim, ClaimDto>();
            // CreateMap<QuoteDto, Quote>();
            // CreateMap<Quote, QuoteDto>();
            // CreateMap<MessageDto, Message>();
            // CreateMap<Message, MessageDto>();
        }
    }
}
