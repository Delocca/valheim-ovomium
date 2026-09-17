using BepInEx.Configuration;

namespace OvoMiam.Features.LoadingArt
{
    /// <summary>Réglages de la fonctionnalité « artworks de chargement ».</summary>
    internal static class LoadingArtConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> Folder { get; private set; }
        public static ConfigEntry<bool> HideTeleportAnimation { get; private set; }

        public static void Bind(ConfigFile config)
        {
            HideTeleportAnimation = config.Bind("LoadingArt", "HideTeleportAnimation", true,
                "Masquer l'animation vanilla de téléportation (images teleport_1…7) pour laisser voir l'artwork.");
            Enabled = config.Bind("LoadingArt", "Enabled", true,
                "Remplacer le fond des écrans de chargement (démarrage du jeu, chargement de partie, mort, sommeil, "
                + "téléportation) par une image tirée au sort dans le dossier Folder.");
            Folder = config.Bind("LoadingArt", "Folder", "loading",
                "Dossier des images (jpg, jpeg, png), relatif au dossier de la DLL du mod ou absolu.");
        }
    }
}
