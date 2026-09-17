using HarmonyLib;
using UnityEngine;

namespace OvoMiam.Features.FirstPerson
{
    /// <summary>
    /// État de la vue subjective. Le jeu contient déjà la caméra première personne : dans
    /// <c>GameCamera.GetCameraOffset</c>, <c>m_distance &lt;= 0</c> place la caméra sur <c>m_fpsOffset</c> depuis l'œil,
    /// mais <c>m_minDistance</c> du prefab (&gt; 0) rend cette branche inatteignable. On la libère (min 0) et on
    /// bascule par hystérésis sur la molette : sous la distance minimale vanilla → 0 ; au-dessus de 0 → distance
    /// minimale vanilla. <see cref="Wanted"/> survit à la mort et aux cinématiques, <see cref="Active"/> non.
    /// Inspiré de Landoria.FirstPerson (MIT).
    /// </summary>
    internal static class FirstPersonMode
    {
        private const float Epsilon = 0.01f;

        /// <summary>m_minDistance vanilla du prefab : distance de retour en troisième personne.</summary>
        internal static float ReturnDistance { get; private set; } = 1f;
        /// <summary>Vue subjective choisie à la molette.</summary>
        internal static bool Wanted { get; private set; }
        /// <summary>Vue subjective réellement appliquée (joueur vivant, hors cinématique et vol libre).</summary>
        internal static bool Active { get; private set; }

        internal static void CaptureReturnDistance(GameCamera camera)
        {
            if (camera.m_minDistance > Epsilon)
                ReturnDistance = camera.m_minDistance;
            Plugin.Log.LogInfo($"FirstPerson : distance minimale vanilla {ReturnDistance}, m_fpsOffset {camera.m_fpsOffset}");
        }

        /// <summary>Hystérésis molette ; après le clamp de m_distance, avant le placement de la caméra.</summary>
        internal static void UpdateWanted(GameCamera camera)
        {
            if (!FirstPersonConfig.Enabled.Value)
            {
                Wanted = false;
                if (camera.m_distance < ReturnDistance)
                    camera.m_distance = ReturnDistance; // désactivé en cours de jeu : ne pas rester sur l'œil
                return;
            }
            if (!Wanted && camera.m_distance < ReturnDistance - Epsilon)
            {
                Wanted = true;
                camera.m_distance = 0f;
            }
            else if (Wanted && camera.m_distance > Epsilon)
            {
                Wanted = false;
                camera.m_distance = ReturnDistance;
            }
        }

        internal static void UpdateActive()
        {
            Player player = Player.m_localPlayer;
            bool active = Wanted && player != null && !player.IsDead() && !player.InCutscene() && !GameCamera.InFreeFly();
            if (active == Active)
                return;
            Active = active;
            if (active)
            {
                FirstPersonVisibility.Hide(player);
                FirstPersonVegetation.Apply();
            }
            else
            {
                FirstPersonVisibility.Restore();
                FirstPersonVegetation.Restore();
            }
        }

        internal static void Reset()
        {
            Wanted = false;
            UpdateActive();
        }
    }

    /// <summary>Mémorise la distance minimale du prefab, puis autorise la distance 0.</summary>
    [HarmonyPatch(typeof(GameCamera), "Awake", new System.Type[0])]
    internal static class FirstPersonAwakePatch
    {
        private static void Prefix(GameCamera __instance) => FirstPersonMode.CaptureReturnDistance(__instance);

        private static void Postfix(GameCamera __instance)
        {
            if (FirstPersonConfig.Enabled.Value)
                __instance.m_minDistance = 0f;
        }
    }

    /// <summary>Met à jour l'état actif chaque frame (UpdateCamera tourne aussi mort, attaché, en vol libre).</summary>
    [HarmonyPatch(typeof(GameCamera), "UpdateCamera", typeof(float))]
    internal static class FirstPersonUpdateCameraPatch
    {
        private static void Postfix() => FirstPersonMode.UpdateActive();
    }

    /// <summary>
    /// Bascule molette juste après le clamp de m_distance, et neutralise m_smoothYTilt (regarder en bas éloigne la
    /// caméra à 1,5 m) le temps du placement.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), "GetCameraPosition", new[] { typeof(float), typeof(Vector3), typeof(Quaternion) },
        new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
    internal static class FirstPersonCameraPositionPatch
    {
        private static bool s_tiltDisabled;

        private static void Prefix(GameCamera __instance)
        {
            FirstPersonMode.UpdateWanted(__instance);
            s_tiltDisabled = FirstPersonMode.Active && __instance.m_smoothYTilt;
            if (s_tiltDisabled)
                __instance.m_smoothYTilt = false;
        }

        private static void Postfix(GameCamera __instance)
        {
            if (s_tiltDisabled)
                __instance.m_smoothYTilt = true;
            s_tiltDisabled = false;
        }
    }

    /// <summary>
    /// Vanilla tourne m_fpsOffset (0, 0, 0.5 dans le prefab : 50 cm devant l'œil) par la rotation complète de l'œil :
    /// la caméra décrit un arc en baissant le regard (impression de se pencher) comme en tournant la tête. Offset
    /// configurable, plus court, tourné par le lacet seul : le point de vue reste quasi fixe, seule l'orientation change.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), "GetCameraOffset", typeof(Player))]
    internal static class FirstPersonCameraOffsetPatch
    {
        private static void Postfix(GameCamera __instance, Player player, ref Vector3 __result)
        {
            if (!FirstPersonMode.Active || __instance.m_distance > 0f)
                return;
            Vector3 forward = player.m_eye.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;
            Vector3 offset = new Vector3(0f, FirstPersonConfig.UpOffset.Value, FirstPersonConfig.ForwardOffset.Value);
            __result = Quaternion.LookRotation(forward) * offset;
        }
    }

    /// <summary>Near clip fixe : le calcul vanilla dégénère quand la caméra est sur l'œil (direction nulle).</summary>
    [HarmonyPatch(typeof(GameCamera), "UpdateNearClipping", typeof(Vector3), typeof(Vector3), typeof(float))]
    internal static class FirstPersonNearClipPatch
    {
        private static void Postfix(GameCamera __instance)
        {
            if (FirstPersonMode.Active)
                __instance.m_camera.nearClipPlane = FirstPersonConfig.NearClip.Value;
        }
    }

    /// <summary>Le corps suit toujours le regard (sinon il ne pivote qu'en se déplaçant ou en combat).</summary>
    [HarmonyPatch(typeof(Player), "AlwaysRotateCamera", new System.Type[0])]
    internal static class FirstPersonRotateCameraPatch
    {
        private static void Postfix(Player __instance, ref bool __result)
        {
            if (FirstPersonMode.Active && __instance == Player.m_localPlayer)
                __result = true;
        }
    }

    /// <summary>Fin de session : tout restaurer avant la destruction des objets.</summary>
    [HarmonyPatch(typeof(ZNet), "OnDestroy", new System.Type[0])]
    internal static class FirstPersonSessionEndPatch
    {
        private static void Prefix() => FirstPersonMode.Reset();
    }
}
