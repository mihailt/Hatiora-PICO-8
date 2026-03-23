namespace Hatiora.Pico8
{
    /// <summary>
    /// Defines pinned memory addresses for the engine's RAM layout.
    /// Use static presets (e.g. <see cref="Pico8"/>) or create custom maps.
    /// </summary>
    public sealed class MemoryMap
    {
        /// <summary>Address of the draw state region (camera, clip, palettes).</summary>
        public int DrawStateAddress { get; init; }

        /// <summary>Address of the screen (VRAM) region.</summary>
        public int ScreenAddress { get; init; }

        // ─── Presets ───

        /// <summary>
        /// Standard PICO-8 memory map.
        /// DrawState at 0x5F00, Screen at 0x6000.
        /// </summary>
        public static MemoryMap Pico8 => new()
        {
            DrawStateAddress = 0x5F00,
            ScreenAddress    = 0x6000,
        };

        /// <summary>
        /// Sequential packing — no gaps between regions.
        /// Addresses are computed by <see cref="MemoryLayout"/> based on region sizes.
        /// Use <c>-1</c> to signal "pack sequentially after previous region".
        /// </summary>
        public static MemoryMap Sequential => new()
        {
            DrawStateAddress = -1,
            ScreenAddress    = -1,
        };
    }
}
