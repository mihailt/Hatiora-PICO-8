using System;
using UnityEngine;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Real-time PICO-8 audio synthesizer. Plain class implementing IAudio.
    /// Wire OnAudioFilterRead from a MonoBehaviour to ProcessAudio().
    /// </summary>
    public class Pico8Synth : IAudio
    {
        private const int MaxSfx = 64;
        private const int NotesPerSfx = 32;

        public struct Note
        {
            public int pitch, waveform, volume, effect;
        }

        public struct SfxTrack
        {
            public int editorMode, speed, loopStart, loopEnd;
            public Note[] notes;
        }

        public struct MusicTrack
        {
            public int flags;
            public int[] sfx; // 4 channels
        }

        private readonly SfxTrack[] _sfxTracks = new SfxTrack[MaxSfx];
        private readonly SfxTrack[] _systemSfxTracks = new SfxTrack[MaxSfx];
        private readonly MusicTrack[] _musicTracks = new MusicTrack[MaxSfx];
        private readonly float _sampleRate;

        private int _currentMusicId = -1;
        private int _nextMusicId = -1;
        private bool _startNewMusic;

        private class ChannelState
        {
            public int sfxId = -1;
            public float noteTime;
            public int noteIndex;
            public float phase;
            public bool isMusic;
        }

        private readonly ChannelState[] _channels = { new(), new(), new(), new() };
        private readonly ChannelState _systemChannel = new();
        private readonly System.Random _rnd = new();

        public Pico8Synth(float sampleRate) => _sampleRate = sampleRate;

        // ─── Master Audio Properties ─────────────────────────────────────────

        private int _volume = 8;
        public int Volume
        {
            get => _volume;
            set => _volume = Mathf.Clamp(value, 0, 8);
        }

        public bool IsMuted { get; set; } = false;

        // ─── Data Loading ───────────────────────────────────

        public void LoadSfx(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Mathf.Min(MaxSfx, lines.Length); i++)
            {
                var line = lines[i];
                if (line.Length < 168) continue;

                var t = new SfxTrack
                {
                    editorMode = Convert.ToInt32(line.Substring(0, 2), 16),
                    speed      = Convert.ToInt32(line.Substring(2, 2), 16),
                    loopStart  = Convert.ToInt32(line.Substring(4, 2), 16),
                    loopEnd    = Convert.ToInt32(line.Substring(6, 2), 16),
                    notes      = new Note[NotesPerSfx]
                };

                for (int n = 0; n < NotesPerSfx; n++)
                {
                    int off = 8 + n * 5;
                    t.notes[n].pitch    = Convert.ToInt32(line.Substring(off, 2), 16);
                    t.notes[n].waveform = Convert.ToInt32(line.Substring(off + 2, 1), 16);
                    t.notes[n].volume   = Convert.ToInt32(line.Substring(off + 3, 1), 16);
                    t.notes[n].effect   = Convert.ToInt32(line.Substring(off + 4, 1), 16);
                }

                _sfxTracks[i] = t;
            }
        }

        public void LoadMusic(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Mathf.Min(MaxSfx, lines.Length); i++)
            {
                var line = lines[i].Trim();
                if (line.Length < 11) continue;

                var m = new MusicTrack
                {
                    flags = Convert.ToInt32(line.Substring(0, 2), 16),
                    sfx = new int[4]
                };
                for (int c = 0; c < 4; c++)
                    m.sfx[c] = Convert.ToInt32(line.Substring(3 + c * 2, 2), 16);

                _musicTracks[i] = m;
            }
        }

        // ─── System SFX (launcher/pause menu) ───────────────

        public void LoadSystemSfx(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Mathf.Min(MaxSfx, lines.Length); i++)
            {
                var line = lines[i];
                if (line.Length < 168) continue;

                var t = new SfxTrack
                {
                    editorMode = Convert.ToInt32(line.Substring(0, 2), 16),
                    speed      = Convert.ToInt32(line.Substring(2, 2), 16),
                    loopStart  = Convert.ToInt32(line.Substring(4, 2), 16),
                    loopEnd    = Convert.ToInt32(line.Substring(6, 2), 16),
                    notes      = new Note[NotesPerSfx]
                };

                for (int n = 0; n < NotesPerSfx; n++)
                {
                    int off = 8 + n * 5;
                    t.notes[n].pitch    = Convert.ToInt32(line.Substring(off, 2), 16);
                    t.notes[n].waveform = Convert.ToInt32(line.Substring(off + 2, 1), 16);
                    t.notes[n].volume   = Convert.ToInt32(line.Substring(off + 3, 1), 16);
                    t.notes[n].effect   = Convert.ToInt32(line.Substring(off + 4, 1), 16);
                }

                _systemSfxTracks[i] = t;
            }
        }

        public void SystemSfx(int n)
        {
            if (n < 0 || n >= MaxSfx || _systemSfxTracks[n].speed == 0) return;

            _systemChannel.sfxId = n;
            _systemChannel.noteIndex = 0;
            _systemChannel.noteTime = 0f;
            _systemChannel.phase = 0f;
            _systemChannel.isMusic = false;
        }

        // ─── IAudio ─────────────────────────────────────────

        public void Sfx(int n)
        {
            if (n < 0 || n >= MaxSfx || _sfxTracks[n].speed == 0) return;

            int ch = 3;
            for (int i = 0; i < 4; i++)
                if (_channels[i].sfxId == -1) { ch = i; break; }

            _channels[ch].sfxId = n;
            _channels[ch].noteIndex = 0;
            _channels[ch].noteTime = 0f;
            _channels[ch].phase = 0f;
            _channels[ch].isMusic = false;
        }

        public void Sfx(int n, int channel, int offset, int length)
        {
            if (n < 0 || n >= MaxSfx || _sfxTracks[n].speed == 0) return;

            int ch = channel >= 0 && channel < 4 ? channel : 3;
            for (int i = 0; channel < 0 && i < 4; i++)
                if (_channels[i].sfxId == -1) { ch = i; break; }

            _channels[ch].sfxId = n;
            _channels[ch].noteIndex = offset;
            _channels[ch].noteTime = 0f;
            _channels[ch].phase = 0f;
            _channels[ch].isMusic = false;
        }

        public void Music(int n)
        {
            _nextMusicId = n;
            _startNewMusic = true;
        }

        public void Music(int n, int fadeLen, int channelMask)
        {
            // fadeLen and channelMask not yet implemented
            Music(n);
        }

        // ─── Call this from MonoBehaviour.OnAudioFilterRead ─

        public void ProcessAudio(float[] data, int channelsOut)
        {
            for (int i = 0; i < data.Length; i += channelsOut)
            {
                float mixed = 0f;
                bool musicPatternFinished = false;

                if (_startNewMusic)
                {
                    _currentMusicId = _nextMusicId;
                    _startNewMusic = false;

                    if (_currentMusicId >= 0 && _currentMusicId < MaxSfx)
                    {
                        var m = _musicTracks[_currentMusicId];
                        if (m.sfx == null) { _startNewMusic = false; continue; }
                        for (int c = 0; c < 4; c++)
                        {
                            int sid = m.sfx[c];
                            if (sid < MaxSfx)
                            {
                                _channels[c].sfxId = sid;
                                _channels[c].noteIndex = 0;
                                _channels[c].noteTime = 0f;
                                _channels[c].phase = 0f;
                                _channels[c].isMusic = true;
                            }
                            else if (_channels[c].isMusic)
                            {
                                _channels[c].sfxId = -1;
                            }
                        }
                    }
                    else
                    {
                        for (int c = 0; c < 4; c++)
                            if (_channels[c].isMusic) _channels[c].sfxId = -1;
                    }
                }

                for (int c = 0; c < 4; c++)
                {
                    var ch = _channels[c];
                    if (ch.sfxId == -1) continue;

                    var track = _sfxTracks[ch.sfxId];
                    var (sample, finished) = ProcessChannel(ch, track);
                    mixed += sample;
                    if (finished) musicPatternFinished = true;
                }

                // System channel (uses _systemSfxTracks)
                if (_systemChannel.sfxId != -1)
                {
                    var sysTrack = _systemSfxTracks[_systemChannel.sfxId];
                    mixed += ProcessChannel(_systemChannel, sysTrack).sample;
                }

                if (musicPatternFinished && _currentMusicId >= 0 && _currentMusicId < MaxSfx)
                {
                    var m = _musicTracks[_currentMusicId];
                    if ((m.flags & 4) != 0)
                    {
                        _currentMusicId = -1;
                        for (int c = 0; c < 4; c++)
                            if (_channels[c].isMusic) _channels[c].sfxId = -1;
                    }
                    else
                    {
                        if ((m.flags & 2) != 0)
                        {
                            int loopStart = _currentMusicId;
                            while (loopStart > 0 && (_musicTracks[loopStart].flags & 1) == 0)
                                loopStart--;
                            _nextMusicId = loopStart;
                        }
                        else
                        {
                            _nextMusicId = _currentMusicId + 1;
                        }
                        _startNewMusic = true;
                    }
                }

                if (IsMuted)
                {
                    mixed = 0f;
                }
                else if (Volume < 8)
                {
                    mixed *= (Volume / 8f);
                }

                for (int o = 0; o < channelsOut; o++)
                    data[i + o] = mixed;
            }
        }

        /// <summary>
        /// Processes a single channel for one audio sample, advancing its note state.
        /// Returns the mixed sample contribution and whether a music pattern finished.
        /// </summary>
        private (float sample, bool musicFinished) ProcessChannel(ChannelState ch, SfxTrack track)
        {
            float result = 0f;
            bool musicFinished = false;
            float durationPerNote = track.speed / 120f;
            if (durationPerNote <= 0) durationPerNote = 0.1f;

            var note = track.notes[ch.noteIndex];

            if (note.volume > 0)
            {
                float freq = 65.406f * Mathf.Pow(2f, note.pitch / 12f);
                ch.phase += freq / _sampleRate;
                if (ch.phase > 1f) ch.phase -= 1f;

                float sample;
                switch (note.waveform)
                {
                    case 0: sample = Mathf.PingPong(ch.phase * 2f, 1f) * 2f - 1f; break;
                    case 1: sample = ch.phase < 0.5f ? -1f : 1f; break;
                    case 2: sample = ch.phase * 2f - 1f; break;
                    case 3: sample = ch.phase < 0.5f ? 1f : -1f; break;
                    case 4: sample = ch.phase < 0.25f ? 1f : -1f; break;
                    case 6: sample = (float)_rnd.NextDouble() * 2f - 1f; break;
                    default: sample = Mathf.Sin(ch.phase * Mathf.PI * 2f); break;
                }

                result = sample * (note.volume / 7f) * 0.15f;
            }

            ch.noteTime += 1f / _sampleRate;
            if (ch.noteTime >= durationPerNote)
            {
                ch.noteTime -= durationPerNote;
                ch.noteIndex++;

                if (ch.noteIndex >= NotesPerSfx)
                {
                    if (!ch.isMusic)
                    {
                        if (track.loopEnd > 0 && track.loopEnd >= track.loopStart)
                            ch.noteIndex = track.loopStart;
                        else
                            ch.sfxId = -1;
                    }
                    else
                    {
                        musicFinished = true;
                        ch.noteIndex = 0;
                    }
                }
            }

            return (result, musicFinished);
        }
    }
}
