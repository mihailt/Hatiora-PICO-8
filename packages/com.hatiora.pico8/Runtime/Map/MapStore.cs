namespace Hatiora.Pico8
{
    /// <summary>
    /// Tile map data access backed by <see cref="Pico8Memory"/>.
    /// Reads/writes map cells and sprite flags directly from RAM.
    /// Stores ROM snapshots for <see cref="Reload"/> support.
    /// </summary>
    public sealed class MapStore : IMap
    {
        private readonly EngineSpec _spec;
        private readonly Pico8Memory _mem;

        private byte[] _romMap;
        private byte[] _romFlags;

        public MapStore(EngineSpec spec, Pico8Memory mem)
        {
            _spec = spec;
            _mem = mem;
        }

        public byte Get(int x, int y)
        {
            if (x < 0 || x >= _spec.MapWidth || y < 0 || y >= 64) return 0;
            if (y < _spec.MapHeight)
                return _mem.Ram[_mem.Layout.MapStart + y * _spec.MapWidth + x];
            // Shared region: rows 32-63 map to lower half of GFX memory
            return _mem.Ram[_mem.Layout.GfxStart + y * _spec.MapWidth + x];
        }

        public void Set(int x, int y, byte val)
        {
            if (x < 0 || x >= _spec.MapWidth || y < 0 || y >= 64) return;
            if (y < _spec.MapHeight)
                _mem.Ram[_mem.Layout.MapStart + y * _spec.MapWidth + x] = val;
            else
                _mem.Ram[_mem.Layout.GfxStart + y * _spec.MapWidth + x] = val;
        }

        public byte GetFlag(int spriteIndex)
        {
            if (spriteIndex < 0 || spriteIndex >= _spec.SpritesPerBank) return 0;
            return _mem.Ram[_mem.Layout.FlagsStart + spriteIndex];
        }

        public bool GetFlag(int spriteIndex, int flagBit)
        {
            return (GetFlag(spriteIndex) & (1 << flagBit)) != 0;
        }

        public void SetFlag(int spriteIndex, int flagBit, bool val)
        {
            if (spriteIndex < 0 || spriteIndex >= _spec.SpritesPerBank) return;
            int addr = _mem.Layout.FlagsStart + spriteIndex;
            if (val)
                _mem.Ram[addr] |= (byte)(1 << flagBit);
            else
                _mem.Ram[addr] &= (byte)~(1 << flagBit);
        }

        /// <summary>Bulk-load map data into memory. Saves a ROM snapshot for Reload.</summary>
        public void LoadMapData(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            int len = System.Math.Min(data.Length, _spec.MapWidth * _spec.MapHeight);
            _romMap = new byte[len];
            System.Buffer.BlockCopy(data, 0, _romMap, 0, len);
            System.Buffer.BlockCopy(data, 0, _mem.Ram, _mem.Layout.MapStart, len);
        }

        /// <summary>Bulk-load sprite flag data into memory. Saves a ROM snapshot for Reload.</summary>
        public void LoadFlagData(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            int len = System.Math.Min(data.Length, _spec.SpritesPerBank);
            _romFlags = new byte[len];
            System.Buffer.BlockCopy(data, 0, _romFlags, 0, len);
            System.Buffer.BlockCopy(data, 0, _mem.Ram, _mem.Layout.FlagsStart, len);
        }

        /// <summary>Restores map RAM to the original ROM data.</summary>
        public void ReloadMap()
        {
            if (_romMap != null)
                System.Buffer.BlockCopy(_romMap, 0, _mem.Ram, _mem.Layout.MapStart, _romMap.Length);
        }

        /// <summary>Restores flag RAM to the original ROM data.</summary>
        public void ReloadFlags()
        {
            if (_romFlags != null)
                System.Buffer.BlockCopy(_romFlags, 0, _mem.Ram, _mem.Layout.FlagsStart, _romFlags.Length);
        }

        /// <summary>Restores all ROM data (map + flags) to their original loaded state.</summary>
        public void Reload()
        {
            ReloadMap();
            ReloadFlags();
        }
    }
}
