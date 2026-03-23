namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Parses PICO-8 hex map text into byte arrays for <see cref="MapStore.LoadMapData"/>.
    /// Each row is 256 hex chars; each pair of hex digits is one tile byte.
    /// </summary>
    public static class MapDataLoader
    {
        public static byte[] Parse(string hexText)
        {
            if (string.IsNullOrEmpty(hexText)) return System.Array.Empty<byte>();

            var lines = hexText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            // Each line has 256 hex chars = 128 tile bytes
            int tilesPerRow = 128;
            var result = new byte[lines.Length * tilesPerRow];

            for (int row = 0; row < lines.Length; row++)
            {
                var line = lines[row];
                int pairs = System.Math.Min(tilesPerRow, line.Length / 2);
                for (int col = 0; col < pairs; col++)
                {
                    int hi = HexVal(line[col * 2]);
                    int lo = HexVal(line[col * 2 + 1]);
                    result[row * tilesPerRow + col] = (byte)(hi * 16 + lo);
                }
            }

            return result;
        }

        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return 0;
        }
    }
}
