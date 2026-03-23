using System;
using UnityEngine.UIElements;

namespace Hatiora.Pico8.Unity
{
    public static class VisualElementExtensions
    {
        public static T Style<T>(this T element, Action<IStyle> configure) where T : VisualElement
        {
            configure?.Invoke(element.style);
            return element;
        }

        public static T Children<T>(this T element, params VisualElement[] children) where T : VisualElement
        {
            foreach (var child in children)
                element.Add(child);
            return element;
        }
    }
}
