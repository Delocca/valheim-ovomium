using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace OvoMiam.Features.FoodRecipeSort
{
    /// <summary>Réglages de la fonctionnalité « tri des recettes des stations de cuisine ».</summary>
    internal static class FoodRecipeSortConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> CraftableFirst { get; private set; }
        public static ConfigEntry<bool> GroupByStat { get; private set; }
        private static ConfigEntry<string> s_stations;

        /// <summary>Noms (m_name) des stations dont la liste de recettes est triée.</summary>
        public static HashSet<string> Stations { get; private set; }

        public static void Bind(ConfigFile config)
        {
            const string section = "FoodRecipeSort";
            Enabled = config.Bind(section, "Enabled", true,
                "Trier les recettes des stations de cuisine par valeur nutritive (santé + endurance + eitr) décroissante. " +
                "Un plat cru compte pour la valeur de sa version cuite.");
            CraftableFirst = config.Bind(section, "CraftableFirst", true,
                "Garder les recettes réalisables en tête de liste (comme le jeu de base) avant les autres critères.");
            GroupByStat = config.Bind(section, "GroupByStat", true,
                "Grouper d'abord par stat dominante (vie, endurance, eitr, puis plats mixtes), puis trier par valeur nutritive.");
            s_stations = config.Bind(section, "Stations", "$piece_cauldron,$piece_preptable",
                "Stations concernées (noms internes séparés par des virgules) : chaudron, table de préparation culinaire.");

            s_stations.SettingChanged += (_, _) => ParseStations();
            ParseStations();
        }

        private static void ParseStations()
        {
            Stations = s_stations.Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToHashSet(StringComparer.Ordinal);
        }
    }
}
