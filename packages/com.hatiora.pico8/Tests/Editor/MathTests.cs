using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class MathTests
    {
        private Pico8Math _math;

        [SetUp]
        public void SetUp()
        {
            _math = new Pico8Math();
        }

        [Test]
        public void Flr_PositiveValue()
        {
            Assert.AreEqual(3, _math.Flr(3.7f));
        }

        [Test]
        public void Flr_NegativeValue_RoundsDown()
        {
            Assert.AreEqual(-3, _math.Flr(-2.3f));
        }

        [Test]
        public void Flr_ExactInteger()
        {
            Assert.AreEqual(5, _math.Flr(5.0f));
        }

        [Test]
        public void Ceil_PositiveValue()
        {
            Assert.AreEqual(4, _math.Ceil(3.1f));
        }

        [Test]
        public void Ceil_NegativeValue()
        {
            Assert.AreEqual(-2, _math.Ceil(-2.3f));
        }

        [Test]
        public void Sin_Quarter_ReturnsNegativeOne()
        {
            // PICO-8: sin(0.25) = -1 (inverted from standard math)
            Assert.AreEqual(-1f, _math.Sin(0.25f), 0.0001f);
        }

        [Test]
        public void Sin_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, _math.Sin(0f), 0.0001f);
        }

        [Test]
        public void Cos_Zero_ReturnsOne()
        {
            Assert.AreEqual(1f, _math.Cos(0f), 0.0001f);
        }

        [Test]
        public void Cos_Quarter_ReturnsZero()
        {
            Assert.AreEqual(0f, _math.Cos(0.25f), 0.0001f);
        }

        [Test]
        public void Sgn_Positive_ReturnsOne()
        {
            Assert.AreEqual(1f, _math.Sgn(42f));
        }

        [Test]
        public void Sgn_Negative_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1f, _math.Sgn(-3f));
        }

        [Test]
        public void Sgn_Zero_ReturnsOne()
        {
            // PICO-8 specific: sgn(0) = 1, not 0
            Assert.AreEqual(1f, _math.Sgn(0f));
        }

        [Test]
        public void Mid_ReturnsMiddleValue()
        {
            Assert.AreEqual(7f, _math.Mid(7f, 5f, 10f));
            Assert.AreEqual(5f, _math.Mid(1f, 5f, 10f));
            Assert.AreEqual(10f, _math.Mid(1f, 100f, 10f));
        }

        [Test]
        public void Rnd_InRange()
        {
            for (int i = 0; i < 100; i++)
            {
                float v = _math.Rnd(10f);
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 10f);
            }
        }

        [Test]
        public void Srand_ProducesDeterministicSequence()
        {
            _math.Srand(42);
            float a = _math.Rnd(100f);
            float b = _math.Rnd(100f);

            _math.Srand(42);
            Assert.AreEqual(a, _math.Rnd(100f));
            Assert.AreEqual(b, _math.Rnd(100f));
        }

        [Test]
        public void Abs_Positive() => Assert.AreEqual(5f, _math.Abs(5f));

        [Test]
        public void Abs_Negative() => Assert.AreEqual(3f, _math.Abs(-3f));

        [Test]
        public void Sqrt_PerfectSquare() => Assert.AreEqual(4f, _math.Sqrt(16f), 0.0001f);

        [Test]
        public void Min_ReturnsSmaller() => Assert.AreEqual(3f, _math.Min(3f, 7f));

        [Test]
        public void Max_ReturnsLarger() => Assert.AreEqual(7f, _math.Max(3f, 7f));

        [Test]
        public void Pow_Basic() => Assert.AreEqual(8f, _math.Pow(2f, 3f), 0.0001f);

        [Test]
        public void Atan2_NegativeAngle()
        {
            // atan2 with arguments that produce a negative raw angle, which wraps to positive
            float a = _math.Atan2(-1, 0);
            Assert.GreaterOrEqual(a, 0f);
            Assert.LessOrEqual(a, 1f);
        }
    }
}
