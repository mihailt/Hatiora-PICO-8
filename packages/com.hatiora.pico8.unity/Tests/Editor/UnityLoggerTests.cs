using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class UnityLoggerTests
    {
        [Test]
        public void Log_DoesNotThrow()
        {
            var logger = new UnityLogger();
            LogAssert.Expect(LogType.Log, "[Pico8] hello");
            Assert.DoesNotThrow(() => logger.Log("hello"));
        }

        [Test]
        public void Log_NullMessage_DoesNotThrow()
        {
            var logger = new UnityLogger();
            LogAssert.Expect(LogType.Log, "[Pico8] ");
            Assert.DoesNotThrow(() => logger.Log(null));
        }

        [Test]
        public void Log_EmptyMessage_DoesNotThrow()
        {
            var logger = new UnityLogger();
            LogAssert.Expect(LogType.Log, "[Pico8] ");
            Assert.DoesNotThrow(() => logger.Log(""));
        }

        [Test]
        public void Logger_ImplementsILogger()
        {
            var logger = new UnityLogger();
            Assert.IsInstanceOf<ILogger>(logger);
        }

        [Test]
        public void Logger_WritesToUnityConsole()
        {
            // We can't read Debug.Log output in tests,
            // but we CAN use LogAssert to verify
            var logger = new UnityLogger();
            logger.Log("test message");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Log, "[Pico8] test message");
        }
    }
}
