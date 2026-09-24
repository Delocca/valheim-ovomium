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
            string error = ErrorOf(status, server);
            if (error != null)
            {
                Plugin.Log.LogWarning($"ServerWake : ajout depuis le lien impossible : {error}");
                UnifiedPopup.Push(new WarningPopup("Ajout du serveur Nodecraft impossible", error, UnifiedPopup.Pop, localizeText: false));
                yield break;
            }
            ServerLinkStore.Set(server, new ServerLink(shareId, status.Name));
            ServerWakeBadges.Remember(shareId, status);
            Plugin.Log.LogInfo($"ServerWake : favori {status.Name} ({server}) ajouté depuis son lien Nodecraft");
            if (gui != null)
                gui.OnManualAddToFavoritesSuccess(server);
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
