using System;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Multi-bank sprite storage. Banks grow dynamically.
    /// Bank 0 is the default for PICO-8 compatibility.
    /// </summary>
    public interface ISpriteStore
    {
        int BankCount { get; }
        void LoadBank(int bankIndex, byte[] data, int width, int height);
        byte GetPixel(int bank, int x, int y);
        void SetPixel(int bank, int x, int y, byte colorIndex);
        ReadOnlySpan<byte> GetRow(int bank, int y);
        int GetBankWidth(int bank);
        int GetBankHeight(int bank);
    }
}
