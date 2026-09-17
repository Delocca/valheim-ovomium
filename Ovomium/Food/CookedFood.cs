using System.Collections.Generic;

namespace Ovomium.Food
{
    /// <summary>
    /// Relie un plat cru à sa version cuite via les conversions des stations de cuisson
    /// (feu, four… : CookingStation.m_conversion), pour évaluer un plat cru à la valeur de son résultat.
    /// Table construite à la demande à partir des prefabs de la scène, et reconstruite à chaque nouveau monde.
    /// </summary>
    internal static class CookedFood
    {
        private static readonly Dictionary<string, ItemDrop.ItemData.SharedData> s_cookedByRawName = new();
        private static ZNetScene s_builtFor;

        /// <summary>La version cuite si l'item est un ingrédient de cuisson, sinon l'item lui-même.</summary>
        public static ItemDrop.ItemData.SharedData Resolve(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return null;
            EnsureBuilt();
            return s_cookedByRawName.TryGetValue(shared.m_name, out var cooked) ? cooked : shared;
        }

        private static void EnsureBuilt()
        {
            var scene = ZNetScene.instance;
            if (scene == null || scene == s_builtFor)
                return;

            s_cookedByRawName.Clear();
            foreach (var prefab in scene.m_prefabs)
            {
                var station = prefab ? prefab.GetComponent<CookingStation>() : null;
                if (station == null)
                    continue;
                foreach (var conversion in station.m_conversion)
                {
                    if (conversion.m_from && conversion.m_to)
                        s_cookedByRawName[conversion.m_from.m_itemData.m_shared.m_name] = conversion.m_to.m_itemData.m_shared;
                }
            }
            s_builtFor = scene;
            Plugin.Log.LogDebug($"Conversions cru→cuit : {s_cookedByRawName.Count}");
        }
    }
}
