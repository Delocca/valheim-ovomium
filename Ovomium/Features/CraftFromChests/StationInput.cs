using System.Collections.Generic;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Objet à transformer qu'une pression de E sur l'interrupteur d'entrée d'un <c>Smelter</c> ajouterait, pris dans
    /// l'inventaire ou les coffres. Stations couvertes : celles dont toutes les entrées donnent le même produit
    /// (raffinerie d'eitr, rouet, four à charbon, bélier : seule la première entrée, le bois ordinaire, est préférée)
    /// et le moulin (orge, ou avoine selon <c>MillOats</c>, l'autre céréale en repli) ; pas la fonderie ni le haut
    /// fourneau, le mod ne choisit pas le minerai. Une entrée portée sur soi passe d'abord (préférée, puis l'ordre du
    /// jeu, comme le vanilla), sinon la première préférée disponible dans les coffres.
    /// </summary>
    internal static class StationInput
    {
        private const string MillPrefab = "windmill";
        private static readonly string[] s_barley = { "Barley" };
        private static readonly string[] s_oats = { "OatSeeds", "Oat" };
        private static readonly List<ItemDrop> s_preferred = new List<ItemDrop>();

        /// <summary>Entrée à ajouter ; null si la station n'est pas couverte ou si rien n'est disponible.</summary>
        internal static ItemDrop Choose(Player player, Smelter smelter)
        {
            FillPreferred(smelter, s_preferred);
            if (s_preferred.Count == 0) return null;
            Inventory inventory = player.m_inventory;
            foreach (ItemDrop input in s_preferred)
                if (inventory.CountItems(Name(input)) > 0) return input;
            foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
                if (conversion.m_from != null && inventory.CountItems(Name(conversion.m_from)) > 0) return conversion.m_from;
            foreach (ItemDrop input in s_preferred)
                if (CraftFromChestsPatch.Total(player, Name(input), -1) > 0) return input;
            return null;
        }

        private static string Name(ItemDrop item) => item.m_itemData.m_shared.m_name;

        /// <summary>Entrées prises dans les coffres, par préférence ; vide si la station n'est pas couverte.</summary>
        private static void FillPreferred(Smelter smelter, List<ItemDrop> preferred)
        {
            preferred.Clear();
            if (Utils.GetPrefabName(smelter.gameObject) == MillPrefab)
            {
                bool oats = CraftFromChestsConfig.MillOats.Value;
                AddByPrefab(smelter, oats ? s_oats : s_barley, preferred);
                AddByPrefab(smelter, oats ? s_barley : s_oats, preferred);
                return;
            }
            ItemDrop first = null;
            ItemDrop product = null;
            foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
            {
                if (conversion.m_from == null) continue;
                if (first == null) { first = conversion.m_from; product = conversion.m_to; }
                else if (conversion.m_to != product) return;
            }
            if (first != null) preferred.Add(first);
        }

        private static void AddByPrefab(Smelter smelter, string[] prefabs, List<ItemDrop> preferred)
        {
            foreach (string prefab in prefabs)
                foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
                    if (conversion.m_from != null && conversion.m_from.name == prefab && !preferred.Contains(conversion.m_from))
                        preferred.Add(conversion.m_from);
        }
    }
}
