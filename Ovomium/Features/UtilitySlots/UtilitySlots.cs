using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ovomium.Features.UtilitySlots
{
    /// <summary>
    /// Objets utilitaires portés en plus de l'emplacement vanilla <c>Humanoid.m_utilityItem</c>, du plus ancien au
    /// plus récent. Le dernier équipé occupe toujours l'emplacement vanilla (visuel sur le personnage, répliqué) ;
    /// les autres ne sont connus que d'ici. Rien n'est sauvegardé : le jeu garde <c>m_equipped</c> sur chaque objet
    /// et <c>Player.EquipInventoryItems</c> les rééquipe un à un au chargement, ce qui reconstruit la liste.
    /// </summary>
    internal static class UtilitySlots
    {
        private static readonly ConditionalWeakTable<Humanoid, List<ItemDrop.ItemData>> s_extras =
            new ConditionalWeakTable<Humanoid, List<ItemDrop.ItemData>>();
        private static readonly List<ItemDrop.ItemData> s_none = new List<ItemDrop.ItemData>(0);

        /// <summary>
        /// Non nul pendant <c>EquipItem</c> (le nouvel objet va prendre l'emplacement vanilla qu'il vient de vider)
        /// et <c>UnequipAllItems</c> (mort comprise) : l'emplacement vidé n'est alors pas repourvu.
        /// </summary>
        internal static int SuppressPromotion;

        private static bool Active => UtilitySlotsConfig.Enabled.Value && UtilitySlotsConfig.MaxItems.Value > 1;

        internal static List<ItemDrop.ItemData> Extras(Humanoid humanoid) =>
            humanoid is Player && s_extras.TryGetValue(humanoid, out var extras) ? extras : s_none;

        internal static bool IsExtra(Humanoid humanoid, ItemDrop.ItemData item) => IndexOf(Extras(humanoid), item) >= 0;

        internal static bool IsUtility(ItemDrop.ItemData item) =>
            item?.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;

        /// <summary>Avant <c>EquipItem</c> : l'occupant de l'emplacement vanilla doit-il rester porté ?</summary>
        internal static bool ShouldKeepOccupant(Humanoid humanoid, ItemDrop.ItemData item)
        {
            ItemDrop.ItemData occupant = humanoid.m_utilityItem;
            return Active && humanoid is Player && IsUtility(item) && occupant != null && occupant != item
                   && !SameKind(occupant, item) && !IsExtra(humanoid, item);
        }

        /// <summary>Libère l'emplacement vanilla sans déséquiper son occupant : <c>EquipItem</c> n'éjecte alors rien.</summary>
        internal static void MoveOccupantToExtras(Humanoid humanoid)
        {
            s_extras.GetOrCreateValue(humanoid).Add(humanoid.m_utilityItem);
            humanoid.m_utilityItem = null;
        }

        /// <summary><c>EquipItem</c> a refusé l'objet : l'occupant reprend sa place.</summary>
        internal static void RestoreOccupant(Humanoid humanoid, ItemDrop.ItemData occupant)
        {
            Remove(Extras(humanoid), occupant);
            humanoid.m_utilityItem = occupant;
        }

        /// <summary>Après un équipement réussi : un seul exemplaire par objet, et pas plus que le plafond.</summary>
        internal static void Trim(Humanoid humanoid, ItemDrop.ItemData newest)
        {
            List<ItemDrop.ItemData> extras = Extras(humanoid);
            for (int i = extras.Count - 1; i >= 0; i--)
            {
                if (i < extras.Count && extras[i] != newest && SameKind(extras[i], newest))
                    humanoid.UnequipItem(extras[i], triggerEquipEffects: false);
            }
            int max = Active ? UtilitySlotsConfig.MaxItems.Value : 1;
            while (extras.Count > 0 && extras.Count + (humanoid.m_utilityItem != null ? 1 : 0) > max)
                humanoid.UnequipItem(extras[0], triggerEquipEffects: false);
        }

        /// <summary>Retire l'objet des emplacements en plus ; vrai s'il y était.</summary>
        internal static bool RemoveExtra(Humanoid humanoid, ItemDrop.ItemData item) => Remove(Extras(humanoid), item);

        /// <summary>L'emplacement vanilla vient d'être vidé : le plus récent des objets en plus y monte (visuel).</summary>
        internal static void Promote(Humanoid humanoid)
        {
            List<ItemDrop.ItemData> extras = Extras(humanoid);
            if (SuppressPromotion > 0 || humanoid.m_utilityItem != null || extras.Count == 0)
                return;
            humanoid.m_utilityItem = extras[extras.Count - 1];
            extras.RemoveAt(extras.Count - 1);
            humanoid.SetupEquipment();
        }

        /// <summary>Tous les objets en plus, déséquipés (mort, déséquipement total).</summary>
        internal static void UnequipAll(Humanoid humanoid)
        {
            List<ItemDrop.ItemData> extras = Extras(humanoid);
            while (extras.Count > 0)
                humanoid.UnequipItem(extras[extras.Count - 1], triggerEquipEffects: false);
        }

        /// <summary>
        /// Remet l'état en accord avec les réglages et l'inventaire : au chargement à chaud (la version précédente
        /// laisse ses objets en plus marqués <c>m_equipped</c> hors de tout emplacement) et à chaque changement de
        /// réglage. Les objets réadoptés le sont dans l'ordre de l'inventaire.
        /// </summary>
        internal static void Reconcile(Player player)
        {
            if (player == null)
                return;
            List<ItemDrop.ItemData> extras = s_extras.GetOrCreateValue(player);
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (item.m_equipped && IsUtility(item) && item != player.m_utilityItem && IndexOf(extras, item) < 0)
                    extras.Add(item);
            }
            if (!Active)
            {
                UnequipAll(player);
                return;
            }
            for (int i = extras.Count - 1; i >= 0; i--)
            {
                if (i < extras.Count && HasNewerTwin(player, extras, i))
                    player.UnequipItem(extras[i], triggerEquipEffects: false);
            }
            Promote(player);
            if (player.m_utilityItem != null)
                Trim(player, player.m_utilityItem);
            player.SetupEquipment();
        }

        /// <summary>Retire de la liste les objets qui ont quitté l'inventaire ; vrai si elle a changé.</summary>
        internal static bool DropMissing(Player player)
        {
            List<ItemDrop.ItemData> extras = Extras(player);
            bool changed = false;
            for (int i = extras.Count - 1; i >= 0; i--)
            {
                if (player.GetInventory().ContainsItem(extras[i]))
                    continue;
                extras[i].m_equipped = false;
                extras.RemoveAt(i);
                changed = true;
            }
            return changed;
        }

        private static bool HasNewerTwin(Humanoid humanoid, List<ItemDrop.ItemData> extras, int index)
        {
            if (humanoid.m_utilityItem != null && SameKind(humanoid.m_utilityItem, extras[index]))
                return true;
            for (int j = index + 1; j < extras.Count; j++)
            {
                if (SameKind(extras[j], extras[index]))
                    return true;
            }
            return false;
        }

        private static bool SameKind(ItemDrop.ItemData a, ItemDrop.ItemData b) => a.m_shared.m_name == b.m_shared.m_name;

        private static int IndexOf(List<ItemDrop.ItemData> list, ItemDrop.ItemData item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return i;
            }
            return -1;
        }

        private static bool Remove(List<ItemDrop.ItemData> list, ItemDrop.ItemData item)
        {
            int index = IndexOf(list, item);
            if (index < 0)
                return false;
            list.RemoveAt(index);
            return true;
        }
    }
}
