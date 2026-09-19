using System.Collections.Generic;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>Un retrait (prévu ou effectué) : <paramref name="Amount"/> exemplaires de l'objet dans un coffre.</summary>
    internal readonly struct Pull
    {
        public readonly Container Chest;
        public readonly ItemDrop Item;
        public readonly int Amount;

        public Pull(Container chest, ItemDrop item, int amount)
        {
            Chest = chest;
            Item = item;
            Amount = amount;
        }
    }

    /// <summary>
    /// Prévision des retraits en coffres pour une liste d'exigences, avec le même ordre coffres / inventaire que la
    /// consommation (<c>CraftFromChestsConsumePatch</c>) ; ne retire rien. Sert à animer le départ des objets dès le
    /// début du craft (ItemFlight), avant la consommation réelle.
    /// </summary>
    internal static class PullPlan
    {
        public static List<Pull> Predict(Player player, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
        {
            var pulls = new List<Pull>();
            CraftingStation station = player.GetCurrentCraftingStation();
            foreach (Piece.Requirement req in requirements)
            {
                if (!CraftFromChestsPatch.Applies(req, station)) continue;
                int need = req.GetAmount(qualityLevel) * multiplier;
                if (!CraftFromChestsConfig.ChestsFirst.Value)
                    need -= player.m_inventory.CountItems(req.m_resItem.m_itemData.m_shared.m_name, itemQuality);
                if (need > 0)
                    NearbyChests.Plan(player.transform.position, req.m_resItem, need, itemQuality, pulls);
            }
            return pulls;
        }
    }
}
