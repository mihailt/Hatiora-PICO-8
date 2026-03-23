namespace Hatiora.Pico8
{
    public interface IPackageInfoService
    {
        string PackageName { get; }
        string PackageVersion { get; }
        string GetDisplayText();
    }
}
