using System.Collections.Generic;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Open-ended input state. Players and buttons grow on demand — no fixed caps.
    /// BTNP auto-repeat delays are configurable at runtime.
    /// </summary>
    public sealed class Pico8Input : IInput
    {
        private readonly Dictionary<(int player, int button), ButtonState> _buttons = new();
        private float _mouseX, _mouseY;
        private readonly Dictionary<(int player, int stick), (float x, float y)> _axes = new();

        public float MouseX => _mouseX;
        public float MouseY => _mouseY;
        public void SetMouse(float x, float y) { _mouseX = x; _mouseY = y; }

        public void SetAxis(int player, int stick, float x, float y) => _axes[(player, stick)] = (x, y);
        public float GetAxisX(int player, int stick) => _axes.TryGetValue((player, stick), out var v) ? v.x : 0f;
        public float GetAxisY(int player, int stick) => _axes.TryGetValue((player, stick), out var v) ? v.y : 0f;

        /// <summary>Seconds before first auto-repeat fires. Default matches PICO-8 (15 frames at 30fps = 0.5s).</summary>
        public float BtnpInitialDelay { get; set; } = 15f / 30f;

        /// <summary>Seconds between subsequent auto-repeats. Default matches PICO-8 (4 frames at 30fps ≈ 0.133s).</summary>
        public float BtnpRepeatDelay { get; set; } = 4f / 30f;

        public void SetButton(int button, int player, bool pressed)
        {
            var key = (player, button);
            if (!_buttons.TryGetValue(key, out var state))
            {
                state = new ButtonState();
                _buttons[key] = state;
            }
            state.Current = pressed;
        }

        public bool Btn(int button, int player = 0)
        {
            if (player == -1)
            {
                foreach (var kvp in _buttons)
                {
                    if (kvp.Key.button == button && Btn(button, kvp.Key.player))
                        return true;
                }
                return false;
            }

            var key = (player, button);
            return _buttons.TryGetValue(key, out var state) && state.Current;
        }

        public bool Btnp(int button, int player = 0)
        {
            if (player == -1)
            {
                foreach (var kvp in _buttons)
                {
                    if (kvp.Key.button == button && Btnp(button, kvp.Key.player))
                        return true;
                }
                return false;
            }

            var key = (player, button);
            if (!_buttons.TryGetValue(key, out var state)) return false;
            if (!state.Current) return false;
            if (!state.Previous) return true; // just pressed this frame

            // Auto-repeat logic (time-based)
            float held = state.HoldTime;
            if (held >= BtnpInitialDelay && state.PrevHoldTime < BtnpInitialDelay) return true;
            if (held > BtnpInitialDelay)
            {
                float sinceInitial = held - BtnpInitialDelay;
                float prevSinceInitial = state.PrevHoldTime - BtnpInitialDelay;
                if (prevSinceInitial < 0) prevSinceInitial = 0;
                // Fire if we crossed a repeat boundary
                int curBucket = (int)(sinceInitial / BtnpRepeatDelay);
                int prevBucket = (int)(prevSinceInitial / BtnpRepeatDelay);
                if (curBucket > prevBucket) return true;
            }
            return false;
        }

        /// <summary>
        /// Called once per frame after Update/Draw. Copies current → previous
        /// and accumulates hold time.
        /// </summary>
        public void Snapshot(float dt = 1f / 30f)
        {
            foreach (var kvp in _buttons)
            {
                var state = kvp.Value;
                state.PrevHoldTime = state.HoldTime;
                if (state.Current)
                    state.HoldTime += dt;
                else
                    state.HoldTime = 0;
                state.Previous = state.Current;
            }
        }

        /// <summary>Clears all button state.</summary>
        public void Clear()
        {
            _buttons.Clear();
        }

        private class ButtonState
        {
            public bool Current;
            public bool Previous;
            public float HoldTime;
            public float PrevHoldTime;
        }
    }
}
