namespace Minimal
{
    public enum LocalOrShare
    {
        Local,
        Share
    }

    public enum UncOrRegular
    {
        Regular,
        UNC
    }

    public enum FileOrDirectory
    {
        File = 0,
        Directory = 1
    }

    public enum SuppressExceptions
    {
        None,
        SuppressAllExceptions
    }

    public enum AdminOrNormal
    {
        Admin = 2,
        Normal = 1
    }
}