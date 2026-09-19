using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Téléchargement de l'archive Windows de la release sur un thread d'arrière-plan : zip dans <c>update.zip.part</c>,
    /// extraction dans <c>update.tmp/</c>, puis seul le dossier <c>BepInEx/plugins/Ovomium/</c> de l'archive est
    /// renommé en <c>update/</c> (même volume : jamais de dossier à moitié rempli). Le patcher Ovomium.Updater
    /// l'installe au lancement suivant. BepInEx lui-même n'est pas touché (installateur .bat).
    /// </summary>
    internal static class UpdateDownloader
    {
        private const string ArchivePluginPath = "BepInEx/plugins/Ovomium";

        /// <summary>À appeler depuis le thread principal (réponse « oui » de la fenêtre).</summary>
        public static void Start()
        {
            if (UpdateState.Downloading || UpdateState.Downloaded || UpdateState.DownloadUrl.Length == 0)
                return;
            UpdateState.Downloading = true;
            Plugin.Log.LogInfo($"Updater : téléchargement de la version {UpdateState.Version} depuis {UpdateState.DownloadUrl}");
            new Thread(Run) { IsBackground = true, Name = "Ovomium.Updater.Download" }.Start();
        }

        private static void Run()
        {
            string plugin = UpdateChecker.PluginFolder;
            string part = Path.Combine(plugin, "update.zip.part");
            string tmp = Path.Combine(plugin, "update.tmp");
            try
            {
                Directory.CreateDirectory(plugin);
                Clean(part, tmp);
                Download(UpdateState.DownloadUrl, part);
                CheckSize(part);
                ZipFile.ExtractToDirectory(part, tmp);
                string source = Path.Combine(tmp, ArchivePluginPath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(Path.Combine(source, "Ovomium.dll")))
                    throw new FileNotFoundException($"archive sans {ArchivePluginPath}/Ovomium.dll");
                string update = UpdateChecker.UpdateFolder;
                if (Directory.Exists(update))
                    Directory.Delete(update, true);
                Directory.Move(source, update);
                UpdateState.Downloaded = true;
                Plugin.Log.LogInfo($"Updater : version {UpdateState.Version} téléchargée dans {update}, installée au prochain lancement");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Updater : téléchargement de la mise à jour échoué : {e.Message}");
            }
            finally
            {
                UpdateState.Downloading = false;
                Clean(part, tmp);
            }
        }

        private static void Download(string url, string destination)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // Mono ancien : TLS 1.0 par défaut
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
            {
                client.Timeout = System.TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"Ovomium/{PluginVersion.Value}");
                using (HttpResponseMessage response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (FileStream output = File.Create(destination))
                        input.CopyTo(output);
                }
            }
        }

        /// <summary>La taille annoncée par l'API GitHub protège d'une archive tronquée.</summary>
        private static void CheckSize(string file)
        {
            long expected = UpdateState.DownloadSize;
            long actual = new FileInfo(file).Length;
            if (expected > 0 && actual != expected)
                throw new IOException($"archive incomplète ({actual} octets au lieu de {expected})");
        }

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
                Plugin.Log.LogWarning($"Updater : nettoyage des temporaires impossible : {e.Message}");
            }
        }
    }
}
