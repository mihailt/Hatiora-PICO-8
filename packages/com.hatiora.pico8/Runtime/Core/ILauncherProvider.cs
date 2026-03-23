namespace Hatiora.Pico8
{
    /// <summary>
    /// Interface for providing system-level OS functionality (pause menu, boot screen).
    /// Implement this on your OS cartridge to provide custom system UI.
    /// </summary>
    public interface ILauncherProvider
    {
        /// <summary>
        /// Draws the pause menu overlay. Called by the engine every frame while paused.
        /// The engine handles pixel backup/restore, input, and state transitions.
        /// </summary>
        void DrawPauseMenu(IPico8 api, EngineSpec spec, PauseMenuState state);

        /// <summary>
        /// Raw SFX data from the launcher cartridge.
        /// Loaded into the system bank at engine build time for pause menu sounds.
        /// </summary>
        string SystemSfxData { get; }
    }

    /// <summary>
    /// Pause menu state passed from the engine to the OS provider.
    /// </summary>
    public class PauseMenuState
    {
        /// <summary>0 = Main menu, 1 = Options menu.</summary>
        public int MenuState { get; set; }

        /// <summary>Currently selected menu item index.</summary>
        public int MenuIndex { get; set; }

        /// <summary>Custom menu item labels registered by the cartridge (up to 5, null = unused).</summary>
        public string[] CustomLabels { get; set; }

        /// <summary>Whether audio is currently muted.</summary>
        public bool IsMuted { get; set; }

        /// <summary>Current volume level (0-8).</summary>
        public int Volume { get; set; }
    }
}
