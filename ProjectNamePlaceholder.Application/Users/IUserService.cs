using ProjectNamePlaceholder.Application.Roles.Dtos;
using ProjectNamePlaceholder.Application.Users.Dtos;

namespace ProjectNamePlaceholder.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ProfileDto?> GetProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<ProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserDto> ReplaceRolesAsync(long userId, UpdateUserRolesRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> AddRoleAsync(long userId, long roleId, CancellationToken cancellationToken = default);
    Task<UserDto> RemoveRoleAsync(long userId, long roleId, CancellationToken cancellationToken = default);
}
