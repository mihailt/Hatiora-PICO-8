using System;
using System.Collections.Generic;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Multi-bank sprite storage. Each bank holds palette-indexed pixel data.
    /// Banks grow dynamically — no fixed count.
    /// </summary>
    public sealed class SpriteStore : ISpriteStore
    {
        private readonly EngineSpec _spec;
        private readonly List<SpriteBank> _banks = new();

        public int BankCount => _banks.Count;

        public SpriteStore(EngineSpec spec)
        {
            _spec = spec;
        }

        public void LoadBank(int bankIndex, byte[] data, int width, int height)
        {
            while (_banks.Count <= bankIndex)
                _banks.Add(new SpriteBank(_spec.SheetWidth, _spec.SheetHeight));

            _banks[bankIndex] = new SpriteBank(width, height, data);
        }

        public byte GetPixel(int bank, int x, int y)
        {
            if (bank < 0 || bank >= _banks.Count) return 0;
            var b = _banks[bank];
            if (x < 0 || y < 0 || x >= b.Width || y >= b.Height) return 0;
            return b.Data[y * b.Width + x];
        }

        public void SetPixel(int bank, int x, int y, byte colorIndex)
        {
            if (bank < 0 || bank >= _banks.Count) return;
            var b = _banks[bank];
            if (x < 0 || y < 0 || x >= b.Width || y >= b.Height) return;
            b.Data[y * b.Width + x] = colorIndex;
        }

        public ReadOnlySpan<byte> GetRow(int bank, int y)
        {
            if (bank < 0 || bank >= _banks.Count) return ReadOnlySpan<byte>.Empty;
            var b = _banks[bank];
            if (y < 0 || y >= b.Height) return ReadOnlySpan<byte>.Empty;
            return new ReadOnlySpan<byte>(b.Data, y * b.Width, b.Width);
        }

        public int GetBankWidth(int bank)
        {
            if (bank < 0 || bank >= _banks.Count) return 0;
            return _banks[bank].Width;
        }

        public int GetBankHeight(int bank)
        {
            if (bank < 0 || bank >= _banks.Count) return 0;
            return _banks[bank].Height;
        }

        private class SpriteBank
        {
            public int Width;
            public int Height;
            public byte[] Data;

            public SpriteBank(int w, int h)
            {
                Width = w;
                Height = h;
                Data = new byte[w * h];
            }

            public SpriteBank(int w, int h, byte[] data)
            {
                Width = w;
                Height = h;
                Data = new byte[w * h];
                Buffer.BlockCopy(data, 0, Data, 0, Math.Min(data.Length, Data.Length));
            }
        }
    }
}
