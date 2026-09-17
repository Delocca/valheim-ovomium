using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OvoMiam.Features.FirstPerson
{
    /// <summary>
    /// Masque le corps du joueur local en vue subjective : <c>forceRenderingOff</c> sur tous ses renderers sauf les
    /// objets tenus en main, animateurs en <c>AlwaysAnimate</c> (sans renderer visible Unity gèlerait l'animation,
    /// donc l'œil qui porte la caméra). Rejoué à chaque changement d'équipement (le jeu recrée les objets).
    /// Le masquage vanilla à moins de 2 m (<c>Character.SetVisible</c>, qui éloigne le point de référence du LODGroup)
    /// est neutralisé : il cacherait aussi les objets en main.
    /// </summary>
    internal static class FirstPersonVisibility
    {
        private static readonly HashSet<Renderer> s_hidden = new HashSet<Renderer>();
        private static readonly Dictionary<Animator, AnimatorCullingMode> s_culling = new Dictionary<Animator, AnimatorCullingMode>();
        private static Player s_player;

        internal static void Hide(Player player)
        {
            Restore();
            s_player = player;
            HideAll();
        }

        /// <summary>Après un changement d'équipement du joueur masqué.</summary>
        internal static void Refresh(Player player)
        {
            if (player != null && player == s_player)
                HideAll();
        }

        internal static void Restore()
        {
            foreach (Renderer renderer in s_hidden)
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            foreach (KeyValuePair<Animator, AnimatorCullingMode> entry in s_culling)
                if (entry.Key != null)
                    entry.Key.cullingMode = entry.Value;
            s_hidden.Clear();
            s_culling.Clear();
            s_player = null;
        }

        private static void HideAll()
        {
            foreach (Renderer renderer in s_player.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.forceRenderingOff)
                    continue; // masqué par un autre : ne pas le rallumer à tort en restaurant
                renderer.forceRenderingOff = true;
                s_hidden.Add(renderer);
            }
            foreach (Animator animator in s_player.GetComponentsInChildren<Animator>(true))
            {
                if (s_culling.ContainsKey(animator))
                    continue;
                s_culling.Add(animator, animator.cullingMode);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            VisEquipment equipment = s_player.m_visEquipment;
            if (equipment == null)
                return;
            ShowItem(equipment.m_leftItemInstance);
            ShowItem(equipment.m_rightItemInstance);
        }

        private static void ShowItem(GameObject item)
        {
            if (item == null)
                return;
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
                if (s_hidden.Remove(renderer))
                    renderer.forceRenderingOff = false;
        }
    }

    /// <summary>Le jeu masque le joueur local à moins de 2 m de la caméra : garder visible, on masque nous-mêmes.</summary>
    [HarmonyPatch(typeof(Character), "SetVisible", typeof(bool))]
    internal static class FirstPersonSetVisiblePatch
    {
        private static void Prefix(Character __instance, ref bool visible)
        {
            if (FirstPersonMode.Active && __instance == Player.m_localPlayer)
                visible = true;
        }
    }

    /// <summary>UpdateLodgroup n'est appelé que lorsqu'un équipement a changé : re-masquer les nouveaux objets.</summary>
    [HarmonyPatch(typeof(VisEquipment), "UpdateLodgroup", new System.Type[0])]
    internal static class FirstPersonEquipmentChangedPatch
    {
        private static void Postfix(VisEquipment __instance)
        {
            Player player = Player.m_localPlayer;
            if (FirstPersonMode.Active && player != null && __instance.gameObject == player.gameObject)
                FirstPersonVisibility.Refresh(player);
        }
    }
}
