namespace Ovomium.Features.SharedSleep
{
    /// <summary>Seuil fixe du sommeil partagé (calcul pur, testé dans Ovomium.Tests) : la moitié arrondie au-dessus.</summary>
    internal static class SleepQuorum
    {
        /// <summary>Dormeuses requises : 1 sur 1 ou 2, 2 sur 3 ou 4, 3 sur 5 ou 6…</summary>
        public static int Required(int total) => total <= 1 ? 1 : (total + 1) / 2;

        /// <summary>Seuil atteint, avec au moins une vraie dormeuse.</summary>
        public static bool IsReached(int sleepers, int total) => total > 0 && sleepers >= Required(total);
    }
}
