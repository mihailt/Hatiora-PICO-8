using System;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class UnityDeviceProviderTests
    {
        private InputTestFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new InputTestFixture();
            _fixture.Setup();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.TearDown();
        }

        [Test]
        public void Instance_IsNotNull()
        {
            Assert.IsNotNull(UnityDeviceProvider.Instance);
        }

        [Test]
        public void CurrentKeyboard_ReturnsKeyboard()
        {
            var kb = InputSystem.AddDevice<Keyboard>();
            var provider = UnityDeviceProvider.Instance;
            Assert.AreSame(kb, provider.CurrentKeyboard);
        }

        [Test]
        public void Gamepads_ReturnsList()
        {
            var gp = InputSystem.AddDevice<Gamepad>();
            var provider = UnityDeviceProvider.Instance;
            Assert.IsTrue(provider.Gamepads.Count > 0);
        }

        [Test]
        public void DeviceChanged_FiresOnAdd()
        {
            var provider = UnityDeviceProvider.Instance;
            bool fired = false;
            Action<InputDevice, InputDeviceChange> handler = (d, c) => fired = true;
            provider.DeviceChanged += handler;
            try
            {
                InputSystem.AddDevice<Gamepad>();
                Assert.IsTrue(fired);
            }
            finally
            {
                provider.DeviceChanged -= handler;
            }
        }
    }
}
