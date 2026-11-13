using System;
using Ovomium.Features.SharedSleep;

namespace Ovomium.Tests
{
    /// <summary>Seuil de <see cref="SleepQuorum"/> : la moitié arrondie au-dessus, au moins une dormeuse.</summary>
    internal static class SleepQuorumTests
    {
        private static int s_failures;

        public static int Run()
        {
            Check("requises sur 1", 1, SleepQuorum.Required(1));
            Check("requises sur 2", 1, SleepQuorum.Required(2));
            Check("requises sur 3", 2, SleepQuorum.Required(3));
            Check("requises sur 4", 2, SleepQuorum.Required(4));
            Check("requises sur 5", 3, SleepQuorum.Required(5));
            Check("requises sur 10", 5, SleepQuorum.Required(10));
            Check("requises sans personne", 1, SleepQuorum.Required(0));
            Check("1 sur 2", true, SleepQuorum.IsReached(1, 2));
            Check("1 sur 3", false, SleepQuorum.IsReached(1, 3));
            Check("2 sur 3", true, SleepQuorum.IsReached(2, 3));
            Check("2 sur 4", true, SleepQuorum.IsReached(2, 4));
            Check("2 sur 5", false, SleepQuorum.IsReached(2, 5));
            Check("personne couchée", false, SleepQuorum.IsReached(0, 2));
            Check("seule et couchée", true, SleepQuorum.IsReached(1, 1));
            Check("liste vide", false, SleepQuorum.IsReached(0, 0));
            Console.WriteLine(s_failures == 0 ? "SleepQuorum : tous les tests passent" : $"SleepQuorum : {s_failures} échec(s)");
            return s_failures;
        }

        private static void Check<T>(string name, T expected, T actual)
        {
            if (Equals(expected, actual))
                return;
            s_failures++;
            Console.WriteLine($"ÉCHEC {name} : attendu {expected}, obtenu {actual}");
        }
    }
}
