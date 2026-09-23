using HarmonyLib;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>Tick de réservation : <c>Player.Update</c> (privée, sans argument).</summary>
    [HarmonyPatch(typeof(Player), "Update", new System.Type[0])]
    internal static class ChestReservationTickPatch
    {
        private static void Postfix(Player __instance) => ChestReservation.Tick(__instance);
    }

    /// <summary>Avale la réponse d'ouverture quand elle répond à une de nos demandes (sinon le jeu ouvrirait la fenêtre).</summary>
    [HarmonyPatch(typeof(Container), "RPC_OpenResponse", new System.Type[] { typeof(long), typeof(bool) })]
    internal static class ChestReservationResponsePatch
    {
        private static bool Prefix(Container __instance, bool granted) => !ChestReservation.OnOpenResponse(__instance, granted);
    }

    /// <summary>Un clic réel sur un coffre réservé : la réponse doit ouvrir la fenêtre.</summary>
    [HarmonyPatch(typeof(Container), "Interact", new System.Type[] { typeof(Humanoid), typeof(bool), typeof(bool) })]
    internal static class ChestReservationInteractPatch
    {
        private static void Prefix(Container __instance) => ChestReservation.OnInteract(__instance);
    }

    /// <summary>
    /// Coffre gardé et possédé : la demande d'une autre joueuse reçoit la réponse vanilla « en cours d'utilisation »
    /// (branche <c>IsInUse</c> de <c>Container.RPC_RequestOpen</c>), la propriété reste ici.
    /// </summary>
    [HarmonyPatch(typeof(Container), "RPC_RequestOpen", new System.Type[] { typeof(long), typeof(long) })]
    internal static class ChestReservationHoldPatch
    {
        private static bool Prefix(Container __instance, long uid)
        {
            if (!ChestReservation.IsHeld(__instance) || !__instance.m_nview.IsOwner() || uid == ZNet.GetUID()) return true;
            Plugin.Log.LogInfo($"ChestReservation : demande sur {__instance.m_name} refusée, coffre gardé ici");
            __instance.m_nview.InvokeRPC(uid, "RPC_OpenResponse", false);
            return false;
        }
    }
}
