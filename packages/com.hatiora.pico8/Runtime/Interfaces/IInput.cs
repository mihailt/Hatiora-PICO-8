namespace Hatiora.Pico8
{
    public interface IInput
    {
        void SetButton(int button, int player, bool pressed);
        bool Btn(int button, int player = 0);
        bool Btnp(int button, int player = 0);

        // Mouse position (virtual coordinates)
        void SetMouse(float x, float y);
        float MouseX { get; }
        float MouseY { get; }

        // Analog axes (stick: 0=left, 1=right)
        void SetAxis(int player, int stick, float x, float y);
        float GetAxisX(int player, int stick);
        float GetAxisY(int player, int stick);
    }
}
