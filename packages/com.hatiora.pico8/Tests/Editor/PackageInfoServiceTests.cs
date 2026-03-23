using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class PackageInfoServiceTests
    {
        private IPackageInfoService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new PackageInfoService();
        }

        [Test]
        public void PackageName_ReturnsCorrectName()
        {
            Assert.AreEqual("com.hatiora.pico8", _service.PackageName);
        }

        [Test]
        public void PackageVersion_ReturnsCorrectVersion()
        {
            Assert.AreEqual("0.1.0", _service.PackageVersion);
        }

        [Test]
        public void GetDisplayText_ReturnsFormattedString()
        {
            Assert.AreEqual("com.hatiora.pico8 v0.1.0", _service.GetDisplayText());
        }
    }
}
