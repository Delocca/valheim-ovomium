using BepInEx.Configuration;

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
        /// <summary>Réglage fin rarement utile : rangé sous « Réglages avancés », replié à l'ouverture de la fenêtre.</summary>
        public bool Advanced { get; }

        public SettingLabel(string label, bool restartRequired = false, bool livePreview = false, bool advanced = false)
        {
            Label = label;
            RestartRequired = restartRequired;
            LivePreview = livePreview;
            Advanced = advanced;
        }

        public const string RestartSuffix = "(au redémarrage)";

        public string Display => RestartRequired ? Label + " " + RestartSuffix : Label;

        /// <summary>Le tag d'une option, null si elle n'en a pas (absente de la fenêtre).</summary>
        public static SettingLabel Of(ConfigEntryBase entry)
        {
            foreach (var tag in entry.Description.Tags)
                if (tag is SettingLabel label)
                    return label;
            return null;
        }
    }
}
