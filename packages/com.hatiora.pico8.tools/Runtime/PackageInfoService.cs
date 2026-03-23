namespace Hatiora.Pico8.Tools
{
    public class PackageInfoService : Hatiora.Pico8.IPackageInfoService
    {
        public string PackageName => "com.hatiora.pico8.tools";
        public string PackageVersion => "0.1.0";

        public string GetDisplayText()
        {
            return $"{PackageName} v{PackageVersion}";
        }
    }
}
