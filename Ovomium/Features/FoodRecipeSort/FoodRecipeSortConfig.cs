using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Ovomium.Features.SettingsMenu;

namespace Ovomium.Features.FoodRecipeSort
{
    /// <summary>Réglages de la fonctionnalité « tri des recettes des stations de cuisine ».</summary>
    internal static class FoodRecipeSortConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> CraftableFirst { get; private set; }
        public static ConfigEntry<bool> GroupByStat { get; private set; }
        public static ConfigEntry<bool> LogSortOrder { get; private set; }
        private static ConfigEntry<string> s_stations;

        /// <summary>Noms (m_name) des stations dont la liste de recettes est triée.</summary>
        public static HashSet<string> Stations { get; private set; }

        public static void Bind(ConfigFile config)
        {
            const string section = "FoodRecipeSort";
            Enabled = config.Bind(section, "Enabled", true,
                new ConfigDescription("Trier les recettes des stations de cuisine par valeur nutritive (santé + endurance + eitr) décroissante. " +
                    "Un plat cru compte pour la valeur de sa version cuite.",
                    null, new SettingLabel("Activé")));
            CraftableFirst = config.Bind(section, "CraftableFirst", true,
                new ConfigDescription("Garder les recettes réalisables en tête de liste (comme le jeu de base) avant les autres critères.",
                    null, new SettingLabel("Réalisables en tête")));
            GroupByStat = config.Bind(section, "GroupByStat", true,
                new ConfigDescription("Grouper d'abord par stat dominante (vie, endurance, eitr, puis plats mixtes), puis trier par valeur nutritive.",
                    null, new SettingLabel("Grouper par stat dominante")));
            s_stations = config.Bind(section, "Stations", "$piece_cauldron,$piece_preptable",
                "Stations concernées (noms internes séparés par des virgules) : chaudron, table de préparation culinaire.");
            LogSortOrder = config.Bind(section, "LogSortOrder", false,
                "Écrire la liste triée (score, réalisable, stats, nom) dans le journal BepInEx à chaque ouverture d'une station. Pour le débogage.");

            s_stations.SettingChanged += (_, _) => ParseStations();
            ParseStations();
        }

        private static void ParseStations()
        {
            Stations = s_stations.Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToHashSet(StringComparer.Ordinal);
        }
    }
}
