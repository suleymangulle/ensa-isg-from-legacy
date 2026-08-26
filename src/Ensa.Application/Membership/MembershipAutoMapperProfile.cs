using AutoMapper;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Domain.Membership;

namespace Ensa.Application.Membership;

/// <summary>
/// Mappings for the membership module (user, role, permission).
/// <para>
/// <see cref="User"/> and <see cref="Role"/> derive from the ASP.NET Core Identity base
/// types, which bring a large set of properties that must never be written from a request
/// payload: the normalized lookup keys, the password hash, the security and concurrency
/// stamps, the lockout counters and the privilege flags. They are all ignored explicitly
/// below rather than left to convention, so that adding a field to an input DTO can never
/// silently start writing one of them.
/// </para>
/// <para>
/// Navigation DTOs are not mapped here; they are projected by hand inside the app service.
/// </para>
/// </summary>
public class MembershipAutoMapperProfile : Profile
{
    public MembershipAutoMapperProfile()
    {
        // ------------------------------------------------------------- User

        CreateMap<User, UserDto>();

        CreateMap<User, UserListDto>();

        IgnoreNonWritableUserMembers(CreateMap<UserInputDto, User>())
            // The user name is the login identifier and appears in audit trails, so it is
            // never taken from the shared input base; only CreateUserDto may set it.
            .ForMember(d => d.UserName, o => o.Ignore());

        CreateMap<CreateUserDto, User>()
            .IncludeBase<UserInputDto, User>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.UserName));

        CreateMap<UpdateUserDto, User>()
            .IncludeBase<UserInputDto, User>();

        // ------------------------------------------------------------- Role

        CreateMap<Role, RoleDto>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name ?? string.Empty))
            // Requires a separate count query; the app service fills it in.
            .ForMember(d => d.UserCount, o => o.Ignore());

        CreateMap<Role, RoleListDto>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name ?? string.Empty));

        CreateMap<RoleInputDto, Role>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            // Maintained by RoleManager.
            .ForMember(d => d.NormalizedName, o => o.Ignore())
            .ForMember(d => d.ConcurrencyStamp, o => o.Ignore())
            // Seeded flag: a role can never become (or stop being) a system role over HTTP.
            .ForMember(d => d.IsStatic, o => o.Ignore());

        CreateMap<CreateRoleDto, Role>().IncludeBase<RoleInputDto, Role>();

        CreateMap<UpdateRoleDto, Role>().IncludeBase<RoleInputDto, Role>();

        // ------------------------------------------------------- Permission

        CreateMap<Permission, PermissionDto>();
    }

    /// <summary>
    /// Ignores every <see cref="User"/> member that must not be written from a request:
    /// the key, the tenant, the audit trail, the Identity infrastructure columns, the
    /// privilege flags and the encrypted Medula credentials.
    /// </summary>
    private static IMappingExpression<TSource, User> IgnoreNonWritableUserMembers<TSource>(
        IMappingExpression<TSource, User> map)
        => map
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            // Audit trail - written by the DbContext interceptor.
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            // Identity infrastructure - maintained by UserManager only.
            .ForMember(d => d.NormalizedUserName, o => o.Ignore())
            .ForMember(d => d.NormalizedEmail, o => o.Ignore())
            .ForMember(d => d.EmailConfirmed, o => o.Ignore())
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.SecurityStamp, o => o.Ignore())
            .ForMember(d => d.ConcurrencyStamp, o => o.Ignore())
            .ForMember(d => d.PhoneNumberConfirmed, o => o.Ignore())
            .ForMember(d => d.TwoFactorEnabled, o => o.Ignore())
            .ForMember(d => d.LockoutEnd, o => o.Ignore())
            .ForMember(d => d.LockoutEnabled, o => o.Ignore())
            .ForMember(d => d.AccessFailedCount, o => o.Ignore())
            // Privilege flags - granted through roles, never through a user form.
            .ForMember(d => d.OrganizationAdmin, o => o.Ignore())
            .ForMember(d => d.SystemAdministrator, o => o.Ignore())
            .ForMember(d => d.ContractApproved, o => o.Ignore())
            .ForMember(d => d.MustChangePassword, o => o.Ignore())
            // Encrypted external credentials - managed by the Medula integration screen.
            .ForMember(d => d.MedulaUserName, o => o.Ignore())
            .ForMember(d => d.MedulaPassword, o => o.Ignore());
}
