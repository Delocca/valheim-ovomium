using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.ManualChest
{
    /// <summary>
    /// Coffre ouvert, un nouvel appui sur E (vanilla : fermeture immédiate dans <c>InventoryGui.Update</c>) est consommé
    /// et suivi : maintenu 0,5 s (<c>m_containerHoldPlaceStackDelay</c>), les objets similaires sont rangés dans le
    /// coffre sans le fermer (même <c>Inventory.StackAll</c> que le bouton vanilla, coffre déjà possédé) ; relâché
    /// avant, fermeture vanilla (juste déplacée au relâchement). Le maintien depuis l'ouverture du coffre reste géré
    /// par <c>UpdateContainer</c>. Manette : inchangé (« JoyUse » ne ferme pas le coffre en vanilla).
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "Update", new System.Type[0])]
    internal static class ManualChestHoldPatch
    {
        private const string Use = "Use";

        private static bool s_tracking;
        private static bool s_stacked;
        private static float s_held;

        internal static void Reset()
        {
            s_tracking = false;
            s_stacked = false;
            s_held = 0f;
        }

        private static void Prefix(InventoryGui __instance)
        {
            if (!ManualChestConfig.Enabled.Value || Player.m_localPlayer == null) { Reset(); return; }
            if (__instance.m_currentContainer == null || !__instance.m_animator.GetBool("visible")) { Reset(); return; }
            if (s_tracking) { Track(__instance); return; }
            // m_shownFrames est incrémenté par Update après ce Prefix : « > 1 » vanilla = « >= 1 » ici.
            if (__instance.m_shownFrames < 1 || !VanillaWouldClose(__instance) || !ZInput.GetButtonDown(Use)) return;
            ZInput.ResetButtonStatus(Use);
            s_tracking = true;
            s_stacked = false;
            s_held = 0f;
        }

        private static void Track(InventoryGui gui)
        {
            if (ZInput.GetButton(Use))
            {
                s_held += Time.deltaTime;
                if (!s_stacked && s_held >= gui.m_containerHoldPlaceStackDelay)
                {
                    s_stacked = true;
                    StackAll(gui);
                }
                return;
            }
            bool close = !s_stacked;
            Reset();
            if (close) gui.Hide();
        }

        /// <summary>Même geste que le bouton vanilla « Objets similaires » : pile en main reposée, puis rangement.</summary>
        private static void StackAll(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            if (player.IsTeleporting()) return;
            gui.SetupDragItem(null, null, 1);
            int moved = gui.m_currentContainer.GetInventory().StackAll(player.GetInventory(), message: true);
            if (moved > 0) gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
        }

        /// <summary>Gardes du bloc clavier d'<c>InventoryGui.Update</c> (<c>build/decompiled/InventoryGui.cs:520</c>).</summary>
        private static bool VanillaWouldClose(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            return gui.m_craftTimer < 0f && (Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible()
                && !Menu.IsVisible() && TextViewer.instance != null && !TextViewer.instance.IsVisible()
                && !player.InCutscene() && !GameCamera.InFreeFly() && !Minimap.IsOpen();
        }
    }
}
