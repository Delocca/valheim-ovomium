using System.Collections.Generic;
using HarmonyLib;

namespace Ovomium.Features.UtilitySlots
{
    /// <summary>
    /// Équiper un objet utilitaire alors que l'emplacement vanilla est pris : l'occupant passe dans les objets en plus
    /// (<see cref="UtilitySlots"/>) avant que <c>EquipItem</c> ne l'éjecte, le nouvel objet prend l'emplacement
    /// vanilla, puis le plus ancien sort au-delà du plafond. Toutes les vérifications vanilla (durabilité, attaque,
    /// nage, niveau du monde…) restent celles du jeu.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem), new[] { typeof(ItemDrop.ItemData), typeof(bool) })]
    internal static class UtilitySlotsPatch
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, out ItemDrop.ItemData __state)
        {
            UtilitySlots.SuppressPromotion++;
            __state = null;
            if (!UtilitySlots.ShouldKeepOccupant(__instance, item))
                return;
            __state = __instance.m_utilityItem;
            UtilitySlots.MoveOccupantToExtras(__instance);
        }

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result, ItemDrop.ItemData __state)
        {
            if (!__result)
            {
                if (__state != null)
                    UtilitySlots.RestoreOccupant(__instance, __state);
                return;
            }
            if (__instance is Player && UtilitySlots.IsUtility(item))
                UtilitySlots.Trim(__instance, item);
        }

        /// <summary>Filet : si l'emplacement vanilla est resté vide, le plus récent des objets en plus y monte.</summary>
        private static void Finalizer(Humanoid __instance)
        {
            UtilitySlots.SuppressPromotion--;
            if (__instance is Player)
                UtilitySlots.Promote(__instance);
        }

        /// <summary>Rechargement à chaud et suivi des réglages.</summary>
        internal static void Install()
        {
            UtilitySlotsConfig.Enabled.SettingChanged += (_, __) => UtilitySlots.Reconcile(Player.m_localPlayer);
            UtilitySlotsConfig.MaxItems.SettingChanged += (_, __) => UtilitySlots.Reconcile(Player.m_localPlayer);
            UtilitySlots.Reconcile(Player.m_localPlayer);
        }
    }

    /// <summary>
    /// Déséquiper : un objet en plus quitte la liste (le jeu a déjà remis <c>m_equipped</c> à faux, faute
    /// d'emplacement correspondant) ; l'emplacement vanilla vidé est repourvu par le plus récent des objets en plus.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem), new[] { typeof(ItemDrop.ItemData), typeof(bool) })]
    internal static class UtilitySlotsUnequipPatch
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, out bool __state) =>
            __state = item != null && item == __instance.m_utilityItem;

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __state)
        {
            if (item == null || !(__instance is Player))
                return;
            if (UtilitySlots.RemoveExtra(__instance, item))
                __instance.SetupEquipment();
            else if (__state)
                UtilitySlots.Promote(__instance);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped), new[] { typeof(ItemDrop.ItemData) })]
    internal static class UtilitySlotsIsEquipedPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result) =>
            __result = __result || UtilitySlots.IsExtra(__instance, item);
    }

    /// <summary>
    /// Déséquipement total, dont la mort (<c>Player.CreateTombStone</c>, sauf monde « garder l'équipement ») : les
    /// objets en plus suivent l'objet de l'emplacement vanilla. <c>Player.UnequipDeathDropItems</c> n'a aucun appelant.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems), new System.Type[0])]
    internal static class UtilitySlotsUnequipAllPatch
    {
        private static void Prefix() => UtilitySlots.SuppressPromotion++;

        private static void Postfix(Humanoid __instance) => UtilitySlots.UnequipAll(__instance);

        private static void Finalizer() => UtilitySlots.SuppressPromotion--;
    }

    /// <summary>Filet : un objet en plus sorti de l'inventaire par un autre chemin que <c>UnequipItem</c>.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged), new System.Type[0])]
    internal static class UtilitySlotsInventoryPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance.m_isLoading && UtilitySlots.DropMissing(__instance))
                __instance.SetupEquipment();
        }
    }

    /// <summary>
    /// Effets d'équipement des objets en plus (lumière du feu-follet, +150 de la ceinture…). Le jeu retire chaque
    /// effet de <c>m_equipmentStatusEffects</c> absent de sa liste recalculée : on les sort de cet ensemble avant,
    /// et on les y remet après, sans jamais les retirer du joueur (<c>SE_Demister.Stop</c> détruirait la boule).
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects), new System.Type[0])]
    internal static class UtilitySlotsEffectsPatch
    {
        private static void Prefix(Humanoid __instance, out List<StatusEffect> __state)
        {
            __state = null;
            List<ItemDrop.ItemData> extras = UtilitySlots.Extras(__instance);
            if (extras.Count == 0)
                return;
            __state = new List<StatusEffect>(extras.Count);
            foreach (ItemDrop.ItemData item in extras)
            {
                StatusEffect effect = item.m_shared.m_equipStatusEffect;
                if (effect == null)
                    continue;
                __state.Add(effect);
                __instance.m_equipmentStatusEffects.Remove(effect);
            }
        }

        private static void Postfix(Humanoid __instance, List<StatusEffect> __state)
        {
            if (__state == null)
                return;
            foreach (StatusEffect effect in __state)
            {
                __instance.m_equipmentStatusEffects.Add(effect);
                if (!__instance.m_seman.HaveStatusEffect(effect.NameHash()))
                    __instance.m_seman.AddStatusEffect(effect, resetTime: false, 0, 0f, -1);
            }
        }
    }

    /// <summary>Usure des objets en plus (aucun objet utilitaire vanilla n'en a, par cohérence avec le jeu).</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment), new[] { typeof(float) })]
    internal static class UtilitySlotsDurabilityPatch
    {
        private static void Postfix(Humanoid __instance, float dt)
        {
            List<ItemDrop.ItemData> extras = UtilitySlots.Extras(__instance);
            for (int i = extras.Count - 1; i >= 0; i--)
            {
                if (i < extras.Count && extras[i].m_shared.m_useDurability)
                    __instance.DrainEquipedItemDurability(extras[i], dt);
            }
        }
    }

    /// <summary>Modificateurs d'équipement (vitesse, endurance, résistance à la chaleur…) des objets en plus.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers), new System.Type[0])]
    internal static class UtilitySlotsModifiersPatch
    {
        private static void Postfix(Player __instance)
        {
            List<ItemDrop.ItemData> extras = UtilitySlots.Extras(__instance);
            if (extras.Count == 0 || Player.s_equipmentModifierSourceFields == null)
                return;
            for (int i = 0; i < __instance.m_equipmentModifierValues.Length; i++)
            {
                foreach (ItemDrop.ItemData item in extras)
                    __instance.m_equipmentModifierValues[i] += (float)Player.s_equipmentModifierSourceFields[i].GetValue(item.m_shared);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier), new System.Type[0])]
    internal static class UtilitySlotsEitrPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            foreach (ItemDrop.ItemData item in UtilitySlots.Extras(__instance))
                __result += item.m_shared.m_eitrRegenModifier;
        }
    }
}
