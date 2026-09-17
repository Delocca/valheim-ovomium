using System.Collections.Generic;

namespace OvoMiam.Food
{
    internal enum FoodStat { Health, Stamina, Eitr }

    /// <summary>
    /// Profil nutritif d'un item : score total et stats dominantes. Un plat cru est évalué à la valeur de sa version cuite.
    /// Même critère que la pastille d'inventaire du jeu (InventoryGrid) : une stat domine seule
    /// si les autres sont sous sa moitié ; sinon le plat est mixte.
    /// </summary>
    internal readonly struct FoodProfile
    {
        private const float DominanceRatio = 0.5f;

        public readonly IReadOnlyList<FoodStat> Dominant;
        /// <summary>Santé + endurance + eitr. Zéro pour tout ce qui n'est pas de la nourriture.</summary>
        public readonly float Score;

        public bool IsFood => Score > 0f;
        public bool IsMixed => Dominant.Count > 1;

        /// <summary>Rang de groupe pour le tri : vie, endurance, eitr, plats mixtes, puis le reste.</summary>
        public int GroupRank => !IsFood ? 4 : IsMixed ? 3 : (int)Dominant[0];

        private FoodProfile(IReadOnlyList<FoodStat> dominant, float score)
        {
            Dominant = dominant;
            Score = score;
        }

        public static FoodProfile Of(ItemDrop.ItemData.SharedData shared)
        {
            shared = CookedFood.Resolve(shared);
            if (shared == null || shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
                return new FoodProfile(new FoodStat[0], 0f);

            var values = new[] { shared.m_food, shared.m_foodStamina, shared.m_foodEitr };
            var score = values[0] + values[1] + values[2];
            if (score <= 0f)
                return new FoodProfile(new FoodStat[0], 0f);

            var max = System.Math.Max(values[0], System.Math.Max(values[1], values[2]));
            var dominant = new List<FoodStat>(3);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] >= max * DominanceRatio)
                    dominant.Add((FoodStat)i);
            }
            return new FoodProfile(dominant, score);
        }
    }
}
