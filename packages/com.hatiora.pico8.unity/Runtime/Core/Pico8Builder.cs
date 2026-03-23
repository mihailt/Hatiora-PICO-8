using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Fluent builder that wires all subsystems together.
    /// Creates a <see cref="Pico8Engine"/> ready for use in Unity.
    /// </summary>
    public sealed class Pico8Builder
    {
        private Cartridge _cart;
        private EngineSpec _spec;
        private ILogger _log;
        private IInputProvider _inputProvider;
        private IAudio _audio;
        private IFontProvider _font;
        private CartridgeRegistry _registry;
        private VisualElement _root;
        private Pico8View _currentView;
        private readonly List<(int bank, Texture2D texture)> _spriteTextures = new();
        private ILauncherProvider _launcherProvider;
        private Action<CartridgeRegistry> _osRegistration;

        public IAudio AudioProvider => _audio;
        public IInputProvider InputProvider => _inputProvider;
        public ILauncherProvider LauncherProvider => _launcherProvider;

        public Pico8Builder WithLauncher<T>(int texW = 256, int texH = 256, int physW = 1024, int physH = 1024) where T : Cartridge, ILauncherProvider, new()
        {
            _launcherProvider = new T();
            _osRegistration = (reg) => reg.Register<T>("os", texW, texH, physW, physH);
            _osRegistration?.Invoke(_registry); // register now if registry already set
            return this;
        }

        internal Pico8Builder WithLauncherProvider(ILauncherProvider os)
        {
            _launcherProvider = os;
            return this;
        }

        public Pico8Builder WithCartridge(Cartridge cart)
        {
            _cart = cart;
            return this;
        }

        public Pico8Builder WithSpec(EngineSpec spec)
        {
            _spec = spec;
            return this;
        }

        public Pico8Builder WithLogger(ILogger log)
        {
            _log = log;
            return this;
        }

        public Pico8Builder WithInput(IInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
            return this;
        }

        public Pico8Builder WithInput(IEnumerable<(int player, int button, InputAction action)> bindings)
        {
            _inputProvider = new UnityInputProvider(bindings);
            return this;
        }

        public Pico8Builder WithAudio(IAudio audio)
        {
            _audio = audio;
            return this;
        }

        public Pico8Builder WithRegistry(CartridgeRegistry registry)
        {
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            registry.SetBuilder(this);
            _osRegistration?.Invoke(registry);
            return this;
        }
        /// <summary>
        /// Mounts a boot cartridge into the given UI root and wires up deferred cartridge loading.
        /// The builder tracks the current view and handles all remounting internally.
        /// </summary>
        public Pico8Builder Mount(VisualElement root, string bootCartridge)
        {
            if (_registry == null) throw new System.InvalidOperationException("Call WithRegistry before Mount.");
            _root = root;
            _currentView = _registry.Mount(root, bootCartridge);
            _registry.OnDeferredLoad += (name) => _currentView = _registry.Mount(_root, name);
            return this;
        }

        /// <summary>
        /// Processes deferred cartridge loads and ticks the active view. Call once per frame.
        /// </summary>
        public void Tick(float deltaTime)
        {
            _registry?.Update();
            _currentView?.Tick(deltaTime);
        }

        public Pico8Builder WithSprites(int bank, Texture2D texture)
        {
            _spriteTextures.Add((bank, texture));
            return this;
        }

        public Pico8Builder WithSprites(Texture2D texture)
        {
            return WithSprites(0, texture);
        }

        public Pico8Builder WithFont(IFontProvider font)
        {
            _font = font;
            return this;
        }

        public Pico8Builder WithFont(Font font, int pixelsPerEm = 0, int charWidth = 0)
        {
            if (pixelsPerEm > 0 && charWidth > 0)
                _font = new FontProvider(font, pixelsPerEm, charWidth);
            else if (pixelsPerEm > 0)
                _font = new FontProvider(font, pixelsPerEm);
            else
                _font = new FontProvider(font);
            return this;
        }

        public Pico8Engine Build<TCartridge>() where TCartridge : Cartridge, new()
        {
            _cart = new TCartridge();
            return Build();
        }

        public Pico8Engine Build()
        {
            if (_cart == null)
                throw new InvalidOperationException("Cartridge is required. Call WithCartridge().");

            // Resolve spec: cartridge may declare its own, or use explicit, or default
            var spec = _spec ?? _cart.Spec ?? EngineSpec.Pico8;

            // Validate configuration
            if (spec.ScreenWidth <= 0 || spec.ScreenHeight <= 0)
                throw new InvalidOperationException($"Invalid Virtual Resolution: {spec.ScreenWidth}x{spec.ScreenHeight}. Must be > 0.");
            if (spec.EffectivePhysW <= 0 || spec.EffectivePhysH <= 0)
                throw new InvalidOperationException($"Invalid Physical Resolution: {spec.EffectivePhysW}x{spec.EffectivePhysH}. Must be > 0.");

            var unityCart = _cart as IUnityCartridge;
            if (_spriteTextures.Count == 0 && (unityCart == null || unityCart.GfxTexture == null))
            {
                var logger = _log ?? new UnityLogger();
                logger.Log("[Warning] Building Pico-8 Engine without any Sprites.");
            }

            // Core components
            var palette = new Palette(spec);
            var mem = new Pico8Memory(spec);
            var state = new DrawState(spec);
            var input = new Pico8Input();
            var sprites = new SpriteStore(spec);
            var map = new MapStore(spec, mem);
            var log = _log ?? new UnityLogger();
            var audio = _audio ?? new NullAudio();

            // Load sprite textures
            byte[] spritePixelData = null;
            if (_spriteTextures.Count > 0)
            {
                foreach (var (bank, tex) in _spriteTextures)
                {
                    var data = UnitySpriteLoader.Load(tex, palette);
                    sprites.LoadBank(bank, data, tex.width, tex.height);
                    if (bank == 0) spritePixelData = data;
                }
            }
            else if (unityCart != null && unityCart.GfxTexture != null)
            {
                spritePixelData = UnitySpriteLoader.Load(unityCart.GfxTexture, palette);
                sprites.LoadBank(0, spritePixelData, unityCart.GfxTexture.width, unityCart.GfxTexture.height);
            }

            // Pack sprite pixel data into RAM at GfxStart (PICO-8 format: 2 pixels per byte)
            // This enables the shared sprite/map memory region (map rows 32-63)
            if (spritePixelData != null)
            {
                int gfxStart = mem.Layout.GfxStart;
                int packedLen = spritePixelData.Length / 2;
                for (int i = 0; i < packedLen && (gfxStart + i) < mem.Ram.Length; i++)
                {
                    int lo = spritePixelData[i * 2] & 0x0F;
                    int hi = spritePixelData[i * 2 + 1] & 0x0F;
                    mem.Ram[gfxStart + i] = (byte)(lo | (hi << 4));
                }
            }

            // Load audio data if possible
            if (unityCart != null)
            {
                if (unityCart.SfxData != null) audio.LoadSfx(unityCart.SfxData);
                if (unityCart.MusicData != null) audio.LoadMusic(unityCart.MusicData);
            }

            // Load launcher SFX into the system bank (for pause menu sounds)
            if (_launcherProvider?.SystemSfxData != null)
                audio.LoadSystemSfx(_launcherProvider.SystemSfxData);

            // Load map data
            if (unityCart?.MapData != null)
                map.LoadMapData(MapDataLoader.Parse(unityCart.MapData));

            // Load sprite flag data (__gff__)
            if (unityCart?.GffData != null)
                map.LoadFlagData(GffDataLoader.Parse(unityCart.GffData));

            // Graphics
            var font = _font ?? new FontProvider(Resources.Load<Font>("Fonts/pico-8"));
            var pixelBuffer = new PixelBuffer(spec, state, sprites, map, font);
            var textureGfx = new TextureGraphics(pixelBuffer, palette, state);

            // API facade
            var api = new Pico8Api(spec, mem, state, palette, pixelBuffer, sprites, map, audio, input, log);

            // Wire cartridge
            _cart.Bind(api, spec);

            // Enable input
            if (_inputProvider is UnityInputProvider uip)
                uip.Enable();

            return new Pico8Engine(api, _cart, _inputProvider, input, pixelBuffer, textureGfx, spec, _launcherProvider);
        }

        /// <summary>Null audio for when no audio provider is configured.</summary>
        public sealed class NullAudio : IAudio
        {
            public void Sfx(int n) { }
            public void Sfx(int n, int channel, int offset, int length) { }
            public void SystemSfx(int n) { }
            public void Music(int n) { }
            public void Music(int n, int fadeLen, int channelMask) { }
            public void LoadSfx(string sfxData) { }
            public void LoadMusic(string musicData) { }
            public void LoadSystemSfx(string sfxData) { }
            public void ProcessAudio(float[] data, int channels) { }
            public int Volume { get; set; } = 8;
            public bool IsMuted { get; set; } = false;
        }
    }
}
