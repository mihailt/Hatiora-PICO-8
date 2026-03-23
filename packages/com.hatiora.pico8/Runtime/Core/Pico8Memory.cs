using System;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Spec-driven memory model. RAM size is computed from <see cref="EngineSpec"/>,
    /// not hardcoded to 64KB. Provides peek/poke/memcpy/memset operations.
    /// </summary>
    public sealed class Pico8Memory
    {
        public byte[] Ram { get; }
        public MemoryLayout Layout { get; }

        public Pico8Memory(EngineSpec spec)
        {
            Layout = new MemoryLayout(spec);
            Ram = new byte[Layout.TotalSize];
        }

        public byte Peek(int addr)
        {
            if (addr < 0 || addr >= Ram.Length) return 0;
            return Ram[addr];
        }

        public void Poke(int addr, byte val)
        {
            if (addr < 0 || addr >= Ram.Length) return;
            Ram[addr] = val;
        }

        public int Peek2(int addr)
        {
            if (addr < 0 || addr + 1 >= Ram.Length) return 0;
            return Ram[addr] | (Ram[addr + 1] << 8);
        }

        public void Poke2(int addr, int val)
        {
            if (addr < 0 || addr + 1 >= Ram.Length) return;
            Ram[addr]     = (byte)(val & 0xFF);
            Ram[addr + 1] = (byte)((val >> 8) & 0xFF);
        }

        public int Peek4(int addr)
        {
            if (addr < 0 || addr + 3 >= Ram.Length) return 0;
            return Ram[addr]
                 | (Ram[addr + 1] << 8)
                 | (Ram[addr + 2] << 16)
                 | (Ram[addr + 3] << 24);
        }

        public void Poke4(int addr, int val)
        {
            if (addr < 0 || addr + 3 >= Ram.Length) return;
            Ram[addr]     = (byte)(val & 0xFF);
            Ram[addr + 1] = (byte)((val >> 8) & 0xFF);
            Ram[addr + 2] = (byte)((val >> 16) & 0xFF);
            Ram[addr + 3] = (byte)((val >> 24) & 0xFF);
        }

        public void Memcpy(int dest, int src, int len)
        {
            if (len <= 0) return;
            int maxLen = Ram.Length;
            if (dest < 0 || src < 0 || dest >= maxLen || src >= maxLen) return;
            len = Math.Min(len, Math.Min(maxLen - dest, maxLen - src));
            Buffer.BlockCopy(Ram, src, Ram, dest, len);
        }

        public void Memset(int dest, byte val, int len)
        {
            if (len <= 0 || dest < 0 || dest >= Ram.Length) return;
            len = Math.Min(len, Ram.Length - dest);
            Array.Fill(Ram, val, dest, len);
        }
    }
}
