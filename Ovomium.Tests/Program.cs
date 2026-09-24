namespace Ovomium.Tests
{
    /// <summary>Lance chaque jeu de tests ; code de sortie = nombre d'échecs (fait échouer le build).</summary>
    internal static class Program
    {
        private static int Main() => StackGroupingTests.Run() + PickupFilterListTests.Run();
    }
}
