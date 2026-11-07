using System;
using Ovomium.Features.SlopeWalk;

namespace Ovomium.Tests
{
    /// <summary>Facteur de <see cref="SlopeSlowdown"/> : réglages par défaut (60°, 30 %) sauf mention.</summary>
    internal static class SlopeSlowdownTests
    {
        private static int s_failures;

        public static int Run()
        {
            RunTarget();
            RunUphill();
            Console.WriteLine(s_failures == 0 ? "SlopeSlowdown : tous les tests passent" : $"SlopeSlowdown : {s_failures} échec(s)");
            return s_failures;
        }

        private static void RunTarget()
        {
            Check("sol plat", 0f, SlopeSlowdown.Target(0f, 1f, 60f, 30f));
            Check("sous le seuil", 0f, SlopeSlowdown.Target(19f, 1f, 60f, 30f));
            Check("au seuil", 0f, SlopeSlowdown.Target(SlopeSlowdown.StartAngle, 1f, 60f, 30f));
            Check("mi-rampe", 0.15f, SlopeSlowdown.Target(40f, 1f, 60f, 30f));
            Check("plein ralentissement", 0.3f, SlopeSlowdown.Target(60f, 1f, 60f, 30f));
            Check("au-delà de la limite", 0.3f, SlopeSlowdown.Target(80f, 1f, 60f, 30f));
            Check("montée en biais", 0.15f, SlopeSlowdown.Target(60f, 0.5f, 60f, 30f));
            Check("traversée", 0f, SlopeSlowdown.Target(60f, 0f, 60f, 30f));
            Check("réglé à 0 %", 0f, SlopeSlowdown.Target(60f, 1f, 60f, 0f));
            Check("limite plus basse", 0.3f, SlopeSlowdown.Target(40f, 1f, 40f, 30f));
            Check("limite sous le seuil, en dessous", 0f, SlopeSlowdown.Target(9f, 1f, 10f, 30f));
            Check("limite sous le seuil, au-dessus", 0.3f, SlopeSlowdown.Target(11f, 1f, 10f, 30f));
            Check("pourcentage borné", 1f, SlopeSlowdown.Target(60f, 1f, 60f, 150f));
        }

        private static void RunUphill()
        {
            // Sol qui descend vers +x : sa normale penche vers +x.
            Check("droit dans la pente", 1f, SlopeSlowdown.Uphill(-1f, 0f, 0.5f, 0f));
            Check("descente", 0f, SlopeSlowdown.Uphill(1f, 0f, 0.5f, 0f));
            Check("traversée", 0f, SlopeSlowdown.Uphill(0f, 1f, 0.5f, 0f));
            Check("en biais, à 45°", (float)Math.Sqrt(0.5), SlopeSlowdown.Uphill(-1f, 1f, 0.5f, 0f));
            Check("longueurs ignorées", 1f, SlopeSlowdown.Uphill(-0.2f, 0f, 3f, 0f));
            Check("immobile", 0f, SlopeSlowdown.Uphill(0f, 0f, 0.5f, 0f));
            Check("sol plat", 0f, SlopeSlowdown.Uphill(1f, 0f, 0f, 0f));
        }

        private static void Check(string name, float expected, float actual)
        {
            if (Math.Abs(expected - actual) < 1e-5f)
                return;
            s_failures++;
            Console.WriteLine($"ÉCHEC {name} : attendu {expected}, obtenu {actual}");
        }
    }
}
