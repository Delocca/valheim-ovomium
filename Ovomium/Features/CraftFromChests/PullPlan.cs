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
    /// début du craft (ItemFlight), avant la consommation réelle, et à vérifier la propriété des coffres avant de
    /// consommer (<c>ChestOwnershipPatch</c>).
    /// </summary>
    internal static class PullPlan
    {
        /// <summary>
        /// Retraits du craft en cours du panneau, avec les paramètres de <c>InventoryGui.DoCrafting</c> ; null quand il
        /// ne passera pas par <c>ConsumeResources</c> (« un seul ingrédient au choix », coût désactivé).
        /// </summary>
        public static List<Pull> PredictCraft(InventoryGui gui, Player player) =>
            PredictRecipe(player, gui.m_craftRecipe, gui.m_craftUpgradeItem, gui.m_multiCrafting ? gui.m_multiCraftAmount : 1);

        /// <summary>
        /// Retraits d'une recette : fabrication (<paramref name="upgrade"/> null) ou amélioration de
        /// <paramref name="upgrade"/> au niveau suivant ; null comme <see cref="PredictCraft"/>.
        /// </summary>
        public static List<Pull> PredictRecipe(Player player, Recipe recipe, ItemDrop.ItemData upgrade, int multiplier)
        {
            if (recipe == null || player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)) return null;
            int quality = upgrade == null ? 1 : upgrade.m_quality + 1;
            recipe.GetAmount(quality, out _, out ItemDrop.ItemData single, multiplier);
            return single != null ? null : Predict(player, recipe.m_resources, quality, -1, multiplier);
        }

        /// <summary>Retraits de la pose de <paramref name="piece"/> (paramètres de <c>Player.TryPlacePiece</c>) ; null si la pose ne coûte rien.</summary>
        public static List<Pull> PredictPiece(Player player, Piece piece)
        {
            if (piece == null || player.m_noPlacementCost || ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())) return null;
            return Predict(player, piece.m_resources, 0, -1, 1);
        }

        public static List<Pull> Predict(Player player, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
        {
            var pulls = new List<Pull>();
            CraftingStation station = player.GetCurrentCraftingStation();
            foreach (Piece.Requirement req in requirements)
            {
                if (!CraftFromChestsPatch.Applies(req, station)) continue;
                int need = req.GetAmount(qualityLevel) * multiplier;
                if (!PullOrder.ChestsFirst)
                    need -= player.m_inventory.CountItems(req.m_resItem.m_itemData.m_shared.m_name, itemQuality);
                if (need > 0)
                    NearbyChests.Plan(player.transform.position, req.m_resItem, need, itemQuality, pulls);
            }
            return pulls;
        }
    }
}
