using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Hatiora.Pico8;
using Hatiora.Pico8.Unity;
using Hatiora.Pico8.Launcher;
using Hatiora.Pico8.Basic;
using System;

/// <summary>
/// App using the new V2 architecture: Pico8Builder + CartridgeV2.
/// </summary>
[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AudioSource))]
public class App : MonoBehaviour
{
    private Pico8AudioProvider _audioProvider;
    
    private VisualElement _root;
    private InputProvider _inputProvider;

    private Pico8Builder _builder;

    void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root
            .Style((s) => {
                s.flexGrow = 1;
                s.alignItems = Align.Center;
                s.justifyContent = Justify.Center;
                s.backgroundColor = Color.black;
            });

        _audioProvider = new Pico8AudioProvider(GetComponent<AudioSource>());

        // Mouse → virtual coordinate conversion
        int virtW = Screen.width / 4;
        int virtH = Screen.height / 4;
        (float x, float y)? MouseProvider()
        {
            var mouse = Mouse.current;
            if (mouse == null) return null;
            var pos = mouse.position.ReadValue();
            // Scale screen coords to virtual coords; flip Y (Unity Y=0 bottom, virtual Y=0 top)
            return (pos.x * virtW / Screen.width, (Screen.height - pos.y) * virtH / Screen.height);
        }
 
        // Input bindings — using InputAction
        _inputProvider = new InputProvider(MouseProvider,
            (0, kb => kb.leftArrowKey.isPressed  || kb.aKey.isPressed,  pad => pad.leftStick.left.isPressed  || pad.dpad.left.isPressed),
            (1, kb => kb.rightArrowKey.isPressed || kb.dKey.isPressed,  pad => pad.leftStick.right.isPressed || pad.dpad.right.isPressed),
            (2, kb => kb.upArrowKey.isPressed    || kb.wKey.isPressed,  pad => pad.leftStick.up.isPressed    || pad.dpad.up.isPressed),
            (3, kb => kb.downArrowKey.isPressed  || kb.sKey.isPressed,  pad => pad.leftStick.down.isPressed  || pad.dpad.down.isPressed),
            (4, kb => kb.zKey.isPressed || kb.cKey.isPressed || kb.nKey.isPressed || (Mouse.current?.leftButton.isPressed ?? false),  pad => pad.buttonEast.isPressed || pad.rightTrigger.isPressed),
            (5, kb => kb.xKey.isPressed || kb.vKey.isPressed || kb.mKey.isPressed || (Mouse.current?.rightButton.isPressed ?? false),  pad => pad.buttonSouth.isPressed || pad.leftTrigger.isPressed),
            (6, kb => kb.escapeKey.isPressed || kb.enterKey.isPressed,              pad => pad.startButton.isPressed),
            (7, kb => kb.tabKey.isPressed    || kb.pKey.isPressed,                  pad => pad.selectButton.isPressed),
            // Right stick (aim) — buttons 8-11
            (8,  kb => false, pad => pad.rightStick.left.isPressed),
            (9,  kb => false, pad => pad.rightStick.right.isPressed),
            (10, kb => false, pad => pad.rightStick.up.isPressed),
            (11, kb => false, pad => pad.rightStick.down.isPressed),
            (100, kb => kb.digit1Key.isPressed, pad => pad.buttonNorth.isPressed));

        // 1. System-wide engine spec (buttons, SFX, memory — shared by all cartridges)
        var baseSpec = new EngineSpec
        {
            MemoryMap = MemoryMap.Pico8,
            // System buttons
            SystemStartButton  = 6,
            SystemSelectButton = 7,
            // System SFX (Porklike asset IDs)
            SfxNavigate  = 56,
            SfxConfirm   = 54,
            SfxCancel    = 53,
            SfxBootReady = 51
        };

        // 2. Cartridge catalog (256×256 virtual canvas, 1024×1024 physical display)
        var registry = new CartridgeRegistry(baseSpec)
            .Register<Hatiora.Pico8.Api.ApiCartridge>("api", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Automata.AutomataCartridge>("automata", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Bounce.BounceCartridge>("bounce", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Cast.CastCartridge>("cast", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Collide.CollideCartridge>("collide", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Coop.CoopCartridge>("coop", Screen.width / 4, Screen.height / 4, Screen.width, Screen.height)
            .Register<Hatiora.Pico8.Dots3D.Dots3DCartridge>("dots3d", 128, 128, 1024, 1024)
            .Register<Hatiora.Pico8.Drippy.DrippyCartridge>("drippy", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Hello.HelloCartridge>("hello", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Jelpi.JelpiCartridge>("jelpi", 128, 128, 1024, 1024)
            .Register<Hatiora.Pico8.Sort.SortCartridge>("sort", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Wander.WanderCartridge>("wander", 256, 256, 1024, 1024)
            .Register<Hatiora.Pico8.Waves.WavesCartridge>("waves", 256, 256, 1024, 1024)
            .Register<BasicCartridge>("basic", Screen.width / 4, Screen.height / 4, Screen.width, Screen.height);

        // 3. Wire everything together and boot
        _builder = new Pico8Builder()
            .WithAudio(_audioProvider)
            .WithInput(_inputProvider)
            .WithRegistry(registry)
            .WithLauncher<LauncherCartridge>(Screen.width / 4, Screen.height / 4, Screen.width, Screen.height)
            .Mount(_root, "hello");
    }

    void Update() => _builder?.Tick(Time.deltaTime);

    void OnAudioFilterRead(float[] data, int channels) => _audioProvider?.ProcessAudio(data, channels);
}
