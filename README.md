# OvoMiam — mod Valheim

Plugin BepInEx 5 pour Valheim 1.0.x (Unity 6, Mono, Linux natif). Organisé par fonctionnalité dans `OvoMiam/Features/`.

## Fonctionnalités

Options dans `BepInEx/config/ovo.ovomiam.cfg`, une section par fonctionnalité.

- **FoodRecipeSort** : trie les recettes du chaudron et de la table de préparation (`Stations`) par groupe de stat
  dominante (`GroupByStat`) puis valeur nutritive décroissante ; recettes réalisables en tête (`CraftableFirst`).
  Un plat cru vaut sa version cuite (conversions des stations de cuisson, `Food/CookedFood.cs`).
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
Les patches Harmony précisent toujours les types d'arguments : la 1.0 a ajouté des surcharges, un patch ambigu fait échouer tout le plugin.

Vérification : `BepInEx/LogOutput.log` doit contenir `OvoMiam 0.1.0 chargé`, puis `Chaudron trié :` à l'ouverture d'un chaudron.
