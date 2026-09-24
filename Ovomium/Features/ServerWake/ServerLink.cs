namespace Ovomium.Features.ServerWake
{
    /// <summary>Lien Nodecraft d'un favori : UUID de partage et nom du serveur (affiché à la place de l'IP).</summary>
    internal sealed class ServerLink
    {
        public readonly string ShareId;
        public readonly string Name;

        public ServerLink(string shareId, string name)
        {
            ShareId = shareId;
            Name = name ?? "";
        }

        public string Serialize() => ShareId + "\n" + Name;

        public static ServerLink Deserialize(string value)
        {
            int newline = value.IndexOf('\n');
            return newline < 0 ? new ServerLink(value, "") : new ServerLink(value.Substring(0, newline), value.Substring(newline + 1));
        }
    }
}
