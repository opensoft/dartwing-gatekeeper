namespace DartWing.ErpNext.Dto;

public sealed class UserCreateRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int SendWelcomeEmail { get; set; } = 1;
    public List<UserRoleDto> Roles { get; set; } = [];
}

public sealed class UserResponseDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<UserRoleDto> Roles { get; set; } = [];
}

public sealed class UserRoleDto
{
    public string Role { get; set; } = string.Empty;
}

public sealed class RoleDto
{
    public string Name { get; set; } = string.Empty;
}

public sealed class RolesResponseDto
{
    public List<RoleDto> Data { get; set; } = [];
}
