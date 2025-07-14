using AutoMapper;
using ChatApp.Backend.DTOs;
using ChatApp.Backend.Models;

namespace ChatApp.Backend.Mappings
{
    /// <summary>
    /// AutoMapper profile for mapping between entities and DTOs
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));

            // Group mappings
            CreateMap<Group, GroupDto>()
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.GroupUsers.Count(gu => gu.IsApproved && !gu.IsDeleted)))
                .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.GroupUsers.Where(gu => gu.IsApproved && !gu.IsDeleted)));

            CreateMap<CreateGroupDto, Group>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.InviteCode, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.GroupUsers, opt => opt.Ignore())
                .ForMember(dest => dest.Messages, opt => opt.Ignore());

            // GroupUser mappings
            CreateMap<GroupUser, GroupMemberDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username));

            // Message mappings
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.ReplyToMessage, opt => opt.MapFrom(src => src.ReplyToMessage));

            CreateMap<SendMessageDto, Message>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.IsEdited, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Group, opt => opt.Ignore())
                .ForMember(dest => dest.ReplyToMessage, opt => opt.Ignore())
                .ForMember(dest => dest.Replies, opt => opt.Ignore())
                .ForMember(dest => dest.AttachmentUrls, opt => opt.MapFrom(src => src.AttachmentUrls ?? new List<string>()));

            // Authentication mappings
            CreateMap<User, UserDto>();
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastActiveAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.ProfilePictureUrl, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Offline"))
                .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
                .ForMember(dest => dest.Messages, opt => opt.Ignore())
                .ForMember(dest => dest.GroupUsers, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedGroups, opt => opt.Ignore());
        }
    }
}

/// <summary>
/// Simple User DTO for basic user information
/// </summary>
public class UserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastActiveAt { get; set; }
}
