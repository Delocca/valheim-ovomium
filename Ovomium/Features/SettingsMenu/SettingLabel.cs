namespace Ovomium.Features.SettingsMenu
{
    /// <summary>
    /// Tag de <c>ConfigDescription</c> : libellé court d'une option dans la fenêtre Ovomium. Une option sans ce tag
    /// n'y apparaît pas (elle reste réglable dans le .cfg).
    /// </summary>
    internal sealed class SettingLabel
    {
        public string Label { get; }
        /// <summary>L'option n'agit qu'au prochain lancement du jeu.</summary>
        public bool RestartRequired { get; }

        public SettingLabel(string label, bool restartRequired = false)
        {
            Label = label;
            RestartRequired = restartRequired;
        }

        public string Display => RestartRequired ? Label + " (au redémarrage)" : Label;
    }
}
