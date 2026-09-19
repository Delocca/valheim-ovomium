using System.Collections.Generic;
using UnityEngine;

namespace Ovomium.Features.TooltipStyle
{
    /// <summary>
    /// Sprites blancs « 9-slice » à coins arrondis, générés une fois par (rayon, largeur d'anneau) : texture de
    /// 2·r + 2 px de côté (les 2 px centraux sont étirés), disque antialiasé sur 1 px à chaque coin, bordure = r de
    /// chaque côté. Largeur 0 = forme pleine, sinon anneau de cette largeur, intérieur transparent. À teinter par
    /// <c>Image.color</c>, <c>Image.type = Sliced</c>.
    /// </summary>
    internal static class RoundedSprite
    {
        private static readonly Dictionary<(int radius, int ring), Sprite> s_sprites = new Dictionary<(int, int), Sprite>();

        public static Sprite Get(int radius, int ringWidth = 0)
        {
            var key = (radius, ringWidth);
            if (s_sprites.TryGetValue(key, out Sprite sprite) && sprite != null)
                return sprite;
            Texture2D texture = Build(radius, ringWidth);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = texture.name;
            s_sprites[key] = sprite;
            return sprite;
        }

        public static void Release()
        {
            foreach (Sprite sprite in s_sprites.Values)
            {
                if (sprite == null)
                    continue;
                Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            }
            s_sprites.Clear();
        }

        private static Texture2D Build(int radius, int ringWidth)
        {
            int side = 2 * radius + 2;
            var tex = new Texture2D(side, side, TextureFormat.RGBA32, false)
            {
                name = ringWidth > 0 ? "OvomiumRoundedRing" : "OvomiumRoundedFill",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float alpha = Coverage(x, y, radius, side);
                    if (ringWidth > 0)
                        alpha -= Coverage(x - ringWidth, y - ringWidth, radius - ringWidth, side - 2 * ringWidth);
                    pixels[y * side + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Couverture (0–1) du pixel (x, y) par un rectangle [0, side]² à coins arrondis de rayon r : 0 hors du
        /// rectangle, 1 hors des coins, disque antialiasé dans les coins.
        /// </summary>
        private static float Coverage(int x, int y, int radius, int side)
        {
            if (x < 0 || y < 0 || x >= side || y >= side)
                return 0f;
            float cx = x < radius ? radius : x >= side - radius ? side - radius : -1f;
            float cy = y < radius ? radius : y >= side - radius ? side - radius : -1f;
            if (cx < 0f || cy < 0f)
                return 1f;
            float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01(radius + 0.5f - dist);
        }
    }
}
