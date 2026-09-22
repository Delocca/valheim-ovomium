using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Ctrl est la touche vanilla d'accroupissement (<c>PlayerController.FixedUpdate</c> : front montant de « Crouch »
    /// → <c>Player.SetControls(crouch: true)</c> → bascule <c>SetCrouch</c>). Pour qu'elle serve de modificateur
    /// (<see cref="CtrlPlacePatch"/>, <c>FuelFromChests.Refuel</c>) sans faire s'accroupir, la bascule est reportée au
    /// relâchement de Ctrl et annulée si Ctrl a servi entre-temps (<see cref="PullOrder.CtrlUsed"/>). <c>SetControls</c>
    /// est appelée à chaque FixedUpdate du joueur local, entrée ou non : le relâchement y est toujours vu. Manette
    /// (« JoyCrouch » sans Ctrl) et feature désactivée : vanilla intact.
    /// </summary>
    [HarmonyPatch(typeof(Player), "SetControls", new System.Type[] { typeof(Vector3), typeof(bool), typeof(bool),
        typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    internal static class CtrlCrouchPatch
    {
        /// <summary>Bascule retenue tant que Ctrl est maintenu.</summary>
        private static bool s_pending;

        private static void Prefix(Player __instance, ref bool crouch)
        {
            if (__instance != Player.m_localPlayer || !CraftFromChestsConfig.Enabled.Value) return;
            bool ctrl = PullOrder.CtrlHeld;
            if (crouch && ctrl)
            {
                crouch = false;
                s_pending = true;
                return;
            }
            if (!s_pending || ctrl) return;
            // Remis à zéro ici et non à la pression : une action Ctrl + E lancée par Player.Update dans la frame de la
            // pression précède parfois ce FixedUpdate et serait perdue.
            s_pending = false;
            bool used = PullOrder.CtrlUsed;
            PullOrder.CtrlUsed = false;
            if (used || !__instance.TakeInput() || InventoryGui.IsVisible() || Hud.InRadial()) return;
            crouch = true;
        }
    }
}
