using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TMPro;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Lien Nodecraft collé dans le dialogue vanilla « Ajouter un serveur » à la place d'une adresse : l'état Nodecraft
    /// donne IP, port et nom, le favori est ajouté par IP comme un favori ordinaire (<c>OnManualAddToFavoritesSuccess</c>,
    /// sans doublon : un favori de même adresse reçoit le lien) et le lien est rangé à part (<see cref="ServerLinkStore"/>).
    /// Supprimer le favori retire son lien. Pendant la requête, <c>m_isAwaitingServerAdd</c> grise les boutons du
    /// dialogue comme pour une résolution DNS vanilla ; coroutine sur FejdStartup, qui survit à la fermeture du dialogue.
    /// Serveur archivé (sans adresse) : réveil proposé, ajout à la fin du réveil (<see cref="OfferWake"/>).
    /// </summary>
    internal static class ServerWakeAdd
    {
        // GuiInputField (gui_framework.dll, non référencé) dérive de TMP_InputField : lecture par réflexion.
        private static readonly FieldInfo s_addServerInput = AccessTools.Field(typeof(ServerListGui), "m_addServerTextInput");

        /// <summary>Prefix d'<c>OnAddServer</c> : faux si le texte est un lien Nodecraft (traité ici).</summary>
        public static bool TryHandle(ServerListGui gui)
        {
            if (!ServerWakeConfig.Enabled.Value || !(s_addServerInput.GetValue(gui) is TMP_InputField input))
                return true;
            string shareId = NodecraftLink.ExtractShareId(input.text);
            if (shareId == null)
                return true;
            if (!gui.m_isAwaitingServerAdd && gui.m_startup != null)
            {
                gui.m_isAwaitingServerAdd = true;
                gui.m_startup.StartCoroutine(Resolve(gui, shareId));
            }
            return false;
        }

        /// <summary>Favori supprimé (<c>OnRemoveServerConfirm</c>) : son lien n'a plus lieu d'être.</summary>
        public static void OnRemoved(ServerListGui gui, ServerJoinData removed)
        {
            if (!removed.IsValid || gui.m_favoriteServersList.Contains(removed) || ServerLinkStore.Get(removed) == null)
                return;
            ServerLinkStore.Set(removed, null);
            Plugin.Log.LogInfo($"ServerWake : favori {removed} supprimé, lien Nodecraft retiré");
        }

        private static IEnumerator Resolve(ServerListGui gui, string shareId)
        {
            Task<NodecraftStatus> task = Task.Run(() => NodecraftClient.GetStatus(shareId));
            while (!task.IsCompleted)
                yield return null;
            gui.m_isAwaitingServerAdd = false;
            NodecraftStatus status = task.Result;
            ServerJoinData server = LinkedFavorite.FromStatus(status);
            if (!server.IsValid && (status.State == WakeState.Offline || status.State == WakeState.Starting))
            {
                OfferWake(gui.m_startup, shareId, status);
                yield break;
            }
            string error = ErrorOf(status, server);
            if (error != null)
            {
                Plugin.Log.LogWarning($"ServerWake : ajout depuis le lien impossible : {error}");
                UnifiedPopup.Push(new WarningPopup("Ajout du serveur Nodecraft impossible", error, UnifiedPopup.Pop, localizeText: false));
                yield break;
            }
            AddLinked(shareId, status.Name, server, status);
        }

        /// <summary>
        /// Serveur archivé (pas d'adresse tant qu'il dort, cas courant) : réveil proposé, puis ajout dès que l'adresse est
        /// connue (<see cref="ServerWakeSession.BeginAdd"/>), sans connexion.
        /// </summary>
        private static void OfferWake(FejdStartup startup, string shareId, NodecraftStatus status)
        {
            string name = status.Name;
            string who = name.Length > 0 ? name : "Le serveur";
            string question = status.State == WakeState.Starting
                ? $"{who} démarre : attendre qu'il soit prêt pour l'ajouter ?"
                : $"{who} dort : le réveiller pour l'ajouter ?";
            Plugin.Log.LogInfo($"ServerWake : {who} sans adresse (status={status.Status}, jit_status={status.JitStatus}), réveil proposé");
            PopupBase ask = null;
            ask = ServerWakePopups.ShowChoice("Ajouter un serveur Nodecraft", question,
                "Oui", () =>
                {
                    ServerWakePopups.Close(ask);
                    ServerWakeSession.BeginAdd(startup, shareId, name, (server, ready) => AddWoken(shareId, name, server, ready));
                },
                "Non", () => ServerWakePopups.Close(ask));
        }

        private static void AddWoken(string shareId, string name, ServerJoinData server, NodecraftStatus status)
        {
            string finalName = status.Name.Length > 0 ? status.Name : name;
            if (AddLinked(shareId, finalName, server, status))
                return; // liste ouverte : le favori y apparaît sélectionné
            string who = finalName.Length > 0 ? finalName : "Le serveur";
            string state = status.State == WakeState.Online ? "Il est en ligne" : "Il finit de démarrer";
            UnifiedPopup.Push(new WarningPopup($"{who} ajouté aux favoris",
                $"{state} : rejoins-le depuis la liste des serveurs, onglet Favoris.", UnifiedPopup.Pop, localizeText: false));
        }

        /// <summary>Lien rangé puis favori ajouté ; vrai s'il est affiché, sélectionné dans la liste ouverte.</summary>
        private static bool AddLinked(string shareId, string name, ServerJoinData server, NodecraftStatus status)
        {
            ServerLinkStore.Set(server, new ServerLink(shareId, name));
            ServerWakeBadges.Remember(shareId, status);
            Plugin.Log.LogInfo($"ServerWake : favori {name} ({server}) ajouté depuis son lien Nodecraft");
            return LinkedFavorite.Add(server);
        }

        private static string ErrorOf(NodecraftStatus status, ServerJoinData server)
        {
            switch (status.State)
            {
                case WakeState.Locked:
                    return "Ce lien de partage est protégé par un mot de passe : le mod ne sait pas l'utiliser.";
                case WakeState.Unavailable:
                    return "Ce serveur est archivé : il ne peut plus être démarré depuis ce lien.";
                case WakeState.Error:
                    return $"Nodecraft ne répond pas correctement ({status.Message}).";
                case WakeState.RateLimited:
                    return $"Nodecraft limite les requêtes : réessaie dans {status.RetryAfterSeconds} s.";
            }
            return server.IsValid ? null : "Nodecraft ne donne pas l'adresse du serveur pour l'instant : réessaie quand il est démarré.";
        }
    }
}
