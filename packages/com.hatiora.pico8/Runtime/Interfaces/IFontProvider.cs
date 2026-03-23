namespace Hatiora.Pico8
{
    public interface IFontProvider
    {
        void Prepare(string str);
        GlyphData GetGlyph(char c);
    }
}
