using System;
using System.Collections.Generic;
using System.Linq;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// A standalone catalog of cartridge registrations.
    /// Wire to a <see cref="Pico8Builder"/> via <c>builder.WithRegistry(registry)</c> to activate.
    /// </summary>
    public class CartridgeRegistry
    {
        private Pico8Builder _builder;
        private readonly EngineSpec _baseSpec;
        private readonly Dictionary<string, Func<Pico8View>> _gameRegistry = new();

        /// <summary>
        /// Gets the available, registered cartridge names.
        /// </summary>
        public IEnumerable<string> GameNames => _gameRegistry.Keys;

        /// <summary>
        /// Fired when a deferred cartridge load has been securely processed on the next frame boundary.
        /// </summary>
        public event Action<string> OnDeferredLoad;

        private string _pendingGameLoad;

        /// <summary>
        /// Creates a new Cartridge Registry with a base engine spec.
        /// </summary>
        /// <param name="baseSpec">The base engine spec with system buttons, SFX, and memory config. Per-cartridge resolution is set via Register.</param>
        public CartridgeRegistry(EngineSpec baseSpec)
        {
            _baseSpec = baseSpec ?? throw new ArgumentNullException(nameof(baseSpec));
        }

        /// <summary>
        /// Called by <see cref="Pico8Builder.WithRegistry"/> to bind the builder's audio/input providers.
        /// </summary>
        internal void SetBuilder(Pico8Builder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Registers a strongly-typed cartridge class into the registry with custom physical resolution mappings.
        /// </summary>
        public CartridgeRegistry Register<TCartridge>(string name, int texW = 128, int texH = 128, int physW = 512, int physH = 512, Action<TCartridge> configure = null) where TCartridge : Cartridge, new()
        {
            _gameRegistry[name] = () =>
            {
                if (_builder == null)
                    throw new InvalidOperationException("CartridgeRegistry has not been bound to a Pico8Builder. Call builder.WithRegistry(registry) first.");

                var spec = new EngineSpec
                {
                    // Per-cartridge resolution
                    ScreenWidth = texW,
                    ScreenHeight = texH,
                    PhysicalWidth = physW,
                    PhysicalHeight = physH,
                    // Inherited from base spec
                    MemoryMap = _baseSpec.MemoryMap,
                    SystemStartButton = _baseSpec.SystemStartButton,
                    SystemSelectButton = _baseSpec.SystemSelectButton,
                    SfxNavigate = _baseSpec.SfxNavigate,
                    SfxConfirm  = _baseSpec.SfxConfirm,
                    SfxCancel   = _baseSpec.SfxCancel,
                    SfxBootReady = _baseSpec.SfxBootReady
                };

                // Clone the base builder configuration to prevent state mutation across load boundaries
                var engine = new Pico8Builder()
                    .WithSpec(spec)
                    .WithAudio(_builder.AudioProvider)
                    .WithInput(_builder.InputProvider)
                    .WithLauncherProvider(_builder.LauncherProvider)
                    .Build<TCartridge>();

                var cart = (TCartridge)engine.ActiveCartridge;
                cart.OnLoadCartridge += (nameToLoad) => _pendingGameLoad = nameToLoad;
                configure?.Invoke(cart);

                // Auto-inject available game names into cartridges that expose a Games property
                var gamesProp = typeof(TCartridge).GetProperty("Games", typeof(string[]));
                if (gamesProp != null && gamesProp.CanWrite)
                {
                    var gamesList = _gameRegistry.Keys.Where(g => g != name).ToList();
                    gamesList.Sort();
                    gamesProp.SetValue(cart, gamesList.ToArray());
                }

                return new Pico8View(engine, physW, physH);
            };
            return this;
        }

        /// <summary>
        /// Loads and instantiates the chosen cartridge view from the registry.
        /// </summary>
        public Pico8View Load(string name)
        {
            if (_gameRegistry.TryGetValue(name, out var factory))
            {
                return factory();
            }

            throw new ArgumentException($"Cartridge '{name}' has not been registered.");
        }

        /// <summary>
        /// Should be called every frame to process deferred cartridge loads securely outside the execution stack.
        /// </summary>
        public void Update()
        {
            if (_pendingGameLoad != null)
            {
                string gameToLoad = _pendingGameLoad;
                _pendingGameLoad = null;
                OnDeferredLoad?.Invoke(gameToLoad);
            }
        }
    }
}
