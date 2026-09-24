namespace Ovomium.Features.PasswordReveal
{
    /// <summary>
    /// Mots de passe serveur mémorisés : fichier BepInEx/config/ovo.ovomium.passwords.txt, chiffré par
    /// <see cref="SecretFile"/>, clé = <c>ZNet.GetServerString(true)</c>.
    /// </summary>
    internal static class PasswordStore
    {
        private static readonly SecretFile s_file = new SecretFile("ovo.ovomium.passwords.txt", "PasswordReveal");

        /// <summary>Mot de passe mémorisé pour ce serveur, ou null (absent ou illisible).</summary>
        internal static string Get(string serverId) => s_file.Get(serverId);

        internal static void Set(string serverId, string password) => s_file.Set(serverId, password);

        internal static void Remove(string serverId) => s_file.Remove(serverId);
    }
}
