namespace Hatiora.Pico8.Coop
{
    public class PackageInfoService : Hatiora.Pico8.IPackageInfoService
    {
        public string PackageName => "com.hatiora.pico8.coop";
        public string PackageVersion => "0.1.0";

        public string GetDisplayText()
        {
            return $"{PackageName} v{PackageVersion}";
        }
    }
}
