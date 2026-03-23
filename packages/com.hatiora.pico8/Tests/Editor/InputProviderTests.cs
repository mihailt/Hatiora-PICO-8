using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class InputProviderTests
    {
        [Test]
        public void Constructor_CreatesProvider()
        {
            var devices = new MockDeviceProvider();
            var provider = new InputProvider(devices,
                (0, kb => kb.leftArrowKey.isPressed, gp => gp.dpad.left.isPressed)
            );
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Constructor_DefaultDeviceProvider_Works()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                // Exercise the 1-arg constructor that uses UnityDeviceProvider.Instance
                var provider = new InputProvider(
                    (0, kb => kb.leftArrowKey.isPressed, gp => gp.dpad.left.isPressed)
                );
                Assert.IsNotNull(provider);
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void Poll_KeyboardInput_SetsButton()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var kb = InputSystem.AddDevice<Keyboard>();
                var devices = new MockDeviceProvider { Keyboard = kb };
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                fixture.Press(kb.leftArrowKey);
                var input = new Pico8Input();
                provider.Poll(input);

                Assert.IsTrue(input.Btn(0, 0));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void Poll_KeyboardNotPressed_ButtonFalse()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var kb = InputSystem.AddDevice<Keyboard>();
                var devices = new MockDeviceProvider { Keyboard = kb };
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                var input = new Pico8Input();
                provider.Poll(input);

                Assert.IsFalse(input.Btn(0, 0));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void Poll_GamepadInput_SetsButtonForPlayer1()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var gp = InputSystem.AddDevice<Gamepad>();
                var devices = new MockDeviceProvider();
                devices.GamepadList.Add(gp);
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                fixture.Press(gp.dpad.left);
                var input = new Pico8Input();
                provider.Poll(input);

                Assert.IsTrue(input.Btn(0, 1));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void OnDeviceChange_Disconnect_NullsSlot()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var gp = InputSystem.AddDevice<Gamepad>();
                var devices = new MockDeviceProvider();
                devices.GamepadList.Add(gp);
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                // Disconnect
                devices.FireDeviceChanged(gp, InputDeviceChange.Disconnected);

                var input = new Pico8Input();
                provider.Poll(input);
                Assert.IsFalse(input.Btn(0, 1));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void OnDeviceChange_Reconnect_FillsNullSlot()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var gp = InputSystem.AddDevice<Gamepad>();
                var devices = new MockDeviceProvider();
                devices.GamepadList.Add(gp);
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                // Disconnect then reconnect with new pad
                devices.FireDeviceChanged(gp, InputDeviceChange.Disconnected);
                var gp2 = InputSystem.AddDevice<Gamepad>();
                devices.FireDeviceChanged(gp2, InputDeviceChange.Added);

                fixture.Press(gp2.dpad.left);
                var input = new Pico8Input();
                provider.Poll(input);

                Assert.IsTrue(input.Btn(0, 1));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void OnDeviceChange_AddExtra_AppendsToList()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var gp = InputSystem.AddDevice<Gamepad>();
                var devices = new MockDeviceProvider();
                devices.GamepadList.Add(gp);
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                var gp2 = InputSystem.AddDevice<Gamepad>();
                devices.FireDeviceChanged(gp2, InputDeviceChange.Added);

                fixture.Press(gp2.dpad.left);
                var input = new Pico8Input();
                provider.Poll(input);

                Assert.IsTrue(input.Btn(0, 2));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void SetLogger_LogsOnDisconnect()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var gp = InputSystem.AddDevice<Gamepad>();
                var devices = new MockDeviceProvider();
                devices.GamepadList.Add(gp);
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                var logger = new TestLogger();
                provider.SetLogger(logger);
                devices.FireDeviceChanged(gp, InputDeviceChange.Disconnected);

                Assert.IsTrue(logger.HasLogged);
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void ToString_IncludesKeyboardAndGamepads()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var gp = InputSystem.AddDevice<Gamepad>();
                var devices = new MockDeviceProvider();
                devices.GamepadList.Add(gp);
                
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                var str = provider.ToString();
                Assert.IsTrue(str.Contains("Keyboard"));
                Assert.IsTrue(str.Contains(gp.displayName));
                Assert.IsTrue(str.Contains("InputProvider"));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void OnDeviceChange_NonGamepad_Ignored()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var mouse = InputSystem.AddDevice<Mouse>();
                var devices = new MockDeviceProvider();
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                // Fire a non-gamepad/non-keyboard device change — should not crash
                Assert.DoesNotThrow(() =>
                    devices.FireDeviceChanged(mouse, InputDeviceChange.Added));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void Poll_NoKeyboard_SkipsPlayer0()
        {
            var devices = new MockDeviceProvider { Keyboard = null };
            var provider = new InputProvider(devices,
                (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
            );

            var input = new Pico8Input();
            Assert.DoesNotThrow(() => provider.Poll(input));
            Assert.IsFalse(input.Btn(0, 0));
        }

        [Test]
        public void OnDeviceChange_KeyboardAdded_SetsKeyboard()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var devices = new MockDeviceProvider { Keyboard = null };
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                // Initially no keyboard
                var input = new Pico8Input();
                provider.Poll(input);
                Assert.IsFalse(input.Btn(0, 0));

                // Add keyboard via device change
                var kb = InputSystem.AddDevice<Keyboard>();
                devices.FireDeviceChanged(kb, InputDeviceChange.Added);

                fixture.Press(kb.leftArrowKey);
                input = new Pico8Input();
                provider.Poll(input);
                Assert.IsTrue(input.Btn(0, 0));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void OnDeviceChange_KeyboardDisconnected_NullsKeyboard()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var kb = InputSystem.AddDevice<Keyboard>();
                var devices = new MockDeviceProvider { Keyboard = kb };
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                // Disconnect keyboard
                devices.FireDeviceChanged(kb, InputDeviceChange.Disconnected);

                var input = new Pico8Input();
                provider.Poll(input);
                Assert.IsFalse(input.Btn(0, 0));
            }
            finally { fixture.TearDown(); }
        }

        [Test]
        public void SetLogger_LogsOnConnect()
        {
            var fixture = new InputTestFixture();
            fixture.Setup();
            try
            {
                var devices = new MockDeviceProvider();
                var provider = new InputProvider(devices,
                    (0, k => k.leftArrowKey.isPressed, g => g.dpad.left.isPressed)
                );

                var logger = new TestLogger();
                provider.SetLogger(logger);

                var gp = InputSystem.AddDevice<Gamepad>();
                devices.FireDeviceChanged(gp, InputDeviceChange.Added);

                Assert.IsTrue(logger.HasLogged);
            }
            finally { fixture.TearDown(); }
        }

        // ─── Mocks ───

        private class MockDeviceProvider : IDeviceProvider
        {
            public Keyboard Keyboard;
            public readonly List<Gamepad> GamepadList = new();

            public Keyboard CurrentKeyboard => Keyboard;
            public IReadOnlyList<Gamepad> Gamepads => GamepadList;

            private event Action<InputDevice, InputDeviceChange> _deviceChanged;

            public event Action<InputDevice, InputDeviceChange> DeviceChanged
            {
                add => _deviceChanged += value;
                remove => _deviceChanged -= value;
            }

            public void FireDeviceChanged(InputDevice device, InputDeviceChange change)
                => _deviceChanged?.Invoke(device, change);
        }

        private class TestLogger : ILogger
        {
            public bool HasLogged;
            public void Log(string message) => HasLogged = true;
        }
    }
}
