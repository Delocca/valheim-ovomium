using HarmonyLib;
using TMPro;

namespace Ovomium.Features.PasswordReveal
{
    /// <summary>
    /// Le dialogue « mot de passe du serveur » est ZNet.m_passwordDialog, ouvert par RPC_ClientHandshake quand le
    /// serveur en demande un (le champ est un GuiInputField, dérivé de TMP_InputField, trouvé par
    /// GetComponentInChildren comme le fait le jeu). Après cette ouverture, on pose le bouton Afficher / Masquer,
    /// la case Mémoriser et le bouton OK (reconstruits à chaque ouverture), on remet le champ dans l'état mémorisé et
    /// on le préremplit si un mot de passe est mémorisé pour ce serveur (identifié par ZNet.GetServerString : backend
    /// + hôte:port, id PlayFab ou SteamID). Si un mot de passe est passé en ligne de commande, le jeu soumet et
    /// referme aussitôt : rien à faire. À la soumission (OnInputSubmit → ZNet.OnPasswordEntered, qui ferme le dialogue
    /// si le mot de passe est non vide), la case décide d'enregistrer ou d'oublier le mot de passe
    /// (<see cref="PasswordStore"/>). Le dialogue vit avec ZNet (détruit au retour au menu) : <see cref="Unload"/>
    /// nettoie celui de la partie en cours, affiché ou non.
    /// </summary>
    internal static class PasswordRevealPatch
    {
        private static string s_serverId;

        /// <summary>Rechargement à chaud : retire les boutons et la case du dialogue et rend au champ son état vanilla.</summary>
        internal static void Unload()
        {
            s_serverId = null;
            ZNet znet = ZNet.instance;
            if (znet == null || znet.m_passwordDialog == null)
                return;
            TMP_InputField field = znet.m_passwordDialog.GetComponentInChildren<TMP_InputField>(true);
            if (field == null)
                return;
            PasswordRevealToggle.Remove(field);
            PasswordRememberToggle.Remove(field);
            PasswordOkButton.Remove(field);
        }

        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake", typeof(ZRpc), typeof(bool), typeof(string))]
        private static class ClientHandshakePatch
        {
            private static void Postfix(ZNet __instance, bool needPassword)
            {
                s_serverId = null;
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

                s_serverId = ZNet.GetServerString(true);
                string remembered = PasswordStore.Get(s_serverId);
                Plugin.Log.LogInfo($"PasswordReveal : serveur {s_serverId}, mot de passe mémorisé : {(remembered != null ? "oui" : "non")}");
                field.text = remembered ?? "";
                field.MoveTextEnd(false);
                PasswordRememberToggle.Setup(field, remembered != null);
                PasswordOkButton.Setup(field);
            }
        }

        [HarmonyPatch(typeof(ZNet), "OnPasswordEntered", typeof(string))]
        private static class PasswordEnteredPatch
        {
            private static void Postfix(ZNet __instance, string pwd)
            {
                if (s_serverId == null || string.IsNullOrEmpty(pwd) || __instance.m_passwordDialog.gameObject.activeSelf)
                    return;
                if (PasswordRememberToggle.Checked)
                    PasswordStore.Set(s_serverId, pwd);
                else
                    PasswordStore.Remove(s_serverId);
                s_serverId = null;
            }
        }
    }
}
