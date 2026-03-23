using UnityEngine;
using Hatiora.Pico8.Unity;
using System;

namespace Hatiora.Pico8.Launcher
{
    public class LauncherCartridge : Cartridge, IUnityCartridge, ILauncherProvider

    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Os/pork/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Os/pork/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Os/pork/Map/map")?.text;
        public string GffData   => Resources.Load<TextAsset>("Os/pork/Gff/gff")?.text;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Os/pork/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Os/pork/Label/label");
        
        public string[] Games { get; set; } = Array.Empty<string>();
        private int _selectedIndex = 0;
        
        private int _fadeT = -1;
        private string _pendingGame;
        private int _startupDelay = 15; // Ignore input for the first 15 frames (~0.5s) to prevent bleed-through from pause menu
        
        private static readonly int[] _dpal = { 0, 0, 1, 1, 2, 1, 13, 6, 4, 4, 9, 3, 13, 1, 13, 14 };

        // ─── Boot Animation State ───
        private int _bootFrame = 0;
        private bool _bootComplete = false;
            // Phase 1: CRT warm-up — full noise clearing to sparse
        private const int BootWarmupFrames = 30;
        // Phase 2: Full rainbow noise peak  
        private const int BootPeakFrames = 8;
        private System.Random _bootRng;

        public override void Init() 
        {
            Music(-1); // Stop any music bleeding over from previous cartridge
            Sfx(-1);   // Stop any sound effects
            _bootFrame = 0;
            _bootComplete = false;
            _bootRng = new System.Random(42); // Deterministic seed for consistent boot pattern
        }

        public override void Update()
        {
            // Advance boot animation
            if (!_bootComplete)
            {
                _bootFrame++;
                int totalBootFrames = BootWarmupFrames + BootPeakFrames;
                if (_bootFrame >= totalBootFrames)
                {
                    Sfx(RuntimeSpec.SfxBootReady);
                    _bootComplete = true;
                    _startupDelay = 0;
                }
                return;
            }

            if (_startupDelay > 0)
            {
                _startupDelay--;
                return;
            }
            if (Games == null || Games.Length == 0) return;


            if (_fadeT >= 0)
            {
                _fadeT++;
                if (_fadeT > 40 && _pendingGame != null)
                {
                    OnLoadCartridge?.Invoke(_pendingGame);
                }
                return;
            }

            int rows = Mathf.CeilToInt((float)Games.Length / 3f);
            int c = _selectedIndex / rows;
            int r = _selectedIndex % rows;

            if (Btnp(0, -1)) // Left
            {
                c--;
                if (c < 0) c = 2;
                if (c * rows + r >= Games.Length) c--;
                _selectedIndex = c * rows + r;
                Sfx(RuntimeSpec.SfxNavigate);
            }
            if (Btnp(1, -1)) // Right
            {
                c++;
                if (c > 2 || c * rows + r >= Games.Length) c = 0;
                _selectedIndex = c * rows + r;
                Sfx(RuntimeSpec.SfxNavigate);
            }
            if (Btnp(2, -1)) // Up
            {
                r--;
                if (r < 0)
                {
                    r = rows - 1;
                    if (c * rows + r >= Games.Length) r -= ((c * rows + r) - (Games.Length - 1));
                }
                _selectedIndex = c * rows + r;
                Sfx(RuntimeSpec.SfxNavigate);
            }
            if (Btnp(3, -1)) // Down
            {
                r++;
                if (r >= rows || c * rows + r >= Games.Length) r = 0;
                _selectedIndex = c * rows + r;
                Sfx(RuntimeSpec.SfxNavigate);
            }

            if (Btnp(4, -1) || Btnp(5, -1) || Btnp(RuntimeSpec.SystemStartButton, -1))
            {
                Sfx(RuntimeSpec.SfxConfirm);
                _fadeT = 0;
                _pendingGame = Games[_selectedIndex];
            }
        }

        public override void Draw()
        {
            if (!_bootComplete)
            {
                DrawBootAnimation();
                return;
            }

            DrawMenu();
        }

        private void DrawBootAnimation()
        {
            Cls(0);
            float scale = ContentScale;
            int s = (int)scale;
            int w = P8.Width;
            int h = P8.Height;

            int endWarmup = BootWarmupFrames;
            int endPeak = endWarmup + BootPeakFrames;

            if (_bootFrame <= endWarmup)
            {
                // Phase 1: Monitor just turned on — full static noise, gradually clearing
                var rng = new System.Random(_bootFrame);
                float progress = (float)_bootFrame / BootWarmupFrames; // 0.0 → 1.0
                // Density curve: starts full, decelerates toward sparse
                float density = 1.0f - (progress * progress); // inverted quadratic

                for (int y = 0; y < h; y += s)
                {
                    // Occasional full scanline flicker for retro CRT feel
                    bool scanlineFlicker = rng.NextDouble() < (1.0f - progress) * 0.06;

                    for (int x = 0; x < w; x += s)
                    {
                        if (scanlineFlicker || rng.NextDouble() < density)
                        {
                            int col;
                            // Late in warm-up: colors dim down to dark blue/purple before fading out
                            if (progress > 0.6 && rng.NextDouble() < progress)
                                col = rng.NextDouble() < 0.5 ? 1 : 2; // Dim blue/purple
                            else
                                col = rng.Next(1, 16); // Full rainbow
                            Rectfill(x, y, x + s - 1, y + s - 1, col);
                        }
                    }
                }
            }
            else if (_bootFrame <= endPeak)
            {
                // Phase 2: Full rainbow noise, animated — monitor fully warmed up
                var rng = new System.Random(_bootFrame);
                for (int y = 0; y < h; y += s)
                {
                    for (int x = 0; x < w; x += s)
                    {
                        int col = rng.Next(1, 16);
                        Rectfill(x, y, x + s - 1, y + s - 1, col);
                    }
                }
            }
        }

        /// <summary>
        /// Draws the menu content (header, version, game list). 
        /// When interactive is true, applies selection highlighting and fade effects.
        /// visibleLines controls line-by-line reveal (-1 = show all).
        /// Lines: 0=header, 1=console ver, 2=os ver, 3+=grid rows (each row = all columns)
        /// </summary>
        private void DrawMenuContent(float scale, int s, bool interactive, int visibleLines = -1)
        {
            int padX = 3 * s;
            int padY = 3 * s;
            int lineIdx = 0;

            // Line 0: Header
            if (visibleLines == -1 || visibleLines > lineIdx)
            {
                Print("COM.HATIORA.PICO8", padX, padY, 7, CoordMode.Virtual, scale);
                int dx = padX + Flr(76 * scale);
                int dy = padY + Flr(1 * scale);
                int dsq = Flr(2 * scale);
                Rectfill(dx - dsq, dy, dx - 1, dy + dsq - 1, 8);
                Rectfill(dx, dy - dsq, dx + dsq - 1, dy - 1, 9);
                Rectfill(dx + dsq, dy, dx + dsq - 1, dy + dsq - 1, 10);
                Rectfill(dx, dy + dsq, dx + dsq - 1, dy + dsq * 2 - 1, 12);
            }
            lineIdx++;

            // Line 1: Console version
            if (visibleLines == -1 || visibleLines > lineIdx)
                Print("CONSOLE V0.1.0", padX, padY + 11 * s, 5, CoordMode.Virtual, scale);
            lineIdx++;

            // Line 2: OS version
            if (visibleLines == -1 || visibleLines > lineIdx)
                Print("OS      V0.1.0", padX, padY + 19 * s, 5, CoordMode.Virtual, scale);
            lineIdx++;

            // Lines 3+: Game grid (one line = one row across all columns)
            if (Games == null || Games.Length == 0) return;

            int startY = padY + 33 * s;
            int lineHeight = 11 * s;
            int colWidth = 41 * s;
            int rowsPerCol = Mathf.CeilToInt((float)Games.Length / 3f);

            for (int row = 0; row < rowsPerCol; row++)
            {
                if (visibleLines != -1 && visibleLines <= lineIdx) break;

                // Draw all columns in this row
                for (int column = 0; column < 3; column++)
                {
                    int i = column * rowsPerCol + row;
                    if (i >= Games.Length) continue;

                    int y = startY + row * lineHeight;
                    int x = padX + column * colWidth;

                    int col = 5; // Default: dark gray
                    if (interactive && i == _selectedIndex)
                    {
                        col = 7;
                        if (_fadeT >= 0 && (_fadeT / 4) % 2 == 0)
                            col = 8;
                        else if (_fadeT >= 0)
                            col = 7;
                    }

                    Print(Games[i].ToUpper(), x, y, col, CoordMode.Virtual, scale);
                }
                lineIdx++;
            }

            // Apply fade-out palette
            if (interactive && _fadeT >= 0)
            {
                for (int j = 1; j <= 15; j++)
                {
                    int col = j;
                    int steps = (_fadeT + (j % 5)) / 4;
                    for (int k = 0; k < steps; k++)
                        col = _dpal[col];
                    Pal(j, col, 1);
                }
            }
        }

        private void DrawMenu()
        {
            Cls(0);
            float scale = 1f; // Always render at native 128×128
            int s = 1;
            DrawMenuContent(scale, s, true);
        }

        // ─── ILauncherProvider ───

        public void DrawPauseMenu(IPico8 api, EngineSpec spec, PauseMenuState state)
        {
            int nr = spec.NativeResolution;
            int scale = spec.Scale;
            float s = 1f; // Always render at native 128×128
            int extentW = nr - 1;
            int extentH = nr - 1;

            // Calculate screen extents (in virtual coords)
            int screenW = api.PhysicalWidth / scale;
            int screenH = api.PhysicalHeight / scale;

            // Reset draw state
            api.Camera(0, 0);
            api.Clip(0, 0, screenW, screenH);
            api.Pal();
            api.Palt();
            api.Fillp();

            // Dim the FULL game area
            api.Fillp(0b0101101001011010);
            api.Rectfill(0, 0, screenW - 1, screenH - 1, 0);
            api.Fillp();

            // Center the 128×128 menu on screen via Camera offset
            int offsetX = (screenW - nr) / 2;
            int offsetY = (screenH - nr) / 2;
            api.Camera(-offsetX, -offsetY);
            api.Clip(offsetX, offsetY, nr, nr);

            // Draw padded container
            int margin = (int)(3 * s);
            api.Rectfill(margin, margin, extentW - margin, extentH - margin, 0);
            api.Rect(margin, margin, extentW - margin, extentH - margin, 5);
            api.Rect(margin - (int)s, margin - (int)s, extentW - margin + (int)s, extentH - margin + (int)s, 0);

            // Header
            int padX = (int)(6 * s);
            int padY = (int)(6 * s);
            api.Print("COM.HATIORA.PICO8", padX, padY, 7, CoordMode.Virtual, s);

            // 4-color diamond
            int dx = padX + (int)(76 * s);
            int dy = padY + (int)(1 * s);
            int dsq = (int)(2 * s);
            api.Rectfill(dx - dsq, dy, dx - 1, dy + dsq - 1, 8);
            api.Rectfill(dx, dy - dsq, dx + dsq - 1, dy - 1, 9);
            api.Rectfill(dx + dsq, dy, dx + dsq - 1, dy + dsq - 1, 10);
            api.Rectfill(dx, dy + dsq, dx + dsq - 1, dy + dsq * 2 - 1, 12);

            // Version Text
            int lineH = (int)(8 * s);
            api.Print("CONSOLE V0.1.0", padX, padY + (int)(11 * s), 5, CoordMode.Virtual, s);
            api.Print("OS      V0.1.0", padX, padY + (int)(19 * s), 5, CoordMode.Virtual, s);

            int startY = padY + (int)(33 * s);
            int itemIdx = 0;

            if (state.MenuState == 0) // Main Menu
            {
                PrintItem(api, "CONTINUE", startY, itemIdx == state.MenuIndex, padX, s);
                startY += lineH; itemIdx++;

                PrintItem(api, "OPTIONS", startY, itemIdx == state.MenuIndex, padX, s);
                startY += lineH; itemIdx++;

                if (state.CustomLabels != null)
                {
                    for (int i = 0; i < state.CustomLabels.Length; i++)
                    {
                        if (state.CustomLabels[i] != null)
                        {
                            PrintItem(api, state.CustomLabels[i].ToUpper(), startY, itemIdx == state.MenuIndex, padX, s);
                            startY += lineH; itemIdx++;
                        }
                    }
                }

                PrintItem(api, "MAIN MENU", startY, itemIdx == state.MenuIndex, padX, s);
                startY += lineH; itemIdx++;

                PrintItem(api, "EXIT", startY, itemIdx == state.MenuIndex, padX, s);
            }
            else // Options Menu
            {
                string soundLabel = state.IsMuted ? "SOUND: OFF" : "SOUND: ON ";
                PrintItem(api, soundLabel, startY, itemIdx == state.MenuIndex, padX, s);
                startY += lineH; itemIdx++;

                PrintItem(api, "VOLUME: ", startY, itemIdx == state.MenuIndex, padX, s);
                int bx = padX + (int)(32 * s);
                for (int i = 0; i < 8; i++)
                {
                    int c = (i < state.Volume) ? 7 : 5;
                    api.Rectfill(bx, startY, bx + (int)(2 * s), startY + (int)(4 * s), c);
                    bx += (int)(4 * s);
                }
                startY += lineH; itemIdx++;

                PrintItem(api, "BACK", startY, itemIdx == state.MenuIndex, padX, s);
            }
        }

        public string SystemSfxData => SfxData;

        private static void PrintItem(IPico8 api, string label, int y, bool selected, int padX, float scale)
        {
            api.Print(label, padX, y, selected ? 7 : 5, CoordMode.Virtual, scale);
        }
    }
}
