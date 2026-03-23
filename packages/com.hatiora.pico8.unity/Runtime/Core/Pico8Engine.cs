using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// The assembled PICO-8 runtime for Unity. Created by <see cref="Pico8Builder"/>.
    /// Orchestrates the game loop: poll input → update → draw → present.
    /// </summary>
    public sealed class Pico8Engine
    {
        public IPico8 Api { get; }
        public TextureGraphics TextureOutput { get; }
        public Cartridge ActiveCartridge => _cart;

        private readonly Cartridge _cart;
        private readonly IInputProvider _inputProvider;
        private readonly Pico8Input _input;
        private readonly IGraphics _gfx;
        private readonly Pico8Api _apiImpl;
        private readonly EngineSpec _spec;
        private readonly Stopwatch _frameSw = new Stopwatch();
        private bool _initialized;
        
        // Pause State
        private bool _isPaused;
        private int _pauseMenuState; // 0 = Main, 1 = Options
        private int _pauseMenuIndex;
        private byte[] _pixelBackup; // Saved PixelBuffer.Pixels on pause
        private readonly PixelBuffer _pixelBuffer;
        private readonly ILauncherProvider _launcherProvider;
        private readonly PauseMenuState _pauseState = new();
        
        private readonly IAudio _audioProvider;

        // 30fps frame budget in seconds
        private const float FrameBudget = 1f / 30f;

        internal Pico8Engine(
            IPico8 api,
            Cartridge cart,
            IInputProvider inputProvider,
            Pico8Input input,
            IGraphics gfx,
            TextureGraphics textureOutput,
            EngineSpec spec,
            ILauncherProvider launcherProvider = null)
        {
            Api = api ?? throw new ArgumentNullException(nameof(api));
            _cart = cart ?? throw new ArgumentNullException(nameof(cart));
            _inputProvider = inputProvider;
            _input = input;
            _gfx = gfx;
            TextureOutput = textureOutput;
            _apiImpl = api as Pico8Api;
            _spec = spec;
            _audioProvider = _apiImpl?.Audio;
            _pixelBuffer = gfx as PixelBuffer;
            _launcherProvider = launcherProvider;
        }

        /// <summary>
        /// Initializes the cartridge. Call once before the first Tick.
        /// </summary>
        public void Init()
        {
            if (_initialized) return;
            Debug.Log($"[Pico8Engine] Init {_cart.GetType().Name} — Virtual: {_spec.ScreenWidth}x{_spec.ScreenHeight}, Physical: {_spec.EffectivePhysW}x{_spec.EffectivePhysH}, Scale: {_spec.Scale}");
            if (_apiImpl != null) _apiImpl.IsHighRes = _spec.ScreenWidth > _spec.NativeResolution;
            _cart.Init();
            _initialized = true;
        }

        /// <summary>
        /// One frame: poll → update → draw → present.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_initialized) Init();

            // Poll input
            _inputProvider?.Poll(_input);

            // Toggle hi-res mode on Select button
            if (_apiImpl != null && _apiImpl.Btnp(_spec.SystemSelectButton, -1))
            {
                _apiImpl.IsHighRes = !_apiImpl.IsHighRes;
                Debug.Log($"[Pico8Engine] HiRes={_apiImpl.IsHighRes} — {_cart.GetType().Name}");
            }

            // Handle System Pause across any player based on Config
            if (_apiImpl != null && _apiImpl.Btnp(_spec.SystemStartButton, -1))
            {
                if (!_isPaused && _cart is not ILauncherProvider && _launcherProvider != null)
                {
                    _isPaused = true;
                    _pauseMenuState = 0;
                    _pauseMenuIndex = 0;
                    PlaySystemSfx(_spec.SfxConfirm); // Pause open
                    
                    // Backup the pixel buffer directly
                    if (_pixelBuffer != null)
                    {
                        _pixelBackup = new byte[_pixelBuffer.Pixels.Length];
                        Array.Copy(_pixelBuffer.Pixels, _pixelBackup, _pixelBackup.Length);
                    }

                    // Consume input so the same press doesn't act as a confirm
                    _input?.Snapshot(0);
                }
                else if (_isPaused)
                {
                    // Start acts as confirm/select inside the pause menu
                    PlaySystemSfx(_spec.SfxConfirm);
                    ExecutePauseMenuAction(GetCurrentMenuItemCount());
                    // Consume input so resume doesn't re-trigger pause next frame
                    _input?.Snapshot(0);
                }
            }

            // Measure CPU usage of Update + Draw
            _frameSw.Restart();

            if (!_isPaused)
            {
                // Update game logic
                _cart.Update();

                // Draw
                _cart.Draw();
            }
            else
            {
                DrawPauseMenu();
            }

            _frameSw.Stop();

            // Report CPU fraction: elapsed / 30fps budget
            _apiImpl?.SetCpuFraction((float)(_frameSw.Elapsed.TotalSeconds / FrameBudget));

            // Output to GPU
            _gfx.Flush();
            TextureOutput?.Present();

            // Snapshot input state for btnp
            _input?.Snapshot(dt);
        }

        private void ResumeGame()
        {
            _isPaused = false;
            // Restore the pixel buffer so the game screen is exactly as it was before pausing
            if (_pixelBuffer != null && _pixelBackup != null)
            {
                Array.Copy(_pixelBackup, _pixelBuffer.Pixels, _pixelBackup.Length);
            }
            // Reset all draw state that the pause menu may have dirtied
            if (_apiImpl != null)
            {
                _apiImpl.Pal();
                _apiImpl.Palt();
                _apiImpl.Fillp();
                _apiImpl.Camera(0, 0);
                _apiImpl.Clip();
            }
        }

        private void DrawPauseMenu()
        {
            if (_apiImpl == null || _launcherProvider == null) return;
            if (_input != null) HandlePauseInput();
            if (!_isPaused) return; // ResumeGame was called — don't draw pause overlay

            // Restore the frozen pixels
            if (_pixelBuffer != null && _pixelBackup != null)
            {
                Array.Copy(_pixelBackup, _pixelBuffer.Pixels, _pixelBackup.Length);
            }

            // Build state for the OS provider
            _pauseState.MenuState = _pauseMenuState;
            _pauseState.MenuIndex = _pauseMenuIndex;
            _pauseState.IsMuted = _audioProvider?.IsMuted ?? false;
            _pauseState.Volume = _audioProvider?.Volume ?? 0;

            // Collect custom labels
            var labels = new string[5];
            for (int i = 0; i < 5; i++)
                labels[i] = _apiImpl.CustomMenuItems[i]?.Label;
            _pauseState.CustomLabels = labels;

            // Delegate rendering to the OS provider
            _launcherProvider.DrawPauseMenu(_apiImpl, _spec, _pauseState);
        }

        private void HandlePauseInput()
        {
            if (_apiImpl == null) return;
            int itemCount = GetCurrentMenuItemCount();

            if (_apiImpl.Btnp(2, -1)) // Up
            {
                _pauseMenuIndex--;
                if (_pauseMenuIndex < 0) _pauseMenuIndex = itemCount - 1;
                PlaySystemSfx(_spec.SfxNavigate);
            }
            if (_apiImpl.Btnp(3, -1)) // Down
            {
                _pauseMenuIndex++;
                if (_pauseMenuIndex >= itemCount) _pauseMenuIndex = 0;
                PlaySystemSfx(_spec.SfxNavigate);
            }

            if (_pauseMenuState == 1 && _pauseMenuIndex == 1 && _audioProvider != null) // VOLUME selected
            {
                if (_apiImpl.Btnp(0, -1)) // Left -> Vol Down
                {
                    _audioProvider.Volume = Math.Max(0, _audioProvider.Volume - 1);
                    PlaySystemSfx(_spec.SfxNavigate);
                }
                if (_apiImpl.Btnp(1, -1)) // Right -> Vol Up
                {
                    _audioProvider.Volume = Math.Min(8, _audioProvider.Volume + 1);
                    PlaySystemSfx(_spec.SfxNavigate);
                }
            }

            if (_apiImpl.Btnp(4, -1) || _apiImpl.Btnp(5, -1)) // Action
            {
                PlaySystemSfx(_spec.SfxConfirm); // Select
                ExecutePauseMenuAction(itemCount);
            }
        }

        private int GetCurrentMenuItemCount()
        {
            if (_pauseMenuState == 0)
            {
                int count = 4; // Continue, Options, Back to Games, Exit
                for (int i = 0; i < 5; i++)
                {
                    if (_apiImpl.CustomMenuItems[i] != null) count++;
                }
                return count;
            }
            else
            {
                return 3; // Sound, Volume, Back
            }
        }

        private void ExecutePauseMenuAction(int totalItems)
        {
            if (_pauseMenuState == 0) // Main Pause Menu
            {
                if (_pauseMenuIndex == 0)
                {
                    ResumeGame();
                }
                else if (_pauseMenuIndex == 1) // OPTIONS
                {
                    _pauseMenuState = 1;
                    _pauseMenuIndex = 0;
                }
                else if (_pauseMenuIndex == totalItems - 2) // BACK TO GAMES
                {
                    ResumeGame();
                    _cart.OnLoadCartridge?.Invoke("os"); 
                }
                else if (_pauseMenuIndex == totalItems - 1) // EXIT APP
                {
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    UnityEngine.Application.Quit();
                    #endif
                }
                else
                {
                    // Custom Items
                    int customIndex = _pauseMenuIndex - 2; 
                    int validCounter = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        var customItem = _apiImpl.CustomMenuItems[i];
                        if (customItem != null)
                        {
                            if (validCounter == customIndex)
                            {
                                customItem.Callback?.Invoke();
                                ResumeGame(); 
                                break;
                            }
                            validCounter++;
                        }
                    }
                }
            }
            else if (_pauseMenuState == 1) // Options sub-menu
            {
                if (_pauseMenuIndex == 0 && _audioProvider != null) // SOUND TOGGLE
                {
                    _audioProvider.IsMuted = !_audioProvider.IsMuted;
                }
                else if (_pauseMenuIndex == 2) // BACK
                {
                    _pauseMenuState = 0; // Return to Main
                    _pauseMenuIndex = 1; // Highlight OPTIONS
                }
            }
        }

        /// <summary>
        /// Plays a system SFX on the dedicated system channel (uses launcher's SFX bank).
        /// </summary>
        private void PlaySystemSfx(int sfxId)
        {
            _audioProvider?.SystemSfx(sfxId);
        }
    }
}
