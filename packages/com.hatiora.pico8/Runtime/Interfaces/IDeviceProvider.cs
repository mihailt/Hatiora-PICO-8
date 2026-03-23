using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Abstracts access to input devices, allowing tests to provide mock devices
    /// without depending on the Unity InputSystem runtime singletons.
    /// </summary>
    public interface IDeviceProvider
    {
        Keyboard CurrentKeyboard { get; }
        IReadOnlyList<Gamepad> Gamepads { get; }
        event Action<InputDevice, InputDeviceChange> DeviceChanged;
    }
}
