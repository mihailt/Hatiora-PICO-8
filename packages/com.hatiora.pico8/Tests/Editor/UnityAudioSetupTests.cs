using NUnit.Framework;
using UnityEngine;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class UnityAudioSetupTests
    {
        [Test]
        public void Instance_IsNotNull()
        {
            Assert.IsNotNull(UnityAudioSetup.Instance);
        }

        [Test]
        public void SampleRate_IsPositive()
        {
            Assert.IsTrue(UnityAudioSetup.Instance.SampleRate > 0);
        }

        [Test]
        public void ConfigureSource_SetsClipAndLoop()
        {
            var go = new GameObject("AudioSetupTest");
            try
            {
                var source = go.AddComponent<AudioSource>();
                UnityAudioSetup.Instance.ConfigureSource(source);

                Assert.IsNotNull(source.clip);
                Assert.IsTrue(source.loop);
                Assert.IsTrue(source.isPlaying);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
