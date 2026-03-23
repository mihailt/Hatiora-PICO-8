using NUnit.Framework;
using UnityEngine;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class Pico8AudioProviderTests
    {
        private Pico8Synth _synth;

        [SetUp]
        public void SetUp()
        {
            _synth = new Pico8Synth(44100);
        }

        // ─── Synth-only constructor ───

        [Test]
        public void Constructor_Synth_CreatesProvider()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.IsNotNull(provider);
        }

        [Test]
        public void LoadSfx_DelegatesToSynth()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.DoesNotThrow(() => provider.LoadSfx(""));
        }

        [Test]
        public void LoadMusic_DelegatesToSynth()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.DoesNotThrow(() => provider.LoadMusic(""));
        }

        [Test]
        public void Sfx_SingleArg_DelegatesToSynth()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.DoesNotThrow(() => provider.Sfx(0));
        }

        [Test]
        public void Sfx_FourArgs_DelegatesToSynth()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.DoesNotThrow(() => provider.Sfx(0, -1, 0, 32));
        }

        [Test]
        public void Music_SingleArg_DelegatesToSynth()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.DoesNotThrow(() => provider.Music(0));
        }

        [Test]
        public void Music_ThreeArgs_DelegatesToSynth()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.DoesNotThrow(() => provider.Music(0, 0, 0));
        }

        [Test]
        public void ProcessAudio_DelegatesToSynth()
        {
            var provider = new Pico8AudioProvider(_synth);
            Assert.DoesNotThrow(() => provider.ProcessAudio(new float[512], 1));
        }

        // ─── AudioSource + IAudioSetup constructor ───

        [Test]
        public void Constructor_AudioSourceWithMockSetup_Works()
        {
            var go = new GameObject("AudioTest");
            try
            {
                var source = go.AddComponent<AudioSource>();
                var setup = new MockAudioSetup();
                var provider = new Pico8AudioProvider(source, setup);
                Assert.IsNotNull(provider);
                Assert.IsTrue(setup.Configured);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Constructor_AudioSourceOnly_UsesDefaultSetup()
        {
            var go = new GameObject("AudioDefaultTest");
            try
            {
                var source = go.AddComponent<AudioSource>();
                var provider = new Pico8AudioProvider(source);
                Assert.IsNotNull(provider);
                Assert.IsTrue(source.loop);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ─── Mock ───

        private class MockAudioSetup : IAudioSetup
        {
            public bool Configured;
            public int SampleRate => 44100;

            public void ConfigureSource(AudioSource source)
            {
                Configured = true;
                // Don't actually create AudioClip or call Play in tests
            }
        }
    }
}
