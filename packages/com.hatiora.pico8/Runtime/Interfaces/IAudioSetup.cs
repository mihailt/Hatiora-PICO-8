using UnityEngine;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Abstracts AudioSource wiring for testability.
    /// </summary>
    public interface IAudioSetup
    {
        int SampleRate { get; }
        void ConfigureSource(AudioSource source);
    }

    /// <summary>
    /// Default implementation using Unity AudioSettings and AudioClip.Create.
    /// </summary>
    public sealed class UnityAudioSetup : IAudioSetup
    {
        public static readonly UnityAudioSetup Instance = new();

        public int SampleRate => AudioSettings.outputSampleRate;

        public void ConfigureSource(AudioSource source)
        {
            source.clip = AudioClip.Create("Pico8AudioStream", 1, 1, SampleRate, false);
            source.loop = true;
            source.Play();
        }
    }
}
