using HarmonyLib;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>Tick de réservation : <c>Player.Update</c> (privée, sans argument).</summary>
    [HarmonyPatch(typeof(Player), "Update", new System.Type[0])]
    internal static class ChestReservationTickPatch
    {
        private static void Postfix(Player __instance) => ChestReservation.Tick(__instance);
    }

    /// <summary>Avale la réponse d'ouverture quand elle répond à une réservation (sinon le jeu ouvrirait la fenêtre).</summary>
    [HarmonyPatch(typeof(Container), "RPC_OpenResponse", new System.Type[] { typeof(long), typeof(bool) })]
    internal static class ChestReservationResponsePatch
    {
        private static bool Prefix(Container __instance) => !ChestReservation.OnOpenResponse(__instance);
    }

    /// <summary>Un clic réel sur un coffre réservé : la réponse doit ouvrir la fenêtre.</summary>
    [HarmonyPatch(typeof(Container), "Interact", new System.Type[] { typeof(Humanoid), typeof(bool), typeof(bool) })]
    internal static class ChestReservationInteractPatch
    {
        private static void Prefix(Container __instance) => ChestReservation.OnInteract(__instance);
    }
}
