namespace Hatiora.Pico8
{
    public class PackageInfoService : IPackageInfoService
    {
        public string PackageName => "com.hatiora.pico8";
        public string PackageVersion => "0.1.0";

        public string GetDisplayText()
        {
            return $"{PackageName} v{PackageVersion}";
        }
    }
}
