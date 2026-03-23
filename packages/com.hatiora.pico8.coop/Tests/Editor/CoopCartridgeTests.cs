using NUnit.Framework;
using UnityEngine;
// using Hatiora.Pico8.Coop; // Uncomment if different from default

namespace Hatiora.Pico8.Coop.Tests.Editor
{
    public class CoopCartridgeTests
    {
        private CoopCartridge _cartridge;
        private Hatiora.Pico8.Unity.Pico8Builder _builder;

        [SetUp]
        public void Setup()
        {
            _cartridge = new CoopCartridge();
            _builder = new Hatiora.Pico8.Unity.Pico8Builder().WithCartridge(_cartridge);
        }


        [Test]
        public void Init_SetsAppropriateInitialState()
        {
            var engine = new Hatiora.Pico8.Unity.Pico8Builder().WithCartridge(_cartridge).Build();
            // TODO: Arrange & assert initial state
            Assert.DoesNotThrow(() => _cartridge.Init());
        }

        [Test]
        public void Update_SetsAppropriateState()
        {
            var engine = new Hatiora.Pico8.Unity.Pico8Builder().WithCartridge(_cartridge).Build();
            _cartridge.Init();
            // TODO: Arrange interactions or time
            _cartridge.Update();
            // TODO: Assert appropriate effects and PICO-8 API side-effects
        }

        [Test]
        public void Draw_CallsExpectedApiFunctions()
        {
            var engine = new Hatiora.Pico8.Unity.Pico8Builder().WithCartridge(_cartridge).Build();
            _cartridge.Init();
            // TODO: Mount test buffers if asserting drawing output
            Assert.DoesNotThrow(() => _cartridge.Draw());
        }

    }
}
