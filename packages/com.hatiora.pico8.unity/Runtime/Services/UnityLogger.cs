using UnityEngine;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Wraps UnityEngine.Debug.Log for the core <see cref="ILogger"/> interface.
    /// </summary>
    public sealed class UnityLogger : ILogger
    {
        public void Log(string message) => Debug.Log($"[Pico8] {message}");
    }
}
