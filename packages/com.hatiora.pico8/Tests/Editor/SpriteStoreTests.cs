using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class SpriteStoreTests
    {
        private EngineSpec _spec;
        private SpriteStore _store;

        [SetUp]
        public void SetUp()
        {
            _spec = EngineSpec.Pico8;
            _store = new SpriteStore(_spec);
        }

        [Test]
        public void BankCount_InitiallyZero()
        {
            Assert.AreEqual(0, _store.BankCount);
        }

        [Test]
        public void LoadBank_GrowsBankList()
        {
            var data = new byte[128 * 128];
            _store.LoadBank(0, data, 128, 128);
            Assert.AreEqual(1, _store.BankCount);
        }

        [Test]
        public void LoadBank_NonContiguous_FillsGaps()
        {
            var data = new byte[128 * 128];
            _store.LoadBank(3, data, 128, 128);
            Assert.AreEqual(4, _store.BankCount); // 0, 1, 2 auto-created
        }

        [Test]
        public void GetPixel_ReturnsLoadedData()
        {
            var data = new byte[128 * 128];
            data[0] = 5;
            data[1] = 10;
            _store.LoadBank(0, data, 128, 128);

            Assert.AreEqual(5, _store.GetPixel(0, 0, 0));
            Assert.AreEqual(10, _store.GetPixel(0, 1, 0));
        }

        [Test]
        public void GetPixel_OutOfBounds_ReturnsZero()
        {
            Assert.AreEqual(0, _store.GetPixel(0, 0, 0)); // no banks
            var data = new byte[128 * 128];
            _store.LoadBank(0, data, 128, 128);
            Assert.AreEqual(0, _store.GetPixel(0, -1, 0));
            Assert.AreEqual(0, _store.GetPixel(0, 128, 0));
        }

        [Test]
        public void SetPixel_ModifiesBank()
        {
            var data = new byte[128 * 128];
            _store.LoadBank(0, data, 128, 128);
            _store.SetPixel(0, 5, 5, 12);
            Assert.AreEqual(12, _store.GetPixel(0, 5, 5));
        }

        [Test]
        public void MultiBanks_Independent()
        {
            var data1 = new byte[64 * 64];
            data1[0] = 1;
            var data2 = new byte[64 * 64];
            data2[0] = 2;

            _store.LoadBank(0, data1, 64, 64);
            _store.LoadBank(1, data2, 64, 64);

            Assert.AreEqual(1, _store.GetPixel(0, 0, 0));
            Assert.AreEqual(2, _store.GetPixel(1, 0, 0));
        }

        [Test]
        public void GetBankWidth_ReturnsCorrectValue()
        {
            _store.LoadBank(0, new byte[256 * 128], 256, 128);
            Assert.AreEqual(256, _store.GetBankWidth(0));
        }

        [Test]
        public void GetBankHeight_ReturnsCorrectValue()
        {
            _store.LoadBank(0, new byte[128 * 256], 128, 256);
            Assert.AreEqual(256, _store.GetBankHeight(0));
        }

        [Test]
        public void GetBankWidth_InvalidBank_ReturnsZero()
        {
            Assert.AreEqual(0, _store.GetBankWidth(5));
        }

        [Test]
        public void GetBankHeight_InvalidBank_ReturnsZero()
        {
            Assert.AreEqual(0, _store.GetBankHeight(5));
        }

        [Test]
        public void SetPixel_InvalidBank_NoOp()
        {
            Assert.DoesNotThrow(() => _store.SetPixel(5, 0, 0, 7));
        }

        [Test]
        public void SetPixel_OutOfBounds_NoOp()
        {
            _store.LoadBank(0, new byte[128 * 128], 128, 128);
            Assert.DoesNotThrow(() => _store.SetPixel(0, -1, 0, 7));
            Assert.DoesNotThrow(() => _store.SetPixel(0, 128, 0, 7));
        }

        [Test]
        public void GetPixel_NegativeY_ReturnsZero()
        {
            _store.LoadBank(0, new byte[128 * 128], 128, 128);
            Assert.AreEqual(0, _store.GetPixel(0, 0, -1));
        }

        [Test]
        public void GetRow_ValidBank_ReturnsRowData()
        {
            var data = new byte[4 * 4];
            data[4] = 7; // row 1, col 0
            data[5] = 8; // row 1, col 1
            _store.LoadBank(0, data, 4, 4);

            var row = _store.GetRow(0, 1);
            Assert.AreEqual(4, row.Length);
            Assert.AreEqual(7, row[0]);
            Assert.AreEqual(8, row[1]);
        }

        [Test]
        public void GetRow_InvalidBank_ReturnsEmpty()
        {
            var row = _store.GetRow(5, 0);
            Assert.AreEqual(0, row.Length);
        }

        [Test]
        public void GetRow_NegativeBank_ReturnsEmpty()
        {
            var row = _store.GetRow(-1, 0);
            Assert.AreEqual(0, row.Length);
        }

        [Test]
        public void GetRow_OutOfBoundsY_ReturnsEmpty()
        {
            _store.LoadBank(0, new byte[4 * 4], 4, 4);
            var row = _store.GetRow(0, -1);
            Assert.AreEqual(0, row.Length);

            var row2 = _store.GetRow(0, 4);
            Assert.AreEqual(0, row2.Length);
        }

        [Test]
        public void GetBankWidth_NegativeBank_ReturnsZero()
        {
            Assert.AreEqual(0, _store.GetBankWidth(-1));
        }

        [Test]
        public void GetBankHeight_NegativeBank_ReturnsZero()
        {
            Assert.AreEqual(0, _store.GetBankHeight(-1));
        }

        [Test]
        public void GetPixel_NegativeBank_ReturnsZero()
        {
            Assert.AreEqual(0, _store.GetPixel(-1, 0, 0));
        }

        [Test]
        public void SetPixel_NegativeBank_NoOp()
        {
            Assert.DoesNotThrow(() => _store.SetPixel(-1, 0, 0, 7));
        }

        [Test]
        public void SetPixel_OutOfBoundsY_NoOp()
        {
            _store.LoadBank(0, new byte[128 * 128], 128, 128);
            Assert.DoesNotThrow(() => _store.SetPixel(0, 0, -1, 7));
            Assert.DoesNotThrow(() => _store.SetPixel(0, 0, 128, 7));
        }

        [Test]
        public void GetPixel_OutOfBoundsY_ReturnsZero()
        {
            _store.LoadBank(0, new byte[128 * 128], 128, 128);
            Assert.AreEqual(0, _store.GetPixel(0, 0, 128));
        }

        [Test]
        public void LoadBank_WithGap_GapBanksReturnZeros()
        {
            // Load bank 2, creating gap banks 0 and 1
            _store.LoadBank(2, new byte[8 * 8], 8, 8);
            Assert.AreEqual(3, _store.BankCount);
            // Gap banks should return 0 for all pixels
            Assert.AreEqual(0, _store.GetPixel(0, 0, 0));
            Assert.AreEqual(0, _store.GetPixel(1, 0, 0));
            // Gap banks should have default spec dimensions
            Assert.AreEqual(128, _store.GetBankWidth(0));
            Assert.AreEqual(128, _store.GetBankHeight(0));
        }

        [Test]
        public void GetRow_GapBank_ReturnsSpecWidthRow()
        {
            _store.LoadBank(2, new byte[8 * 8], 8, 8);
            var row = _store.GetRow(0, 0);
            Assert.AreEqual(128, row.Length); // default spec width for gap bank
            Assert.AreEqual(0, row[0]);
        }
    }
}
