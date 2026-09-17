# OvoMiam — mod Valheim

Plugin BepInEx 5 pour Valheim 1.0.x (Unity 6, Mono, Linux natif). Organisé par fonctionnalité dans `OvoMiam/Features/`,
logique nutritive partagée dans `OvoMiam/Food/`.

## Fonctionnalités

Options dans `BepInEx/config/ovo.ovomiam.cfg`, une section par fonctionnalité.

- **FoodRecipeSort** : trie les recettes du chaudron et de la table de préparation (`Stations`) par groupe de stat
  dominante (`GroupByStat`) puis valeur nutritive décroissante ; recettes réalisables en tête (`CraftableFirst`).
  Un plat cru vaut sa version cuite (conversions des stations de cuisson, `Food/CookedFood.cs`).
  `LogSortOrder` (défaut : non) écrit la liste triée dans le journal à chaque ouverture, pour le débogage.
- **FoodMarker** : point coloré devant chaque plat selon sa stat dominante (rouge vie, jaune endurance, bleu eitr,
  plusieurs points si mixte, même critère que la pastille d'inventaire du jeu). `Glyph`, `GlyphScale`.
- **RecipeKeyboardNav** : flèches haut/bas pour changer de recette dans toute station.

## Prérequis

- podman (toute la chaîne .NET tourne dans le conteneur `ovomiam-build`, rien sur l'hôte)
- Valheim installé via Steam (`VALHEIM_DIR` pour un autre chemin que `~/.local/share/Steam/steamapps/common/Valheim`)
- BepInExPack_Valheim : `tools/install-bepinex.sh`, puis option de lancement Steam `./start_game_bepinex.sh %command%`

## Développement

```
podman build -t ovomiam-build -f tools/Containerfile tools   # une fois
tools/build.sh            # → OvoMiam/bin/Release/net48/OvoMiam.dll
tools/deploy.sh           # copie dans BepInEx/plugins/OvoMiam/
tools/decompile.sh build/decompiled   # code du jeu décompilé, pour référence (non versionné)
```

Le csproj référence les DLL du jeu (publicisées) et `BepInEx.Core` 5.4.21 depuis `nuget.bepinex.dev`.
La version du mod se change uniquement dans `<Version>` du csproj (constante `PluginVersion.Value` générée au build).
Les patches Harmony précisent toujours les types d'arguments : la 1.0 a ajouté des surcharges, un patch ambigu fait échouer tout le plugin.

Vérification : `BepInEx/LogOutput.log` doit contenir `OvoMiam <version> chargé`, puis, si `LogSortOrder = true`,
`$piece_cauldron trié :` à l'ouverture d'un chaudron.
