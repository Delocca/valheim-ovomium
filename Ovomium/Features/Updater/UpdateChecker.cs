using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Vérification unique par session, sur un thread d'arrière-plan, des releases GitHub : si une est plus récente que
    /// la version du mod, publie la plus récente (version, asset Windows) et le changelog cumulé des versions manquées
    /// (<see cref="ReleaseNotes"/>) dans <see cref="UpdateState"/>. Dépôt privé (404 ou page HTML) : rien, silencieusement, comme LoadingArt.
    /// </summary>
    internal static class UpdateChecker
    {
        private static readonly Regex AssetName = new Regex(@"^Ovomium-(.+)-windows\.zip$", RegexOptions.Compiled);

        public static string PluginFolder => Path.Combine(Paths.PluginPath, "Ovomium");
        /// <summary>Dossier déposé par le téléchargement, consommé par le patcher Ovomium.Updater au lancement suivant.</summary>
        public static string UpdateFolder => Path.Combine(PluginFolder, "update");

        /// <summary>À appeler au menu principal (thread principal).</summary>
        public static void StartIfNeeded()
        {
            if (UpdateState.Started)
                return;
            UpdateState.Started = true;
            if (!UpdaterConfig.Enabled.Value)
                return;
            // Dossier update/ de la version qui tourne déjà (installée à chaud) : rien en attente, on revérifie.
            if (Directory.Exists(UpdateFolder) && ReadPendingVersion() != PluginVersion.Value)
            {
                UpdateState.Version = ReadPendingVersion();
                UpdateState.Downloaded = true;
                Plugin.Log.LogInfo($"Updater : mise à jour {UpdateState.Version} déjà téléchargée, installée au prochain lancement");
                return;
            }
            string url = (UpdaterConfig.ReleasesApiUrl.Value ?? "").Trim();
            if (url.Length == 0)
                return;
            url = ReleaseNotes.ListUrl(url);
            new Thread(() => Run(url)) { IsBackground = true, Name = "Ovomium.Updater.Check" }.Start();
        }

        public static string ReadPendingVersion()
        {
            string file = Path.Combine(UpdateFolder, "version.txt");
            return File.Exists(file) ? File.ReadAllText(file).Trim() : "?";
        }

        private static void Run(string url)
        {
            try
            {
                string json = Fetch(url);
                if (json == null)
                {
                    Plugin.Log.LogInfo("Updater : dépôt fermé, pas de vérification de version possible (nouvel essai au prochain lancement)");
                    return;
                }
                Publish(MiniJson.Parse(json));
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Updater : vérification de version échouée : {e.Message}");
            }
        }

        /// <summary>Corps JSON de la release, ou null si le dépôt est privé (404, ou page HTML de connexion).</summary>
        private static string Fetch(string url)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // Mono ancien : TLS 1.0 par défaut
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
            {
                client.Timeout = System.TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"Ovomium/{PluginVersion.Value}");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                using (HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return null;
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentType?.MediaType == "text/html")
                        return null;
                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            }
        }

        private static void Publish(object json)
        {
            ReleaseNotes.Pending pending = ReleaseNotes.Select(json, System.Version.Parse(PluginVersion.Value));
            if (pending == null)
            {
                Plugin.Log.LogInfo($"Updater : version {PluginVersion.Value} à jour (dernière publiée : "
                    + $"{ReleaseNotes.Latest(json)?.ToString() ?? "aucune lisible"})");
                return;
            }
            System.Version latest = pending.Version;
            var asset = FindWindowsAsset(MiniJson.Get<List<object>>(pending.Release, "assets"));
            if (asset == null)
            {
                Plugin.Log.LogWarning($"Updater : release {latest} sans archive Ovomium-*-windows.zip");
                return;
            }
            UpdateState.Version = latest.ToString();
            UpdateState.Changelog = pending.Changelog;
            UpdateState.DownloadUrl = MiniJson.Get<string>(asset, "browser_download_url") ?? "";
            UpdateState.DownloadSize = MiniJson.Get<object>(asset, "size") is double size ? (long)size : 0L;
            UpdateState.Available = true;
            Plugin.Log.LogInfo($"Updater : version {latest} disponible ({UpdateState.DownloadSize / 1024} Ko)");
        }

        private static object FindWindowsAsset(List<object> assets)
        {
            if (assets == null)
                return null;
            foreach (object asset in assets)
                if (AssetName.IsMatch(MiniJson.Get<string>(asset, "name") ?? ""))
                    return asset;
            return null;
        }
    }
}
