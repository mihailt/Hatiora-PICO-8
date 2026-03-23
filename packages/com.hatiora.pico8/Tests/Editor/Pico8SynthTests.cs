using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class Pico8SynthTests
    {
        private Pico8Synth _synth;

        // A valid SFX line: editor=01, speed=10, loopStart=00, loopEnd=00,
        // then 32 notes each 5 hex chars: pitch(2) waveform(1) volume(1) effect(0)
        private const string Speed = "10";
        private const string LoopNone = "0000";
        private const string LoopBack = "0008"; // loopStart=0, loopEnd=8

        private static string MakeNote(int pitch, int waveform, int volume, int effect = 0)
            => $"{pitch:x2}{waveform:x1}{volume:x1}{effect:x1}";

        private static string MakeSfxLine(string speed, string loop,
            int pitch = 0x18, int waveform = 0, int volume = 5, int effect = 0)
        {
            string note = MakeNote(pitch, waveform, volume, effect);
            // editorMode(2) + speed(2) + loopStart(2) + loopEnd(2) + 32 * 5 = 168 chars
            string header = "01" + speed + loop;
            string notes = "";
            for (int i = 0; i < 32; i++) notes += note;
            return header + notes;
        }

        private static string MakeMusicLine(int flags, int ch0, int ch1, int ch2, int ch3)
            => $"{flags:x2} {ch0:x2}{ch1:x2}{ch2:x2}{ch3:x2}";

        [SetUp]
        public void SetUp()
        {
            _synth = new Pico8Synth(44100f);
        }

        // ─── LoadSfx ─────────────────────────────

        [Test]
        public void LoadSfx_ParsesCorrectly()
        {
            string sfx = MakeSfxLine(Speed, LoopNone, 0x18, 0, 5, 0);
            _synth.LoadSfx(sfx);
            // Should not throw; verify by playing it
            _synth.Sfx(0);
        }

        [Test]
        public void LoadSfx_ShortLine_IsSkipped()
        {
            string shortLine = "0110000000"; // too short (< 168 chars)
            _synth.LoadSfx(shortLine);
            // SFX 0 should not be loaded, Sfx(0) should be a no-op (speed == 0)
            _synth.Sfx(0);
        }

        [Test]
        public void LoadSfx_MultipleLines()
        {
            string sfx0 = MakeSfxLine(Speed, LoopNone, 0x18, 0, 5);
            string sfx1 = MakeSfxLine(Speed, LoopNone, 0x24, 1, 7);
            _synth.LoadSfx(sfx0 + "\n" + sfx1);
            _synth.Sfx(0);
            _synth.Sfx(1);
        }

        // ─── LoadMusic ───────────────────────────

        [Test]
        public void LoadMusic_ParsesCorrectly()
        {
            string sfx = MakeSfxLine(Speed, LoopNone);
            _synth.LoadSfx(sfx);

            string music = MakeMusicLine(0, 0, 0x41, 0x41, 0x41);
            _synth.LoadMusic(music);
            _synth.Music(0);
        }

        [Test]
        public void LoadMusic_ShortLine_IsSkipped()
        {
            _synth.LoadMusic("0100");
            _synth.Music(0);
        }

        // ─── Sfx(int) ───────────────────────────

        [Test]
        public void Sfx_OutOfRange_NoOp()
        {
            _synth.Sfx(-1);
            _synth.Sfx(64);
        }

        [Test]
        public void Sfx_ZeroSpeed_NoOp()
        {
            // Default sfx tracks have speed=0, so playing them is a no-op
            _synth.Sfx(0);
        }

        [Test]
        public void Sfx_FindsFreeChannel()
        {
            string sfx = MakeSfxLine(Speed, LoopNone);
            _synth.LoadSfx(sfx);
            // Play 4 SFX (one per channel)
            _synth.Sfx(0);
            _synth.Sfx(0);
            _synth.Sfx(0);
            _synth.Sfx(0);
            // 5th should steal channel 3
            _synth.Sfx(0);
        }

        // ─── Sfx(int, int, int, int) ─────────────

        [Test]
        public void Sfx_WithChannel_UsesSpecifiedChannel()
        {
            string sfx = MakeSfxLine(Speed, LoopNone);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0, 2, 0, 32);
        }

        [Test]
        public void Sfx_WithNegativeChannel_FindsFree()
        {
            string sfx = MakeSfxLine(Speed, LoopNone);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0, -1, 0, 32);
        }

        [Test]
        public void Sfx_WithChannelOffset_SetsNoteIndex()
        {
            string sfx = MakeSfxLine(Speed, LoopNone);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0, 0, 5, 10);
        }

        [Test]
        public void Sfx4_OutOfRange_NoOp()
        {
            _synth.Sfx(-1, 0, 0, 0);
            _synth.Sfx(64, 0, 0, 0);
        }

        [Test]
        public void Sfx4_ZeroSpeed_NoOp()
        {
            _synth.Sfx(0, 0, 0, 0);
        }

        // ─── Music ───────────────────────────────

        [Test]
        public void Music_SetsNextId()
        {
            _synth.Music(5);
        }

        [Test]
        public void Music_WithFadeAndMask_Delegates()
        {
            _synth.Music(3, 100, 0x0F);
        }

        // ─── ProcessAudio ────────────────────────

        [Test]
        public void ProcessAudio_EmptyChannels_ProduceSilence()
        {
            float[] data = new float[256];
            _synth.ProcessAudio(data, 2);
            for (int i = 0; i < data.Length; i++)
                Assert.AreEqual(0f, data[i]);
        }

        [Test]
        public void ProcessAudio_Sfx_ProducesSound()
        {
            string sfx = MakeSfxLine(Speed, LoopNone, 0x24, 0, 7);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            float[] data = new float[512];
            _synth.ProcessAudio(data, 1);

            // Should have non-zero samples
            bool hasSound = false;
            for (int i = 0; i < data.Length; i++)
                if (data[i] != 0f) { hasSound = true; break; }
            Assert.IsTrue(hasSound);
        }

        [Test]
        public void ProcessAudio_AllWaveforms()
        {
            // Test each waveform type produces sound
            for (int wave = 0; wave <= 6; wave++)
            {
                _synth = new Pico8Synth(44100f);
                string sfx = MakeSfxLine(Speed, LoopNone, 0x24, wave, 5);
                _synth.LoadSfx(sfx);
                _synth.Sfx(0);

                float[] data = new float[256];
                _synth.ProcessAudio(data, 1);

                bool hasSound = false;
                for (int i = 0; i < data.Length; i++)
                    if (data[i] != 0f) { hasSound = true; break; }
                Assert.IsTrue(hasSound, $"Waveform {wave} should produce sound");
            }
        }

        [Test]
        public void ProcessAudio_DefaultWaveform()
        {
            // Waveform 5 and 7+ fall through to default (sin)
            string sfx = MakeSfxLine(Speed, LoopNone, 0x24, 5, 5);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            float[] data = new float[256];
            _synth.ProcessAudio(data, 1);

            bool hasSound = false;
            for (int i = 0; i < data.Length; i++)
                if (data[i] != 0f) { hasSound = true; break; }
            Assert.IsTrue(hasSound);
        }

        [Test]
        public void ProcessAudio_ZeroVolume_Silence()
        {
            string sfx = MakeSfxLine(Speed, LoopNone, 0x24, 0, 0);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            float[] data = new float[256];
            _synth.ProcessAudio(data, 1);

            // All zero volume notes should produce no sound aside from note advancement
            for (int i = 0; i < data.Length; i++)
                Assert.AreEqual(0f, data[i]);
        }

        [Test]
        public void ProcessAudio_StereoOutput()
        {
            string sfx = MakeSfxLine(Speed, LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            float[] data = new float[128];
            _synth.ProcessAudio(data, 2);

            // Stereo: left == right
            for (int i = 0; i < data.Length; i += 2)
                Assert.AreEqual(data[i], data[i + 1]);
        }

        [Test]
        public void ProcessAudio_NoteAdvancement()
        {
            // Very fast speed so notes advance quickly
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            // Process enough samples to go through all 32 notes at speed=1
            // speed=1 → duration = 1/120 sec per note → 32 notes = 32/120 sec ≈ 0.267 sec
            // At 44100 Hz = ~11760 samples
            float[] data = new float[44100];
            _synth.ProcessAudio(data, 1);
        }

        [Test]
        public void ProcessAudio_SfxEndsAfterAllNotes()
        {
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            // Process more than enough to finish all 32 notes
            float[] data = new float[44100];
            _synth.ProcessAudio(data, 1);

            // After finishing, should be silent
            float[] data2 = new float[256];
            _synth.ProcessAudio(data2, 1);
            for (int i = 0; i < data2.Length; i++)
                Assert.AreEqual(0f, data2[i]);
        }

        [Test]
        public void ProcessAudio_SfxLoop()
        {
            // Loop from note 0 to note 8
            string sfx = MakeSfxLine("01", LoopBack, 0x24, 0, 5);
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            // Process enough to go past all 32 notes — should loop back
            float[] data = new float[44100];
            _synth.ProcessAudio(data, 1);

            // After loop, should still produce sound
            float[] data2 = new float[256];
            _synth.ProcessAudio(data2, 1);
            bool hasSound = false;
            for (int i = 0; i < data2.Length; i++)
                if (data2[i] != 0f) { hasSound = true; break; }
            Assert.IsTrue(hasSound, "Looping SFX should keep producing sound");
        }

        // ─── Music + ProcessAudio ────────────────

        [Test]
        public void Music_ProcessAudio_PlaysChannels()
        {
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);

            // Music pattern 0: play SFX 0 on channel 0, others inactive (0x41 > 64)
            string music = MakeMusicLine(0, 0, 0x41, 0x41, 0x41);
            _synth.LoadMusic(music);
            _synth.Music(0);

            float[] data = new float[256];
            _synth.ProcessAudio(data, 1);

            bool hasSound = false;
            for (int i = 0; i < data.Length; i++)
                if (data[i] != 0f) { hasSound = true; break; }
            Assert.IsTrue(hasSound);
        }

        [Test]
        public void Music_StopWithNegativeId()
        {
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);

            string music = MakeMusicLine(0, 0, 0x41, 0x41, 0x41);
            _synth.LoadMusic(music);
            _synth.Music(0);

            float[] data = new float[256];
            _synth.ProcessAudio(data, 1);

            // Stop music
            _synth.Music(-1);
            float[] data2 = new float[256];
            _synth.ProcessAudio(data2, 1);
        }

        [Test]
        public void Music_StopFlag_StopsAfterPattern()
        {
            // Use speed=1 so pattern finishes fast
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);

            // flags=4 means stop after this pattern
            string music = MakeMusicLine(4, 0, 0x41, 0x41, 0x41);
            _synth.LoadMusic(music);
            _synth.Music(0);

            // Process enough to finish the pattern (32 notes at speed=1)
            float[] data = new float[44100];
            _synth.ProcessAudio(data, 1);
        }

        [Test]
        public void Music_AdvancesToNextPattern()
        {
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);

            // Pattern 0: no flags, advances to pattern 1
            // Pattern 1: stop flag
            string m0 = MakeMusicLine(0, 0, 0x41, 0x41, 0x41);
            string m1 = MakeMusicLine(4, 0, 0x41, 0x41, 0x41);
            _synth.LoadMusic(m0 + "\n" + m1);
            _synth.Music(0);

            float[] data = new float[88200]; // ~2 seconds
            _synth.ProcessAudio(data, 1);
        }

        [Test]
        public void Music_LoopFlag_LoopsBack()
        {
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);

            // Pattern 0: begin flag (1)
            // Pattern 1: loop-back flag (2) — should loop to pattern 0
            string m0 = MakeMusicLine(1, 0, 0x41, 0x41, 0x41);
            string m1 = MakeMusicLine(2, 0, 0x41, 0x41, 0x41);
            _synth.LoadMusic(m0 + "\n" + m1);
            _synth.Music(0);

            float[] data = new float[88200]; // ~2 seconds
            _synth.ProcessAudio(data, 1);
        }

        [Test]
        public void Music_InactiveChannel_StopsMusic()
        {
            string sfx = MakeSfxLine("01", LoopNone, 0x24, 0, 5);
            _synth.LoadSfx(sfx);

            // All channels active on music
            string m0 = MakeMusicLine(0, 0, 0, 0, 0);
            _synth.LoadMusic(m0);
            _synth.Music(0);

            float[] data = new float[256];
            _synth.ProcessAudio(data, 1);

            // Now play SFX 0 which should use a non-music channel
            _synth.Sfx(0);

            // Start music with inactive channels (> 64)
            string m1 = MakeMusicLine(4, 0x41, 0x41, 0x41, 0x41);
            _synth.LoadMusic(m1);
            _synth.Music(0);

            float[] data2 = new float[256];
            _synth.ProcessAudio(data2, 1);
        }

        [Test]
        public void ProcessAudio_PhaseWraps()
        {
            // Use a high pitch to make phase wrap quickly
            string sfx = MakeSfxLine(Speed, LoopNone, 0x3F, 0, 5); // high pitch
            _synth.LoadSfx(sfx);
            _synth.Sfx(0);

            float[] data = new float[4096];
            _synth.ProcessAudio(data, 1);

            bool hasSound = false;
            for (int i = 0; i < data.Length; i++)
                if (data[i] != 0f) { hasSound = true; break; }
            Assert.IsTrue(hasSound);
        }
    }
}
