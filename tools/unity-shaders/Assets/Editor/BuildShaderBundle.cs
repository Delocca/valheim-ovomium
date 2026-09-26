using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Construit le bundle du shader de SmoothShading, lancé par tools/build-shaders.sh
/// (<c>-executeMethod BuildShaderBundle.Build</c>). Un seul bundle StandaloneWindows64 compilé pour Direct3D 11, Vulkan
/// et OpenGL Core, chargé par le mod sous Windows (D3D11) comme sous Linux (Vulkan). Toute erreur (shader, build)
/// termine l'éditeur avec un code non nul.
/// </summary>
public static class BuildShaderBundle
{
    private const string ShaderPath = "Assets/Shaders/OvoSmoothDeferredShading.shader";
    private const string BundleName = "smoothshading.bundle";
    /// <summary>Nom de chargement dans le bundle, lu par le mod (DeferredShaderSwap.AssetName).</summary>
    private const string AssetName = "SmoothDeferredShading";
    private const BuildTarget Target = BuildTarget.StandaloneWindows64;
    private static readonly GraphicsDeviceType[] Apis =
        { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLCore };

    public static void Build()
    {
        try
        {
            Run();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("BuildShaderBundle : ÉCHEC : " + e.Message);
            EditorApplication.Exit(1);
        }
    }

    private static void Run()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(Target, false);
        PlayerSettings.SetGraphicsAPIs(Target, Apis);

        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
            throw new Exception("shader introuvable : " + ShaderPath);
        CheckShader(shader);

        var outDir = Path.Combine(Path.GetTempPath(), "ovomium-shader-bundle");
        Directory.CreateDirectory(outDir);
        var build = new AssetBundleBuild
        {
            assetBundleName = BundleName,
            assetNames = new[] { ShaderPath },
            addressableNames = new[] { AssetName },
        };
        int errors = 0;
        Application.LogCallback count = (_, __, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors++;
        };
        Application.logMessageReceived += count;
        AssetBundleManifest manifest;
        try
        {
            // StrictMode : échoue si une erreur est journalisée pendant le build (erreur de compilation d'une variante,
            // pour une des trois API, par exemple).
            manifest = BuildPipeline.BuildAssetBundles(outDir, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, Target);
        }
        finally
        {
            Application.logMessageReceived -= count;
        }
        if (manifest == null || errors > 0)
            throw new Exception($"build du bundle raté ({errors} erreur(s) journalisée(s) ci-dessus)");

        var destination = Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../../Ovomium/Features/SmoothShading", BundleName));
        if (!Directory.Exists(Path.GetDirectoryName(destination)))
            throw new Exception("dossier de la feature introuvable : " + Path.GetDirectoryName(destination));
        File.Copy(Path.Combine(outDir, BundleName), destination, true);
        Debug.Log($"BuildShaderBundle : OK : {destination} ({new FileInfo(destination).Length} octets, "
            + $"API : {string.Join(", ", Apis)})");
    }

    /// <summary>Erreurs de compilation connues de l'éditeur (celles propres à une API sortent au build, StrictMode).</summary>
    private static void CheckShader(Shader shader)
    {
        foreach (var message in ShaderUtil.GetShaderMessages(shader))
            Debug.Log($"BuildShaderBundle : shader {message.severity} ({message.platform}) ligne {message.line} : {message.message}");
        if (ShaderUtil.ShaderHasError(shader))
            throw new Exception("le shader a des erreurs de compilation : " + ShaderPath);
        Debug.Log($"BuildShaderBundle : shader « {shader.name} » importé sans erreur");
    }
}
