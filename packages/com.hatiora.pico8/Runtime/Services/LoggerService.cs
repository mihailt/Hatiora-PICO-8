using UnityEngine;

namespace Hatiora.Pico8
{
    public class LoggerService : ILogger
    {
        public void Log(string message) => Debug.Log(message);
    }
}
