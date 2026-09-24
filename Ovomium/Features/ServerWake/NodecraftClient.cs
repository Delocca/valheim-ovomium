using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Appels HTTP à l'API de la page de partage Nodecraft, tels que les fait la page web (chemin relatif à
    /// app.nodecraft.com, en-têtes <c>oauth: false</c> et <c>currentpage</c>, aucun cookie). Bloquants : à appeler hors du
    /// thread principal. Le corps est lu quel que soit le code HTTP (les erreurs Nodecraft sont du JSON <c>success:false</c>).
    /// Un 429 suspend tous les appels (<see cref="NodecraftThrottle"/>) ; pendant la pause, aucun appel ne part.
    /// </summary>
    internal static class NodecraftClient
    {
        // Cloudflare est devant le site : un User-Agent de navigateur évite d'être pris pour un robot.
        private const string BrowserUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36";
        private const int StartLogLength = 400;

        public static NodecraftStatus GetStatus(string shareId)
        {
            return Send(HttpMethod.Get, NodecraftLink.StatusUrl(shareId), shareId, null);
        }

        /// <summary>Équivalent du bouton « Start Server » de la page (partage sans mot de passe).</summary>
        public static NodecraftStatus Start(string shareId)
        {
            return Send(HttpMethod.Post, NodecraftLink.StartUrl(shareId), shareId, "{\"password\":\"\"}");
        }

        private static NodecraftStatus Send(HttpMethod method, string url, string shareId, string jsonBody)
        {
            int paused = NodecraftThrottle.SecondsLeft;
            if (paused > 0)
                return NodecraftStatus.RateLimited(paused);
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // Mono ancien : TLS 1.0 par défaut
                using (var client = new HttpClient())
                using (HttpRequestMessage request = BuildRequest(method, url, shareId, jsonBody))
                {
                    client.Timeout = System.TimeSpan.FromSeconds(20);
                    using (HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult())
                        return Read(method, response);
                }
            }
            catch (System.Exception e)
            {
                System.Exception root = e.GetBaseException();
                return NodecraftStatus.Failure($"réseau : {root.Message}");
            }
        }

        private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string shareId, string jsonBody)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.TryAddWithoutValidation("oauth", "false");
            request.Headers.TryAddWithoutValidation("currentpage", NodecraftLink.PagePath(shareId));
            if (jsonBody != null)
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }

        /// <summary>Réponse → état ; journalise la réponse au démarrage et toute réponse inattendue ; 429 → pause globale.</summary>
        private static NodecraftStatus Read(HttpMethod method, HttpResponseMessage response)
        {
            string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
            int code = (int)response.StatusCode;
            string call = method == HttpMethod.Post ? "démarrage" : "état";
            if (method == HttpMethod.Post)
                Plugin.Log.LogInfo($"ServerWake : réponse au démarrage : HTTP {code}, "
                    + NodecraftHttp.Truncate(NodecraftHttp.Collapse(body), StartLogLength));
            if (!response.IsSuccessStatusCode || !body.TrimStart().StartsWith("{", System.StringComparison.Ordinal))
                Plugin.Log.LogWarning($"ServerWake : réponse inattendue de Nodecraft ({call}) : " + NodecraftHttp.Describe(code,
                    Header(response, "Retry-After"), Header(response, "cf-ray"), Header(response, "Server"), body));
            if (code == 429)
            {
                int seconds = NodecraftHttp.RetryAfterSeconds(Header(response, "Retry-After"), System.DateTime.UtcNow);
                NodecraftThrottle.Pause(seconds);
                Plugin.Log.LogWarning($"ServerWake : Nodecraft limite les requêtes, appels suspendus {seconds} s");
                return NodecraftStatus.RateLimited(seconds);
            }
            NodecraftStatus status = NodecraftStatus.Parse(body);
            if (status.State == WakeState.Error && !response.IsSuccessStatusCode && status.Code.Length == 0)
                return NodecraftStatus.Failure($"HTTP {code} ({status.Message})");
            return status;
        }

        private static string Header(HttpResponseMessage response, string name)
        {
            return response.Headers.TryGetValues(name, out IEnumerable<string> values) ? string.Join(", ", values) : "";
        }
    }
}
