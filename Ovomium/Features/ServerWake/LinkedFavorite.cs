using System;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Favori lié à Nodecraft, tenu à jour d'après l'état lu : nom du serveur mémorisé avec le lien, et adresse. L'IP d'un
    /// serveur Nodecraft Lite peut changer après une hibernation, et le favori est enregistré par IP (le nom DNS se
    /// propage trop lentement, décision d'Edia 2026-09-24) : quand Nodecraft annonce une autre adresse, le favori est
    /// remplacé sur place (même position) et son lien suit. Liste vivante de <c>ServerListGui</c> si elle existe (elle
    /// réécrit le fichier à sa fermeture), sinon le fichier. Un favori enregistré par nom d'hôte garde son adresse.
    /// Ajout d'un favori lié : <see cref="Add"/>, commun à l'ajout direct et à l'ajout après réveil.
    /// </summary>
    internal static class LinkedFavorite
    {
        /// <summary>
        /// Favori ajouté depuis un lien (dialogue « Ajouter un serveur ») : adresse de l'état Nodecraft, ou
        /// <c>ServerJoinData.None</c> si Nodecraft n'en donne pas.
        /// </summary>
        public static ServerJoinData FromStatus(NodecraftStatus status)
        {
            if (!IsIp(status.Address) || status.GamePort <= 0 || status.GamePort > ushort.MaxValue)
                return ServerJoinData.None;
            return new ServerJoinData(new ServerJoinDataDedicated(status.Address, (ushort)status.GamePort));
        }

        /// <summary>Met à jour nom et adresse ; rend le serveur à rejoindre (nouvelle adresse si elle a changé).</summary>
        public static ServerJoinData Sync(ServerJoinData server, NodecraftStatus status)
        {
            ServerLink link = ServerLinkStore.Get(server);
            if (link == null)
                return server;
            if (status.Name.Length > 0 && status.Name != link.Name)
                ServerLinkStore.Set(server, new ServerLink(link.ShareId, status.Name));
            return status.State == WakeState.Online ? Relocate(server, status) : server;
        }

        private static ServerJoinData Relocate(ServerJoinData server, NodecraftStatus status)
        {
            if (!server.Dedicated.TryGetIPAddress(out _))
                return server;
            ServerJoinData fresh = FromStatus(status);
            if (!fresh.IsValid || fresh == server)
                return server;
            ServerLinkStore.Move(server, fresh);
            bool replaced = ReplaceInFavorites(server, fresh);
            Plugin.Log.LogInfo($"ServerWake : nouvelle adresse {server} → {fresh}" + (replaced ? ", favori mis à jour" : ""));
            return fresh;
        }

        /// <summary>
        /// Ajoute un favori (sans doublon) par le code vanilla du dialogue « Ajouter un serveur »
        /// (<c>OnManualAddToFavoritesSuccess</c> : onglet Favoris, favori sélectionné, dialogue refermé), même liste
        /// fermée ; écrit tout de suite (liste fermée, elle ne le ferait qu'à sa prochaine fermeture). Rend vrai si la
        /// liste est affichée, favori sélectionné sous les yeux de la joueuse.
        /// </summary>
        public static bool Add(ServerJoinData server)
        {
            ServerListGui gui = ServerListGui.s_instance;
            if (gui != null && gui.m_favoriteServersList != null)
            {
                gui.OnManualAddToFavoritesSuccess(server);
                gui.m_favoriteServersList.Save();
                return gui.isActiveAndEnabled;
            }
            EditFavoritesFile(list =>
            {
                if (list.Contains(server))
                    return false;
                list.Add(server);
                return true;
            });
            return false;
        }

        private static bool IsIp(string text) => !string.IsNullOrEmpty(text) && System.Net.IPAddress.TryParse(text, out _);

        private static bool ReplaceInFavorites(ServerJoinData old, ServerJoinData fresh)
        {
            ServerListGui gui = ServerListGui.s_instance;
            if (gui == null || gui.m_favoriteServersList == null)
                return EditFavoritesFile(list => Replace(list, old, fresh));
            if (!Replace(gui.m_favoriteServersList, old, fresh))
                return false;
            gui.m_favoriteServersList.Save();
            if (gui.m_startup != null && gui.m_startup.GetServerToJoin() == old)
                gui.m_startup.SetServerToJoin(fresh);
            gui.m_filteredListOutdated = true;
            gui.UpdateServerListGui(false);
            return true;
        }

        /// <summary>Fichier des favoris quand la liste n'existe pas (jamais ouverte) : écrit si <paramref name="edit"/> rend vrai.</summary>
        private static bool EditFavoritesFile(Func<LocalServerList, bool> edit)
        {
            var list = new LocalServerList(null, ServerListGui.GetServerListLocations("favorite"));
            try
            {
                if (!edit(list))
                    return false;
                list.Save();
                return true;
            }
            finally
            {
                list.Dispose();
            }
        }

        private static bool Replace(LocalServerList list, ServerJoinData old, ServerJoinData fresh)
        {
            if (!list.TryGetIndexOf(old, out int index))
                return false;
            if (list.Contains(fresh))
                list.Remove(old); // la nouvelle adresse est déjà en favori : pas de doublon
            else
            {
                list.m_list[index] = fresh;
                list.m_wasModified = true;
            }
            return true;
        }
    }
}
