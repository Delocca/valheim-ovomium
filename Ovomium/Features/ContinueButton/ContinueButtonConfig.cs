using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.ContinueButton
{
    /// <summary>Réglages de la fonctionnalité « bouton Continuer au menu principal ».</summary>
    internal static class ContinueButtonConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("ContinueButton", "Enabled", true,
                new ConfigDescription("Menu principal : bouton « Continuer » qui relance la dernière partie (serveur ou monde local) "
                    + "avec le dernier personnage, sans passer par les menus.",
                    null, new SettingLabel("Activé")));
        }
    }
}
