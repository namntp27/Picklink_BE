namespace Picklink.Domain.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Owner = "Owner";
    public const string Player = "Player";

    public static readonly string[] All = [Admin, Owner, Player];
}
