using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Maps Unity Input System actions to the headless <see cref="IInput"/> interface.
    /// Configured via a list of (player, button, InputAction) mappings.
    /// </summary>
    public sealed class UnityInputProvider : IInputProvider
    {
        private readonly List<Mapping> _mappings = new();
        private bool _subscribed;

        public UnityInputProvider(IEnumerable<(int player, int button, InputAction action)> bindings)
        {
            foreach (var (player, button, action) in bindings)
            {
                _mappings.Add(new Mapping(player, button, action));
            }
        }

        /// <summary>
        /// Enables all input actions and subscribes to events.
        /// Call once when the runtime starts.
        /// </summary>
        public void Enable()
        {
            if (_subscribed) return;
            foreach (var m in _mappings)
                m.Action.Enable();
            _subscribed = true;
        }

        /// <summary>
        /// Disables all input actions.
        /// </summary>
        public void Disable()
        {
            foreach (var m in _mappings)
                m.Action.Disable();
            _subscribed = false;
        }

        public void Poll(IInput input)
        {
            foreach (var m in _mappings)
            {
                bool pressed = m.Action.IsPressed();
                input.SetButton(m.Button, m.Player, pressed);
            }
        }

        private readonly struct Mapping
        {
            public readonly int Player;
            public readonly int Button;
            public readonly InputAction Action;

            public Mapping(int player, int button, InputAction action)
            {
                Player = player;
                Button = button;
                Action = action ?? throw new ArgumentNullException(nameof(action));
            }
        }
    }
}
