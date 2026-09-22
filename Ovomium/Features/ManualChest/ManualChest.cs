using Ovomium.Features.CraftFromChests;

namespace Ovomium.Features.ManualChest
{
    /// <summary>
    /// Drapeau « coffre manuel » porté par le ZDO du coffre (clé custom, répliquée comme n'importe quelle valeur de ZDO,
    /// invisible pour le jeu vanilla ; modèle : <c>Container.RPC_Discovered</c>). Défaut : coffre automatique.
    /// L'écriture exige de posséder le ZDO : coffre ouvert dans l'interface on l'est déjà, sinon prise directe.
    /// </summary>
    internal static class ManualChest
    {
        private static readonly int s_hash = "ovomium_manual".GetStableHashCode();

        public static bool IsManual(Container container)
        {
            var nview = container != null ? container.m_nview : null;
            if (nview == null || !nview.IsValid()) return false;
            return nview.GetZDO().GetBool(s_hash);
        }

        public static void SetManual(Container container, bool manual)
        {
            var nview = container != null ? container.m_nview : null;
            if (nview == null || !nview.IsValid()) return;
            if (!nview.IsOwner()) NearbyChests.TakeOwnership(container);
            nview.GetZDO().Set(s_hash, manual);
            Plugin.Log.LogInfo($"ManualChest : {container.m_name} → {(manual ? "manuel" : "automatique")}");
        }
    }
}
