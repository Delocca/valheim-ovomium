using HarmonyLib;

namespace Ovomium.Features.ManualChest
{
    /// <summary>Pose du bouton au démarrage de l'interface, rafraîchissement à chaque changement de coffre, remise à zéro à la fermeture.</summary>
    internal static class ManualChestPatch
    {
        private static Container s_shown;

        [HarmonyPatch(typeof(InventoryGui), "Awake", new System.Type[0])]
        private static class AwakePatch
        {
            private static void Postfix() => ManualChestButton.Install();
        }

        /// <summary>Le panneau coffre n'est affiché qu'ici (coffre possédé) : le drapeau du ZDO est alors lisible.</summary>
        [HarmonyPatch(typeof(InventoryGui), "UpdateContainer", new System.Type[] { typeof(Player) })]
        private static class UpdateContainerPatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                Container current = __instance.m_currentContainer;
                if (current == s_shown) return;
                s_shown = current;
                if (current != null) ManualChestButton.Refresh(__instance);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Hide", new System.Type[0])]
        private static class HidePatch
        {
            private static void Postfix()
            {
                s_shown = null;
                ManualChestHoldPatch.Reset();
            }
        }
    }
}
