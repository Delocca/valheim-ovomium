using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.SkillTooltip
{
    /// <summary>
    /// Fenêtre des compétences : <c>SkillsDialog.Setup(Player)</c> repose à chaque ouverture, ligne par ligne
    /// (<c>m_elements[j]</c> ↔ <c>GetSkillList()[j]</c>), le <c>UITooltip</c> avec la description vanilla. Le postfix
    /// ajoute sous cette description l'effet chiffré au niveau actuel (bonus d'effets de statut compris, comme le jeu)
    /// et au niveau 100, et remplace le prefab « texte nu » de ces lignes par celui, cadré, des infobulles d'objets
    /// (nom de la compétence en titre). Idempotent : le texte vanilla est réécrit par le jeu avant chaque passage.
    /// </summary>
    [HarmonyPatch(typeof(SkillsDialog), "Setup", new System.Type[] { typeof(Player) })]
    internal static class SkillTooltipPatch
    {
        private const string DimColor = "#9A9A9A";

        private static void Postfix(SkillsDialog __instance, Player player)
        {
            if (!SkillTooltipConfig.Enabled.Value)
                return;
            Skills skills = player.GetSkills();
            List<Skills.Skill> list = skills.GetSkillList();
            GameObject framed = ItemTooltipPrefab();
            for (int j = 0; j < list.Count && j < __instance.m_elements.Count; j++)
            {
                UITooltip tooltip = __instance.m_elements[j].GetComponentInChildren<UITooltip>();
                if (tooltip == null)
                    continue;
                Skills.SkillType type = list[j].m_info.m_skill;
                if (framed != null)
                {
                    tooltip.m_tooltipPrefab = framed;
                    tooltip.m_topic = "$skill_" + type.ToString().ToLower();
                }
                string extra = Describe(type, skills.GetSkillLevel(type));
                if (extra != null)
                    tooltip.m_text += extra;
            }
        }

        /// <summary>Prefab cadré des infobulles d'objets (celui des cases d'inventaire), ou null hors partie.</summary>
        private static GameObject ItemTooltipPrefab()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_playerGrid == null || gui.m_playerGrid.m_elementPrefab == null)
                return null;
            UITooltip model = gui.m_playerGrid.m_elementPrefab.GetComponentInChildren<UITooltip>();
            return model != null ? model.m_tooltipPrefab : null;
        }

        /// <summary>Bloc « Niveau N : … » (valeurs en orange) puis « Niveau 100 : … » en gris, ou null si inconnu.</summary>
        private static string Describe(Skills.SkillType type, float level)
        {
            string now = SkillEffects.Describe(type, Mathf.Clamp01(level / 100f), "orange");
            if (now == null)
                return null;
            string text = $"\n\nNiveau {(int)level} : {now}";
            if (level < 100f)
                text += $"\n<color={DimColor}>Niveau 100 : {SkillEffects.Describe(type, 1f, null)}</color>";
            return text;
        }
    }
}
