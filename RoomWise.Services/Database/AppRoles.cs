namespace RoomWise.Model;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Guest = "Guest";

    public static readonly string[] All = new[] { Administrator, Guest };
}