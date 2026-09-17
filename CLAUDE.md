# OvoMiam — mod Valheim (BepInEx 5 + Harmony, C#)

Voir `README.md` pour les fonctionnalités et options. Ce fichier : ce qu'un agent doit savoir pour travailler ici.

## Environnement

- Jeu : `~/.local/share/Steam/steamapps/common/Valheim`, Valheim **1.0.12**, Unity 6, Mono, Linux natif.
  BepInExPack_Valheim 5.4.2350 installé (`tools/install-bepinex.sh`). Journal : `BepInEx/LogOutput.log` (lisible).
- Aucun outil .NET sur l'hôte : tout passe par l'image podman `ovomiam-build` (`tools/Containerfile`).
- `tools/build.sh` et `tools/deploy.sh` sont **exclus du bac à sable** (`.claude/settings.json`) : les lancer
  directement. Toute autre commande podman ou accès à nuget/mcr/thunderstore échoue dans le bac à sable.
- Le jeu doit être **relancé complètement** après chaque déploiement (Mono ne recharge pas les DLL).
  Les tests en jeu sont faits par Edia ; comparer son retour avec les traces `… trié :` du journal
  (option `LogSortOrder = true` dans `BepInEx/config/ovo.ovomiam.cfg`, désactivée par défaut).

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
  (2 s d'attente, déplacement, puis 8 s en dur + `ZNetScene.IsAreaReady`) et `Hud.GetFadeDuration` (fondu 1 s).
  L'apparition au login/respawn (`Game.FindSpawnPoint`, 8 s + `IsAreaReady`) est volontairement laissée vanilla
  (décision d'Edia, 2026-09-17) même si le décor y est parfois incomplet à l'arrivée sur un serveur.
- Stations par `CraftingStation.m_name` : `$piece_cauldron`, `$piece_preptable` (ce dernier supposé, à confirmer).

## Règles de code

- **Une fonctionnalité = un dossier** `OvoMiam/Features/<Nom>/` avec `<Nom>Config.cs` (ConfigEntry, section
  du même nom) et `<Nom>Patch.cs`. Le partagé métier va dans `OvoMiam/Food/`. `Plugin.cs` ne fait que binder
  les configs et `PatchAll`.
- Patches Harmony **toujours avec types d'arguments explicites** (`typeof(...)` ou `new System.Type[0]`) :
  la 1.0 a ajouté des surcharges, un patch ambigu fait échouer tout le plugin au chargement.
- `Console` du jeu masque `System.Console` : ne pas importer `System` dans les patches qui l'utilisent.
- Warnings = erreurs (`TreatWarningsAsErrors`). Version du mod : `<Version>` du csproj uniquement (cible
  `GeneratePluginVersion` → `PluginVersion.Value`).
- Journal : `Plugin.Log`. Traces de tri en `LogInfo` derrière `LogSortOrder` ; le niveau Debug
  n'est pas écrit dans `LogOutput.log` par défaut.
