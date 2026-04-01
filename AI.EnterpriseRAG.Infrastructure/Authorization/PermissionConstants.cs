

namespace AI.EnterpriseRAG.Infrastructure.Authorization;

public static class PermissionConstants
{
    // 超级管理员角色编码
    public const string AdminRoleCode = "admin";

    // Redis 键（实时权限）
    public const string UserPermissionCacheKey = "user:perm:{0}";

    // Redis 键（Token 黑名单，踢人用）
    public const string TokenBlacklistKey = "token:blacklist";
}
