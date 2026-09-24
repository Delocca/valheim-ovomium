using System.Collections.Generic;
using Ovomium.Features.Updater;

namespace Ovomium.Features.ServerWake
{
    /// <summary>État d'un serveur Nodecraft vu par le mod, déduit de la réponse de l'API de partage.</summary>
    internal enum WakeState
    {
        /// <summary>Pas encore interrogé (pastille de la liste).</summary>
        Unknown,
        /// <summary>Arrêté ou en hibernation : un démarrage est possible.</summary>
        Offline,
        /// <summary>En cours de démarrage (ou tout statut transitoire inconnu).</summary>
        Starting,
        Online,
        /// <summary>Partage verrouillé par un mot de passe (<c>instances.jit_locked</c>).</summary>
        Locked,
        /// <summary>Archivé et plus démarrable depuis le lien (<c>jit_available</c> faux).</summary>
        Unavailable,
        /// <summary>Réponse inexploitable : erreur réseau, page HTML (Cloudflare), JSON sans succès.</summary>
        Error,
        /// <summary>HTTP 429 ou appels en pause (<see cref="NodecraftThrottle"/>) : transitoire, l'état précédent tient.</summary>
        RateLimited,
    }

    /// <summary>
    /// Réponse de <c>GET /shared/{uuid}/status</c> (ou de <c>POST …/start</c>, dont seule l'enveloppe
    /// <c>{code, message, success, data}</c> sert). Exemple réel (2026-09-24) : <c>data.status</c> ∈ offline / starting /
    /// started / online, <c>data.jit_status</c> ∈ stored / ready_to_store / ready / waking / live,
    /// <c>data.network.address</c>, <c>data.ports[]</c> avec <c>purpose</c> « join » ou « query ». Logique pure, testée
    /// dans Ovomium.Tests.
    /// </summary>
    internal sealed class NodecraftStatus
    {
        public WakeState State { get; private set; }
        public bool Success { get; private set; }
        public string Code { get; private set; } = "";
        /// <summary>Message d'erreur lisible (vide si succès).</summary>
        public string Message { get; private set; } = "";
        public string Name { get; private set; } = "";
        public string Status { get; private set; } = "";
        public string JitStatus { get; private set; } = "";
        public string Address { get; private set; } = "";
        public int GamePort { get; private set; }
        /// <summary>Attente avant le prochain appel (état <see cref="WakeState.RateLimited"/> seulement).</summary>
        public int RetryAfterSeconds { get; private set; }

        public static NodecraftStatus Failure(string message)
        {
            return new NodecraftStatus { State = WakeState.Error, Message = message };
        }

        public static NodecraftStatus RateLimited(int seconds)
        {
            return new NodecraftStatus
            {
                State = WakeState.RateLimited,
                RetryAfterSeconds = seconds,
                Message = $"Nodecraft limite les requêtes, nouvel essai dans {seconds} s",
            };
        }

        public static NodecraftStatus Parse(string body)
        {
            string trimmed = (body ?? "").TrimStart();
            if (trimmed.Length == 0)
                return Failure("réponse vide");
            if (trimmed[0] == '<')
                return Failure("page web reçue au lieu de données (protection anti-robots de Nodecraft ?)");
            object root;
            try
            {
                root = MiniJson.Parse(trimmed);
            }
            catch (System.FormatException e)
            {
                return Failure(e.Message);
            }
            var result = new NodecraftStatus
            {
                Success = MiniJson.Get<object>(root, "success") is bool ok && ok,
                Code = MiniJson.Get<string>(root, "code") ?? "",
                Message = MiniJson.Get<string>(root, "message") ?? "",
            };
            result.ReadData(MiniJson.Get<Dictionary<string, object>>(root, "data"));
            result.State = result.Decide(MiniJson.Get<Dictionary<string, object>>(root, "data"));
            if (result.Success)
                result.Message = "";
            else if (result.Message.Length == 0)
                result.Message = result.Code.Length > 0 ? result.Code : "échec sans message";
            return result;
        }

        private void ReadData(Dictionary<string, object> data)
        {
            if (data == null)
                return;
            Name = MiniJson.Get<string>(data, "name") ?? "";
            Status = MiniJson.Get<string>(data, "status") ?? "";
            JitStatus = MiniJson.Get<string>(data, "jit_status") ?? "";
            Address = MiniJson.Get<string>(MiniJson.Get<object>(data, "network"), "address") ?? "";
            if (MiniJson.Get<object>(data, "default_port") is double port)
                GamePort = (int)port;
            List<object> ports = MiniJson.Get<List<object>>(data, "ports");
            if (ports == null)
                return;
            foreach (object entry in ports)
            {
                if (!(MiniJson.Get<object>(entry, "host") is double host))
                    continue;
                if (MiniJson.Get<string>(entry, "purpose") == "join" && GamePort == 0)
                    GamePort = (int)host;
            }
        }

        private WakeState Decide(Dictionary<string, object> data)
        {
            if (Code == "instances.jit_locked")
                return WakeState.Locked;
            if (!Success || data == null)
                return WakeState.Error;
            bool available = !(MiniJson.Get<object>(data, "jit_available") is bool jit) || jit;
            if (JitStatus == "stored")
                // Archivé : plus de « status » dans la réponse ; la page web autorise alors le démarrage.
                return available ? WakeState.Offline : WakeState.Unavailable;
            switch (Status)
            {
                case "online": return WakeState.Online;
                case "offline": return WakeState.Offline;
                default: return WakeState.Starting;
            }
        }
    }
}
