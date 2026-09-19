namespace Ovomium.Features.Updater
{
    /// <summary>
    /// État partagé de la mise à jour, écrit par les threads de fond (vérification, téléchargement) et lu par le thread
    /// principal pour l'affichage. Stocké dans l'AppDomain, pas dans des statiques : une seule vérification par session
    /// et un état qui survit au rechargement à chaud (les types du mod changent d'assembly, donc seulement des
    /// primitives).
    /// </summary>
    internal static class UpdateState
    {
        private const string Prefix = "Ovomium.Updater.";

        /// <summary>Vérification lancée (ou état posé depuis un dossier update/ déjà présent).</summary>
        public static bool Started { get => Flag("Started"); set => Set("Started", value); }
        /// <summary>Une version plus récente est publiée.</summary>
        public static bool Available { get => Flag("Available"); set => Set("Available", value); }
        /// <summary>Téléchargement en cours.</summary>
        public static bool Downloading { get => Flag("Downloading"); set => Set("Downloading", value); }
        /// <summary>Mise à jour déposée dans update/ : installée par le patcher au prochain lancement.</summary>
        public static bool Downloaded { get => Flag("Downloaded"); set => Set("Downloaded", value); }
        /// <summary>Fenêtre affichée, réponse en attente ; remis à faux si la fenêtre a disparu sans réponse (changement de scène).</summary>
        public static bool PopupShown { get => Flag("PopupShown"); set => Set("PopupShown", value); }
        /// <summary>Le joueur a répondu à la fenêtre (oui ou non) : ne plus la proposer cette session.</summary>
        public static bool PopupDone { get => Flag("PopupDone"); set => Set("PopupDone", value); }
        /// <summary>Message en jeu « disponible » (cas AutoJoin) déjà affiché.</summary>
        public static bool HudMessageDone { get => Flag("HudMessageDone"); set => Set("HudMessageDone", value); }
        /// <summary>Message en jeu « téléchargée » déjà affiché.</summary>
        public static bool HudDownloadedDone { get => Flag("HudDownloadedDone"); set => Set("HudDownloadedDone", value); }

        public static string Version { get => Text("Version"); set => Set("Version", value); }
        public static string Changelog { get => Text("Changelog"); set => Set("Changelog", value); }
        public static string DownloadUrl { get => Text("DownloadUrl"); set => Set("DownloadUrl", value); }
        public static long DownloadSize
        {
            get => System.AppDomain.CurrentDomain.GetData(Prefix + "DownloadSize") is long size ? size : 0L;
            set => Set("DownloadSize", value);
        }

        private static bool Flag(string key)
        {
            return System.AppDomain.CurrentDomain.GetData(Prefix + key) is bool value && value;
        }

        private static string Text(string key)
        {
            return System.AppDomain.CurrentDomain.GetData(Prefix + key) as string ?? "";
        }

        private static void Set(string key, object value)
        {
            System.AppDomain.CurrentDomain.SetData(Prefix + key, value);
        }
    }
}
