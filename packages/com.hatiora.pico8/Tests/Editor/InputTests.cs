using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class InputTests
    {
        private Pico8Input _input;

        [SetUp]
        public void SetUp()
        {
            _input = new Pico8Input();
        }

        [Test]
        public void Btn_Unpressed_ReturnsFalse()
        {
            Assert.IsFalse(_input.Btn(0, 0));
        }

        [Test]
        public void Btn_Pressed_ReturnsTrue()
        {
            _input.SetButton(0, 0, true);
            Assert.IsTrue(_input.Btn(0, 0));
        }

        [Test]
        public void Btn_Released_ReturnsFalse()
        {
            _input.SetButton(0, 0, true);
            _input.SetButton(0, 0, false);
            Assert.IsFalse(_input.Btn(0, 0));
        }

        [Test]
        public void Btnp_FirstPress_ReturnsTrue()
        {
            _input.SetButton(0, 0, true);
            Assert.IsTrue(_input.Btnp(0, 0));
        }

        [Test]
        public void Btnp_HeldSecondFrame_ReturnsFalse()
        {
            _input.SetButton(0, 0, true);
            _input.Snapshot(); // frame 1 complete
            Assert.IsFalse(_input.Btnp(0, 0));
        }

        [Test]
        public void Btnp_AutoRepeat_FiresAtInitialDelay()
        {
            _input.BtnpInitialDelay = 5;
            _input.SetButton(0, 0, true);

            // Advance through frames
            for (int i = 0; i < 5; i++)
                _input.Snapshot();

            // Frame 5 should trigger auto-repeat
            Assert.IsTrue(_input.Btnp(0, 0));
        }

        [Test]
        public void Btnp_AutoRepeat_DoesNotFireBetweenRepeats()
        {
            _input.BtnpInitialDelay = 5;
            _input.BtnpRepeatDelay = 3;
            _input.SetButton(0, 0, true);

            // Advance past initial delay
            for (int i = 0; i < 6; i++)
                _input.Snapshot();

            // Frame 6 should NOT trigger (6-5=1, 1%3 != 0)
            Assert.IsFalse(_input.Btnp(0, 0));
        }

        [Test]
        public void Btnp_AutoRepeat_FiresAtRepeatInterval()
        {
            _input.BtnpInitialDelay = 5;
            _input.BtnpRepeatDelay = 3;
            _input.SetButton(0, 0, true);

            // Advance to frame 8 (5 + 3)
            for (int i = 0; i < 8; i++)
                _input.Snapshot();

            Assert.IsTrue(_input.Btnp(0, 0));
        }

        [Test]
        public void MultiPlayer_IndependentState()
        {
            _input.SetButton(0, 0, true);
            _input.SetButton(0, 1, false);
            Assert.IsTrue(_input.Btn(0, 0));
            Assert.IsFalse(_input.Btn(0, 1));
        }

        [Test]
        public void DynamicGrowth_HighPlayerNumber()
        {
            _input.SetButton(5, 99, true);
            Assert.IsTrue(_input.Btn(5, 99));
        }

        [Test]
        public void DynamicGrowth_HighButtonNumber()
        {
            _input.SetButton(42, 0, true);
            Assert.IsTrue(_input.Btn(42, 0));
        }

        [Test]
        public void Clear_ResetsAllState()
        {
            _input.SetButton(0, 0, true);
            _input.Clear();
            Assert.IsFalse(_input.Btn(0, 0));
        }

        [Test]
        public void Snapshot_Release_ResetsHoldFrames()
        {
            _input.SetButton(0, 0, true);
            _input.Snapshot(); // hold 1 frame
            _input.Snapshot(); // hold 2 frames

            // Release button
            _input.SetButton(0, 0, false);
            _input.Snapshot(); // should reset hold frames

            // Re-press → btnp should fire as fresh press
            _input.SetButton(0, 0, true);
            Assert.IsTrue(_input.Btnp(0, 0));
        }
    }
}
