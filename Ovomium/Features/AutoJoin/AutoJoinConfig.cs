using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.AutoJoin
{
    /// <summary>Réglages de la fonctionnalité « connexion automatique au lancement » (outil de développement).</summary>
    internal static class AutoJoinConfig
    {
        /// <summary>Argument de ligne de commande qui force l'activation (passé par tools/deploy.sh --relaunch).</summary>
        public const string CommandLineFlag = "-ovomium-autojoin";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> Character { get; private set; }
        public static ConfigEntry<string> Server { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("AutoJoin", "Enabled", false,
                new ConfigDescription("Outil de développement : à chaque lancement du jeu, sélectionner le personnage et se connecter "
                    + "au serveur sans passer par les menus (un seul essai par lancement). L'argument de ligne de commande "
                    + CommandLineFlag + " force l'activation.",
                    null, new SettingLabel("Activé", restartRequired: true)));
            Character = config.Bind("AutoJoin", "Character", "",
                "Nom ou fichier du personnage à utiliser. Vide : celui que le jeu sélectionne par défaut (dernier utilisé).");
            Server = config.Bind("AutoJoin", "Server", "",
                "Serveur à rejoindre : nom d'un serveur des listes Favoris / Récents, ou hôte:port. Vide : dernier serveur rejoint.");
        }

        /// <summary>Vrai si la connexion automatique est demandée (option ou argument de ligne de commande).</summary>
        public static bool IsRequested()
        {
            if (Enabled.Value)
                return true;
            foreach (string arg in System.Environment.GetCommandLineArgs())
                if (arg == CommandLineFlag)
                    return true;
            return false;
        }
    }
}
