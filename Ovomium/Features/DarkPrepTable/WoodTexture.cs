using System.Collections.Generic;
using UnityEngine;

namespace Ovomium.Features.DarkPrepTable
{
    /// <summary>
    /// Copie d'un atlas de la table de préparation où seule la zone du bois est assombrie : le décor posé sur la table
    /// (ustensiles, aliments, pots) partage le même mesh, le même matériau et le même atlas que le bois.
    /// </summary>
    internal sealed class WoodTexture
    {
        // Atlas 128 × 128 de preptable_d et preptable_broken_d (même disposition), en fractions de la taille, origine
        // en bas à gauche (comme les UV). Le bois est la grande zone de gauche, en deux rectangles (plus large en bas) ;
        // le décor occupe des rectangles à droite et en haut à gauche. Un liseré sombre semi-transparent sépare le bois
        // du décor en haut et à droite du rectangle supérieur, pas à droite du rectangle inférieur (les pots touchent le
        // bois à x = 73 px). Pas d'extension dans le liseré : sa teinte (~0,33) est déjà proche du bois assombri, le
        // filtrage ne fait pas de ligne claire ; les bords gauche et bas sont ceux de l'atlas.
        private static readonly Rect UpperWood = new Rect(0f, 51f / 128f, 63f / 128f, 68f / 128f); // x 0-62, y 51-118 px
        private static readonly Rect LowerWood = new Rect(0f, 0f, 73f / 128f, 51f / 128f);         // x 0-72, y 0-50 px

        /// <summary>Couleur moyenne (sRGB) de la zone du bois par atlas : garde-fou contre un atlas changé par une mise
        /// à jour du jeu (mesurée sur la version 1.0.15).</summary>
        private static readonly Dictionary<string, Color> ExpectedWood = new Dictionary<string, Color>
        {
            { "preptable_d", new Color(0.650f, 0.515f, 0.401f) },
            { "preptable_broken_d", new Color(0.382f, 0.350f, 0.250f) },
        };
        private const float Tolerance = 0.08f;
        private static readonly HashSet<string> s_warned = new HashSet<string>();

        public Texture2D Copy { get; }
        private readonly Color32[] m_base;
        private readonly Color32[] m_pixels;
        private readonly RectInt[] m_zone;

        private WoodTexture(Texture2D source, Color32[] pixels, RectInt[] zone)
        {
            m_base = pixels;
            m_pixels = (Color32[])pixels.Clone();
            m_zone = zone;
            Copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount > 1, false)
            {
                name = source.name + " (Ovomium sombre)",
                filterMode = source.filterMode,
                wrapModeU = source.wrapModeU,
                wrapModeV = source.wrapModeV,
                anisoLevel = source.anisoLevel,
                mipMapBias = source.mipMapBias,
            };
        }

        /// <summary>Null (avec un avertissement, une fois par atlas) si l'atlas n'a pas la disposition attendue.</summary>
        public static WoodTexture Create(Texture2D source)
        {
            Color32[] pixels = ReadPixels(source);
            RectInt[] zone = { ToPixels(UpperWood, source), ToPixels(LowerWood, source) };
            string problem = Check(source, pixels, zone);
            if (problem == null)
                return new WoodTexture(source, pixels, zone);
            if (s_warned.Add(source.name))
                Plugin.Log.LogWarning($"DarkPrepTable : atlas {source.name} non reconnu ({problem}), table laissée claire " +
                                      "(mise à jour du jeu ?)");
            return null;
        }

        /// <summary>Recalcule la copie : zone du bois multipliée par <paramref name="brightness"/>, reste inchangé.</summary>
        public void Apply(float brightness)
        {
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            int width = Copy.width;
            foreach (RectInt r in m_zone)
                for (int y = r.yMin; y < r.yMax; y++)
                    for (int x = r.xMin; x < r.xMax; x++)
                        m_pixels[y * width + x] = Darken(m_base[y * width + x], brightness, linear);
            Copy.SetPixels32(m_pixels);
            Copy.Apply(true);
        }

        public void Destroy() => Object.Destroy(Copy);

        /// <summary>Multiplication en espace linéaire si le rendu l'est, comme une teinte <c>_Color</c>.</summary>
        private static Color32 Darken(Color32 pixel, float k, bool linear)
        {
            Color c = pixel;
            Color dark = linear ? (c.linear * k).gamma : c * k;
            dark.a = c.a;
            return dark;
        }

        private static string Check(Texture2D source, Color32[] pixels, RectInt[] zone)
        {
            if (!ExpectedWood.TryGetValue(source.name, out Color expected))
                return "nom inattendu";
            if (source.width != source.height)
                return $"format {source.width} × {source.height} au lieu d'un carré";
            Color mean = MeanColor(pixels, source.width, zone);
            if (Mathf.Abs(mean.r - expected.r) > Tolerance || Mathf.Abs(mean.g - expected.g) > Tolerance ||
                Mathf.Abs(mean.b - expected.b) > Tolerance)
                return $"couleur moyenne du bois {mean.r:F2} {mean.g:F2} {mean.b:F2} au lieu de " +
                       $"{expected.r:F2} {expected.g:F2} {expected.b:F2}";
            return null;
        }

        private static Color MeanColor(Color32[] pixels, int width, RectInt[] zone)
        {
            Color sum = Color.clear;
            int count = 0;
            foreach (RectInt r in zone)
                for (int y = r.yMin; y < r.yMax; y++)
                    for (int x = r.xMin; x < r.xMax; x++, count++)
                        sum += pixels[y * width + x];
            return sum / Mathf.Max(count, 1);
        }

        private static RectInt ToPixels(Rect fraction, Texture2D texture)
            => new RectInt(Mathf.RoundToInt(fraction.x * texture.width), Mathf.RoundToInt(fraction.y * texture.height),
                Mathf.RoundToInt(fraction.width * texture.width), Mathf.RoundToInt(fraction.height * texture.height));

        /// <summary>Atlas non lisible par le CPU : copie par le GPU (octets sRGB d'origine), taille réelle de la texture.</summary>
        private static Color32[] ReadPixels(Texture2D source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Color32[] pixels = readable.GetPixels32();
            Object.Destroy(readable);
            return pixels;
        }
    }
}
