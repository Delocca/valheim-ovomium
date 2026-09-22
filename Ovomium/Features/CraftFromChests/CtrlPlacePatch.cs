using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Ctrl + clic gauche en mode construction : pose vanilla de la pièce, ordre coffres/inventaire inversé. Le clic
    /// gauche est déjà la pose (<c>Player.UpdatePlacement</c> : « Attack » → <c>m_placePressedTime</c>, puis
    /// <c>PlacePiece</c> dans une fenêtre de 0,2 s dès que le délai d'outil le permet) : on ne fait que lever le drapeau
    /// pendant cette fenêtre. Ctrl n'a pas d'autre effet sur la pose (le jeu s'en sert pour afficher l'ondulation de
    /// distance des fantômes) ; touche vanilla d'accroupissement : voir <see cref="CtrlCrouchPatch"/>. Manette : rien.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacement", new System.Type[] { typeof(bool), typeof(float) })]
    internal static class CtrlPlacePatch
    {
        /// <summary>Fenêtre vanilla entre la pression et la pose effective.</summary>
        private const float PressWindow = 0.2f;

        private static float s_armedTime = -1f;

        private static void Prefix(Player __instance, bool takeInput)
        {
            if (__instance != Player.m_localPlayer || !takeInput) return;
            if (!CraftFromChestsConfig.Enabled.Value || !__instance.InPlaceMode() || __instance.IsDead()) return;
            if (!ZInput.GetButtonDown("Attack") || !PullOrder.CtrlHeld) return;
            if (Hud.IsPieceSelectionVisible() || Hud.InRadial()) return;
            s_armedTime = Time.time;
            PullOrder.Inverted = true;
            PullOrder.CtrlUsed = true;
        }

        /// <summary>Pose faite (ou refusée) par le vanilla, ou fenêtre passée : ordre normal.</summary>
        private static void Postfix(Player __instance)
        {
            if (s_armedTime < 0f) return;
            if (__instance.m_placePressedTime >= 0f && Time.time - s_armedTime < PressWindow) return;
            s_armedTime = -1f;
            PullOrder.Inverted = false;
        }
    }
}
