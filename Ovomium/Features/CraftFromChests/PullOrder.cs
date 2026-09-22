using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Ordre coffres / inventaire effectif : celui de l'option <c>ChestsFirst</c>, inversé le temps d'une action lancée
    /// Ctrl maintenu. Un seul drapeau suffit, les trois porteurs ne se recouvrent jamais : recharge d'un feu (levé le
    /// temps du retrait, <c>FuelFromChests.Refuel</c>), pose d'une pièce (levé le temps de la fenêtre de 0,2 s du clic,
    /// <c>CtrlPlacePatch</c>) et craft (levé tant que la barre de craft tourne, <c>CtrlCraftPatch</c>).
    /// </summary>
    internal static class PullOrder
    {
        internal static bool Inverted;

        internal static bool ChestsFirst => CraftFromChestsConfig.ChestsFirst.Value != Inverted;

        /// <summary>Ctrl maintenu, lu comme le jeu le fait pour Ctrl + clic d'un objet (<c>InventoryGrid</c>).</summary>
        internal static bool CtrlHeld => ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl);

        /// <summary>
        /// Ctrl a servi de modificateur depuis sa pression : l'accroupissement vanilla n'aura pas lieu à son relâchement
        /// (<c>CtrlCrouchPatch</c>). Posé par chaque porteur quand il lève <see cref="Inverted"/> à cause de Ctrl.
        /// </summary>
        internal static bool CtrlUsed;

        /// <summary>Ligne d'aide au survol du bouton Fabriquer.</summary>
        internal static string CraftHint => Hint("Ctrl + clic");

        /// <summary>Ligne d'aide au survol d'un feu, d'un four ou d'une marmite.</summary>
        internal static string FuelHint => Hint("Ctrl + E");

        private static string Hint(string trigger) =>
            $"[<color=yellow><b>{trigger}</b></color>] "
            + (CraftFromChestsConfig.ChestsFirst.Value ? "inventaire d'abord" : "coffres d'abord");
    }
}
