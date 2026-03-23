using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class UnityInputProviderTests
    {
        [Test]
        public void Constructor_WithBindings_DoesNotThrow()
        {
            var bindings = new (int player, int button, InputAction action)[]
            {
                (0, 0, new InputAction(binding: "<Keyboard>/leftArrow")),
                (0, 1, new InputAction(binding: "<Keyboard>/rightArrow")),
            };

            var provider = new UnityInputProvider(bindings);
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Constructor_EmptyBindings_DoesNotThrow()
        {
            var bindings = System.Array.Empty<(int player, int button, InputAction action)>();
            var provider = new UnityInputProvider(bindings);
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Poll_WithNoInput_AllButtonsFalse()
        {
            var action = new InputAction(binding: "<Keyboard>/space");
            var bindings = new (int, int, InputAction)[] { (0, 4, action) };
            var provider = new UnityInputProvider(bindings);
            var input = new Pico8Input();

            provider.Enable();
            provider.Poll(input);

            Assert.IsFalse(input.Btn(4, 0));
            provider.Disable();
        }

        [Test]
        public void Enable_DoesNotThrow()
        {
            var bindings = new (int, int, InputAction)[]
            {
                (0, 0, new InputAction(binding: "<Keyboard>/a")),
            };
            var provider = new UnityInputProvider(bindings);
            Assert.DoesNotThrow(() => provider.Enable());
            provider.Disable();
        }

        [Test]
        public void Disable_DoesNotThrow()
        {
            var bindings = new (int, int, InputAction)[]
            {
                (0, 0, new InputAction(binding: "<Keyboard>/a")),
            };
            var provider = new UnityInputProvider(bindings);
            provider.Enable();
            Assert.DoesNotThrow(() => provider.Disable());
        }

        [Test]
        public void Enable_Idempotent_CalledTwice()
        {
            var bindings = new (int, int, InputAction)[]
            {
                (0, 0, new InputAction(binding: "<Keyboard>/a")),
            };
            var provider = new UnityInputProvider(bindings);
            provider.Enable();
            Assert.DoesNotThrow(() => provider.Enable()); // second enable
            provider.Disable();
        }

        [Test]
        public void Provider_ImplementsIInputProvider()
        {
            var bindings = new (int, int, InputAction)[]
            {
                (0, 0, new InputAction(binding: "<Keyboard>/a")),
            };
            var provider = new UnityInputProvider(bindings);
            Assert.IsInstanceOf<IInputProvider>(provider);
        }

        [Test]
        public void MultiPlayer_Bindings_AllRegistered()
        {
            var bindings = new (int, int, InputAction)[]
            {
                (0, 0, new InputAction(binding: "<Keyboard>/a")),
                (0, 1, new InputAction(binding: "<Keyboard>/d")),
                (1, 0, new InputAction(binding: "<Gamepad>/dpad/left")),
                (1, 1, new InputAction(binding: "<Gamepad>/dpad/right")),
            };
            var provider = new UnityInputProvider(bindings);
            var input = new Pico8Input();

            provider.Enable();
            provider.Poll(input);

            // All buttons should be false (nothing pressed)
            Assert.IsFalse(input.Btn(0, 0));
            Assert.IsFalse(input.Btn(1, 0));
            Assert.IsFalse(input.Btn(0, 1));
            Assert.IsFalse(input.Btn(1, 1));

            provider.Disable();
        }
    }
}
