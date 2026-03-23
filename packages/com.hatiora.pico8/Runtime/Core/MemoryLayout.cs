namespace Hatiora.Pico8
{
    /// <summary>
    /// Computes memory region addresses from an <see cref="EngineSpec"/>.
    /// Replaces PICO-8's fixed memory map with a spec-driven layout.
    /// </summary>
    public sealed class MemoryLayout
    {
        public int GfxStart       { get; }
        public int GfxSize        { get; }
        public int MapStart       { get; }
        public int MapSize        { get; }
        public int FlagsStart     { get; }
        public int FlagsSize      { get; }
        public int MusicStart     { get; }
        public int MusicSize      { get; }
        public int SfxStart       { get; }
        public int SfxSize        { get; }
        public int DrawStateStart { get; }
        public int DrawStateSize  { get; } = 64;
        public int ScreenStart    { get; }
        public int ScreenSize     { get; }
        public int TotalSize      { get; }

        public MemoryLayout(EngineSpec spec)
        {
            GfxSize   = spec.SheetWidth * spec.SheetHeight / 2;  // 2 pixels per byte
            MapSize   = spec.MapWidth * spec.MapHeight;
            FlagsSize = spec.SpritesPerBank;
            MusicSize = spec.MaxMusic * 4;
            SfxSize   = spec.MaxSfx * 68;
            ScreenSize = spec.ScreenWidth * spec.ScreenHeight / 2;

            var map = spec.MemoryMap;

            // Pack data regions sequentially from 0x0000
            GfxStart       = 0x0000;
            MapStart       = GfxStart + GfxSize;
            FlagsStart     = MapStart + MapSize;
            MusicStart     = FlagsStart + FlagsSize;
            SfxStart       = MusicStart + MusicSize;

            // DrawState and Screen use preset addresses, or pack sequentially
            int sequentialNext = SfxStart + SfxSize;
            DrawStateStart = map.DrawStateAddress >= 0 ? map.DrawStateAddress : sequentialNext;
            sequentialNext = DrawStateStart + DrawStateSize;
            ScreenStart    = map.ScreenAddress >= 0 ? map.ScreenAddress : sequentialNext;
            TotalSize      = ScreenStart + ScreenSize;
        }
    }
}
