using HarmonyLib;
using UnityEngine;

namespace Ovomium.Features.FocusClick
{
    /// <summary>
    /// Le jeu n'a aucune gestion du focus fenêtre : le clic qui le lui redonne est lu comme un clic normal. Au retour
    /// du focus (composant <see cref="FocusWatcher"/> sur l'objet du plugin), deux fenêtres d'ignorance de même durée :
    /// le délai d'entrée vanilla <c>PlayerController.SetTakeInputDelay</c> (attaque, attaque secondaire, blocage ;
    /// pendant ce délai le contrôleur continue de mettre à jour <c>m_attackWasPressed</c>, donc pas d'attaque au
    /// relâchement si le clic est maintenu), et <c>Player.TakeInput()</c> forcé à faux (interaction « Use »,
    /// placement et suppression de construction, lus en <c>GetButtonDown</c> dans <c>Player.Update</c>).
    /// </summary>
    internal static class FocusClickPatch
    {
        private static float s_ignoreUntil;

        private sealed class FocusWatcher : MonoBehaviour
        {
            private void OnApplicationFocus(bool hasFocus)
            {
                if (!hasFocus || !FocusClickConfig.Enabled.Value || Player.m_localPlayer == null)
                    return;
                float seconds = FocusClickConfig.IgnoreSeconds.Value;
                s_ignoreUntil = Time.unscaledTime + seconds;
                PlayerController.SetTakeInputDelay(seconds);
                Plugin.Log.LogInfo($"FocusClick : focus repris, clics ignorés {seconds:0.00} s");
            }
        }

        /// <summary>Pose le guetteur sur l'objet du plugin (détruit avec lui au rechargement à chaud).</summary>
        public static void Install(GameObject host)
        {
            if (host.GetComponent<FocusWatcher>() == null)
                host.AddComponent<FocusWatcher>();
        }

        [HarmonyPatch(typeof(Player), "TakeInput", new System.Type[0])]
        private static class TakeInputPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (__result && Time.unscaledTime < s_ignoreUntil)
                    __result = false;
            }
        }
    }
}
