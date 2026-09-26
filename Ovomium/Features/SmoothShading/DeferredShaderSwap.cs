using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGraphicsSettings = UnityEngine.Rendering.GraphicsSettings;

namespace Ovomium.Features.SmoothShading
{
    /// <summary>
    /// Remplace le shader d'éclairage différé du jeu (<c>ToonDeferredShading2017</c>, qui prend la place de
    /// <c>Hidden/Internal-DeferredShading</c> et dont la rampe en dur crée des bandes de couleur) par le nôtre, lu dans le
    /// bundle embarqué (<c>smoothshading.bundle</c>, produit par tools/build-shaders.sh). Réglage global de Unity
    /// (<c>GraphicsSettings</c>, qu'aucun code du jeu ne réécrit) : posé au chargement, défait au décochage et à l'Unload.
    /// </summary>
    internal static class DeferredShaderSwap
    {
        private const string ResourceName = "Ovomium.SmoothShading.bundle";
        /// <summary>Nom de chargement donné au build (tools/unity-shaders/Assets/Editor/BuildShaderBundle.cs).</summary>
        private const string AssetName = "SmoothDeferredShading";
        /// <summary>
        /// État vanilla (mode, shader) gardé dans l'AppDomain : une version rechargée à chaud ne doit pas prendre notre
        /// shader, laissé en place par une version précédente mal déchargée, pour celui du jeu.
        /// </summary>
        private const string VanillaKey = "Ovomium.SmoothShading.Vanilla";

        private static AssetBundle s_bundle;
        private static Shader s_shader;

        internal static void Install()
        {
            Vanilla();
            SmoothShadingConfig.Enabled.SettingChanged += (_, __) => Refresh();
            Refresh();
        }

        /// <summary>Retour au shader du jeu, puis bundle déchargé : la version suivante recharge le même.</summary>
        internal static void Unload()
        {
            Restore();
            if (s_bundle != null)
                s_bundle.Unload(true);
            s_bundle = null;
            s_shader = null;
        }

        private static void Refresh()
        {
            if (SmoothShadingConfig.Enabled.Value)
                Apply();
            else
                Restore();
        }

        private static void Apply()
        {
            var shader = LoadShader();
            if (shader == null || UnityGraphicsSettings.GetCustomShader(BuiltinShaderType.DeferredShading) == shader)
                return;
            UnityGraphicsSettings.SetShaderMode(BuiltinShaderType.DeferredShading, BuiltinShaderMode.UseCustom);
            UnityGraphicsSettings.SetCustomShader(BuiltinShaderType.DeferredShading, shader);
            Plugin.Log.LogInfo($"SmoothShading : éclairage différé remplacé par {shader.name}");
        }

        /// <summary>Rend l'état vanilla si notre shader (de cette version ou d'une précédente) est en place.</summary>
        private static void Restore()
        {
            var current = UnityGraphicsSettings.GetCustomShader(BuiltinShaderType.DeferredShading);
            if (current == null || (current != s_shader && current.name != ShaderName()))
                return;
            var vanilla = Vanilla();
            UnityGraphicsSettings.SetCustomShader(BuiltinShaderType.DeferredShading, (Shader)vanilla[1]);
            UnityGraphicsSettings.SetShaderMode(BuiltinShaderType.DeferredShading, (BuiltinShaderMode)vanilla[0]);
            Plugin.Log.LogInfo("SmoothShading : éclairage différé du jeu rétabli");
        }

        private static string ShaderName() => s_shader != null ? s_shader.name : "Hidden/Ovomium/SmoothDeferredShading";

        /// <summary>{ mode, shader } du jeu, lus une fois par lancement (avant tout remplacement) et journalisés.</summary>
        private static object[] Vanilla()
        {
            var domain = System.AppDomain.CurrentDomain;
            if (domain.GetData(VanillaKey) is object[] saved)
                return saved;
            var mode = UnityGraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredShading);
            var shader = UnityGraphicsSettings.GetCustomShader(BuiltinShaderType.DeferredShading);
            Plugin.Log.LogInfo($"SmoothShading : éclairage différé du jeu : {(shader != null ? shader.name : "aucun")} (mode {mode})");
            saved = new object[] { mode, shader };
            domain.SetData(VanillaKey, saved);
            return saved;
        }

        /// <summary>Shader chargé et utilisable par la carte graphique, sinon null (journalisé : on reste vanilla).</summary>
        private static Shader LoadShader()
        {
            if (s_shader != null)
                return s_shader;
            if (s_bundle == null)
                s_bundle = FindLoadedBundle() ?? LoadEmbeddedBundle();
            if (s_bundle == null)
                return null;
            var shader = s_bundle.LoadAsset<Shader>(AssetName);
            if (shader == null)
                Plugin.Log.LogWarning($"SmoothShading : « {AssetName} » absent du bundle : éclairage du jeu conservé");
            else if (!shader.isSupported)
                Plugin.Log.LogWarning($"SmoothShading : {shader.name} non pris en charge ({SystemInfo.graphicsDeviceType}) : "
                    + "éclairage du jeu conservé");
            else
                s_shader = shader;
            return s_shader;
        }

        /// <summary>
        /// Bundle laissé chargé par une version précédente mal déchargée : Unity refuserait d'en charger une seconde
        /// copie (mêmes fichiers internes).
        /// </summary>
        private static AssetBundle FindLoadedBundle()
        {
            foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
                if (bundle != null && bundle.Contains(AssetName))
                    return bundle;
            return null;
        }

        private static AssetBundle LoadEmbeddedBundle()
        {
            using (var stream = typeof(DeferredShaderSwap).Assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                {
                    Plugin.Log.LogWarning("SmoothShading : bundle du shader absent de la DLL (tools/build-shaders.sh pas encore "
                        + "lancé) : éclairage du jeu conservé");
                    return null;
                }
                using (var bytes = new MemoryStream())
                {
                    stream.CopyTo(bytes);
                    var bundle = AssetBundle.LoadFromMemory(bytes.ToArray());
                    if (bundle == null)
                        Plugin.Log.LogWarning("SmoothShading : chargement du bundle refusé par Unity (erreur ci-dessus) : "
                            + "éclairage du jeu conservé");
                    return bundle;
                }
            }
        }
    }
}
