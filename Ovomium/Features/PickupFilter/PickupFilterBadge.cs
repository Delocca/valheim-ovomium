using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.PickupFilter
{
    /// <summary>
    /// Badge « interdit » sur les cases d'inventaire et de coffre dont l'objet est exclu du ramassage automatique :
    /// enfant Image de l'élément, créé à son premier affichage, coin haut-gauche (choix d'Edia en jeu ; en bas à
    /// gauche il couvrait la quantité). Sprite généré une fois, partagé.
    /// </summary>
    internal static class PickupFilterBadge
    {
        private const string Name = "OvomiumPickupFilterBadge";
        /// <summary>Côté du badge, en fraction de la largeur de la case.</summary>
        private const float Scale = 0.24f;
        private const int TextureSize = 32;
        private const float Radius = 15f;
        private const float Stroke = 3.5f;
        /// <summary>Opacité du disque sombre derrière l'anneau, pour rester lisible sur toute icône.</summary>
        private const float Backdrop = 0.45f;
        private static readonly Color Tint = new Color(0.85f, 0.28f, 0.22f, 0.85f);

        private static Sprite s_sprite;

        /// <summary>Affiche ou masque le badge de la case ; ne touche à rien si l'état ne change pas.</summary>
        public static void Show(InventoryElement element, bool show)
        {
            Transform badge = element.transform.Find(Name);
            if (badge == null)
            {
                if (!show)
                    return;
                badge = Create(element);
            }
            if (badge.gameObject.activeSelf != show)
                badge.gameObject.SetActive(show);
        }

        /// <summary>Rechargement à chaud : retire les badges des deux grilles et libère le sprite.</summary>
        public static void Unload()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui != null)
            {
                Remove(gui.m_playerGrid);
                Remove(gui.m_containerGrid);
            }
            if (s_sprite != null)
            {
                Object.Destroy(s_sprite.texture);
                Object.Destroy(s_sprite);
            }
            s_sprite = null;
        }

        private static void Remove(InventoryGrid grid)
        {
            if (grid == null)
                return;
            foreach (InventoryElement element in grid.m_elements)
            {
                Transform badge = element != null ? element.transform.Find(Name) : null;
                if (badge != null)
                    Object.Destroy(badge.gameObject);
            }
        }

        private static Transform Create(InventoryElement element)
        {
            var go = new GameObject(Name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(element.transform, false);
            float side = ((RectTransform)element.transform).rect.width * Scale;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(side, side);
            rect.anchoredPosition = new Vector2(side * 0.15f, -side * 0.15f);
            var image = go.GetComponent<Image>();
            image.sprite = GetSprite();
            image.raycastTarget = false;
            return rect;
        }

        private static Sprite GetSprite()
        {
            if (s_sprite != null)
                return s_sprite;
            Texture2D texture = BuildTexture();
            s_sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
            s_sprite.name = Name;
            return s_sprite;
        }

        /// <summary>
        /// Panneau « interdit » : anneau et barre (haut-gauche → bas-droite) rouge atténué sur un disque sombre
        /// translucide, bords antialiasés sur 1 px.
        /// </summary>
        private static Texture2D BuildTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = Name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[TextureSize * TextureSize];
            const float center = TextureSize / 2f;
            for (int y = 0; y < TextureSize; y++)
                for (int x = 0; x < TextureSize; x++)
                {
                    float dx = x + 0.5f - center, dy = y + 0.5f - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float disc = Mathf.Clamp01(Radius + 0.5f - dist);
                    float ring = Mathf.Clamp01(dist - (Radius - Stroke) + 0.5f);
                    // y vers le haut : la barre suit la droite y = -x.
                    float bar = Mathf.Clamp01(Stroke / 2f + 0.5f - Mathf.Abs(dx + dy) / Mathf.Sqrt(2f));
                    float red = disc * Mathf.Max(ring, bar) * Tint.a;
                    float alpha = red + disc * Backdrop * (1f - red);
                    float share = alpha > 0f ? red / alpha : 0f;
                    pixels[y * TextureSize + x] = new Color(Tint.r * share, Tint.g * share, Tint.b * share, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
