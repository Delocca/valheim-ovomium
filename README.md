# Ovomium — mod Valheim

Plugin BepInEx 5 pour Valheim 1.0.x (Unity 6, Mono, Linux natif). Organisé par fonctionnalité dans `Ovomium/Features/`,
logique nutritive partagée dans `Ovomium/Food/`.

## Fonctionnalités

Options dans `BepInEx/config/ovo.ovomium.cfg`, une section par fonctionnalité.

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
  `BepInEx/config/ovo.ovomium.passwords.txt`, chiffré avec une clé dérivée du nom de machine et d'utilisateur :
  simple obfuscation, pas une protection contre quelqu'un ayant accès à la session. Bouton « OK » sous le champ,
  à droite : même effet que la touche Entrée. Libellés `ShowLabel`, `HideLabel`, `RememberLabel`, `OkLabel`.
- **FirstPerson** : zoomer (molette, ou zoom caméra à la manette) au-delà de la distance minimale du jeu passe en
  vue subjective, dézoomer en ressort. Le corps est masqué, les objets en main restent visibles, la végétation
  n'est plus effacée près de la caméra et le corps suit toujours le regard. La vue survit à la mort et aux
  cinématiques. `NearClip` (0,05 m) : distance minimale de dessin en vue subjective. `ForwardOffset` (0,1 m) et
  `UpOffset` (0) : position de la caméra par rapport au point œil du personnage, tournée avec le regard horizontal
  seulement (vanilla met la caméra 50 cm devant l'œil et la fait tourner avec le regard complet : elle décrit un
  arc). Accroupi, la caméra descend à une hauteur fixe au-dessus des pieds, `CrouchEyeHeight` (1,23 m), avec une
  transition rapide (le point œil vanilla ne bouge pas ; l'os tête, lui, balance avec la marche furtive). Inspiré de
  [Landoria.FirstPerson](https://github.com/landoria-gaming/Landoria.FirstPerson) (MIT).
- **StartupSkip** : au lancement du jeu, saute les logos Coffee Stain et Iron Gate (`SkipLogos`). La vidéo
  d'introduction se désactive dans les options vanilla depuis le patch du 2026-09-17 (option `IntroVideo` retirée).
- **LoadingArt** : le fond des écrans de chargement (démarrage du jeu, chargement de partie, mort, sommeil,
  téléportation) est une image tirée au sort, différente de la précédente, dans le dossier `Folder` (`loading`,
  relatif à la DLL du mod : `BepInEx/plugins/Ovomium/loading/`, jpg/jpeg/png). Image entière, non déformée, bandes
  noires si le ratio diffère de l'écran. Couvre aussi l'écran « Loading » du menu et la connexion au serveur.
  `HideTeleportAnimation` (oui) masque l'animation vanilla de téléportation pour laisser voir l'artwork. Le jeu n'embarque aucun artwork exploitable : `valheim_art/` du dépôt en
  est la source, copiée par `tools/deploy.sh`. Sans dossier `loading/`, le mod télécharge au premier lancement le zip
  de la release GitHub « artworks » (`DownloadUrl` ; produit par `tools/package-art.sh` puis publié par
  `tools/release-art.sh`), pendant une fenêtre où le dépôt est public ; hors fenêtre, nouvel essai au lancement
  suivant (écrans vanilla en attendant).
- **SettingsMenu** : bouton « Ovomium » sous « Paramètres » dans le menu principal et le menu Échap. Il ouvre la
  fenêtre Paramètres du jeu avec, à la place des onglets vanilla, trois onglets (Cuisine, Interface, Jeu) listant les
  options du mod : bascule pour les oui/non, curseur pour les nombres ; Appliquer écrit le `.cfg`, Retour annule.
  Les options texte (glyphe, libellés, stations, dossier) restent dans le `.cfg`. Les options marquées
  « (au redémarrage) » n'agissent qu'au prochain lancement. Navigation à la manette non câblée pour ce bouton (les
  listes de navigation `FejdStartup.m_menuButtons` et celle de `Menu` sont laissées vanilla). `DumpHierarchy` (non)
  journalise la hiérarchie du prefab Paramètres à l'ouverture, pour le débogage.
- **AutoJoin** (outil de développement, `Enabled` : non) : au lancement du jeu, sélectionne le personnage et se
  connecte au serveur sans passer par les menus, par le chemin natif de `+connect` (un seul essai par lancement ;
  après une déconnexion, le menu revient vanilla). `Character` : nom ou fichier du personnage (vide : celui que le
  jeu sélectionne par défaut, le dernier utilisé). `Server` : nom d'un serveur des listes Favoris / Récents ou
  `hôte:port` (vide : dernier serveur rejoint ; aucun serveur connu → rien ne se passe, journal). Le mot de passe
  mémorisé par PasswordReveal est soumis tout seul. L'argument de ligne de commande `-ovomium-autojoin` force
  l'activation même si `Enabled` est non : c'est ce que passe `tools/deploy.sh --relaunch`
  (`steam -applaunch 892970 -ovomium-autojoin`).

## Prérequis

- podman (toute la chaîne .NET tourne dans le conteneur `ovomiam-build`, rien sur l'hôte)
- Valheim installé via Steam (`VALHEIM_DIR` pour un autre chemin que `~/.local/share/Steam/steamapps/common/Valheim`)
- BepInExPack_Valheim : `tools/install-bepinex.sh`, puis option de lancement Steam `./start_game_bepinex.sh %command%`

## Développement

```
podman build -t ovomiam-build -f tools/Containerfile tools   # une fois
tools/build.sh            # → Ovomium/bin/Release/net48/Ovomium.dll
tools/deploy.sh           # copie dans BepInEx/plugins/Ovomium/
tools/decompile.sh build/decompiled   # code du jeu décompilé, pour référence (non versionné)
```

Le csproj référence les DLL du jeu (publicisées) et `BepInEx.Core` 5.4.21 depuis `nuget.bepinex.dev`.
La version du mod se change uniquement dans `<Version>` du csproj (constante `PluginVersion.Value` générée au build).
Les patches Harmony précisent toujours les types d'arguments : la 1.0 a ajouté des surcharges, un patch ambigu fait échouer tout le plugin.

Vérification : `BepInEx/LogOutput.log` doit contenir `Ovomium <version> chargé`, puis, si `LogSortOrder = true`,
`$piece_cauldron trié :` à l'ouverture d'un chaudron.

## Distribution (Windows)

Releases GitHub sur `Delocca/valheim-ovomium` (nom du dépôt à garder identique dans `tools/release.sh`,
`tools/repo-visibility.sh` et `installer/Installer-Ovomium.bat`). Pas de Thunderstore. **Le dépôt est privé** sauf
pendant les fenêtres de mise à jour : Edia l'ouvre, prévient les amies, referme. Privé, l'API `releases/latest` répond
404 et l'installateur affiche « Les mises à jour ne sont pas ouvertes en ce moment ».

```
tools/package.sh                       # → build/dist/Ovomium-<version>-windows.zip (BepInEx inclus, sans artworks)
tools/package-art.sh [px max]          # → build/dist/Ovomium-loading.zip (artworks jpg 95 %, ~60 Mo)
tools/release-art.sh                   # publie/remplace ce zip dans la release « artworks » (téléchargée par le mod)
tools/release.sh --notes "…"           # build + package + tag v<version> + release avec le zip et l'installateur
tools/repo-visibility.sh public|private|status   # fenêtre de mise à jour (la release se fait dépôt privé)
```

Côté amies : télécharger `Installer-Ovomium.bat` depuis la dernière release et le lancer (Windows affiche un
avertissement de sécurité sur un `.bat` téléchargé : « Exécuter »). Il trouve Valheim via Steam, télécharge la
dernière release et l'installe ; relancer le même fichier met à jour (compare `BepInEx/plugins/Ovomium/version.txt`).
Ces scripts ont besoin du réseau (Thunderstore, GitHub) : à lancer hors bac à sable.
