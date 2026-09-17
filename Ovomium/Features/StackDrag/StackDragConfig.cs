using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.StackDrag
{
    /// <summary>Réglages de la fonctionnalité « manipulation fine des piles à la souris ».</summary>
    internal static class StackDragConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> DoubleClickSeconds { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("StackDrag", "Enabled", true,
                new ConfigDescription("Alt+clic prend 1 unité ; pile en main : Ctrl/Alt+clic en pose 1, Shift+clic ouvre le split vers la case, "
                    + "glisser bouton maintenu pose 1 unité par case vide survolée ; double-clic rassemble les piles identiques.",
                    null, new SettingLabel("Activé")));
            DoubleClickSeconds = config.Bind("StackDrag", "DoubleClickSeconds", 0.4f,
                new ConfigDescription("Délai maximal entre les deux clics d'un double-clic, en secondes.",
                    new AcceptableValueRange<float>(0.1f, 1f), new SettingLabel("Délai du double-clic (s)")));
        }
    }
}
