# Ovomium — mod Valheim (BepInEx 5 + Harmony, C#)

Voir `README.md` pour les fonctionnalités et options. Ce fichier : ce qu'un agent doit savoir pour travailler ici.

## Environnement

- Jeu : `~/.local/share/Steam/steamapps/common/Valheim`, Valheim **1.0.12**, Unity 6, Mono, Linux natif.
  BepInExPack_Valheim 5.4.2350 installé (`tools/install-bepinex.sh`). Journal : `BepInEx/LogOutput.log` (lisible).
- Aucun outil .NET sur l'hôte : tout passe par l'image podman `ovomiam-build` (`tools/Containerfile`).
- `tools/build.sh` et `tools/deploy.sh` sont **exclus du bac à sable** (`.claude/settings.json`) : les lancer
  directement. Toute autre commande podman ou accès à nuget/mcr/thunderstore échoue dans le bac à sable.
- **Ne jamais déployer jeu lancé** (`deploy.sh` refuse) : Mono lit les méthodes à la demande dans le fichier, la DLL
  remplacée casse les patches pas encore exécutés (`BadImageFormatException: Method has zero rva`, symptôme :
  raccourcis qui « ne marchent plus »). Après un build, lancer `tools/deploy.sh --wait` en arrière-plan
  (`run_in_background`) : il attend la fermeture du jeu et copie ; Edia relance ensuite le jeu sans prévenir.
  Les tests en jeu sont faits par Edia ; comparer son retour avec les traces `… trié :` du journal
  (option `LogSortOrder = true` dans `BepInEx/config/ovo.ovomium.cfg`, désactivée par défaut).

## Code du jeu

- Décompilé dans `build/decompiled/` (gitignoré) : `tools/decompile.sh build/decompiled`. À régénérer après
  une mise à jour du jeu. Types dans le namespace global ; `assembly_valheim.dll` est publicisée dans le csproj
  (accès direct aux membres privés, ex. `InventoryGui.m_availableRecipes`).
- Points d'ancrage utilisés : `InventoryGui.UpdateRecipeList` (tri natif : craftable → `Recipe.m_listSortWeight`
  → SortMethod ; positions posées à la main via `anchoredPosition` ; sélection mémorisée par valeur, donc
  réordonner est sûr ; nom d'une ligne = `TMP_Text` « name », rich text), `InventoryGui.Update` (clavier),
  `CookingStation.m_conversion` (cru → cuit), `InventoryGui.OnSelectedItem` / `UpdateItemDrag` (piles :
  à la souris il n'y a pas de drag Unity, « prendre » et « poser » sont deux clics ; avec une pile en main le
  modificateur Shift/Ctrl est ignoré par le jeu et tout dépôt réussi détruit le drag), `Player.UpdateTeleport`
  (2 s d'attente, déplacement, puis 8 s en dur + `ZNetScene.IsAreaReady`), `Hud.GetFadeDuration` (fondu 1 s) et
  `Minimap.Update` (échelle de `m_smallRoot` via `localScale`, pivot déplacé sur le coin d'ancrage ; les actions
  `MapZoomIn`/`MapZoomOut` déclenchent aussi le zoom vanilla, neutralisé en restaurant `SmallZoom` mémorisé en Prefix)
  et `Minimap.UpdateMap` (privée : zoom `LargeZoom` puis `CenterMap(player + m_mapOffset)` qui pose le `uvRect` lu par
  `ScreenToWorldPoint` ; `m_mapOffset` jamais clampé, modifié par drag souris, stick, tactile et remis à zéro à
  l'ouverture ; le zoom vers le curseur existe dans le jeu sous `if (false && …)`), `FejdStartup.OnSelectWorld(int)` et
  `ServerListGui.OnSelectedServer(ServerJoinData)` (privées, seules cibles du `onClick` des lignes de liste ; validation
  par `FejdStartup.OnWorldStart` / `OnJoinStart`, boutons `m_worldStart` / `m_joinGameButton`), et
  `ZNet.RPC_ClientHandshake` (ouvre `ZNet.m_passwordDialog`, le dialogue de mot de passe serveur ; son champ est un
  `GuiInputField` de `gui_framework.dll`, non référencé : dérivé de `TMP_InputField`, on passe par ce type) et
  `ZNet.OnPasswordEntered(string)` (privée, branchée sur `OnInputSubmit` ; ne ferme le dialogue que si le mot de
  passe est non vide ; le serveur est identifié par `ZNet.GetServerString(true)` ; mots de passe mémorisés dans
  `BepInEx/config/ovo.ovomium.passwords.txt`, AES à clé dérivée machine+utilisateur, cf. `PasswordStore`).
  Le champ `FejdStartup.m_serverPassword` (mot de passe d'un monde qu'on héberge) est un autre champ, non traité.
  Caméra : `GameCamera.GetCameraOffset` a une branche première personne vanilla (`m_distance <= 0 → m_fpsOffset`
  depuis `m_eye`), bloquée par `m_minDistance` du prefab (mis à 0 par FirstPerson, molette clampée dans
  `UpdateCamera`, placement dans `GetCameraPosition` : `m_smoothYTilt` éloigne à 1,5 m en regardant en bas,
  clamp au-dessus de l'eau `m_minWaterDistance`, `ApplyCameraTilt` roulis bateau max à distance min) ;
  `Character.SetVisible` masque le joueur local à moins de 2 m via le point de référence du LODGroup ;
  `Player.AlwaysRotateCamera` fait suivre le regard au corps ; `VisEquipment.UpdateLodgroup` n'est appelé
  qu'à un changement d'équipement ; shaders végétation : propriété `_CamCull`.
  Démarrage : une seule scène native (EntryPoint) puis `loading.unity` → `start.unity` → `main.unity` en bundles.
  `SceneLoader.Awake` force `_showLogos = true` (logos affichés par la coroutine `LoadSceneAsync`, 2 s chacun) ;
  `CinematicsManager.m_introOnStartup` conditionne la vidéo d'intro (`FejdStartup.PlayIntroCinematic`, seulement
  au premier passage par le menu) ; pas de flag `-skipintro` vanilla. Écrans de chargement : `Hud.m_loadingScreen`
  (partie, mort, sommeil, téléportation) piloté par `Hud.UpdateBlackScreen`, image de fond `Hud.m_loadingImage`
  unique, jamais tirée au sort en vanilla ; écran de démarrage = `SceneLoader.gameLogo` + `LoadingIndicator` sur
  noir. Aucun artwork en clair dans les données du jeu (tout est dans les bundles `StreamingAssets/SoftRef/`).
  L'apparition au login/respawn (`Game.FindSpawnPoint`, 8 s + `IsAreaReady`) est volontairement laissée vanilla
  (décision d'Edia, 2026-09-17) même si le décor y est parfois incomplet à l'arrivée sur un serveur.
  Menu Paramètres : prefab `Menu.m_settingsPrefab` / `FejdStartup.m_settingsPrefab`, composant `Settings` (`Awake`
  privé → `InitializeTabs` lit `TabHandler.m_tabs` public, une page = un MonoBehaviour `ISettingsTab` public de
  `Valheim.SettingsGui` ; `OnOkAsync` doit invoquer son callback sinon la fenêtre ne se ferme jamais ; `OnBack`
  puis `CloseSettings` détruit l'objet). L'interface a des membres à implémentation par défaut : les redéclarer
  tous, le compilateur net48 refuse d'en hériter. Boutons des menus : `Menu.m_settingsButton` public (`Menu.Start`
  privé pose `m_instance`), menu principal = boutons de `FejdStartup.m_menuList` (celui dont le listener persistant
  vise `OnButtonSettings`). Fermeture par Échap du menu de jeu : `Menu.m_settingsInstance` et
  `m_closeMenuState = SettingsOpen` (privés, publicisés). Lignes vanilla clonées (SettingsMenu) : champs sérialisés
  d'`AccessibilitySettings` (`m_toggleRun`, `m_guiScaleSlider`, `m_guiScaleText`) ; leurs `onValueChanged`
  persistants visent le composant vanilla, à remplacer par un événement neuf sur le clone.
- Stations par `CraftingStation.m_name` : `$piece_cauldron`, `$piece_preptable` (ce dernier supposé, à confirmer).

## Règles de code

- **Une fonctionnalité = un dossier** `Ovomium/Features/<Nom>/` avec `<Nom>Config.cs` (ConfigEntry, section
  du même nom) et `<Nom>Patch.cs`. Le partagé métier va dans `Ovomium/Food/`. `Plugin.cs` ne fait que binder
  les configs et `PatchAll`.
- Patches Harmony **toujours avec types d'arguments explicites** (`typeof(...)` ou `new System.Type[0]`) :
  la 1.0 a ajouté des surcharges, un patch ambigu fait échouer tout le plugin au chargement.
- `Console` du jeu masque `System.Console` : ne pas importer `System` dans les patches qui l'utilisent.
- Warnings = erreurs (`TreatWarningsAsErrors`). Version du mod : `<Version>` du csproj uniquement (cible
  `GeneratePluginVersion` → `PluginVersion.Value`).
- Journal : `Plugin.Log`. Traces de tri en `LogInfo` derrière `LogSortOrder` ; le niveau Debug
  n'est pas écrit dans `LogOutput.log` par défaut.
