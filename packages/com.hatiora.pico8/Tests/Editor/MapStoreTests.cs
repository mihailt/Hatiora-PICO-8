using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class MapStoreTests
    {
        private EngineSpec _spec;
        private Pico8Memory _mem;
        private MapStore _map;

        [SetUp]
        public void SetUp()
        {
            _spec = EngineSpec.Pico8;
            _mem = new Pico8Memory(_spec);
            _map = new MapStore(_spec, _mem);
        }

        [Test]
        public void GetSet_RoundTrip()
        {
            _map.Set(10, 5, 42);
            Assert.AreEqual(42, _map.Get(10, 5));
        }

        [Test]
        public void Get_OutOfBounds_ReturnsZero()
        {
            Assert.AreEqual(0, _map.Get(-1, 0));
            Assert.AreEqual(0, _map.Get(0, -1));
            Assert.AreEqual(0, _map.Get(128, 0));
            Assert.AreEqual(0, _map.Get(0, 32));
        }

        [Test]
        public void Set_OutOfBounds_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _map.Set(-1, 0, 42));
            Assert.DoesNotThrow(() => _map.Set(0, -1, 42));
            Assert.DoesNotThrow(() => _map.Set(128, 0, 42));
        }

        [Test]
        public void GetFlag_Default_ReturnsZero()
        {
            Assert.AreEqual(0, _map.GetFlag(0));
        }

        [Test]
        public void SetGetFlag_BitOperations()
        {
            _map.SetFlag(10, 0, true);  // set bit 0
            _map.SetFlag(10, 3, true);  // set bit 3

            Assert.IsTrue(_map.GetFlag(10, 0));
            Assert.IsFalse(_map.GetFlag(10, 1));
            Assert.IsTrue(_map.GetFlag(10, 3));
            Assert.AreEqual(0x09, _map.GetFlag(10)); // 0b00001001
        }

        [Test]
        public void SetFlag_ClearBit()
        {
            _map.SetFlag(5, 2, true);
            Assert.IsTrue(_map.GetFlag(5, 2));
            _map.SetFlag(5, 2, false);
            Assert.IsFalse(_map.GetFlag(5, 2));
        }

        [Test]
        public void GetFlag_OutOfBounds_ReturnsZero()
        {
            Assert.AreEqual(0, _map.GetFlag(-1));
            Assert.AreEqual(0, _map.GetFlag(256)); // sprite count for PICO-8
        }

        [Test]
        public void LoadMapData_WritesToMemory()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            _map.LoadMapData(data);

            Assert.AreEqual(1, _map.Get(0, 0));
            Assert.AreEqual(2, _map.Get(1, 0));
            Assert.AreEqual(5, _map.Get(4, 0));
        }

        [Test]
        public void LoadFlagData_WritesToMemory()
        {
            var data = new byte[] { 0x01, 0x02, 0x04, 0x08 };
            _map.LoadFlagData(data);

            Assert.AreEqual(0x01, _map.GetFlag(0));
            Assert.AreEqual(0x02, _map.GetFlag(1));
            Assert.AreEqual(0x04, _map.GetFlag(2));
            Assert.AreEqual(0x08, _map.GetFlag(3));
        }

        [Test]
        public void Map_WritesToCorrectMemoryRegion()
        {
            _map.Set(3, 7, 99);
            int addr = _mem.Layout.MapStart + 7 * 128 + 3;
            Assert.AreEqual(99, _mem.Ram[addr]);
        }
    }
}
