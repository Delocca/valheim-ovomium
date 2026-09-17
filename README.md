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
- **StackDrag** : manipulation fine des piles dans l'inventaire et les coffres. Alt+clic sur une pile prend 1 objet
  en main, et 1 de plus à chaque Alt+clic sur une pile du même objet. Avec une pile en main : Ctrl+clic pose 1 objet
  dans la case, Shift+clic pose la quantité choisie dans la case et rend le reste, clic maintenu + glisser pose
  1 objet sur chaque case vide survolée (pour préparer l'emplacement des futurs transferts par Ctrl+clic).
  Le clic simple pose toute la pile, au relâchement du bouton. Ctrl+clic mains vides reste le transfert rapide du jeu.
  Double-clic sur une pile : toutes les piles du même objet de cet inventaire sont versées dans celle la plus en bas à
  droite (parcours vers la gauche puis vers le haut) jusqu'à la remplir, et cette pile est prise en main
  (`DoubleClickSeconds`).
- **FastPortal** : la téléportation par portail se termine dès que la zone d'arrivée est chargée, au lieu des 8 s
  imposées par le jeu (≈ 1 s entre deux portails d'une même base). Vers une zone non chargée, attend le chargement
  complet de l'aire d'arrivée (zones voisines, objets lointains) plus `SettleSeconds` (1 s). `FadeSeconds` : durée
  de chaque fondu au noir (0,5 s ; 1 s dans le jeu).

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
