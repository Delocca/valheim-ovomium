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
        /// <summary>
        /// L'effet du réglage se voit sur le rendu pendant qu'on glisse le curseur : la fenêtre s'efface alors
        /// (<see cref="SliderPeek"/>). Faux pour les durées, portées, densités… dont rien n'est visible sur-le-champ.
        /// </summary>
        public bool LivePreview { get; }

        public SettingLabel(string label, bool restartRequired = false, bool livePreview = false)
        {
            Label = label;
            RestartRequired = restartRequired;
            LivePreview = livePreview;
        }

        public string Display => RestartRequired ? Label + " (au redémarrage)" : Label;
    }
}
