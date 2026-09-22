namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Ordre coffres / inventaire effectif : celui de l'option <c>ChestsFirst</c>, inversé le temps d'une action lancée
    /// au clic du milieu. Un seul drapeau suffit, les trois porteurs ne se recouvrent jamais : recharge d'un feu
    /// (levé le temps de l'<c>Interact</c>, <c>FuelFromChestsMiddleClickPatch</c>), pose d'une pièce (levé le temps de
    /// la fenêtre de 0,2 s du clic, <c>MiddleClickPlacePatch</c>) et craft (levé tant que la barre de craft tourne,
    /// <c>MiddleClickCraftPatch</c>).
    /// </summary>
    internal static class PullOrder
    {
        internal static bool Inverted;

        internal static bool ChestsFirst => CraftFromChestsConfig.ChestsFirst.Value != Inverted;

        /// <summary>Ligne d'aide commune aux survols : ce que ferait le clic du milieu.</summary>
        internal static string MiddleClickHint =>
            "[<color=yellow><b>Clic milieu</b></color>] "
            + (CraftFromChestsConfig.ChestsFirst.Value ? "inventaire d'abord" : "coffres d'abord");
    }
}
