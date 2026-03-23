// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for C# 9 init-only setters.
    /// Unity's Roslyn compiler supports the syntax but doesn't ship this type.
    /// </summary>
    internal static class IsExternalInit { }
}
