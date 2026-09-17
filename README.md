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
  Double-clic sur une pile : les autres piles du même objet de cet inventaire y sont versées (depuis le bas à droite,
  vers la gauche puis vers le haut) jusqu'à la remplir, et elle reste en main (`DoubleClickSeconds`).
- **FastPortal** : la téléportation par portail se termine dès que la zone d'arrivée est chargée, au lieu des 8 s
  imposées par le jeu (≈ 1 s entre deux portails d'une même base). Vers une zone non chargée, attend le chargement
  complet de l'aire d'arrivée (zones voisines, objets lointains) plus `SettleSeconds` (1 s). `FadeSeconds` : durée
  de chaque fondu au noir (0,5 s ; 1 s dans le jeu).
- **MinimapSize** : Shift + touches de zoom de la carte (pavé num + / - par défaut) agrandit / réduit la minicarte
  du HUD par pas de `Step` (10 %), de 50 % à 300 % ; maintenir la touche répète. La densité terrain / pixel est
  conservée (le zoom de la carte suit la taille du cadre), les icônes (pins, marqueurs du joueur et du bateau)
  gardent leur taille et les indicateurs d'état (En forme, Repos, Abri…) sont poussés à gauche de la carte
  agrandie. La carte grandit vers l'intérieur de l'écran. `Scale` mémorise la taille choisie
  d'un lancement à l'autre. `MinZoom` (0.0025) permet deux pas de zoom avant de plus qu'en vanilla (0.01, qui est
  aussi le zoom de départ).
- **MapZoomToCursor** : sur la grande carte, le zoom (molette, touches de zoom) se fait autour du point sous le
  curseur au lieu du centre de l'écran. Sans effet à la manette ou au tactile.
- **MenuDoubleClick** : dans le menu principal, un double-clic sur un monde le démarre et un double-clic sur un
  serveur (favoris, récents, amis, communauté) s'y connecte — même effet que le bouton Démarrer / Connecter, et
  seulement si ce bouton est actif (`DoubleClickSeconds`). Sans effet à la manette.
- **PasswordReveal** : bouton « Afficher / Masquer » dans le champ de mot de passe demandé à la connexion à un
  serveur ; l'état choisi est mémorisé (`ShowPassword`). Case « Mémoriser » sous le champ : le mot de passe est
  retrouvé prérempli à la prochaine connexion à ce serveur (décocher puis valider l'oublie). Stocké dans
  `BepInEx/config/ovo.ovomiam.passwords.txt`, chiffré avec une clé dérivée du nom de machine et d'utilisateur :
  simple obfuscation, pas une protection contre quelqu'un ayant accès à la session. Libellés `ShowLabel`,
  `HideLabel`, `RememberLabel`.
- **FirstPerson** : zoomer (molette, ou zoom caméra à la manette) au-delà de la distance minimale du jeu passe en
  vue subjective, dézoomer en ressort. Le corps est masqué, les objets en main restent visibles, la végétation
  n'est plus effacée près de la caméra et le corps suit toujours le regard. La vue survit à la mort et aux
  cinématiques. `NearClip` (0,05 m) : distance minimale de dessin en vue subjective. `ForwardOffset` (0,1 m) et
  `UpOffset` (0) : position de la caméra par rapport au point œil du personnage, tournée avec le regard horizontal
  seulement (vanilla met la caméra 50 cm devant l'œil et la fait tourner avec le regard complet : elle décrit un
  arc). Inspiré de
  [Landoria.FirstPerson](https://github.com/landoria-gaming/Landoria.FirstPerson) (MIT).
- **StartupSkip** : au lancement du jeu, saute les logos Coffee Stain et Iron Gate (`SkipLogos`) ; `IntroVideo`
  (oui par défaut) joue ou non la vidéo d'introduction avant le menu principal.
- **LoadingArt** : le fond des écrans de chargement (démarrage du jeu, chargement de partie, mort, sommeil,
  téléportation) est une image tirée au sort, différente de la précédente, dans le dossier `Folder` (`loading`,
  relatif à la DLL du mod : `BepInEx/plugins/OvoMiam/loading/`, jpg/jpeg/png). Image entière, non déformée, bandes
  noires si le ratio diffère de l'écran. Couvre aussi l'écran « Loading » du menu et la connexion au serveur.
  `HideTeleportAnimation` (oui) masque l'animation vanilla de téléportation pour laisser voir l'artwork. Le jeu n'embarque aucun artwork exploitable : `valheim_art/` du dépôt en
  est la source, copiée par `tools/deploy.sh`. Pour partager le mod, distribuer le dossier `BepInEx/plugins/OvoMiam/`
  complet (DLL + `loading/`).

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
