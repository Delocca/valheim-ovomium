using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Journal des réponses à nos demandes de propriété (<see cref="ChestReservation"/>) : une ligne par réponse
    /// tardive, refus ou absence de réponse, avec le délai et la propriétaire visée ; les réponses accordées à temps
    /// ne sont que comptées, bilan au plus une fois par minute.
    /// </summary>
    internal static class ReservationLog
    {
        private const float SummaryInterval = 60f;

        private static int s_onTime;
        private static float s_onTimeDelays;
        private static int s_late;
        private static int s_refused;
        private static int s_silent;
        private static int s_redirected;
        private static float s_nextSummary;

        public static void Granted(Container chest, float delay, bool late)
        {
            if (!late)
            {
                s_onTime++;
                s_onTimeDelays += delay;
                return;
            }
            s_late++;
            Plugin.Log.LogInfo($"ChestReservation : {chest.m_name} accordé en retard, après {Ms(delay)}");
        }

        public static void Refused(Container chest, float delay)
        {
            s_refused++;
            Plugin.Log.LogInfo($"ChestReservation : {chest.m_name} refusé par sa propriétaire après {Ms(delay)} (ouvert ou gardé chez elle)");
        }

        /// <summary><paramref name="asked"/> : propriétaire à qui la demande est partie (<c>ZNetView.InvokeRPC</c> vise la propriétaire du moment).</summary>
        /// <summary>Demande renvoyée à la nouvelle propriétaire, l'ancienne ne répondra pas : seulement comptée.</summary>
        public static void Redirected() => s_redirected++;

        public static void Silent(Container chest, float timeout, long asked)
        {
            s_silent++;
            long owner = chest.m_nview.IsValid() ? chest.m_nview.GetZDO().GetOwner() : 0L;
            string now = owner == asked ? "toujours elle" : $"maintenant {Describe(owner)}";
            Plugin.Log.LogInfo($"ChestReservation : {chest.m_name} sans réponse en {Ms(timeout)} ; demandé à {Describe(asked)}, "
                + $"propriétaire {now} (réponse tardive encore acceptée)");
        }

        /// <summary>Bilan des réponses depuis le précédent, s'il y en a eu.</summary>
        public static void Tick()
        {
            if (Time.time < s_nextSummary) return;
            s_nextSummary = Time.time + SummaryInterval;
            if (s_onTime + s_late + s_refused + s_silent + s_redirected == 0) return;
            string mean = s_onTime > 0 ? $" (moyenne {Ms(s_onTimeDelays / s_onTime)})" : "";
            Plugin.Log.LogInfo($"ChestReservation : bilan, {s_onTime} accordée(s) à temps{mean}, {s_late} en retard, "
                + $"{s_refused} refus, {s_silent} sans réponse à temps, {s_redirected} renvoyée(s) à la nouvelle propriétaire");
            s_onTime = s_late = s_refused = s_silent = s_redirected = 0;
            s_onTimeDelays = 0f;
        }

        private static string Ms(float seconds) => $"{seconds * 1000f:0} ms";

        /// <summary>
        /// Identifiant de session et ce qu'on en sait : nous, serveur, joueuse connectée (d'après l'identifiant de son
        /// personnage, créé par sa session), pair direct (hôte ou serveur seulement : un client ne connaît que le
        /// serveur), sinon inconnue (partie ?).
        /// </summary>
        private static string Describe(long uid)
        {
            if (uid == 0L) return "personne";
            if (uid == ZNet.GetUID()) return "nous";
            ZNet net = ZNet.instance;
            if (net == null) return uid.ToString();
            if (net.GetServerPeer()?.m_uid == uid) return $"{uid} (serveur)";
            foreach (var info in net.GetPlayerList())
                if (info.m_characterID.UserID == uid) return $"{uid} ({info.m_name}, connectée)";
            return net.GetPeer(uid) != null ? $"{uid} (pair connu)" : $"{uid} (inconnue : partie ?)";
        }

        /// <summary>Rechargement à chaud.</summary>
        public static void Unload()
        {
            s_onTime = s_late = s_refused = s_silent = s_redirected = 0;
            s_onTimeDelays = 0f;
            s_nextSummary = 0f;
        }
    }
}
