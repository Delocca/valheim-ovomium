using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx;

namespace Ovomium.Features.PasswordReveal
{
    /// <summary>
    /// Mots de passe serveur mémorisés : fichier BepInEx/config/ovo.ovomium.passwords.txt, une ligne par serveur
    /// « identifiant TAB base64(IV + AES-CBC(mot de passe)) ». Clé dérivée (PBKDF2) du nom de machine et du nom
    /// d'utilisateur avec un sel fixe : ce n'est qu'une obfuscation locale, pas une protection contre quelqu'un
    /// ayant accès à la session (il peut lancer le jeu, ou dériver la clé avec ce code).
    /// </summary>
    internal static class PasswordStore
    {
        private const string FileName = "ovo.ovomium.passwords.txt";
        private const int KeyBytes = 32;
        private const int Iterations = 10000;
        // Ancien nom du mod conservé volontairement : changer le sel rendrait illisibles les mots de passe déjà mémorisés.
        private static readonly byte[] s_salt = Encoding.UTF8.GetBytes("OvoMiam.PasswordReveal.v1");

        private static string FilePath => Path.Combine(Paths.ConfigPath, FileName);

        /// <summary>Mot de passe mémorisé pour ce serveur, ou null (absent ou illisible).</summary>
        internal static string Get(string serverId)
        {
            if (!Load().TryGetValue(serverId, out string encrypted))
                return null;
            try
            {
                return Decrypt(encrypted);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"PasswordReveal : mot de passe illisible pour {serverId} ({e.GetType().Name})");
                return null;
            }
        }

        internal static void Set(string serverId, string password)
        {
            Dictionary<string, string> entries = Load();
            entries[serverId] = Encrypt(password);
            Save(entries);
        }

        internal static void Remove(string serverId)
        {
            Dictionary<string, string> entries = Load();
            if (entries.Remove(serverId))
                Save(entries);
        }

        private static Dictionary<string, string> Load()
        {
            Dictionary<string, string> entries = new Dictionary<string, string>();
            if (!File.Exists(FilePath))
                return entries;
            try
            {
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int tab = line.IndexOf('\t');
                    if (tab > 0)
                        entries[line.Substring(0, tab)] = line.Substring(tab + 1);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"PasswordReveal : lecture de {FilePath} impossible ({e.Message})");
            }
            return entries;
        }

        private static void Save(Dictionary<string, string> entries)
        {
            List<string> lines = new List<string>(entries.Count);
            foreach (KeyValuePair<string, string> entry in entries)
                lines.Add(entry.Key + "\t" + entry.Value);
            try
            {
                File.WriteAllLines(FilePath, lines);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"PasswordReveal : écriture de {FilePath} impossible ({e.Message})");
            }
        }

        private static byte[] DeriveKey()
        {
            string secret = Environment.MachineName + "|" + Environment.UserName;
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(secret, s_salt, Iterations))
                return kdf.GetBytes(KeyBytes);
        }

        private static string Encrypt(string password)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKey();
                aes.GenerateIV();
                byte[] plain = Encoding.UTF8.GetBytes(password);
                byte[] cipher;
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
                byte[] blob = new byte[aes.IV.Length + cipher.Length];
                Buffer.BlockCopy(aes.IV, 0, blob, 0, aes.IV.Length);
                Buffer.BlockCopy(cipher, 0, blob, aes.IV.Length, cipher.Length);
                return Convert.ToBase64String(blob);
            }
        }

        private static string Decrypt(string encoded)
        {
            byte[] blob = Convert.FromBase64String(encoded);
            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKey();
                int ivLength = aes.BlockSize / 8;
                byte[] iv = new byte[ivLength];
                Buffer.BlockCopy(blob, 0, iv, 0, ivLength);
                aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] plain = decryptor.TransformFinalBlock(blob, ivLength, blob.Length - ivLength);
                    return Encoding.UTF8.GetString(plain);
                }
            }
        }
    }
}
