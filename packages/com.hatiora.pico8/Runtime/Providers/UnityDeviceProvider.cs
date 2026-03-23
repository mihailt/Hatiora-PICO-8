using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Default device provider using Unity InputSystem singletons.
    /// </summary>
    public sealed class UnityDeviceProvider : IDeviceProvider
    {
        public static readonly UnityDeviceProvider Instance = new();

        public Keyboard CurrentKeyboard => Keyboard.current;
        public IReadOnlyList<Gamepad> Gamepads => Gamepad.all;

        public event Action<InputDevice, InputDeviceChange> DeviceChanged
        {
            add => InputSystem.onDeviceChange += value;
            remove => InputSystem.onDeviceChange -= value;
        }
    }
}
