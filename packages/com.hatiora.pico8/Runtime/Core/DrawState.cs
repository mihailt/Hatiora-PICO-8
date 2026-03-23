namespace Hatiora.Pico8
{
    /// <summary>
    /// Holds all mutable draw state: camera, clip rect, palettes, transparency,
    /// fill pattern, cursor, and current color. All arrays sized from <see cref="EngineSpec"/>.
    /// </summary>
    public sealed class DrawState
    {
        private readonly EngineSpec _spec;

        public int CameraX, CameraY;
        public int ClipX, ClipY, ClipW, ClipH;
        public int CurrentColor;
        public int CursorX, CursorY;
        public int FillPattern;

        /// <summary>Draw palette: remaps color indices at draw time. pal(c0,c1,0)</summary>
        public byte[] DrawPalette;

        /// <summary>Display palette: remaps indices at screen output. pal(c0,c1,1)</summary>
        public byte[] DisplayPalette;

        /// <summary>Per-color transparency flags. palt(c, true/false)</summary>
        public bool[] Transparency;

        public DrawState(EngineSpec spec)
        {
            _spec = spec;
            DrawPalette    = new byte[spec.PaletteSize];
            DisplayPalette = new byte[spec.PaletteSize];
            Transparency   = new bool[spec.PaletteSize];
            Reset();
        }

        /// <summary>
        /// Resets all draw state to defaults. Called at cart init and by pal()/palt() with no args.
        /// </summary>
        public void Reset()
        {
            CameraX = CameraY = 0;
            ClipX = ClipY = 0;
            ClipW = _spec.ScreenWidth;
            ClipH = _spec.ScreenHeight;
            CurrentColor = _spec.DefaultColor;
            CursorX = CursorY = 0;
            FillPattern = 0;

            for (int i = 0; i < _spec.PaletteSize; i++)
            {
                DrawPalette[i]    = (byte)i;  // identity
                DisplayPalette[i] = (byte)i;  // identity
                Transparency[i]   = (i == 0); // color 0 transparent by default
            }
        }
    }
}
