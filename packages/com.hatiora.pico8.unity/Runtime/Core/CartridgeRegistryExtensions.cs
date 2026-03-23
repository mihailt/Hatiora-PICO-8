using UnityEngine;
using UnityEngine.UIElements;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Unity
{
    public static class CartridgeRegistryExtensions
    {
        /// <summary>
        /// Instantiates a cartridge view from the registry and injects it directly into the provided Unity UI Document root.
        /// Replaces any existing content in the root.
        /// </summary>
        /// <param name="registry">The populated cartridge registry.</param>
        /// <param name="root">The VisualElement root to host the cartridge display.</param>
        /// <param name="gameName">The registered name of the cartridge to load.</param>
        /// <returns>The instantiated Pico8View, or null if loading fails.</returns>
        public static Pico8View Mount(this CartridgeRegistry registry, VisualElement root, string gameName)
        {
            root.Clear();

            try
            {
                var view = registry.Load(gameName);
                
                root
                    .Add(
                        view.Style((s) => 
                        { 
                            s.flexGrow = 1;
                            s.alignItems = Align.Center;
                            s.justifyContent = Justify.Center;
                        })
                    );

                return view;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load cartridge '{gameName}': {ex.Message}");
                return null;
            }
        }
    }
}
