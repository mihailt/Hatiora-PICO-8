using NUnit.Framework;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class MapDataLoaderTests
    {
        [Test]
        public void Parse_Empty_ReturnsEmptyArray()
        {
            var result = MapDataLoader.Parse("");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void Parse_Null_ReturnsEmptyArray()
        {
            var result = MapDataLoader.Parse(null);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void Parse_SingleRow_Returns128Bytes()
        {
            // 256 hex chars = 128 tile bytes
            var hex = new string('0', 256);
            var result = MapDataLoader.Parse(hex);
            Assert.AreEqual(128, result.Length);
        }

        [Test]
        public void Parse_HexValues_Correct()
        {
            // First pair "2a" = 0x2A = 42, rest zeros
            var hex = "2a" + new string('0', 254);
            var result = MapDataLoader.Parse(hex);
            Assert.AreEqual(42, result[0]);
            Assert.AreEqual(0, result[1]);
        }

        [Test]
        public void Parse_MultiRow_CorrectDimensions()
        {
            var row = new string('0', 256);
            var hex = row + "\n" + row + "\n" + row;
            var result = MapDataLoader.Parse(hex);
            Assert.AreEqual(128 * 3, result.Length);
        }

        [Test]
        public void Parse_UppercaseHex_Works()
        {
            var hex = "FF" + new string('0', 254);
            var result = MapDataLoader.Parse(hex);
            Assert.AreEqual(255, result[0]);
        }

        [Test]
        public void Parse_MixedCase_Works()
        {
            var hex = "aB" + new string('0', 254);
            var result = MapDataLoader.Parse(hex);
            Assert.AreEqual(0xAB, result[0]);
        }

        [Test]
        public void Parse_WindowsLineEndings_Work()
        {
            var row = new string('0', 256);
            var hex = row + "\r\n" + row;
            var result = MapDataLoader.Parse(hex);
            Assert.AreEqual(128 * 2, result.Length);
        }

        [Test]
        public void Parse_RealMapRow_CorrectTileValues()
        {
            // Simulate a mix of tile references: 00 26 27 06 07 16
            var hex = "0026270607160000" + new string('0', 240);
            var result = MapDataLoader.Parse(hex);
            Assert.AreEqual(0x00, result[0]);
            Assert.AreEqual(0x26, result[1]); // star tile
            Assert.AreEqual(0x27, result[2]); // star tile
            Assert.AreEqual(0x06, result[3]); // cloud tile
            Assert.AreEqual(0x07, result[4]); // cloud tile
            Assert.AreEqual(0x16, result[5]); // cloud tile
        }
    }
}
