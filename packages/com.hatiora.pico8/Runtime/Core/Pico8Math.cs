using System;

namespace Hatiora.Pico8
{
    /// <summary>
    /// PICO-8-compatible math functions using System.MathF.
    /// Key differences from standard math: sin/cos use PICO-8 turn-based angles (0..1),
    /// rnd is seedable and deterministic.
    /// </summary>
    public sealed class Pico8Math
    {
        private Random _rng = new Random();

        /// <summary>Returns a random float in [0, x).</summary>
        public float Rnd(float x = 1f)
        {
            return (float)(_rng.NextDouble() * x);
        }

        /// <summary>Seeds the random number generator for deterministic sequences.</summary>
        public void Srand(int seed)
        {
            _rng = new Random(seed);
        }

        /// <summary>Floor. PICO-8 semantics: flr(-2.3) → -3.</summary>
        public int Flr(float x) => (int)MathF.Floor(x);

        /// <summary>Ceiling.</summary>
        public int Ceil(float x) => (int)MathF.Ceiling(x);

        /// <summary>
        /// PICO-8 sine. Input is in turns (0..1 = full circle).
        /// Result is inverted: sin(0.25) = -1 (not +1 like standard math).
        /// </summary>
        public float Sin(float x) => -MathF.Sin(x * MathF.PI * 2f);

        /// <summary>
        /// PICO-8 cosine. Input is in turns (0..1 = full circle).
        /// </summary>
        public float Cos(float x) => MathF.Cos(x * MathF.PI * 2f);

        /// <summary>PICO-8 atan2. Returns turns (0..1). Note: arguments are (dx, dy), not (dy, dx).</summary>
        public float Atan2(float dx, float dy)
        {
            float a = MathF.Atan2(-dy, dx) / (MathF.PI * 2f);
            if (a < 0) a += 1f;
            return a;
        }

        public float Sqrt(float x) => MathF.Sqrt(x);
        public float Abs(float x) => MathF.Abs(x);

        /// <summary>Sign. PICO-8: sgn(0) returns 1.</summary>
        public float Sgn(float x) => x >= 0f ? 1f : -1f;

        public float Min(float x, float y) => MathF.Min(x, y);
        public float Max(float x, float y) => MathF.Max(x, y);

        /// <summary>Returns the middle of three values.</summary>
        public float Mid(float x, float y, float z)
        {
            if (x > y) (x, y) = (y, x);
            // now x <= y
            if (z <= x) return x;
            if (z >= y) return y;
            return z;
        }

        /// <summary>Integer power (for note frequency etc).</summary>
        public float Pow(float b, float e) => MathF.Pow(b, e);
    }
}
