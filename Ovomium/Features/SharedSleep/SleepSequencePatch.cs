using HarmonyLib;

namespace Ovomium.Features.SharedSleep
{
    /// <summary>
    /// Les éveillées ne s'endorment pas : le serveur envoie <c>SleepStart</c> puis <c>SleepStop</c> à tout le monde
    /// (RPC routés de <c>Game</c>). <c>SleepStart</c> ne fait que <c>Player.SetSleeping(true)</c> (fondu, plus de
    /// commandes, rêve) : sauté pour une joueuse qui n'est pas vraiment au lit, la nuit défile sous ses yeux.
    /// <c>SleepStop</c> fait <c>SetSleeping(false)</c> (sans effet si elle ne dort pas : pas de bonjour, de Reposé ni
    /// de statistique), puis <c>AttachStop()</c>, qui la lèverait d'une chaise : neutralisé pendant l'appel ; la
    /// sauvegarde et l'usure des constructions (<c>WearNTear.OnSleep</c>) qui suivent restent vanilla.
    /// </summary>
    internal static class SleepSequencePatch
    {
        private static bool s_keepSeated;

        [HarmonyPatch(typeof(Game), "SleepStart", new[] { typeof(long) })]
        private static class GameSleepStartPatch
        {
            private static bool Prefix()
            {
                Player player = Player.m_localPlayer;
                if (!SharedSleepConfig.Enabled.Value || player == null || SharedSleepPatch.InRealBed(player)) return true;
                Plugin.Log.LogInfo("SharedSleep : nuit passée en accéléré, éveillée");
                return false;
            }
        }

        [HarmonyPatch(typeof(Game), "SleepStop", new[] { typeof(long) })]
        private static class GameSleepStopPatch
        {
            private static void Prefix()
            {
                Player player = Player.m_localPlayer;
                s_keepSeated = SharedSleepConfig.Enabled.Value && player != null && !player.IsSleeping();
            }

            private static void Finalizer() => s_keepSeated = false;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AttachStop), new System.Type[0])]
        private static class PlayerAttachStopPatch
        {
            private static bool Prefix(Player __instance) => !(s_keepSeated && __instance == Player.m_localPlayer);
        }
    }
}
