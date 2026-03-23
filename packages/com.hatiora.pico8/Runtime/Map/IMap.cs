namespace Hatiora.Pico8
{
    /// <summary>
    /// Tile map data access: mget/mset for map cells, fget/fset for sprite flags.
    /// </summary>
    public interface IMap
    {
        byte Get(int x, int y);
        void Set(int x, int y, byte val);
        byte GetFlag(int spriteIndex);
        bool GetFlag(int spriteIndex, int flagBit);
        void SetFlag(int spriteIndex, int flagBit, bool val);

        /// <summary>Restores all ROM data (map + flags) to their original loaded state.</summary>
        void Reload();
    }
}
