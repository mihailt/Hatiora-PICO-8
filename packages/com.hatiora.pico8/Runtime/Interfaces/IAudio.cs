namespace Hatiora.Pico8
{
    public interface IAudio
    {
        void Sfx(int n);
        void Sfx(int n, int channel, int offset, int length);
        void Music(int n);
        void Music(int n, int fadeLen, int channelMask);
        void LoadSfx(string sfxData);
        void LoadMusic(string musicData);
        void ProcessAudio(float[] data, int channels);

        /// <summary>Loads SFX data into the system bank (used for launcher/pause menu sounds).</summary>
        void LoadSystemSfx(string sfxData);

        /// <summary>Plays an SFX from the system bank on the dedicated system channel.</summary>
        void SystemSfx(int n);
        
        /// <summary>Master volume (0-8).</summary>
        int Volume { get; set; }
        
        /// <summary>Master mute toggle.</summary>
        bool IsMuted { get; set; }
    }
}
