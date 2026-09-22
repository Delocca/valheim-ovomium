using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Clic du milieu en mode construction : pose de la pièce sélectionnée, ordre coffres/inventaire inversé. En
    /// vanilla ce bouton est « Retirer » (<c>Player.UpdatePlacement</c> : relâchement → <c>m_removePressedTime</c>,
    /// puis <c>RemovePiece</c> sur un lancer de rayon caméra), et Shift + clic du milieu copie la pièce visée.
    /// Arbitrage : la pose inversée n'a lieu que si un fantôme valide est posable (pièce sélectionnée ni réparation ni
    /// retrait, <c>m_placementStatus</c> Valid) <b>et</b> que le rayon de <c>RemovePiece</c> ne trouverait aucune pièce
    /// retirable ; sinon rien ne change (le clic retire, comme en vanilla). La pose est déléguée au vanilla en posant
    /// <c>m_placePressedTime</c> comme le ferait le clic gauche (délai d'outil, endurance, durabilité, compétence,
    /// effets), le drapeau restant levé pendant sa fenêtre de 0,2 s ; le relâchement qui suit est neutralisé par
    /// <c>m_blockRemove</c>, sinon il retirerait la pièce tout juste posée.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacement", new System.Type[] { typeof(bool), typeof(float) })]
    internal static class MiddleClickPlacePatch
    {
        /// <summary>Fenêtre vanilla entre la pression et la pose effective.</summary>
        private const float PressWindow = 0.2f;

        /// <summary>Clic du milieu pris pour une pose : son relâchement ne doit pas retirer.</summary>
        private static bool s_held;
        private static float s_armedTime = -1f;

        private static void Prefix(Player __instance, bool takeInput)
        {
            if (__instance != Player.m_localPlayer) return;
            TrackRelease(__instance);
            if (!takeInput || !CraftFromChestsConfig.Enabled.Value || !__instance.InPlaceMode() || __instance.IsDead()) return;
            if (!ZInput.GetMouseButtonDown(2) || Hud.IsPieceSelectionVisible() || Hud.InRadial()) return;
            if (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltKeys")) return;
            if (!CanPlace(__instance) || RemoveWouldTarget(__instance)) return;
            s_held = true;
            s_armedTime = Time.time;
            PullOrder.Inverted = true;
            __instance.m_placePressedTime = Time.time;
        }

        /// <summary>Pose faite (ou refusée) par le vanilla, ou fenêtre passée : ordre normal.</summary>
        private static void Postfix(Player __instance)
        {
            if (s_armedTime < 0f) return;
            if (__instance.m_placePressedTime >= 0f && Time.time - s_armedTime < PressWindow) return;
            s_armedTime = -1f;
            PullOrder.Inverted = false;
        }

        private static void TrackRelease(Player player)
        {
            if (!s_held) return;
            if (ZInput.GetMouseButtonUp(2))
            {
                player.m_blockRemove = true;  // remis à faux par le vanilla dans la même passe
                s_held = false;
            }
            else if (!ZInput.GetMouseButton(2)) s_held = false;
        }

        private static bool CanPlace(Player player)
        {
            Piece selected = player.m_buildPieces.GetSelectedPiece();
            if (selected == null || selected.m_repairPiece || selected.m_removePiece) return false;
            GameObject ghost = player.m_placementGhost;
            return ghost != null && ghost.activeSelf && player.m_placementStatus == Player.PlacementStatus.Valid;
        }

        /// <summary>Même lancer de rayon que <c>Player.RemovePiece</c> ; vrai dès qu'une pièce retirable est visée.</summary>
        private static bool RemoveWouldTarget(Player player)
        {
            Transform camera = GameCamera.instance.transform;
            if (!Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, 50f, player.m_removeRayMask)
                || Vector3.Distance(hit.point, player.m_eye.position) >= player.m_maxPlaceDistance) return false;
            Piece piece = hit.collider.GetComponentInParent<Piece>();
            if (piece == null && hit.collider.GetComponent<Heightmap>() != null)
                piece = TerrainModifier.FindClosestModifierPieceInRange(hit.point, 2.5f);
            if (piece == null || !piece.m_canBeRemoved) return false;
            PieceTable table = player.RightItem?.m_shared.m_buildPieces;
            return table == null || table.m_canRemovePieces || table.m_canRemoveFeasts;
        }
    }
}
