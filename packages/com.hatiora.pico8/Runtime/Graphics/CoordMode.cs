namespace Hatiora.Pico8
{
    /// <summary>
    /// Coordinate mode for draw calls.
    /// Virtual = game coordinates (scaled to physical on output).
    /// Physical = raw pixel coordinates on the output buffer.
    /// </summary>
    public enum CoordMode
    {
        /// <summary>Game coordinates, auto-scaled by EngineSpec.Scale.</summary>
        Virtual,

        /// <summary>Raw pixel coordinates on the physical output buffer.</summary>
        Physical
    }
}
