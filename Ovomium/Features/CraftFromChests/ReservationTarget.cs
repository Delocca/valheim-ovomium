using System.Collections.Generic;
using UnityEngine;

namespace Ovomium.Features.CraftFromChests
{
    /// <summary>
    /// Prochaine action de la joueuse qui puiserait dans les coffres, pour la réservation anticipée
    /// (<see cref="ChestReservation"/>) : recette sélectionnée du panneau de craft (fabrication ou amélioration),
    /// pièce choisie au marteau, ou combustible du feu, four ou marmite visé. Comparer deux cibles ne coûte rien
    /// (identités d'objets) ; le plan de retrait, lui, parcourt les coffres.
    /// </summary>
    internal readonly struct ReservationTarget
    {
        /// <summary><c>Recipe</c>, <c>Piece</c> ou objet à recharger (<c>Fireplace</c>, <c>Smelter</c>, <c>CookingStation</c>) ; null : rien à anticiper.</summary>
        private readonly Object m_subject;
        /// <summary>Objet à améliorer (<c>ItemDrop.ItemData</c>, null en fabrication) ou combustible (<c>ItemDrop</c>).</summary>
        private readonly object m_detail;
        /// <summary>Ordre coffres / inventaire inversé : action lancée Ctrl maintenu, ou barre de craft lancée ainsi.</summary>
        private readonly bool m_inverted;

        private ReservationTarget(Object subject, object detail, bool inverted)
        {
            m_subject = subject;
            m_detail = detail;
            m_inverted = inverted;
        }

        public bool SameAs(ReservationTarget other) =>
            ReferenceEquals(m_subject, other.m_subject) && ReferenceEquals(m_detail, other.m_detail)
            && m_inverted == other.m_inverted;

        /// <summary>Inventaire ouvert : recette sélectionnée ; sinon marteau en main : pièce choisie ; sinon objet à recharger visé.</summary>
        public static ReservationTarget Current(Player player)
        {
            if (!CraftFromChestsPatch.Active(player)) return default;
            bool inverted = PullOrder.Inverted || PullOrder.CtrlHeld;
            if (InventoryGui.IsVisible())
            {
                var selected = InventoryGui.instance.m_selectedRecipe;
                return selected.Recipe == null ? default : new ReservationTarget(selected.Recipe, selected.ItemData, inverted);
            }
            if (player.InPlaceMode())
            {
                Piece piece = player.GetSelectedPiece();
                return piece == null ? default : new ReservationTarget(piece, null, inverted);
            }
            MonoBehaviour consumer = FuelFromChests.Hovered(player, out ItemDrop fuel);
            return consumer == null || fuel == null ? default : new ReservationTarget(consumer, fuel, inverted);
        }

        /// <summary>Coffres (sans doublon) que l'action puiserait maintenant ; vide si elle ne puiserait rien.</summary>
        public void Chests(Player player, List<Container> chests)
        {
            chests.Clear();
            if (m_subject == null) return;
            bool saved = PullOrder.Inverted;
            PullOrder.Inverted = m_inverted;
            try
            {
                var pulls = Pulls(player);
                if (pulls == null) return;
                foreach (var pull in pulls)
                    if (!chests.Contains(pull.Chest)) chests.Add(pull.Chest);
            }
            finally { PullOrder.Inverted = saved; }
        }

        /// <summary>Même prévision que la consommation (craft simple : la quantité multiple n'est connue qu'au clic).</summary>
        private List<Pull> Pulls(Player player)
        {
            switch (m_subject)
            {
                case Recipe recipe:
                    return PullPlan.PredictRecipe(player, recipe, m_detail as ItemDrop.ItemData, 1);
                case Piece piece:
                    return PullPlan.PredictPiece(player, piece);
                case MonoBehaviour consumer when !FuelFromChests.IsFull(consumer):
                    var pulls = new List<Pull>();
                    return FuelFromChests.PlanOne(player, (ItemDrop)m_detail, pulls) ? pulls : null;
                default:
                    return null;
            }
        }
    }
}
