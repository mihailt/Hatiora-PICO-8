using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Hatiora.Pico8
{
    public class InputProvider : IInputProvider
    {
        private readonly ButtonMapping[] _mappings;
        private readonly List<Gamepad> _pads = new();
        private Keyboard _keyboard;
        private ILogger _logger;
        private readonly Func<(float x, float y)?> _mouseProvider;

        public void SetLogger(ILogger logger) => _logger = logger;

        /// <summary>Default constructor using Unity InputSystem singletons.</summary>
        public InputProvider(params (int button, Func<Keyboard, bool> keyboard, Func<Gamepad, bool> gamepad)[] mappings)
            : this(UnityDeviceProvider.Instance, null, mappings) { }

        /// <summary>Constructor with mouse provider.</summary>
        public InputProvider(Func<(float x, float y)?> mouseProvider, params (int button, Func<Keyboard, bool> keyboard, Func<Gamepad, bool> gamepad)[] mappings)
            : this(UnityDeviceProvider.Instance, mouseProvider, mappings) { }

        /// <summary>Injectable constructor for testability (no mouse).</summary>
        public InputProvider(IDeviceProvider devices, params (int button, Func<Keyboard, bool> keyboard, Func<Gamepad, bool> gamepad)[] mappings)
            : this(devices, null, mappings) { }

        /// <summary>Full injectable constructor.</summary>
        public InputProvider(IDeviceProvider devices, Func<(float x, float y)?> mouseProvider, params (int button, Func<Keyboard, bool> keyboard, Func<Gamepad, bool> gamepad)[] mappings)
        {
            _mappings = new ButtonMapping[mappings.Length];
            for (int i = 0; i < mappings.Length; i++)
                _mappings[i] = new ButtonMapping(mappings[i].button, mappings[i].keyboard, mappings[i].gamepad);

            _mouseProvider = mouseProvider;

            _keyboard = devices.CurrentKeyboard;

            var gamepads = devices.Gamepads;
            for (int i = 0; i < gamepads.Count; i++)
                _pads.Add(gamepads[i]);

            devices.DeviceChanged += OnDeviceChange;
        }

        public void Poll(IInput input)
        {
            var kb = _keyboard;

            foreach (var m in _mappings)
            {
                // Player 0: keyboard
                if (kb != null)
                    input.SetButton(m.Button, 0, m.Keyboard(kb));

                // Players 1+: gamepads
                for (int p = 0; p < _pads.Count; p++)
                {
                    if (_pads[p] != null)
                        input.SetButton(m.Button, p + 1, m.Gamepad(_pads[p]));
                }
            }

            // Mouse position
            if (_mouseProvider != null)
            {
                var pos = _mouseProvider();
                if (pos.HasValue)
                    input.SetMouse(pos.Value.x, pos.Value.y);
            }

            // Analog sticks
            for (int p = 0; p < _pads.Count; p++)
            {
                if (_pads[p] != null)
                {
                    var ls = _pads[p].leftStick.ReadValue();
                    input.SetAxis(p + 1, 0, ls.x, ls.y);
                    var rs = _pads[p].rightStick.ReadValue();
                    input.SetAxis(p + 1, 1, rs.x, rs.y);
                }
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Keyboard kb)
            {
                if (change == InputDeviceChange.Added) _keyboard = kb;
                else if (change == InputDeviceChange.Disconnected) _keyboard = null;
                return;
            }

            if (device is not Gamepad gamepad) return;

            if (change == InputDeviceChange.Disconnected)
            {
                int idx = _pads.IndexOf(gamepad);
                if (idx >= 0)
                {
                    _pads[idx] = null;
                    _logger?.Log($"[Input] Player {idx + 1} disconnected: {gamepad.displayName}");
                }
            }
            else if (change == InputDeviceChange.Added)
            {
                int idx = _pads.IndexOf(null);
                if (idx >= 0) _pads[idx] = gamepad;
                else { _pads.Add(gamepad); idx = _pads.Count - 1; }
                _logger?.Log($"[Input] Player {idx + 1} connected: {gamepad.displayName}");
            }
        }

        public override string ToString()
        {
            var devices = "Keyboard";
            for (int i = 0; i < _pads.Count; i++)
                if (_pads[i] != null) devices += $", {_pads[i].displayName}";
            return $"InputProvider ({devices})";
        }

        private readonly struct ButtonMapping
        {
            public readonly int Button;
            public readonly Func<Keyboard, bool> Keyboard;
            public readonly Func<Gamepad, bool> Gamepad;

            public ButtonMapping(int button, Func<Keyboard, bool> keyboard, Func<Gamepad, bool> gamepad)
            {
                Button = button;
                Keyboard = keyboard;
                Gamepad = gamepad;
            }
        }
    }
}
