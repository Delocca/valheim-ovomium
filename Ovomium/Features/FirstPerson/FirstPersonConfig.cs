using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.FirstPerson
{
    /// <summary>Réglages de la fonctionnalité « vue subjective ».</summary>
    internal static class FirstPersonConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> NearClip { get; private set; }
        public static ConfigEntry<float> ForwardOffset { get; private set; }
        public static ConfigEntry<float> UpOffset { get; private set; }
        public static ConfigEntry<float> CrouchEyeHeight { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("FirstPerson", "Enabled", true,
                new ConfigDescription("Zoomer (molette, ou zoom caméra à la manette) au-delà de la distance minimale du jeu passe en vue "
                    + "subjective : corps masqué, objets en main visibles, végétation non effacée près de la caméra. "
                    + "Dézoomer revient en vue à la troisième personne.",
                    null, new SettingLabel("Activé", restartRequired: true)));
            NearClip = config.Bind("FirstPerson", "NearClip", 0.05f,
                new ConfigDescription("Distance (m) en deçà de laquelle la caméra ne dessine rien, en vue subjective. "
                    + "Plus bas = moins de trous dans les murs collés au visage, mais moins de précision de profondeur au loin.",
                    new AcceptableValueRange<float>(0.01f, 0.3f), new SettingLabel("Distance de coupe (m)")));
            ForwardOffset = config.Bind("FirstPerson", "ForwardOffset", 0.1f,
                new ConfigDescription("Distance (m) de la caméra devant le point œil du personnage. Le prefab vanilla met 0,5, "
                    + "ce qui fait tourner la caméra en arc quand on tourne la tête.",
                    new AcceptableValueRange<float>(-0.3f, 0.5f), new SettingLabel("Caméra en avant (m)")));
            UpOffset = config.Bind("FirstPerson", "UpOffset", 0f,
                new ConfigDescription("Décalage vertical (m) de la caméra par rapport au point œil du personnage.",
                    new AcceptableValueRange<float>(-0.3f, 0.3f), new SettingLabel("Caméra en hauteur (m)")));
            CrouchEyeHeight = config.Bind("FirstPerson", "CrouchEyeHeight", 1.23f,
                new ConfigDescription("Accroupi en vue subjective : hauteur (m) de l'œil au-dessus des pieds du personnage. "
                    + "Fixe, comme le point œil debout du jeu : la caméra ne suit pas le balancement de la marche furtive.",
                    new AcceptableValueRange<float>(0.6f, 1.8f), new SettingLabel("Accroupi : hauteur de la caméra (m)")));
        }
    }
}
