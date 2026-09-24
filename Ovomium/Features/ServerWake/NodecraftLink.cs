using System.Text.RegularExpressions;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Lien de partage Nodecraft (https://app.nodecraft.com/shared/&lt;uuid&gt;) : l'UUID suffit à lire l'état du serveur
    /// et à le démarrer, sans compte (il fait office de secret). Logique pure, testée dans Ovomium.Tests.
    /// </summary>
    internal static class NodecraftLink
    {
        private const string SharedBase = "https://app.nodecraft.com/shared/";
        private static readonly Regex Uuid = new Regex(
            "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

        /// <summary>UUID de partage (minuscules) contenu dans un lien complet ou saisi seul, sinon null.</summary>
        public static string ExtractShareId(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            Match match = Uuid.Match(text);
            return match.Success ? match.Value.ToLowerInvariant() : null;
        }

        public static string PageUrl(string shareId) => SharedBase + shareId;
        public static string StatusUrl(string shareId) => SharedBase + shareId + "/status";
        public static string StartUrl(string shareId) => SharedBase + shareId + "/start";
        /// <summary>Valeur de l'en-tête <c>currentpage</c> envoyé par la page web.</summary>
        public static string PagePath(string shareId) => "/shared/" + shareId;
    }
}
