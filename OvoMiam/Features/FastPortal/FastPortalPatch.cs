using HarmonyLib;
using UnityEngine;

namespace OvoMiam.Features.FastPortal
{
    /// <summary>
    /// Téléportation par portail (m_distantTeleport) réécrite sans le seuil fixe de 8 s : déplacement dès que l'écran
    /// est noir, retour dès que la zone d'arrivée est prête et qu'un sol est trouvé. Les téléporteurs de donjon
    /// (non distants) gardent le comportement du jeu.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport), new System.Type[] { typeof(float) })]
    internal static class FastPortalPatch
    {
        /// <summary>Secours du jeu : au-delà de ce délai, on pose le joueur sur la hauteur du terrain même sans sol trouvé.</summary>
        private const float FallbackSeconds = 15f;

        /// <summary>Destination déjà chargée au départ (même base) : pas de délai de stabilisation.</summary>
        private static bool s_loadedAtStart;
        private static float s_readySince;

        private static bool Prefix(Player __instance, float dt)
        {
            if (!FastPortalConfig.Enabled.Value || !__instance.m_teleporting || !__instance.m_distantTeleport)
                return true;

            if (__instance.m_teleportTimer == 0f)
            {
                s_loadedAtStart = FastPortalArrival.IsFullyLoaded(__instance.m_teleportTargetPos);
                s_readySince = -1f;
            }
            __instance.m_teleportCooldown = 0f;
            __instance.m_teleportTimer += dt;
            if (WaitingForBlackScreen(__instance))
                return false;

            MoveToTarget(__instance);
            if (!ArrivalReady(__instance))
                return false;

            if (ZoneSystem.instance.FindFloor(__instance.m_teleportTargetPos, out float _))
            {
                Finish(__instance);
            }
            else if (__instance.m_teleportTimer > FallbackSeconds)
            {
                Vector3 position = __instance.transform.position;
                position.y = ZoneSystem.instance.GetSolidHeight(__instance.m_teleportTargetPos) + 0.5f;
                __instance.transform.position = position;
                Finish(__instance);
            }
            return false;
        }

        /// <summary>
        /// Condition du jeu (zone cible et objets proches), puis, si la destination n'était pas chargée au départ :
        /// aire active complète et délai de stabilisation. Le secours à 15 s s'applique quoi qu'il arrive.
        /// </summary>
        private static bool ArrivalReady(Player player)
        {
            if (!ZNetScene.instance.IsAreaReady(player.m_teleportTargetPos))
                return false;
            if (s_loadedAtStart || player.m_teleportTimer > FallbackSeconds)
                return true;
            if (s_readySince < 0f)
            {
                if (!FastPortalArrival.IsFullyLoaded(player.m_teleportTargetPos))
                    return false;
                s_readySince = player.m_teleportTimer;
            }
            return player.m_teleportTimer - s_readySince >= FastPortalConfig.SettleSeconds.Value;
        }

        /// <summary>Le fondu du HUD n'est que réactif : on attend qu'il soit complet, avec le délai vanilla (2 s) en secours.</summary>
        private static bool WaitingForBlackScreen(Player player)
        {
            if (player.m_teleportTimer > 2f || Hud.instance == null)
                return false;
            return Hud.instance.m_loadingScreen.alpha < 1f;
        }

        /// <summary>Mêmes opérations que le jeu ; répétées à chaque frame tant que la zone n'est pas prête.</summary>
        private static void MoveToTarget(Player player)
        {
            Vector3 dir = player.m_teleportTargetRot * Vector3.forward;
            player.transform.position = player.m_teleportTargetPos;
            player.transform.rotation = player.m_teleportTargetRot;
            player.m_body.linearVelocity = Vector3.zero;
            player.m_maxAirAltitude = player.transform.position.y;
            EnvMan.instance.ForceInstantEnvironmentSwitch();
            player.SetLookDir(dir);
        }

        private static void Finish(Player player)
        {
            player.m_teleportTimer = 0f;
            player.m_teleporting = false;
            player.ResetCloth();
        }
    }

    /// <summary>
    /// Fondu au noir raccourci pour les téléportations. Le fondu de retour a lieu quand le joueur ne se téléporte
    /// plus : on retient qu'il suit une téléportation jusqu'à ce que l'écran soit redevenu transparent.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.GetFadeDuration), new System.Type[] { typeof(Player) })]
    internal static class FastPortalFadePatch
    {
        private static bool s_teleportFade;

        private static void Postfix(Hud __instance, Player player, ref float __result)
        {
            if (!FastPortalConfig.Enabled.Value)
            {
                s_teleportFade = false;
                return;
            }
            if (player != null && player.IsTeleporting())
                s_teleportFade = true;
            else if (player == null || player.IsDead() || player.IsSleeping() || __instance.m_loadingScreen.alpha <= 0f)
                s_teleportFade = false;

            if (s_teleportFade)
                __result = Mathf.Max(FastPortalConfig.FadeSeconds.Value, 0.01f);
        }
    }
}
