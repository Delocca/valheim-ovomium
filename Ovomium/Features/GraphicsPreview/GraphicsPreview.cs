using Valheim.SettingsGui;

namespace Ovomium.Features.GraphicsPreview
{
    /// <summary>
    /// État de l'aperçu : réglages du manager mémorisés à l'ouverture de l'onglet, réappliqués à chaque changement
    /// (coalescés par frame), restaurés à la fermeture sans OK. L'application passe par les champs du manager
    /// (<c>m_currentPlayerSettings</c>, <c>m_currentPresetID</c>) puis <c>ApplyGraphicsSettingsToCurrentSession</c>,
    /// exactement ce que fait OK moins l'écriture des préférences : rien ne fuit si la fenêtre est annulée.
    /// </summary>
    internal static class GraphicsPreview
    {
        private static bool s_active;
        private static bool s_dirty;
        private static bool s_applying;
        private static GraphicsSettingsState s_savedRaw;
        private static int s_savedPresetId;

        /// <summary>Vrai pendant notre application : <c>GraphicsSettings.UpdateUI</c> doit alors être sauté.</summary>
        public static bool Applying => s_applying;

        public static void Begin()
        {
            var manager = GraphicsSettingsManager.Instance;
            if (manager == null)
                return;
            s_savedRaw = manager.m_currentPlayerSettings;
            s_savedPresetId = manager.m_currentPresetID;
            s_active = true;
            s_dirty = false;
        }

        public static void MarkDirty()
        {
            if (s_active)
                s_dirty = true;
        }

        /// <summary>Applique l'état courant de l'onglet si un réglage a changé depuis la dernière frame.</summary>
        public static void Flush(GraphicsSettings tab)
        {
            if (!s_active || !s_dirty)
                return;
            s_dirty = false;
            var manager = GraphicsSettingsManager.Instance;
            if (manager == null)
                return;
            var config = manager.GetCurrentGraphicsModeConfiguration();
            var preset = tab.m_currentPresetModified ? null : tab.GetCurrentPreset(config);
            Apply(tab.m_currentSettingsRaw, preset != null ? preset.m_type.ID : GraphicsSettingsManager.c_customPresetId);
        }

        /// <summary>OK : le jeu sauvegarde et applique lui-même, plus rien à restaurer.</summary>
        public static void Confirm() => s_active = false;

        /// <summary>Fermeture sans OK : retour aux réglages mémorisés.</summary>
        public static void End()
        {
            if (!s_active)
                return;
            s_active = false;
            Apply(s_savedRaw, s_savedPresetId);
        }

        private static void Apply(GraphicsSettingsState raw, int presetId)
        {
            var manager = GraphicsSettingsManager.Instance;
            if (manager == null)
                return;
            manager.m_currentPlayerSettings = raw;
            manager.m_currentPresetID = presetId;
            s_applying = true;
            try { manager.ApplyGraphicsSettingsToCurrentSession(); }
            finally { s_applying = false; }
        }

        public static void Unload() => End();
    }
}
