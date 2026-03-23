using System;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Defines the engine configuration. PICO-8 is a preset, not the ceiling.
    /// Every subsystem reads its dimensions and limits from this spec.
    /// </summary>
    public sealed class EngineSpec
    {
        // ─── Screen ───
        public int ScreenWidth   { get; init; } = 128;
        public int ScreenHeight  { get; init; } = 128;
        public int PhysicalWidth  { get; init; } = 0; // 0 = same as virtual
        public int PhysicalHeight { get; init; } = 0;

        // ─── Sprites ───
        public int SpriteSize   { get; init; } = 8;
        public int SheetWidth   { get; init; } = 128;
        public int SheetHeight  { get; init; } = 128;

        // ─── Map ───
        public int MapWidth     { get; init; } = 128;
        public int MapHeight    { get; init; } = 32;

        // ─── Palette ───
        public int PaletteSize  { get; init; } = 16;
        public byte[] DefaultPalette { get; init; } = Palette.Pico8Rgb;

        // ─── Audio ───
        public int MaxSfx       { get; init; } = 64;
        public int MaxMusic     { get; init; } = 64;
        public int AudioChannels { get; init; } = 4;
        public int NotesPerSfx  { get; init; } = 32;
        public int SampleRate   { get; init; } = 22050;

        // ─── Draw defaults ───
        public int DefaultColor { get; init; } = 6;

        // ─── Memory Map ───
        public MemoryMap MemoryMap { get; init; } = MemoryMap.Pico8;

        // ─── Engine Navigation ───
        public int SystemStartButton { get; init; } = 6;
        public int SystemSelectButton { get; init; } = 7;

        // ─── System SFX ───
        public int SfxNavigate    { get; init; } = 56;  // Menu cursor move
        public int SfxConfirm     { get; init; } = 54;  // Menu select / open
        public int SfxCancel      { get; init; } = 53;  // Menu close / back
        public int SfxBootReady   { get; init; } = 61;  // Boot complete chime

        // ─── Computed ───
        public int SpritesPerRow  => SheetWidth / SpriteSize;
        public int SpritesPerBank => SpritesPerRow * (SheetHeight / SpriteSize);
        public int EffectivePhysW => PhysicalWidth  > 0 ? PhysicalWidth  : ScreenWidth;
        public int EffectivePhysH => PhysicalHeight > 0 ? PhysicalHeight : ScreenHeight;
        public int Scale => Math.Max(1, Math.Min(
            EffectivePhysW / ScreenWidth,
            EffectivePhysH / ScreenHeight));

        // ─── Hi-Res ───
        public int NativeResolution { get; init; } = 128;
        public float ContentScaleX => (float)ScreenWidth / NativeResolution;
        public float ContentScaleY => (float)ScreenHeight / NativeResolution;
        public float ContentScale => Math.Min(ContentScaleX, ContentScaleY);

        // ─── Presets ───
        public static EngineSpec Pico8 => new();

        public static EngineSpec Pico8At4K => new()
        {
            PhysicalWidth = 3840,
            PhysicalHeight = 2160
        };

        public static EngineSpec Extended => new()
        {
            ScreenWidth = 256,
            ScreenHeight = 256,
            SheetWidth = 256,
            SheetHeight = 256,
            MapWidth = 256,
            MapHeight = 256,
            PaletteSize = 32,
            MaxSfx = 128,
            MaxMusic = 128,
            AudioChannels = 8
        };
    }
}
