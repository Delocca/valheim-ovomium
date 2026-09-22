using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovomium.Features.ManualChest
{
    /// <summary>
    /// Le bouton vanilla « Objets similaires » du panneau coffre (<c>InventoryGui.m_stackAllButton</c>, listener
    /// <c>OnStackAll</c> posé dans <c>Awake</c>) devient l'interrupteur du drapeau manuel du coffre ouvert : libellé
    /// « Coffre auto : oui / non », infobulle explicative. Son action d'origine passe par E maintenu
    /// (<see cref="ManualChestHoldPatch"/>). <see cref="Unload"/> restaure libellé, listener et infobulle.
    /// </summary>
    internal static class ManualChestButton
    {
        private const string MarkerName = "OvomiumManualChestMarker";
        private const string LabelAuto = "Coffre auto : oui";
        private const string LabelManual = "Coffre auto : non";
        private const string TooltipTopic = "Coffre automatique";
        private const string TooltipText = "Oui : le rangement rapide, la fabrication et le combustible depuis les coffres "
            + "peuvent s'en servir.\nNon (coffre manuel) : ignoré par tout ça.\n\nE maintenu, coffre ouvert : y range les "
            + "objets similaires sans le fermer.";
        private static readonly Color ManualColor = new Color(0.6f, 0.6f, 0.6f);

        private static Button s_button;
        private static TMP_Text s_label;
        private static Color s_originalColor;
        private static UITooltip s_tooltip;
        private static bool s_tooltipAdded;
        private static string s_tooltipOriginalText;
        private static string s_tooltipOriginalTopic;

        private static bool Installed => s_button != null;

        /// <summary>Rechargement à chaud : l'<c>Awake</c> d'InventoryGui a déjà eu lieu, le bouton est en scène.</summary>
        public static void Install()
        {
            if (InventoryGui.instance != null) Attach(InventoryGui.instance);
        }

        internal static void Unload() => Detach();

        /// <summary>Aligne le bouton sur l'option et le coffre ouvert : à appeler à chaque changement de coffre.</summary>
        public static void Refresh(InventoryGui gui)
        {
            if (!ManualChestConfig.Enabled.Value) { Detach(); return; }
            if (!Installed) Attach(gui);
            if (!Installed) return;
            bool manual = ManualChest.IsManual(gui.m_currentContainer);
            s_label.text = manual ? LabelManual : LabelAuto;
            s_label.color = manual ? ManualColor : s_originalColor;
        }

        private static void Attach(InventoryGui gui)
        {
            if (Installed || !ManualChestConfig.Enabled.Value || gui.m_stackAllButton == null) return;
            var button = gui.m_stackAllButton;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                Plugin.Log.LogWarning("ManualChest : aucun TMP_Text dans le bouton « Objets similaires », bouton laissé vanilla");
                return;
            }
            bool residue = button.transform.Find(MarkerName) != null;
            if (residue) Plugin.Log.LogWarning("ManualChest : bouton laissé modifié par une version précédente, libellé vanilla supposé");
            s_button = button;
            s_label = label;
            s_originalColor = label.color;
            if (!residue) new GameObject(MarkerName).transform.SetParent(button.transform, false);
            // UIInputHint relance ReLocalizeVisible sur l'inventaire à chaque ouverture : sans ceci, le cache de
            // Localization remettrait « $inventory_stackall » par-dessus notre libellé.
            Localization.instance.RemoveTextFromCache(label);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
            AttachTooltip(gui);
        }

        private static void AttachTooltip(InventoryGui gui)
        {
            s_tooltip = s_button.GetComponent<UITooltip>();
            s_tooltipAdded = s_tooltip == null;
            if (s_tooltipAdded)
            {
                UITooltip model = gui.m_craftButton != null ? gui.m_craftButton.GetComponent<UITooltip>() : null;
                if (model == null) return;
                s_tooltip = s_button.gameObject.AddComponent<UITooltip>();
                s_tooltip.m_tooltipPrefab = model.m_tooltipPrefab;
            }
            s_tooltipOriginalText = s_tooltip.m_text;
            s_tooltipOriginalTopic = s_tooltip.m_topic;
            s_tooltip.m_topic = TooltipTopic;
            s_tooltip.m_text = TooltipText;
        }

        private static void Detach()
        {
            if (!Installed) return;
            s_button.onClick.RemoveAllListeners();
            InventoryGui gui = InventoryGui.instance;
            if (gui != null) s_button.onClick.AddListener(gui.OnStackAll);
            s_label.text = "$inventory_stackall";
            Localization.instance.Localize(s_label.transform); // réinscrit le libellé vanilla dans le cache
            s_label.color = s_originalColor;
            Transform marker = s_button.transform.Find(MarkerName);
            if (marker != null) Object.Destroy(marker.gameObject);
            DetachTooltip();
            s_button = null;
            s_label = null;
        }

        private static void DetachTooltip()
        {
            if (s_tooltip == null) return;
            if (s_tooltipAdded) Object.Destroy(s_tooltip);
            else
            {
                s_tooltip.m_text = s_tooltipOriginalText;
                s_tooltip.m_topic = s_tooltipOriginalTopic;
            }
            s_tooltip = null;
        }

        private static void OnClick()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_currentContainer == null) return;
            ManualChest.SetManual(gui.m_currentContainer, !ManualChest.IsManual(gui.m_currentContainer));
            Refresh(gui);
        }
    }
}
