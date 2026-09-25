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
    /// Réservation anticipée (option CraftFromChests) : seulement les coffres que la prochaine action puiserait
    /// (<see cref="ReservationTarget"/> : recette sélectionnée, pièce du marteau, feu visé), demandés dès que cette
    /// action change puis toutes les 2 s ; réserver tous les coffres à portée faisait naviguer leur propriété entre
    /// deux joueuses proches. La réponse est avalée (pas de fenêtre), y compris tardive ; journal : <see cref="ReservationLog"/>.
    /// Garde (<see cref="Hold"/>) : un coffre gardé et possédé refuse les demandes des autres (barre de craft en cours,
    /// rangement en attente), comme un coffre ouvert en vanilla.
    /// </summary>
    internal static class ChestReservation
    {
        private const float RequestInterval = 2f;
        /// <summary>Plan de la cible recalculé au plus à cette cadence tant que la cible ne change pas (coffres vidés, remplis).</summary>
        private const float PlanInterval = 0.25f;
        private const float PendingTimeout = 1f;
        private const float LateResponseWindow = 5f;
        /// <summary>Garde posée après un refus local : le nouvel essai de la joueuse trouvera le coffre encore à elle, sans bloquer l'autre longtemps.</summary>
        private const float RetryHold = 1.5f;

        /// <summary>Demande envoyée : quand, et à quelle propriétaire (celle du moment, visée par <c>InvokeRPC</c>).</summary>
        private readonly struct Request
        {
            public readonly float Time;
            public readonly long Owner;

            public Request(float time, long owner)
            {
                Time = time;
                Owner = owner;
            }
        }

        private static readonly Dictionary<Container, Request> s_pending = new Dictionary<Container, Request>();
        /// <summary>Demandes restées sans réponse à temps : une réponse tardive est encore la nôtre (avalée).</summary>
        private static readonly Dictionary<Container, Request> s_late = new Dictionary<Container, Request>();
        private static readonly Dictionary<Container, float> s_refusedAt = new Dictionary<Container, float>();
        private static readonly Dictionary<Container, float> s_heldUntil = new Dictionary<Container, float>();
        private static readonly List<Container> s_expired = new List<Container>();
        private static readonly List<Container> s_planned = new List<Container>();
        private static readonly List<Container> s_nextPlanned = new List<Container>();
        private static ReservationTarget s_target;
        private static float s_nextPlan;
        private static float s_nextRequest;

        /// <summary>
        /// Chaque frame du joueur local : expire les demandes sans réponse, puis réserve les coffres de la cible courante,
        /// tout de suite quand elle change ou qu'un coffre entre dans son plan, sinon toutes les 2 s.
        /// </summary>
        public static void Tick(Player player)
        {
            if (player != Player.m_localPlayer) return;
            ExpirePending();
            ReservationLog.Tick();
            var target = ReservationTarget.Current(player);
            bool changed = !target.SameAs(s_target);
            if (!changed && Time.time < s_nextPlan) return;
            s_target = target;
            s_nextPlan = Time.time + PlanInterval;
            bool renew = changed || Time.time >= s_nextRequest;
            if (renew) s_nextRequest = Time.time + RequestInterval;
            target.Chests(player, s_nextPlanned);
            foreach (var chest in s_nextPlanned)
                if (renew || !s_planned.Contains(chest)) RequestNow(chest);
            s_planned.Clear();
            s_planned.AddRange(s_nextPlanned);
        }

        /// <summary>Demande la propriété tout de suite, sauf si on l'a déjà, qu'une demande est en vol ou que personne ne la détient.</summary>
        public static void RequestNow(Container container)
        {
            var nview = container != null ? container.m_nview : null;
            if (nview == null || !nview.IsValid() || nview.IsOwner() || !nview.HasOwner()) return;
            if (s_pending.ContainsKey(container)) return;
            s_pending[container] = new Request(Time.time, nview.GetZDO().GetOwner());
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
                if (pair.Key == null || Time.time - pair.Value.Time > PendingTimeout) s_expired.Add(pair.Key);
            foreach (var container in s_expired)
            {
                Request request = s_pending[container];
                s_pending.Remove(container);
                if (container == null) continue;
                s_late[container] = request;
                ReservationLog.Silent(container, PendingTimeout, request.Owner);
            }
            s_expired.Clear();
            foreach (var pair in s_late)
                if (pair.Key == null || Time.time > pair.Value.Time + PendingTimeout + LateResponseWindow) s_expired.Add(pair.Key);
            foreach (var container in s_expired) s_late.Remove(container);
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
            bool late = false;
            if (!s_pending.TryGetValue(container, out Request request))
            {
                if (!s_late.TryGetValue(container, out request)) return false;
                late = true;
            }
            s_pending.Remove(container);
            s_late.Remove(container);
            float delay = Time.time - request.Time;
            if (granted) ReservationLog.Granted(container, delay, late);
            else
            {
                s_refusedAt[container] = Time.time;
                ReservationLog.Refused(container, delay);
            }
            return true;
        }

        /// <summary>Un clic réel sur le coffre : sa réponse doit ouvrir la fenêtre.</summary>
        public static void OnInteract(Container container)
        {
            s_pending.Remove(container);
            s_late.Remove(container);
        }

        /// <summary>Rechargement à chaud.</summary>
        public static void Unload()
        {
            s_pending.Clear();
            s_late.Clear();
            s_refusedAt.Clear();
            s_heldUntil.Clear();
            s_planned.Clear();
            s_nextPlanned.Clear();
            s_target = default;
            ReservationLog.Unload();
        }
    }
}
