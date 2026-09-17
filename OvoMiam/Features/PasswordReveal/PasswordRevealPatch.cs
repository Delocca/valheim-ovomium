using HarmonyLib;
using TMPro;

namespace OvoMiam.Features.PasswordReveal
{
    /// <summary>
    /// Le dialogue « mot de passe du serveur » est ZNet.m_passwordDialog, ouvert par RPC_ClientHandshake quand le
    /// serveur en demande un (le champ est un GuiInputField, dérivé de TMP_InputField, trouvé par
    /// GetComponentInChildren comme le fait le jeu). Après cette ouverture, on pose le bouton (une fois par instance)
    /// et on remet le champ dans l'état mémorisé. Si un mot de passe est passé en ligne de commande, le jeu soumet
    /// et referme aussitôt : rien à faire.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake", typeof(ZRpc), typeof(bool), typeof(string))]
    internal static class PasswordRevealPatch
    {
        private static void Postfix(ZNet __instance, bool needPassword)
        {
            if (!PasswordRevealConfig.Enabled.Value || !needPassword || __instance.m_passwordDialog == null
                || !__instance.m_passwordDialog.gameObject.activeSelf)
                return;
            TMP_InputField field = __instance.m_passwordDialog.GetComponentInChildren<TMP_InputField>(true);
            if (field == null)
            {
                Plugin.Log.LogWarning("PasswordReveal : aucun TMP_InputField dans ZNet.m_passwordDialog");
                return;
            }
            PasswordRevealToggle.Setup(field);
        }
    }
}
