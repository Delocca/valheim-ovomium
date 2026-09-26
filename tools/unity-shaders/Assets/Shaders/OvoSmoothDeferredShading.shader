// Ovomium : éclairage différé de Valheim sans effet de bandes.
//
// Réplique de ToonDeferredShading2017 (shader d'éclairage différé de Valheim, remplace
// Hidden/Internal-DeferredShading dans les GraphicsSettings), relevée sur son GLSL compilé
// (26 variantes d'éclairage + passe de décodage LDR) :
//   - mêmes passes, états de rendu, mots-clés et variantes que le shader standard de Unity ;
//   - position, atténuation, cookies, ombres et fondu : UnityDeferredCalculateLightParams (identique) ;
//   - spéculaire : GGX simplifié de BRDF2_Unity_PBS (espace linéaire), sans terme indirect ;
//   - couleur = (albedo + spéculaire × couleur spéculaire) × couleur de la lumière × rampe(NdotL) × 1,1.
// Seule différence voulue : la rampe en paliers sur NdotL (bandes de couleur) est remplacée par
// SmoothRamp, continue et croissante, de même luminosité moyenne par zone.

Shader "Hidden/Ovomium/SmoothDeferredShading" {
Properties {
    _LightTexture0 ("", any) = "" {}
    _LightTextureB0 ("", 2D) = "" {}
    _ShadowMapTexture ("", any) = "" {}
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
}
SubShader {

// Passe 1 : éclairage (LDR : tampon ARGB8 soustractif exp2(-c) ; HDR : addition en flottant)
Pass {
    ZWrite Off
    Blend [_SrcBlend] [_DstBlend]

CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass
#pragma multi_compile ___ UNITY_HDR_ON

#pragma exclude_renderers nomrt

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

// Remplace la rampe vanilla : smoothstep de largeur 0,05 aux seuils NdotL 0 / 0,2 / 0,4 / 0,6 / 0,8 / 0,9,
// paliers 0 → 0,1 → 0,3 → 0,5 → 0,7 → 0,9 → 1,1 (atteint dès NdotL = 0,95).
// Ici : ligne brisée passant par le centre de chaque palier, (0,125 ; 0,1) (0,325 ; 0,3) … (0,875 ; 0,9)
// (0,975 ; 1,1), partant de (0 ; 0) et plafonnée à 1,1 : la moyenne sur chaque palier reste celle
// du vanilla (écart ≤ 0,013, sauf le fondu 0 → 0,05 du terminateur, adouci). Pentes croissantes
// (0,8 ; 1 ; 4/3 ; 2) jusqu'au plafond : la ligne brisée est le max des quatre droites.
float SmoothRamp(float ndotl)
{
    float x = saturate(ndotl);
    float y = max(max(0.8 * x, x - 0.025), max((4.0 * x - 0.8) / 3.0, 2.0 * x - 0.85));
    return min(y, 1.1);
}

half4 CalculateLight (unity_v2f_deferred i)
{
    float3 wpos;
    float2 uv;
    float atten, fadeDist;
    half3 lightDir;
    UnityDeferredCalculateLightParams (i, wpos, uv, lightDir, atten, fadeDist);

    half3 lightColor = _LightColor.rgb * atten;

    // G-buffer
    half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
    half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
    half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);
    UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);

    float3 viewDir = -normalize(wpos - _WorldSpaceCameraPos);
    float3 normal = data.normalWorld;

    // Spéculaire : BRDF2_Unity_PBS, branche GGX en espace linéaire (comme le vanilla)
    float3 halfDir = Unity_SafeNormalize (float3(lightDir) + viewDir);
    float nl = saturate(dot(normal, lightDir));
    float nh = saturate(dot(normal, halfDir));
    float lh = saturate(dot(lightDir, halfDir));

    float perceptualRoughness = SmoothnessToPerceptualRoughness (data.smoothness);
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    float a2 = roughness * roughness;
    float d = nh * nh * (a2 - 1.f) + 1.00001f;
    float specularTerm = a2 / (max(0.1f, lh * lh) * (roughness + 0.5f) * (d * d) * 4);

    // Rampe (seul changement) puis gain 1,1 du vanilla
    float ramp = SmoothRamp(nl) * 1.1;

    half3 color = (data.diffuseColor + specularTerm * data.specularColor) * lightColor * ramp;
    return half4(color, 1);
}

#ifdef UNITY_HDR_ON
half4
#else
fixed4
#endif
frag (unity_v2f_deferred i) : SV_Target
{
    half4 c = CalculateLight(i);
    #ifdef UNITY_HDR_ON
    return c;
    #else
    return exp2(-c);
    #endif
}

ENDCG
}


// Passe 2 : décodage final, seulement sans HDR (tampon logarithmique → cible principale)
Pass {
    ZTest Always Cull Off ZWrite Off
    Stencil {
        ref [_StencilNonBackground]
        readmask [_StencilNonBackground]
        // Seul l'état de la face avant serait appliqué avec comp seul (case 583207)
        compback equal
        compfront equal
    }

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers nomrt

#include "UnityCG.cginc"

sampler2D _LightBuffer;
struct v2f {
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(vertex);
    o.texcoord = texcoord.xy;
#ifdef UNITY_SINGLE_PASS_STEREO
    o.texcoord = TransformStereoScreenSpaceTex(o.texcoord, 1.0f);
#endif
    return o;
}

fixed4 frag (v2f i) : SV_Target
{
    return -log2(tex2D(_LightBuffer, i.texcoord));
}
ENDCG
}

}
Fallback Off
}
