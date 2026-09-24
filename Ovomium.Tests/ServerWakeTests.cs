using System;
using Ovomium.Features.ServerWake;

namespace Ovomium.Tests
{
    /// <summary>Lien Nodecraft, lecture de l'état Nodecraft, forme stockée d'un lien, réponses HTTP (Retry-After, page Cloudflare), pause après un 429.</summary>
    internal static class ServerWakeTests
    {
        private const string Id = "312f5c1d-4ee8-4357-9a35-3572d108185a";
        // Réponse réelle abrégée (2026-09-24), bloc « game » retiré.
        private const string RealStatus = "{\"code\":\"instances.jit_found\",\"message\":\"Successfully found Nodecraft Lite instance by given share URL.\",\"success\":true,"
            + "\"data\":{\"jit_status\":\"live\",\"jit_share_password_enabled\":false,\"name\":\"Crapoutchblouf\",\"module\":\"valheim\","
            + "\"network\":{\"address\":\"46.151.199.8\",\"hostname\":\"crapoutchblouf\",\"domain\":\"nodecraft.gg\"},"
            + "\"ports\":[{\"container\":2456,\"default\":true,\"host\":2456,\"name\":\"Game Port\",\"purpose\":\"join\",\"type\":\"udp\"},"
            + "{\"container\":2457,\"host\":2457,\"name\":\"Steam Query\",\"purpose\":\"query\",\"type\":\"udp\"}],"
            + "\"default_port\":2456,\"jit_available\":true,\"status\":\"offline\"}}";

        private static int s_failures;

        public static int Run()
        {
            LinkTests();
            StatusTests();
            ServerLinkTests();
            HttpTests();
            ThrottleTests();
            Console.WriteLine(s_failures == 0 ? "ServerWake : tous les tests passent" : $"ServerWake : {s_failures} échec(s)");
            return s_failures;
        }

        private static void LinkTests()
        {
            Check("lien complet", NodecraftLink.ExtractShareId("https://app.nodecraft.com/shared/" + Id), Id);
            Check("sans https, espaces, slash final", NodecraftLink.ExtractShareId("  app.nodecraft.com/shared/" + Id + "/ "), Id);
            Check("majuscules normalisées", NodecraftLink.ExtractShareId(Id.ToUpperInvariant()), Id);
            Check("adresse ordinaire", NodecraftLink.ExtractShareId("46.151.199.8:2456"), null);
            Check("vide", NodecraftLink.ExtractShareId(""), null);
            Check("URL d'état", NodecraftLink.StatusUrl(Id), "https://app.nodecraft.com/shared/" + Id + "/status");
        }

        private static void StatusTests()
        {
            NodecraftStatus real = NodecraftStatus.Parse(RealStatus);
            Check("réel : état", real.State, WakeState.Offline);
            Check("réel : succès", real.Success, true);
            Check("réel : nom", real.Name, "Crapoutchblouf");
            Check("réel : adresse", real.Address, "46.151.199.8");
            Check("réel : port de jeu", real.GamePort, 2456);
            Check("en ligne", NodecraftStatus.Parse(RealStatus.Replace("\"offline\"", "\"online\"")).State, WakeState.Online);
            Check("démarrage", NodecraftStatus.Parse(RealStatus.Replace("\"offline\"", "\"starting\"")).State, WakeState.Starting);
            Check("statut inconnu = transitoire", NodecraftStatus.Parse(RealStatus.Replace("\"offline\"", "\"stopping\"")).State, WakeState.Starting);
            string archived = RealStatus.Replace("\"jit_available\":true", "\"jit_available\":false").Replace("\"live\"", "\"stored\"");
            Check("archivé", NodecraftStatus.Parse(archived).State, WakeState.Unavailable);
            string storedAvailable = RealStatus.Replace("\"live\"", "\"stored\"").Replace(",\"status\":\"offline\"", "");
            Check("archivé démarrable, sans status", NodecraftStatus.Parse(storedAvailable).State, WakeState.Offline);
            // Cas courant à l'ajout : archivé sans bloc network → réveil proposé (démarrable, adresse vide).
            string storedNoNetwork = storedAvailable.Replace("\"network\":{\"address\":\"46.151.199.8\",\"hostname\":\"crapoutchblouf\",\"domain\":\"nodecraft.gg\"},", "");
            NodecraftStatus sleeping = NodecraftStatus.Parse(storedNoNetwork);
            Check("archivé sans network : état", sleeping.State, WakeState.Offline);
            Check("archivé sans network : adresse", sleeping.Address, "");
            Check("archivé sans network : nom", sleeping.Name, "Crapoutchblouf");
            NodecraftStatus locked = NodecraftStatus.Parse("{\"code\":\"instances.jit_locked\",\"message\":\"Locked\",\"success\":false}");
            Check("verrouillé", locked.State, WakeState.Locked);
            Check("verrouillé : message", locked.Message, "Locked");
            Check("page HTML", NodecraftStatus.Parse("<!DOCTYPE html><title>Just a moment...</title>").State, WakeState.Error);
            Check("JSON cassé", NodecraftStatus.Parse("{\"success\":").State, WakeState.Error);
            Check("échec sans données", NodecraftStatus.Parse("{\"success\":false,\"code\":\"x.y\"}").State, WakeState.Error);
            Check("échec : message = code", NodecraftStatus.Parse("{\"success\":false,\"code\":\"x.y\"}").Message, "x.y");
        }

        private static void ServerLinkTests()
        {
            ServerLink link = ServerLink.Deserialize(new ServerLink(Id, "Crapoutchblouf").Serialize());
            Check("aller-retour : UUID", link.ShareId, Id);
            Check("aller-retour : nom", link.Name, "Crapoutchblouf");
            Check("sans nom", ServerLink.Deserialize(Id).Name, "");
        }

        private static void HttpTests()
        {
            var now = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
            Check("Retry-After : secondes", NodecraftHttp.RetryAfterSeconds("45", now), 45);
            Check("Retry-After : absent = 30 s", NodecraftHttp.RetryAfterSeconds(null, now), 30);
            Check("Retry-After : illisible = 30 s", NodecraftHttp.RetryAfterSeconds("bientôt", now), 30);
            Check("Retry-After : plafond", NodecraftHttp.RetryAfterSeconds("3600", now), 120);
            Check("Retry-After : zéro = 1 s", NodecraftHttp.RetryAfterSeconds("0", now), 1);
            Check("Retry-After : date", NodecraftHttp.RetryAfterSeconds("Thu, 24 Sep 2026 12:01:00 GMT", now), 60);
            Check("Retry-After : date passée", NodecraftHttp.RetryAfterSeconds("Thu, 24 Sep 2026 11:00:00 GMT", now), 1);
            const string page = "<!DOCTYPE html><html><head><title>Access denied | app.nodecraft.com used Cloudflare to restrict access</title>"
                + "<style>body{color:red}</style><script>var x = '<b>';</script></head><body><h1>Error 1015</h1>\n  <p>You are being "
                + "<b>rate limited</b> &amp; blocked.</p></body></html>";
            Check("titre", NodecraftHttp.PageTitle(page), "Access denied | app.nodecraft.com used Cloudflare to restrict access");
            Check("titre absent", NodecraftHttp.PageTitle("<p>x</p>"), "");
            Check("texte utile", NodecraftHttp.TextSnippet(page, 200), "Error 1015 You are being rate limited & blocked.");
            Check("texte tronqué", NodecraftHttp.TextSnippet(page, 10), "Error 1015…");
            Check("résumé page", NodecraftHttp.Describe(429, "30", "8c1f-CDG", "cloudflare", page),
                "HTTP 429, Retry-After 30, cf-ray 8c1f-CDG, server cloudflare, titre « Access denied | app.nodecraft.com used "
                + "Cloudflare to restrict access », texte « Error 1015 You are being rate limited & blocked. »");
            Check("résumé JSON", NodecraftHttp.Describe(500, "", null, "", "{\"success\": false}"),
                "HTTP 500, corps « {\"success\": false} »");
            Check("état en pause", NodecraftStatus.RateLimited(30).State, WakeState.RateLimited);
        }

        private static void ThrottleTests()
        {
            Check("pas de pause au départ", NodecraftThrottle.IsPaused, false);
            NodecraftThrottle.Pause(30);
            Check("pause posée", NodecraftThrottle.SecondsLeft, 30);
            NodecraftThrottle.Pause(5);
            Check("pause plus longue gardée", NodecraftThrottle.SecondsLeft, 30);
        }

        private static void Check<T>(string label, T actual, T expected)
        {
            if (Equals(actual, expected))
                return;
            s_failures++;
            Console.WriteLine($"ÉCHEC {label} : attendu « {expected} », obtenu « {actual} »");
        }
    }
}
