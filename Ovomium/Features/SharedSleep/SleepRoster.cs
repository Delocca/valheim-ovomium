using System.Collections.Generic;
using Ovomium.Net;
using UnityEngine;

namespace Ovomium.Features.SharedSleep
{
    /// <summary>
    /// État de sommeil des joueuses : chaque joueuse du mod diffuse le sien (vraiment couchée dans un lit, jamais le faux
    /// drapeau) par message routé, au changement et périodiquement de nuit. Le décompte suit la liste des joueuses de
    /// <c>ZNet</c> (synchronisée par le serveur toutes les 2 s, serveur dédié absent) ; une joueuse sans nouvelles
    /// récentes est jugée sans le mod : son drapeau <c>inBed</c> compte si son ZDO est chargé ici, sinon elle est
    /// éveillée, et elle retient la nuit. Expéditrice d'un message = identifiant de session = <c>ZDOID.UserID</c> de
    /// son personnage.
    /// </summary>
    internal static class SleepRoster
    {
        /// <summary>Format du paquet ; un paquet d'une autre version est ignoré.</summary>
        private const byte FormatVersion = 2;
        /// <summary>Rafraîchissement périodique (de nuit) et oubli d'une joueuse muette.</summary>
        public const float RefreshSeconds = 5f;
        private const float ExpirySeconds = 15f;

        private struct Report
        {
            public bool InBed;
            public float Time;
        }

        /// <summary>Décompte vu d'ici, avec les éveillées sans le mod (elles retiennent la nuit).</summary>
        public sealed class Tally
        {
            public int Sleepers;
            public int Total;
            public readonly List<string> Blockers = new List<string>();
        }

        private static readonly RoutedChannel s_channel = new RoutedChannel("Ovomium_SharedSleep", OnReceive);
        private static readonly Dictionary<long, Report> s_reports = new Dictionary<long, Report>();

        public static void Install() => s_channel.Install();

        public static void Unload()
        {
            s_channel.Unload();
            Clear();
        }

        public static void Clear() => s_reports.Clear();

        public static void Send(bool inBed)
        {
            var pkg = new ZPackage();
            pkg.Write(FormatVersion);
            pkg.Write(inBed);
            s_channel.SendToEverybody(pkg);
        }

        /// <summary>Paquet d'une autre joueuse (entrée externe : un paquet illisible est ignoré).</summary>
        private static void OnReceive(long senderPeer, ZPackage pkg)
        {
            if (RoutedChannel.IsFromSelf(senderPeer)) return;
            try
            {
                if (pkg.ReadByte() != FormatVersion) return;
                s_reports[senderPeer] = new Report { InBed = pkg.ReadBool(), Time = Time.realtimeSinceStartup };
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"SharedSleep : paquet d'une autre joueuse illisible ({e.Message})");
            }
        }

        /// <summary>Joueuses en jeu (personnage présent : vanilla ignore aussi les autres), soi comprise.</summary>
        public static Tally Count(bool localInBed)
        {
            var tally = new Tally();
            if (ZNet.instance == null) return tally;
            long self = ZDOMan.GetSessionID();
            float now = Time.realtimeSinceStartup;
            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
            {
                if (info.m_characterID.IsNone()) continue;
                long uid = info.m_characterID.UserID;
                tally.Total++;
                bool inBed;
                if (uid == self) inBed = localInBed;
                else if (s_reports.TryGetValue(uid, out Report report) && now - report.Time <= ExpirySeconds) inBed = report.InBed;
                else
                {
                    inBed = ZdoInBed(info.m_characterID);
                    if (!inBed) tally.Blockers.Add(info.m_name);
                }
                if (inBed) tally.Sleepers++;
            }
            return tally;
        }

        private static bool ZdoInBed(ZDOID id)
        {
            ZDO zdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(id) : null;
            return zdo != null && zdo.GetBool(ZDOVars.s_inBed);
        }
    }
}
