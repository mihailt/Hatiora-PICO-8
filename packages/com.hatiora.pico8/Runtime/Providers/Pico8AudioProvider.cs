using UnityEngine;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Unity-side audio provider — wraps Pico8Synth with AudioSource wiring.
    /// </summary>
    public class Pico8AudioProvider : IAudio
    {
        private readonly Pico8Synth _synth;

        /// <summary>Full Unity constructor using default audio setup.</summary>
        public Pico8AudioProvider(AudioSource source)
            : this(source, UnityAudioSetup.Instance) { }

        /// <summary>Injectable constructor for testability.</summary>
        public Pico8AudioProvider(AudioSource source, IAudioSetup setup)
        {
            setup.ConfigureSource(source);
            _synth = new Pico8Synth(setup.SampleRate);
        }

        /// <summary>Synth-only constructor (no AudioSource wiring).</summary>
        public Pico8AudioProvider(Pico8Synth synth)
        {
            _synth = synth;
        }

        public void LoadSfx(string sfxData) => _synth.LoadSfx(sfxData);
        public void LoadMusic(string musicData) => _synth.LoadMusic(musicData);
        public void LoadSystemSfx(string sfxData) => _synth.LoadSystemSfx(sfxData);
        public void Sfx(int n) => _synth.Sfx(n);
        public void Sfx(int n, int channel, int offset, int length) => _synth.Sfx(n, channel, offset, length);
        public void SystemSfx(int n) => _synth.SystemSfx(n);
        public void Music(int n) => _synth.Music(n);
        public void Music(int n, int fadeLen, int channelMask) => _synth.Music(n, fadeLen, channelMask);
        public void ProcessAudio(float[] data, int channels) => _synth.ProcessAudio(data, channels);

        public int Volume 
        {
            get => _synth.Volume;
            set => _synth.Volume = value;
        }

        public bool IsMuted 
        {
            get => _synth.IsMuted;
            set => _synth.IsMuted = value;
        }
    }
}
