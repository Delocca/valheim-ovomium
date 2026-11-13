using UnityEngine;

namespace Ovomium.Features.SharedSleep
{
    /// <summary>
    /// Message en haut à gauche, à toutes les joueuses du mod, quand l'état du sommeil change (quelqu'un se couche,
    /// seuil atteint, éveillées sans le mod qui retiennent la nuit) ; rien tant que personne n'est couchée, ni seule en
    /// jeu. Un état doit rester stable une seconde avant d'être affiché (pas de rafale aux arrivées et départs).
    /// </summary>
    internal static class SleepNotice
    {
        private const float StableSeconds = 1f;
        private const int MaxNames = 3;

        private static string s_pending;
        private static float s_pendingSince;
        private static string s_shown;

        public static void Reset()
        {
            s_pending = null;
            s_shown = null;
        }

        public static void Update(bool night, SleepRoster.Tally tally)
        {
            string text = night && tally.Total >= 2 && tally.Sleepers >= 1 ? Compose(tally) : null;
            float now = Time.realtimeSinceStartup;
            if (text != s_pending)
            {
                s_pending = text;
                s_pendingSince = now;
                return;
            }
            if (text == s_shown || now - s_pendingSince < StableSeconds) return;
            s_shown = text;
            if (text != null && MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
        }

        private static string Compose(SleepRoster.Tally tally)
        {
            string head = $"Sommeil : {tally.Sleepers}/{tally.Total} couchées";
            if (!SleepQuorum.IsReached(tally.Sleepers, tally.Total))
                return $"{head}, {SleepQuorum.Required(tally.Total)} requises pour passer la nuit";
            if (tally.Blockers.Count == 0) return $"{head}, la nuit va passer";
            return $"{head}, la nuit attend {Names(tally)} (sans Ovomium à jour)";
        }

        private static string Names(SleepRoster.Tally tally)
        {
            int shown = Mathf.Min(tally.Blockers.Count, MaxNames);
            string names = string.Join(", ", tally.Blockers.GetRange(0, shown));
            return tally.Blockers.Count > shown ? names + "…" : names;
        }
    }
}
