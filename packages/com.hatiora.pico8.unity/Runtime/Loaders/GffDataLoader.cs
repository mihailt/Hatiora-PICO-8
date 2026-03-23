namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Parses PICO-8 hex sprite flag text (__gff__) into byte arrays for <see cref="MapStore.LoadFlagData"/>.
    /// Each pair of hex digits is one byte of sprite flags (8 flags per sprite, 256 sprites max).
    /// </summary>
    public static class GffDataLoader
    {
        public static byte[] Parse(string hexText)
        {
            if (string.IsNullOrEmpty(hexText)) return System.Array.Empty<byte>();

            // Strip newlines and whitespace to get raw hex stream
            var sb = new System.Text.StringBuilder();
            foreach (char c in hexText)
            {
                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                    sb.Append(c);
            }

            string hex = sb.ToString();
            int count = hex.Length / 2;
            var result = new byte[count];

            for (int i = 0; i < count; i++)
            {
                int hi = HexVal(hex[i * 2]);
                int lo = HexVal(hex[i * 2 + 1]);
                result[i] = (byte)(hi * 16 + lo);
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
