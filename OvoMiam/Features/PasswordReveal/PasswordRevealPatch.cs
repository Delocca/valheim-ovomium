using HarmonyLib;
using TMPro;

namespace OvoMiam.Features.PasswordReveal
{
    /// <summary>
    /// Le dialogue « mot de passe du serveur » est ZNet.m_passwordDialog, ouvert par RPC_ClientHandshake quand le
    /// serveur en demande un (le champ est un GuiInputField, dérivé de TMP_InputField, trouvé par
    /// GetComponentInChildren comme le fait le jeu). Après cette ouverture, on pose le bouton Afficher / Masquer et
    /// la case Mémoriser (une fois par instance), on remet le champ dans l'état mémorisé et on le préremplit si un
    /// mot de passe est mémorisé pour ce serveur (identifié par ZNet.GetServerString : backend + hôte:port, id
    /// PlayFab ou SteamID). Si un mot de passe est passé en ligne de commande, le jeu soumet et referme aussitôt :
    /// rien à faire. À la soumission (OnInputSubmit → ZNet.OnPasswordEntered, qui ferme le dialogue si le mot de
    /// passe est non vide), la case décide d'enregistrer ou d'oublier le mot de passe (<see cref="PasswordStore"/>).
    /// </summary>
    internal static class PasswordRevealPatch
    {
        private static string s_serverId;

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
