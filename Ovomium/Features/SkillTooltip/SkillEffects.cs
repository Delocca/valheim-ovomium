using System.Globalization;
using UnityEngine;

namespace Ovomium.Features.SkillTooltip
{
    /// <summary>
    /// Effet chiffré d'une compétence pour un facteur f = niveau/100 (formules du jeu 1.0.15 : Player.CheckRun /
    /// GetRunSpeedFactor, Character.Jump, Player.OnSwimming / OnSneaking / UpdateStealth / GetDodgeStaminaUse,
    /// ItemData.GetBlockPower, Skills.GetRandomSkillRange, Attack.GetAttackStamina, ItemData.GetDrawStaminaDrain /
    /// GetWeaponLoadingTime, Humanoid.GetAttackDrawPercentage, FishingFloat.FixedUpdate, CookingStation.OnAddItem,
    /// Pickable.Interact, InventoryGui.UpdateRecipe / DoCrafting, Player.GetPlaceStamina / GetPlaceDurability,
    /// Sadle.UpdateStamina, Character.GetRunSpeedFactor). Les constantes marquées « prefab » sont les valeurs par
    /// défaut, un objet ou une plante peut en dévier.
    /// </summary>
    internal static class SkillEffects
    {
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        /// <summary>Texte d'une ligne, valeurs colorées par <paramref name="valueColor"/> (null : sans balise).</summary>
        public static string Describe(Skills.SkillType type, float f, string valueColor)
        {
            var w = new Writer(valueColor);
            switch (type)
            {
                case Skills.SkillType.Run:
                    return $"{w.Pct(-0.5f * f)} d'endurance en courant, {w.Pct(0.25f * f)} de vitesse";
                case Skills.SkillType.Jump:
                    return $"{w.Pct(0.4f * f)} de force de saut";
                case Skills.SkillType.Swim:
                    return $"{w.Pct(Mathf.Lerp(5f, 2f, f) / 5f - 1f)} d'endurance en nageant";
                case Skills.SkillType.Sneak:
                    return Sneak(w, f);
                case Skills.SkillType.Blocking:
                    return $"{w.Pct(0.5f * f)} de blocage";
                case Skills.SkillType.Dodge:
                    return $"{w.Pct(-0.5f * f)} d'endurance en esquivant";
                case Skills.SkillType.Fishing:
                    return $"{w.Pct(-0.8f * f)} d'endurance (ligne tendue et traction), enroulement {w.Mul(Mathf.Lerp(1f, 2f, f))}";
                case Skills.SkillType.Cooking:
                    return $"{w.Pct(BonusChance() * f, signed: false)} de chance d'un plat en plus";
                case Skills.SkillType.Farming:
                    return $"{w.Pct(0.25f * f, signed: false)} de chance d'une récolte en plus, rayon de récolte élargi";
                case Skills.SkillType.Crafting:
                    return Crafting(w, f);
                case Skills.SkillType.Ride:
                    return $"{w.Pct(-0.5f * f)} d'endurance de la monture, {w.Pct(0.25f * f)} de vitesse";
                default:
                    return IsWeapon(type) ? Weapon(w, type, f) : null;
            }
        }

        private static bool IsWeapon(Skills.SkillType type) => type >= Skills.SkillType.Swords && type <= Skills.SkillType.Crossbows;

        private static string Sneak(Writer w, float f)
        {
            float stamina = Mathf.Lerp(1f, 0.25f, Mathf.Pow(f, 0.5f)) - 1f;
            float dark = Mathf.Lerp(0.5f, 0.2f, f);
            float light = Mathf.Lerp(1f, 0.6f, f);
            return $"{w.Pct(stamina)} d'endurance accroupi, détection à {w.Mul(dark)} dans le noir / {w.Mul(light)} en lumière";
        }

        private static string Crafting(Writer w, float f)
        {
            InventoryGui gui = InventoryGui.instance;
            float duration = gui != null ? gui.m_craftDurationSkillMaxDecrease : 0.6f;
            return $"{w.Pct(-duration * f)} de durée de fabrication, {w.Pct(BonusChance() * f, signed: false)} de chance "
                 + $"d'un objet empilable en plus, {w.Pct(-0.5f * f)} d'endurance et d'usure d'outil en construisant";
        }

        private static string Weapon(Writer w, Skills.SkillType type, float f)
        {
            float mid = Mathf.Lerp(0.4f, 1f, f);
            string text = $"dégâts de {w.Pct(Mathf.Clamp01(mid - 0.15f), signed: false)} à "
                        + $"{w.Pct(Mathf.Clamp01(mid + 0.15f), signed: false)}, {w.Pct(-0.33f * f)} de coût d'attaque";
            if (type == Skills.SkillType.Bows)
                text += $", {w.Pct(-0.33f * f)} d'endurance et {w.Pct(-0.8f * f)} de temps à l'armement";
            else if (type == Skills.SkillType.Crossbows)
                text += $", {w.Pct(-0.5f * f)} de temps de rechargement";
            return text;
        }

        private static float BonusChance()
        {
            InventoryGui gui = InventoryGui.instance;
            return gui != null ? gui.m_craftBonusChance : 0.25f;
        }

        /// <summary>Met en forme les valeurs (« −18 % », « ×1,5 ») et les colore si une couleur est donnée.</summary>
        private sealed class Writer
        {
            private readonly string m_color;

            public Writer(string color) => m_color = color;

            public string Pct(float ratio, bool signed = true)
            {
                int pct = Mathf.RoundToInt(ratio * 100f);
                string sign = !signed ? "" : pct < 0 ? "−" : "+";
                return Wrap($"{sign}{Mathf.Abs(pct)} %");
            }

            public string Mul(float factor) => Wrap("×" + factor.ToString("0.##", Fr));

            private string Wrap(string value) => m_color == null ? value : $"<color={m_color}>{value}</color>";
        }
    }
}
