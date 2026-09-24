using System;
using System.Diagnostics;
using System.Threading;

namespace Ovomium.Features.ServerWake
{
    /// <summary>
    /// Pause globale des appels Nodecraft après un HTTP 429 (limitation de débit Cloudflare) : un seul « prochain appel
    /// autorisé » pour tous les appels, respecté par <see cref="NodecraftClient"/>, les pastilles et la session de réveil.
    /// Tout thread. Logique pure, testée dans Ovomium.Tests.
    /// </summary>
    internal static class NodecraftThrottle
    {
        private static readonly Stopwatch s_clock = Stopwatch.StartNew();
        private static long s_resumeAtMs;

        /// <summary>Secondes restantes avant le prochain appel autorisé (0 : pas de pause).</summary>
        public static int SecondsLeft
        {
            get
            {
                long left = Interlocked.Read(ref s_resumeAtMs) - s_clock.ElapsedMilliseconds;
                return left <= 0 ? 0 : (int)Math.Ceiling(left / 1000.0);
            }
        }

        public static bool IsPaused => SecondsLeft > 0;

        /// <summary>Suspend les appels <paramref name="seconds"/> s (une pause plus longue déjà en cours est gardée).</summary>
        public static void Pause(int seconds)
        {
            long wanted = s_clock.ElapsedMilliseconds + seconds * 1000L;
            long current;
            do
            {
                current = Interlocked.Read(ref s_resumeAtMs);
                if (current >= wanted)
                    return;
            }
            while (Interlocked.CompareExchange(ref s_resumeAtMs, wanted, current) != current);
        }
    }
}
