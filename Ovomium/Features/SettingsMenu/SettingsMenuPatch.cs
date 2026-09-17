using HarmonyLib;

namespace Ovomium.Features.SettingsMenu
{
    /// <summary><c>Menu.Start</c> pose <c>m_instance</c> ; le bouton Paramètres est alors câblé.</summary>
    [HarmonyPatch(typeof(Menu), "Start", new System.Type[0])]
    internal static class SettingsMenuMenuPatch
    {
        private static void Postfix(Menu __instance)
        {
            if (SettingsMenuConfig.Enabled.Value)
                OvomiumMenuButton.AddToMenu(__instance);
        }
    }

    /// <summary><c>FejdStartup.Start</c> : après <c>Awake</c>, qui a rempli <c>m_menuButtons</c> depuis <c>m_menuList</c>.</summary>
    [HarmonyPatch(typeof(FejdStartup), "Start", new System.Type[0])]
    internal static class SettingsMenuStartupPatch
    {
        private static void Postfix(FejdStartup __instance)
        {
            if (SettingsMenuConfig.Enabled.Value)
                OvomiumMenuButton.AddToMainMenu(__instance);
        }
    }

    /// <summary>Diagnostic (DumpHierarchy) : état des onglets une fois que le jeu les a câblés.</summary>
    [HarmonyPatch(typeof(TabHandler), "Init", new[] { typeof(bool) })]
    internal static class SettingsMenuTabHandlerPatch
    {
        private static void Postfix(TabHandler __instance)
        {
            if (SettingsMenuConfig.DumpHierarchy.Value && __instance.GetComponentInParent<Settings>() != null)
                OvomiumSettingsWindow.LogTabs("après TabHandler.Init", __instance);
        }
    }

    /// <summary>Remplace les onglets vanilla par les nôtres quand l'ouverture vient d'un bouton Ovomium.</summary>
    [HarmonyPatch(typeof(Settings), "Awake", new System.Type[0])]
    internal static class SettingsMenuSettingsPatch
    {
        private static void Prefix(Settings __instance)
        {
            OvomiumSettingsWindow.OnSettingsAwake(__instance);
        }
    }
}
