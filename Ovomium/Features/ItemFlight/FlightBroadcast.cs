using System.Collections.Generic;
using HarmonyLib;
using Ovomium.Features.CraftFromChests;
using UnityEngine;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>
    /// Vols vus par les autres joueuses : chaque transfert réel (craft à la consommation, pose, feu, QuickStash) est
    /// diffusé à tous les pairs par un RPC routé custom. Le serveur dédié vanilla le relaie sans le connaître
    /// (<c>ZRoutedRpc.RPC_RoutedRPC</c> → <c>RouteRPC</c>), une joueuse sans le mod l'ignore en silence (hash non
    /// enregistré). À la réception, un retrait n'est rejoué que si son coffre est chargé localement : pas d'autre filtre
    /// de distance. <c>ZRoutedRpc</c> est recréé à chaque <c>ZNet.Awake</c> et n'a pas de désenregistrement :
    /// on retire le hash de <c>m_functions</c> à la main (rechargement à chaud).
    /// </summary>
    internal static class FlightBroadcast
    {
        private const string RpcName = "Ovomium_ItemFlight";
        /// <summary>Format du paquet ; un paquet d'une autre version est ignoré (amie sur une autre version du mod).</summary>
        private const byte FormatVersion = 1;
        private const byte ToPoint = 0;
        private const byte ToChest = 1;

        private static readonly int s_rpcHash = RpcName.GetStableHashCode();

        public static void Install() => Register();

        public static void Unload() => ZRoutedRpc.instance?.m_functions.Remove(s_rpcHash);

        private static void Register()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null) return;
            rpc.m_functions.Remove(s_rpcHash);
            rpc.Register<ZPackage>(RpcName, OnReceive);
        }

        [HarmonyPatch(typeof(ZNet), "Awake", new System.Type[0])]
        private static class ZNetAwakePatch
        {
            private static void Postfix() => Register();
        }

        /// <summary>Retraits de coffres volant vers <paramref name="destination"/> (station, fantôme, feu, joueuse).</summary>
        public static void SendToPoint(List<Pull> pulls, Vector3 destination)
        {
            var entries = new List<Pull>();
            foreach (Pull pull in pulls)
                if (ChestId(pull.Chest) != ZDOID.None && pull.Item != null && pull.Amount > 0) entries.Add(pull);
            if (entries.Count > 0) Send(ToPoint, destination, ZDOID.None, entries);
        }

        /// <summary>QuickStash : l'objet part du centre de l'expéditrice, suivie à la réception si elle y est chargée.</summary>
        public static void SendToChest(Player sender, ItemDrop item, int amount, Container chest)
        {
            if (sender == null || item == null || amount <= 0 || ChestId(chest) == ZDOID.None) return;
            Send(ToChest, sender.GetCenterPoint(), sender.GetZDOID(), new List<Pull> { new Pull(chest, item, amount) });
        }

        private static void Send(byte kind, Vector3 point, ZDOID sender, List<Pull> entries)
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || ZNet.instance == null) return;
            var pkg = new ZPackage();
            pkg.Write(FormatVersion);
            pkg.Write(kind);
            pkg.Write(point);
            pkg.Write(sender);
            pkg.Write(entries.Count);
            foreach (Pull pull in entries)
            {
                pkg.Write(ChestId(pull.Chest));
                pkg.Write(pull.Item.gameObject.name.GetStableHashCode());
                pkg.Write(pull.Amount);
            }
            rpc.InvokeRoutedRPC(ZRoutedRpc.Everybody, RpcName, pkg);
        }

        /// <summary>Paquet venu d'une autre joueuse (entrée externe : tout est validé, un paquet illisible est ignoré).</summary>
        private static void OnReceive(long senderPeer, ZPackage pkg)
        {
            if (ZRoutedRpc.instance == null || senderPeer == ZRoutedRpc.instance.m_id) return;
            if (!ItemFlightConfig.Enabled.Value || !ItemFlightConfig.ShowOthers.Value) return;
            try
            {
                if (pkg.ReadByte() != FormatVersion) return;
                byte kind = pkg.ReadByte();
                Vector3 point = pkg.ReadVector3();
                ZDOID sender = pkg.ReadZDOID();
                List<Pull> pulls = ReadPulls(pkg);
                if (pulls.Count == 0) return;
                if (kind == ToPoint) ItemFlight.Launch(pulls, point, false);
                else if (kind == ToChest) ItemFlight.LaunchToChest(pulls[0].Item, pulls[0].Amount, SenderCenter(sender, point), pulls[0].Chest);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"ItemFlight : paquet d'une autre joueuse illisible ({e.Message})");
            }
        }

        /// <summary>Retraits dont le coffre et l'objet existent ici ; les autres (coffre non chargé) sont sautés.</summary>
        private static List<Pull> ReadPulls(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            var pulls = new List<Pull>();
            for (int i = 0; i < count; i++)
            {
                ZDOID chestId = pkg.ReadZDOID();
                int itemHash = pkg.ReadInt();
                int amount = pkg.ReadInt();
                GameObject chestObject = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(chestId) : null;
                Container chest = chestObject != null ? chestObject.GetComponent<Container>() : null;
                GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(itemHash) : null;
                ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (chest != null && item != null && amount > 0) pulls.Add(new Pull(chest, item, amount));
            }
            return pulls;
        }

        private static Vector3 SenderCenter(ZDOID sender, Vector3 fallback)
        {
            GameObject body = sender != ZDOID.None && ZNetScene.instance != null ? ZNetScene.instance.FindInstance(sender) : null;
            Player player = body != null ? body.GetComponent<Player>() : null;
            return player != null ? player.GetCenterPoint() : fallback;
        }

        private static ZDOID ChestId(Container chest)
        {
            ZDO zdo = chest != null && chest.m_nview != null ? chest.m_nview.GetZDO() : null;
            return zdo != null ? zdo.m_uid : ZDOID.None;
        }
    }
}
