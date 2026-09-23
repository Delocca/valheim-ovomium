using System.Collections.Generic;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Seul accès en écriture aux coffres non ouverts (CraftFromChests, FuelFromChests, QuickStash). Règle : on n'écrit
    /// que dans un coffre dont on est propriétaire, et la propriété ne s'obtient que par la demande d'ouverture vanilla
    /// (<c>RPC_RequestOpen</c>) : la propriétaire refuse si le coffre est ouvert chez elle, sinon envoie sa version
    /// fraîche du ZDO, cède la propriété et répond « accordé ». Jamais de prise de force : deux écrivaines simultanées
    /// perdent l'écriture de l'une (le serveur ignore une révision de données déjà vue). Un coffre sans propriétaire
    /// ou dont la propriétaire est partie est réattribué par le serveur (<c>ZDOMan.ReleaseNearbyZDOS</c>).
    /// La réponse « accordé » peut précéder le ZDO : seul <c>IsOwner()</c> fait foi.
    /// Réservation anticipée (option CraftFromChests) : demande renouvelée toutes les 2 s pour les coffres à portée
    /// pendant que le panneau de craft est ouvert, le marteau en main ou un feu visé ; la réponse est avalée (pas de
    /// fenêtre), y compris tardive. Garde (<see cref="Hold"/>) : un coffre gardé et possédé refuse les demandes des
    /// autres (barre de craft en cours, rangement en attente), comme un coffre ouvert en vanilla.
    /// </summary>
    internal static class ChestReservation
    {
        private const float RequestInterval = 2f;
        private const float PendingTimeout = 1f;
        private const float LateResponseWindow = 5f;
        /// <summary>Garde posée après un refus local : le nouvel essai de la joueuse trouvera le coffre encore à elle.</summary>
        private const float RetryHold = 3f;

        private static readonly Dictionary<Container, float> s_pending = new Dictionary<Container, float>();
        private static readonly Dictionary<Container, float> s_lateUntil = new Dictionary<Container, float>();
        private static readonly Dictionary<Container, float> s_refusedAt = new Dictionary<Container, float>();
        private static readonly Dictionary<Container, float> s_heldUntil = new Dictionary<Container, float>();
        private static readonly List<Container> s_expired = new List<Container>();
        private static float s_nextRequest;

        /// <summary>Chaque frame du joueur local : expire les demandes sans réponse, puis toutes les 2 s renouvelle les réservations.</summary>
        public static void Tick(Player player)
        {
            if (player != Player.m_localPlayer) return;
            ExpirePending();
            if (!CraftFromChestsConfig.Enabled.Value || Time.time < s_nextRequest) return;
            s_nextRequest = Time.time + RequestInterval;
            if (!InventoryGui.IsVisible() && !player.InPlaceMode() && !HoveringFireplace(player)) return;
            foreach (var container in NearbyChests.Find(player.transform.position))
                RequestNow(container);
        }

        /// <summary>Un feu, four ou marmite en visée : sa recharge (FuelFromChests) puisera dans les coffres à la pression suivante.</summary>
        private static bool HoveringFireplace(Player player)
        {
            var hovering = player.GetHoverObject();
            return hovering != null && (hovering.GetComponentInParent<Fireplace>() != null
                || hovering.GetComponentInParent<Smelter>() != null || hovering.GetComponentInParent<CookingStation>() != null);
        }

        /// <summary>Demande la propriété tout de suite, sauf si on l'a déjà, qu'une demande est en vol ou que personne ne la détient.</summary>
        public static void RequestNow(Container container)
        {
            var nview = container != null ? container.m_nview : null;
            if (nview == null || !nview.IsValid() || nview.IsOwner() || !nview.HasOwner()) return;
            if (s_pending.ContainsKey(container)) return;
            s_pending[container] = Time.time;
            nview.InvokeRPC("RPC_RequestOpen", Game.instance.GetPlayerProfile().GetPlayerID());
        }

        /// <summary>
        /// Tous les coffres de <paramref name="pulls"/> sont à nous : vrai. Sinon demande et garde chacun des autres,
        /// affiche le message vanilla « Utilisé par quelqu'un d'autre » et retourne faux : l'action est à annuler sans
        /// rien consommer, un nouvel essai une seconde après passe.
        /// </summary>
        public static bool EnsureOwned(Player player, List<Pull> pulls, string action)
        {
            bool owned = true;
            foreach (var pull in pulls)
            {
                if (pull.Chest.m_nview.IsOwner()) continue;
                owned = false;
                RequestNow(pull.Chest);
                Hold(pull.Chest, RetryHold);
                Plugin.Log.LogInfo($"CraftFromChests : {action} annulé(e), {pull.Chest.m_name} pas encore à nous (demande envoyée)");
            }
            if (!owned) player.Message(MessageHud.MessageType.Center, "$msg_inuse");
            return owned;
        }

        /// <summary>Refus reçu pour ce coffre depuis <paramref name="since"/> (réponse à une de nos demandes).</summary>
        public static bool RefusedSince(Container container, float since) =>
            s_refusedAt.TryGetValue(container, out float at) && at >= since;

        /// <summary>Garde le coffre <paramref name="seconds"/> secondes (prolonge une garde plus courte).</summary>
        public static void Hold(Container container, float seconds)
        {
            float until = Time.time + seconds;
            if (!s_heldUntil.TryGetValue(container, out float current) || current < until) s_heldUntil[container] = until;
        }

        public static bool IsHeld(Container container) =>
            s_heldUntil.TryGetValue(container, out float until) && Time.time < until;

        private static void ExpirePending()
        {
            s_expired.Clear();
            foreach (var pair in s_pending)
                if (pair.Key == null || Time.time - pair.Value > PendingTimeout) s_expired.Add(pair.Key);
            foreach (var container in s_expired)
            {
                s_pending.Remove(container);
                if (container == null) continue;
                s_lateUntil[container] = Time.time + LateResponseWindow;
                Plugin.Log.LogInfo($"ChestReservation : {container.m_name} sans réponse de sa propriétaire");
            }
            Prune(s_lateUntil, 0f);
            Prune(s_heldUntil, 0f);
            Prune(s_refusedAt, LateResponseWindow);
        }

        /// <summary>Retire les entrées d'objets détruits ou dont l'échéance (+ <paramref name="keep"/>) est passée.</summary>
        private static void Prune(Dictionary<Container, float> times, float keep)
        {
            s_expired.Clear();
            foreach (var pair in times)
                if (pair.Key == null || Time.time > pair.Value + keep) s_expired.Add(pair.Key);
            foreach (var container in s_expired) times.Remove(container);
        }

        /// <summary>Réponse à une de nos demandes, en attente ou expirée depuis peu : consommée (true) ; sinon vrai clic (false).</summary>
        public static bool OnOpenResponse(Container container, bool granted)
        {
            bool ours = s_pending.Remove(container) | s_lateUntil.Remove(container);
            if (ours && !granted)
            {
                s_refusedAt[container] = Time.time;
                Plugin.Log.LogInfo($"ChestReservation : {container.m_name} refusé par sa propriétaire (ouvert ou gardé chez elle)");
            }
            return ours;
        }

        /// <summary>Un clic réel sur le coffre : sa réponse doit ouvrir la fenêtre.</summary>
        public static void OnInteract(Container container)
        {
            s_pending.Remove(container);
            s_lateUntil.Remove(container);
        }

        /// <summary>Rechargement à chaud.</summary>
        public static void Unload()
        {
            s_pending.Clear();
            s_lateUntil.Clear();
            s_refusedAt.Clear();
            s_heldUntil.Clear();
        }
    }
}
