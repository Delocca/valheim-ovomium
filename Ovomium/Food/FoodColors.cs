namespace Ovomium.Food
{
    /// <summary>Couleurs hex des stats, celles du tooltip d'item du jeu (ItemDrop.ItemData.GetTooltip).</summary>
    internal static class FoodColors
    {
        public static string Hex(FoodStat stat)
        {
            switch (stat)
            {
                case FoodStat.Health: return "#ff8080";
                case FoodStat.Stamina: return "#ffff80";
                default: return "#9090ff";
            }
        }
    }
}
