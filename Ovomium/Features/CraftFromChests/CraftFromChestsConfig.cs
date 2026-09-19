using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>Réglages de la fonctionnalité « craft et construction depuis les coffres ».</summary>
    internal static class CraftFromChestsConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> Range { get; private set; }
        public static ConfigEntry<bool> ChestsFirst { get; private set; }
        public static ConfigEntry<bool> LogPulls { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("CraftFromChests", "Enabled", true,
                new ConfigDescription("Le craft, l'amélioration et la construction au marteau prennent les ingrédients "
                    + "dans les coffres, chariots et bateaux à portée (le plus proche d'abord) et dans l'inventaire.",
                    null, new SettingLabel("Activé")));
            Range = config.Bind("CraftFromChests", "Range", 10f,
                new ConfigDescription("Distance, en mètres autour du joueur, jusqu'à laquelle un coffre est utilisable (10 = portée d'un établi).",
                    new AcceptableValueRange<float>(2f, 50f), new SettingLabel("Portée des coffres (m)")));
            ChestsFirst = config.Bind("CraftFromChests", "ChestsFirst", true,
                new ConfigDescription("Puiser dans les coffres avant l'inventaire (sinon l'inventaire d'abord, puis les coffres).",
                    null, new SettingLabel("Coffres avant l'inventaire")));
            LogPulls = config.Bind("CraftFromChests", "LogPulls", false,
                new ConfigDescription("Écrit dans le journal chaque retrait d'un coffre : objet, quantité, coffre, distance.",
                    null, new SettingLabel("Journaliser les retraits")));
        }
    }
}
