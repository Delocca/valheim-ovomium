using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>Réglages de l'animation des ingrédients volant des coffres vers la station.</summary>
    internal static class ItemFlightConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> MinDuration { get; private set; }
        public static ConfigEntry<float> MaxDuration { get; private set; }
        public static ConfigEntry<int> MaxPerType { get; private set; }
        public static ConfigEntry<float> TrailLinger { get; private set; }
        public static ConfigEntry<float> TrailDensity { get; private set; }
        public static ConfigEntry<float> TrailSize { get; private set; }
        public static ConfigEntry<float> TrailSpread { get; private set; }
        public static ConfigEntry<int> MaxInFlight { get; private set; }
        public static ConfigEntry<string> TrailItem { get; private set; }
        public static ConfigEntry<string> TrailSystem { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("ItemFlight", "Enabled", true,
                new ConfigDescription("Les ingrédients pris dans les coffres volent jusqu'à la station, la pièce construite "
                    + "ou le joueur, avec une traînée de particules (visible seulement par soi).",
                    null, new SettingLabel("Activé")));
            MinDuration = config.Bind("ItemFlight", "MinDuration", 1.5f,
                new ConfigDescription("Durée du vol, en secondes, pour un coffre tout proche.",
                    new AcceptableValueRange<float>(0.5f, 10f), new SettingLabel("Durée minimale (s)")));
            MaxDuration = config.Bind("ItemFlight", "MaxDuration", 3f,
                new ConfigDescription("Durée du vol, en secondes, pour un coffre à la limite de la portée de CraftFromChests.",
                    new AcceptableValueRange<float>(0.5f, 15f), new SettingLabel("Durée maximale (s)")));
            MaxPerType = config.Bind("ItemFlight", "MaxPerType", 10,
                new ConfigDescription("Nombre maximal d'exemplaires animés pour un même ingrédient : ils s'envolent en file "
                    + "sur la même trajectoire, et un seul porte la traînée.",
                    new AcceptableValueRange<int>(1, 20), new SettingLabel("Exemplaires par ingrédient")));
            TrailLinger = config.Bind("ItemFlight", "TrailLinger", 10f,
                new ConfigDescription("Durée de vie, en secondes, de chaque particule de la traînée (elle s'efface derrière l'objet à ce rythme).",
                    new AcceptableValueRange<float>(1f, 30f), new SettingLabel("Persistance de la traînée (s)")));
            TrailDensity = config.Bind("ItemFlight", "TrailDensity", 23f,
                new ConfigDescription("Particules de traînée émises par mètre parcouru.",
                    new AcceptableValueRange<float>(1f, 60f), new SettingLabel("Densité de la traînée (par m)")));
            TrailSize = config.Bind("ItemFlight", "TrailSize", 0.22f,
                new ConfigDescription("Taille des particules de traînée, en proportion de leur taille d'origine dans le projectile.",
                    new AcceptableValueRange<float>(0.05f, 2f), new SettingLabel("Taille des particules")));
            TrailSpread = config.Bind("ItemFlight", "TrailSpread", 0.04f,
                new ConfigDescription("Rayon, en mètres, autour de la ligne de vol dans lequel les particules apparaissent.",
                    new AcceptableValueRange<float>(0f, 0.5f), new SettingLabel("Dispersion des particules (m)")));
            MaxInFlight = config.Bind("ItemFlight", "MaxInFlight", 60,
                new ConfigDescription("Nombre maximal d'objets en vol en même temps (les suivants ne sont pas animés).",
                    new AcceptableValueRange<int>(1, 200), new SettingLabel("Objets en vol au maximum")));
            TrailItem = config.Bind("ItemFlight", "TrailItem", "ArrowFire",
                "Nom du prefab de flèche ou carreau dont la traînée de projectile est réutilisée "
                + "(ArrowFire, ArrowFrost, ArrowPoison, ArrowSilver, BoltIron, etc.). Fichier cfg seulement.");
            TrailSystem = config.Bind("ItemFlight", "TrailSystem", "flames",
                "Nom du système de particules gardé dans ce projectile (flèche de feu : trail, flames, flames_local, flare). "
                + "Fichier cfg seulement.");
        }
    }
}
