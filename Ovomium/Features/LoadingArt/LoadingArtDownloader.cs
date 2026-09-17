using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Ovomium.Features.LoadingArt
{
    /// <summary>
    /// Téléchargement unique du zip des artworks (option DownloadUrl) quand le dossier Folder n'existe pas, sur un
    /// thread d'arrière-plan. Écriture atomique : zip dans <c>loading.zip.part</c>, extraction dans
    /// <c>loading.tmp/</c>, puis renommage vers Folder (même volume : jamais de dossier à moitié rempli). Les
    /// temporaires sont posés à côté de Folder. Une seule tentative par session.
    /// </summary>
    internal static class LoadingArtDownloader
    {
        private static bool s_started;

        /// <summary>À appeler une fois au démarrage (thread principal).</summary>
        internal static void StartIfNeeded()
        {
            if (s_started)
                return;
            s_started = true;
            string url = (LoadingArtConfig.DownloadUrl.Value ?? "").Trim();
            string folder = LoadingArtLibrary.Folder;
            if (!LoadingArtConfig.Enabled.Value || url.Length == 0 || Directory.Exists(folder))
                return;
            Plugin.Log.LogInfo($"LoadingArt : dossier absent, téléchargement des artworks depuis {url}");
            new Thread(() => Run(url, folder)) { IsBackground = true, Name = "Ovomium.LoadingArt" }.Start();
        }

        private static void Run(string url, string folder)
        {
            string parent = Path.GetDirectoryName(folder);
            string part = Path.Combine(parent, "loading.zip.part");
            string tmp = Path.Combine(parent, "loading.tmp");
            try
            {
                Directory.CreateDirectory(parent);
                Clean(part, tmp);
                if (!Download(url, part))
                {
                    Plugin.Log.LogInfo("LoadingArt : artworks pas disponibles pour l'instant (dépôt fermé), nouvel essai au prochain lancement");
                    return;
                }
                ZipFile.ExtractToDirectory(part, tmp);
                string source = Path.Combine(tmp, "loading"); // le zip contient un dossier loading/
                if (!Directory.Exists(source))
                    source = tmp; // zip sans dossier racine : images directement à la racine
                Directory.Move(source, folder);
                LoadingArtLibrary.Invalidate(); // relecture sur le thread principal au prochain écran de chargement
                Plugin.Log.LogInfo($"LoadingArt : {Directory.GetFiles(folder).Length} artworks téléchargés dans {folder}");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"LoadingArt : téléchargement des artworks échoué : {e.Message}");
            }
            finally
            {
                Clean(part, tmp);
            }
        }

        /// <summary>
        /// Renvoie false si l'archive n'est pas publiée (404, ou page HTML de connexion GitHub) ; lève sur toute
        /// autre erreur. Redirections suivies (GitHub renvoie vers objects.githubusercontent.com).
        /// </summary>
        private static bool Download(string url, string destination)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // Mono ancien : TLS 1.0 par défaut
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
            {
                client.Timeout = System.TimeSpan.FromMinutes(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"Ovomium/{PluginVersion.Value}");
                using (HttpResponseMessage response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return false;
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentType?.MediaType == "text/html")
                        return false;
                    using (Stream input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (FileStream output = File.Create(destination))
                        input.CopyTo(output);
                }
            }
            return true;
        }

        /// <summary>Supprime nos temporaires (restes d'un essai précédent ou de celui-ci).</summary>
        private static void Clean(string part, string tmp)
        {
            try
            {
                if (File.Exists(part))
                    File.Delete(part);
                if (Directory.Exists(tmp))
                    Directory.Delete(tmp, true);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"LoadingArt : nettoyage des temporaires impossible : {e.Message}");
            }
        }
    }
}
