using HarmonyLib;

namespace Ovomium.Features.MenuDoubleClick
{
    /// <summary>
    /// Double-clic dans les listes du menu principal. Les lignes sont des Button dont onClick appelle la méthode
    /// de sélection (FejdStartup.OnSelectWorld(index) pour les mondes, ServerListGui.OnSelectedServer(joinData)
    /// pour les serveurs, toutes listes confondues) : un Postfix sur ces méthodes suffit à compter les clics.
    /// Au second clic sur la même entrée, on appelle ce que le bouton de validation appelle (OnWorldStart /
    /// OnJoinStart), uniquement si ce bouton est actif (interactable, mis à jour par le jeu dès la sélection).
    /// À la manette, le bouton A sur une ligne passe aussi par onClick : laissé vanilla.
    /// </summary>
    internal static class MenuDoubleClickPatch
    {
        private static readonly DoubleClickTracker<int> s_worlds = new DoubleClickTracker<int>();
        private static readonly DoubleClickTracker<ServerJoinData> s_servers = new DoubleClickTracker<ServerJoinData>();

        private static bool IsActive()
        {
            return MenuDoubleClickConfig.Enabled.Value && !ZInput.IsGamepadActive();
        }

        [HarmonyPatch(typeof(FejdStartup), "OnSelectWorld", typeof(int))]
        private static class OnSelectWorldPatch
        {
            private static void Postfix(FejdStartup __instance, int index)
            {
                if (!IsActive() || !s_worlds.Click(index))
                    return;
                if (__instance.m_worldStart != null && __instance.m_worldStart.interactable)
                    __instance.OnWorldStart();
            }
        }

        [HarmonyPatch(typeof(ServerListGui), "OnSelectedServer", typeof(ServerJoinData))]
        private static class OnSelectedServerPatch
        {
            private static void Postfix(ServerListGui __instance, ServerJoinData selected)
            {
                if (!IsActive() || !s_servers.Click(selected))
                    return;
                if (__instance.m_joinGameButton != null && __instance.m_joinGameButton.interactable
                    && __instance.m_startup != null)
                    __instance.m_startup.OnJoinStart();
            }
        }
    }
}
