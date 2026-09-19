# TODO

Chantiers restants, par priorité. Une entrée par chantier avec le contexte utile (décisions, ancrages dans `build/decompiled/`). Règles de maintenance : `CLAUDE.md`, section « Suivi ».

1. **Release 0.9.0** : `tools/release.sh` (notes = section « Non publié » de `CHANGELOG.md`), dépôt ouvert par `tools/repo-visibility.sh public`, puis Edia le referme une fois les amies à jour.

2. **Graphismes vanilla en direct** : appliquer immédiatement les réglages de l'onglet Graphismes du menu Paramètres du jeu (aperçu comme dans la fenêtre Ovomium). Reporté par Edia à une session neuve, risque moyen. Ancrages (1.0.15) : `Valheim.SettingsGui.GraphicsSettings` ne fait rien avant OK (`ModifySetting` privées écrivent `m_currentSettingsRaw`, `OnBack` vide) ; OK → `GraphicsSettingsManager.SaveAndApplyGraphicsSettingsCustom/WithPreset` (publiques) → `ApplyGraphicsSettingsToCurrentSession()` (privée) → event `GraphicsSettingsChanged` (CameraEffects, Clutter, Heightmap…), qui appelle aussi `GraphicsSettings.UpdateUI` (resynchronise `m_currentSettingsRaw`). Aperçu : mémoriser `CurrentPlayerSettingsRaw` + `CurrentPresetID` à l'ouverture, pousser à chaque `ModifySetting`, restaurer sur `OnBack`. Aucun réglage n'exige un redémarrage.

3. **Inventaire / coffres**, trois features (Edia, 2026-09-19). Regarder les mods existants avant de coder (licences, ancrages) :
   - filtre du ramassage automatique (choisir quels objets sont ramassés) ;
   - rangement automatique vers les coffres proches contenant déjà l'objet (type QuickStack) ;
   - craft et construction puisant les ingrédients dans les coffres proches (type CraftFromContainers / AzuCraftyBoxes).

4. **FirstPerson phase 2 : corps visible** : caméra sur l'os tête, tête en ShadowsOnly ou os rétréci. Références : Landoria.FirstPerson (MIT), ImmersiveFirstPerson (GPL).

5. **Updater phase 2** : notification « nouvelle version » dans le jeu + mini plugin `Ovomium.Updater` qui remplace la DLL au lancement suivant (Windows verrouille la DLL en cours d'exécution).
