using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.ServerWake
{
    /// <summary>Réglages du réveil des serveurs Nodecraft avant la connexion.</summary>
    internal static class ServerWakeConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<int> MaxWaitMinutes { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("ServerWake", "Enabled", true,
                new ConfigDescription("Serveurs Nodecraft : coller le lien de partage du serveur (app.nodecraft.com/shared/…) dans "
                    + "« Ajouter un serveur » l'ajoute aux favoris sous son nom. À la connexion, un serveur en hibernation est "
                    + "démarré, puis la connexion part toute seule quand il est prêt. L'état Nodecraft s'affiche à côté du nom, "
                    + "et le favori suit les changements d'IP du serveur.",
                    null, new SettingLabel("Activé")));
            MaxWaitMinutes = config.Bind("ServerWake", "MaxWaitMinutes", 5,
                new ConfigDescription("Attente maximale du démarrage d'un serveur avant d'abandonner (minutes).",
                    new AcceptableValueRange<int>(1, 20), new SettingLabel("Attente maximale (min)")));
        }
    }
}
