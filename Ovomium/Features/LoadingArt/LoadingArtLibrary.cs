using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Ovomium.Features.LoadingArt
{
    /// <summary>
    /// Liste des images du dossier (lue une fois au démarrage) et chargement à la demande d'une image tirée au sort,
    /// différente de la précédente. Une seule texture vivante à la fois : les artworks font ~6000 × 3300 px, soit
    /// ~80 Mo en VRAM chacun.
    /// </summary>
    internal static class LoadingArtLibrary
    {
        private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png" };
        /// <summary>
        /// <c>ImageConversion.LoadImage(Texture2D, byte[], bool)</c> résolu par réflexion : la surcharge
        /// <c>ReadOnlySpan&lt;byte&gt;</c> d'Unity 6 fait échouer la compilation net48 (type prédéfini absent).
        /// </summary>
        private static readonly System.Func<Texture2D, byte[], bool, bool> LoadImage =
            (System.Func<Texture2D, byte[], bool, bool>)System.Delegate.CreateDelegate(
                typeof(System.Func<Texture2D, byte[], bool, bool>),
                typeof(ImageConversion).GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) }));

        private static string[] s_files;
        private static int s_last = -1;
        private static Texture2D s_texture;
        private static Sprite s_sprite;

        internal static bool Available
        {
            get
            {
                if (s_files == null)
                    Scan();
                return LoadingArtConfig.Enabled.Value && s_files.Length > 0;
            }
        }

        private static void Scan()
        {
            s_files = new string[0];
            string folder = LoadingArtConfig.Folder.Value;
            if (!Path.IsPathRooted(folder))
                folder = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), folder);
            if (!Directory.Exists(folder))
            {
                Plugin.Log.LogInfo($"LoadingArt : dossier absent, fonctionnalité inactive : {folder}");
                return;
            }
            var files = new List<string>();
            foreach (string file in Directory.GetFiles(folder))
                if (System.Array.IndexOf(Extensions, Path.GetExtension(file).ToLowerInvariant()) >= 0)
                    files.Add(file);
            files.Sort(System.StringComparer.Ordinal);
            s_files = files.ToArray();
            Plugin.Log.LogInfo($"LoadingArt : {s_files.Length} image(s) dans {folder}");
        }

        /// <summary>Charge une nouvelle image au hasard ; l'ancienne est détruite.</summary>
        internal static Sprite Next()
        {
            int index = Random.Range(0, s_files.Length);
            if (s_files.Length > 1 && index == s_last)
                index = (index + 1) % s_files.Length;
            s_last = index;
            Release();
            var watch = Stopwatch.StartNew();
            s_texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!LoadImage(s_texture, File.ReadAllBytes(s_files[index]), true))
            {
                Plugin.Log.LogWarning($"LoadingArt : image illisible : {s_files[index]}");
                Release();
                return null;
            }
            s_sprite = Sprite.Create(s_texture, new Rect(0, 0, s_texture.width, s_texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Plugin.Log.LogInfo($"LoadingArt : {Path.GetFileName(s_files[index])} ({s_texture.width}×{s_texture.height}) en {watch.ElapsedMilliseconds} ms");
            return s_sprite;
        }

        private static void Release()
        {
            if (s_sprite != null)
                Object.Destroy(s_sprite);
            if (s_texture != null)
                Object.Destroy(s_texture);
            s_sprite = null;
            s_texture = null;
        }
    }
}
