using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.SharedSleep
{
    /// <summary>
    /// Nuit passée à la moitié du groupe couchée, sans mod côté serveur : le serveur vanilla (<c>Game.UpdateSleeping</c>,
    /// toutes les 2 s) ne passe la nuit que si le drapeau <c>inBed</c> de tous les ZDO de personnage est vrai. Quand le
    /// seuil est atteint (<see cref="SleepRoster"/>, <see cref="SleepQuorum"/>), chaque joueuse éveillée du mod pose ce
    /// drapeau sur son propre personnage (faux « couchée ») ; elle ne s'endort pas pour autant et voit la nuit défiler
    /// en accéléré (<see cref="SleepSequencePatch"/>). Chez soi, le faux drapeau est masqué à <c>Player.InBed()</c>, dont
    /// les seuls lecteurs (caméra, accroupi) visent le joueur local.
    /// </summary>
    internal static class SharedSleepPatch
    {
        private const float TickSeconds = 0.5f;

        private static float s_nextTick;
        private static float s_nextSend;
        private static bool s_sentInBed;
        private static bool s_fake;
        private static bool s_attachedToBed;
        private static ZNet s_session;

        public static void Install()
        {
            SleepRoster.Install();
            Player player = Player.m_localPlayer;
            // Rechargement à chaud au lit : l'ancienne version a retiré son faux drapeau, celui qui reste est le vrai.
            s_attachedToBed = player != null && player.m_attached && player.InBed();
        }

        public static void Unload()
        {
            ClearFake(Player.m_localPlayer);
            SleepRoster.Unload();
            SleepNotice.Reset();
            s_session = null;
        }

        /// <summary>Vraiment couchée : attachée par un lit (<c>Bed.Interact</c> → <c>AttachStart(isBed: true)</c>).</summary>
        public static bool InRealBed(Player player) => s_attachedToBed && player.m_attached;

        private static void Tick(Player player)
        {
            float now = Time.realtimeSinceStartup;
            if (now < s_nextTick) return;
            s_nextTick = now + TickSeconds;
            if (!SharedSleepConfig.Enabled.Value)
            {
                ClearFake(player);
                return;
            }
            StartSessionIfNew();
            bool alive = !player.IsDead();
            bool inBed = alive && InRealBed(player);
            bool night = EnvMan.IsAfternoon() || EnvMan.IsNight();
            SendState(inBed, night, now);
            SleepRoster.Tally tally = SleepRoster.Count(inBed);
            if (night && alive && !inBed && SleepQuorum.IsReached(tally.Sleepers, tally.Total)) SetFake(player);
            else ClearFake(player);
            SleepNotice.Update(night, tally);
        }

        /// <summary>Nouvelle connexion (ZNet recréé) : les états reçus et le faux drapeau de la précédente ne valent plus.</summary>
        private static void StartSessionIfNew()
        {
            if (s_session == ZNet.instance) return;
            s_session = ZNet.instance;
            SleepRoster.Clear();
            SleepNotice.Reset();
            s_fake = false;
            s_nextSend = 0f;
        }

        /// <summary>Au changement d'état, et de nuit toutes les <see cref="SleepRoster.RefreshSeconds"/> (arrivées, pertes).</summary>
        private static void SendState(bool inBed, bool night, float now)
        {
            if (inBed == s_sentInBed && !(night && now >= s_nextSend)) return;
            s_sentInBed = inBed;
            s_nextSend = now + SleepRoster.RefreshSeconds;
            SleepRoster.Send(inBed);
        }

        private static ZDO OwnZdo(Player player)
        {
            ZNetView view = player != null ? player.m_nview : null;
            return view != null && view.IsValid() && view.IsOwner() ? view.GetZDO() : null;
        }

        /// <summary>Reposé à chaque tick : <c>AttachStart</c> d'une chaise ou <c>AttachStop</c> le remettent à faux.</summary>
        private static void SetFake(Player player)
        {
            ZDO zdo = OwnZdo(player);
            if (zdo == null) return;
            if (!zdo.GetBool(ZDOVars.s_inBed)) zdo.Set(ZDOVars.s_inBed, true);
            if (!s_fake) Plugin.Log.LogInfo("SharedSleep : seuil atteint, personnage marqué couché pour le serveur");
            s_fake = true;
        }

        private static void ClearFake(Player player)
        {
            if (!s_fake) return;
            s_fake = false;
            ZDO zdo = OwnZdo(player);
            if (zdo != null && !InRealBed(player)) zdo.Set(ZDOVars.s_inBed, false);
            Plugin.Log.LogInfo("SharedSleep : marque couchée retirée");
        }

        [HarmonyPatch(typeof(Player), "Update", new System.Type[0])]
        private static class PlayerUpdatePatch
        {
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer) Tick(__instance);
            }
        }

        /// <summary>Retiré tout de suite à la mort : le ZDO du mort reste jusqu'à la réapparition.</summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath), new System.Type[0])]
        private static class PlayerOnDeathPatch
        {
            private static void Prefix(Player __instance)
            {
                if (__instance == Player.m_localPlayer) ClearFake(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AttachStart), new[]
        {
            typeof(Transform), typeof(GameObject), typeof(bool), typeof(bool), typeof(bool), typeof(string),
            typeof(Vector3), typeof(Transform),
        })]
        private static class PlayerAttachStartPatch
        {
            private static void Prefix(Player __instance, out bool __state) => __state = __instance.m_attached;

            private static void Postfix(Player __instance, bool isBed, bool __state)
            {
                if (!__state && __instance.m_attached && __instance == Player.m_localPlayer) s_attachedToBed = isBed;
            }
        }

        /// <summary>Le faux drapeau ne change rien chez soi : caméra et accroupi voient la joueuse debout.</summary>
        [HarmonyPatch(typeof(Player), nameof(Player.InBed), new System.Type[0])]
        private static class PlayerInBedPatch
        {
            private static void Postfix(Player __instance, ref bool __result)
            {
                if (__result && s_fake && __instance == Player.m_localPlayer && !InRealBed(__instance)) __result = false;
            }
        }
    }
}
