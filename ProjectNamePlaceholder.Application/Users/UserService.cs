
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Roles.Dtos;
using ProjectNamePlaceholder.Application.Users.Dtos;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        return users
            .Select(user => new UserDto(
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()))
            .ToList();
    }

    public async Task<UserDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(id, cancellationToken);

        return user is null
            ? null
            : new UserDto(
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList());
    }


    public async Task<ProfileDto?> GetProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
            return null;
        return new ProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            user.ProfileImageUrl,
            user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
        );
    }



    public async Task<ProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        user.UpdateProfile(request.FullName, request.Email, request.ProfileImageUrl);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            user.ProfileImageUrl,
            user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
        );
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        return user.UserRoles
            .Select(userRole => new RoleDto(
                userRole.Role.Id,
                userRole.Role.Name,
                userRole.Role.Description,
                userRole.Role.RolePermissions.Select(rolePermission => rolePermission.Permission.Name).Distinct().ToList()))
            .ToList();
    }

    public async Task<UserDto> ReplaceRolesAsync(long userId, UpdateUserRolesRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var roles = await GetRolesByIdsAsync(request.RoleIds, cancellationToken);
        user.SetRoles(roles.Select(role => new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        }));

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapUserAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> AddRoleAsync(long userId, long roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var role = await GetRoleByIdAsync(roleId, cancellationToken);
        if (user.UserRoles.Any(userRole => userRole.RoleId == role.Id))
        {
            return await MapUserAsync(user.Id, cancellationToken);
        }

        var updatedRoles = user.UserRoles.ToList();
        updatedRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        });

        user.SetRoles(updatedRoles);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapUserAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> RemoveRoleAsync(long userId, long roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var userRole = user.UserRoles.FirstOrDefault(userRole => userRole.RoleId == roleId)
            ?? throw new InvalidOperationException("User role not found.");

        var updatedRoles = user.UserRoles.Where(userRole => userRole.RoleId != roleId).ToList();
        user.SetRoles(updatedRoles);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapUserAsync(user.Id, cancellationToken);
    }

    private async Task<Role> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken)
    {
        return await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");
    }

    private async Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var distinctIds = roleIds.Distinct().ToArray();
        var roles = new List<Role>(distinctIds.Length);

        foreach (var roleId in distinctIds)
        {
            roles.Add(await GetRoleByIdAsync(roleId, cancellationToken));
        }

        return roles;
    }

    private async Task<UserDto> MapUserAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        return new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            user.UserRoles.Select(userRole => userRole.Role.Name).Distinct().ToList());
    }
}
