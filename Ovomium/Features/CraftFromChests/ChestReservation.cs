using System.Collections.Generic;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Réservation anticipée des coffres à portée pendant que le panneau de craft est ouvert ou que le marteau est en
    /// main : on envoie à la propriétaire de chaque coffre la demande d'ouverture vanilla (<c>RPC_RequestOpen</c>). Sa
    /// réponse est celle du jeu : elle refuse si le coffre est ouvert chez elle, sinon elle envoie sa version fraîche
    /// de l'inventaire, cède la propriété et répond « accordé ». On avale la réponse (pas de fenêtre) : au moment du
    /// craft les coffres nous appartiennent déjà, avec des données à jour, et la propriété passe de main en main
    /// proprement entre deux crafteuses. Fonctionne même si l'amie n'a pas le mod (c'est son jeu qui répond). Sans
    /// réponse au bout d'une seconde (propriétaire partie), repli sur la prise directe de <c>NearbyChests.TakeOwnership</c> ;
    /// une réponse qui arriverait quand même après coup (lag) reste avalée quelques secondes, sinon elle ouvrirait la fenêtre.
    /// </summary>
    internal static class ChestReservation
    {
        private const float RequestInterval = 2f;
        private const float PendingTimeout = 1f;
        private const float LateResponseWindow = 5f;

        private static readonly Dictionary<Container, float> s_pending = new Dictionary<Container, float>();
        private static readonly Dictionary<Container, float> s_lateUntil = new Dictionary<Container, float>();
        private static readonly List<Container> s_expired = new List<Container>();
        private static float s_nextRequest;

        /// <summary>Chaque frame du joueur local : expire les demandes sans réponse, puis toutes les 2 s renouvelle les réservations.</summary>
        public static void Tick(Player player)
        {
            if (!CraftFromChestsConfig.Enabled.Value || player != Player.m_localPlayer) return;
            ExpirePending();
            if (Time.time < s_nextRequest) return;
            s_nextRequest = Time.time + RequestInterval;
            if (!InventoryGui.IsVisible() && !player.InPlaceMode()) return;
            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            foreach (var container in NearbyChests.Find(player.transform.position))
                Request(container, playerId);
        }

        private static void Request(Container container, long playerId)
        {
            var nview = container.m_nview;
            if (nview == null || !nview.IsValid() || nview.IsOwner()) return;
            if (!nview.HasOwner()) { nview.ClaimOwnership(); return; }
            if (s_pending.ContainsKey(container)) return;
            s_pending[container] = Time.time;
            nview.InvokeRPC("RPC_RequestOpen", playerId);
        }

        private static void ExpirePending()
        {
            s_expired.Clear();
            foreach (var pair in s_pending)
                if (pair.Key == null || Time.time - pair.Value > PendingTimeout) s_expired.Add(pair.Key);
            foreach (var container in s_expired)
            {
                s_pending.Remove(container);
                if (container == null || container.m_nview == null || !container.m_nview.IsValid()) continue;
                s_lateUntil[container] = Time.time + LateResponseWindow;
                NearbyChests.TakeOwnership(container);
            }
            s_expired.Clear();
            foreach (var pair in s_lateUntil)
                if (pair.Key == null || Time.time > pair.Value) s_expired.Add(pair.Key);
            foreach (var container in s_expired) s_lateUntil.Remove(container);
        }

        /// <summary>Réponse à une réservation, en attente ou expirée depuis peu : consommée (true) ; sinon vrai clic (false).</summary>
        public static bool OnOpenResponse(Container container) =>
            s_pending.Remove(container) | s_lateUntil.Remove(container);

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
        }
    }
}
