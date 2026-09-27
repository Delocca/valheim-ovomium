# Changelog

Notes de version destinées aux joueuses : un titre en français, une phrase, le nom de la section du fichier de config entre parenthèses. Dans chaque version, du plus important au moins important. La section « Non publié » devient la version courante à la release (`tools/release.sh` la date et la publie comme notes). Règles : `CLAUDE.md` « Suivi ».

## Non publié

- **Trois objets utilitaires à la fois** : Megingjord, Bréchet et Lumière de feu-follet se portent ensemble et leurs effets s'additionnent ; équiper l'un ne retire plus les autres (UtilitySlots).
- **Éclairage sans bandes de couleur** : la lumière sur le décor, les constructions et les personnages passe en dégradé continu au lieu de marches de couleur, l'ambiance du jeu reste la même ; désactivé par défaut (pas encore essayé sous Windows), case « Ombrage lisse » de l'onglet Affichage de la fenêtre Ovomium, effet immédiat : si l'image devient noire ou rose, décochez-le (SmoothShading).
- **Fenêtre Ovomium réorganisée** : cinq onglets (Craft, Inventaire, Monde, Affichage, Menus), la case « Activé » de chaque fonctionnalité dans son titre et ses autres options grisées quand elle est décochée, les réglages fins repliés sous « Réglages avancés » ; le grappin, le bouton Continuer, le clic de retour au jeu et la table de préparation sombre y sont maintenant réglables (SettingsMenu).
- **Plus de faux « échec de la mise à jour »** : une mise à jour installée au menu s'annonçait ratée alors qu'elle avait réussi ; le bon message s'affiche désormais (Updater).
- **Coffres réservés plus vite à plusieurs** : quand un coffre change de mains pendant qu'on le demande, la demande suit tout de suite sa nouvelle propriétaire au lieu d'attendre, d'où moins de « Utilisé par quelqu'un d'autre » (CraftFromChests).
- **Table de préparation en bois sombre** : le bois de la table de préparation culinaire devient brun foncé (même texture, luminosité et saturation réglables), les ustensiles et aliments posés dessus gardent leurs couleurs ; visible chez vous seulement (DarkPrepTable).
- **Grappin plus pratique** : grappin accroché, un clic gauche vous tire jusqu'au point d'accroche, même depuis le sol ou vers un point plus bas ; et le champ de vision ne change plus pendant la traction (Grappling).

## 1.3.0 — 2026-09-25

- **Moins de « Utilisé par quelqu'un d'autre » à plusieurs** : le mod ne réserve plus tous les coffres à portée mais seulement ceux de la recette sélectionnée, de la pièce choisie au marteau ou du feu visé, et un coffre bloqué se libère deux fois plus vite : deux joueuses qui craftent ou construisent près de la même réserve se gênent beaucoup moins (CraftFromChests).
- **Eitr sans danger** : les particules de la raffinerie d'Eitr et de l'Eitr raffiné n'abîment plus les constructions et ne blessent plus personne, elles restent visibles (EitrRadiation).

## 1.2.0 — 2026-09-25

- **Mises à jour sans relancer le jeu** : au menu principal, une nouvelle version s'installe toute seule pendant qu'une fenêtre affiche les nouveautés, puis OK et c'est fini ; en partie, rien ne change (proposition au menu Échap, puis relance). Cette version-ci s'installe encore avec une relance, les suivantes non (Updater).

## 1.1.0 — 2026-09-24

- **Plus d'objets perdus à deux sur un même coffre** : ranger, fabriquer, construire ou recharger un feu en même temps qu'une autre joueuse depuis le même coffre ne perd plus rien, et si le coffre est occupé chez elle rien ne bouge et « Utilisé par quelqu'un d'autre » s'affiche (mise à jour à installer chez tout le monde) (QuickStash, CraftFromChests).
- **Serveurs Nodecraft réveillés tout seuls** : collez le lien de partage Nodecraft du serveur (app.nodecraft.com/shared/…) dans « Ajouter un serveur » : il arrive dans les favoris sous son nom, avec son état (en hibernation, démarrage, en ligne) ; s'il hiberne, le mod propose de le réveiller pour l'ajouter. En le rejoignant, un serveur en hibernation est démarré et la connexion part toute seule dès qu'il est prêt (Annuler pour arrêter l'attente). Si son adresse change, le favori suit (ServerWake).
- **Rangement rapide des grosses piles** : Ctrl + clic sur une pile qui ne rentre pas en entier dans le coffre le plus proche remplit ce coffre puis envoie le reste aux suivants qui contiennent déjà l'objet ; ce qui ne rentre nulle part reste dans l'inventaire au lieu d'être jeté (QuickStash).
- **Choisir ce que le ramassage auto ignore** : V sur un objet survolé dans l'inventaire (ou un coffre) ou visé au sol fait qu'il n'est plus ramassé automatiquement, un nouvel appui le réautorise ; ses cases portent un petit panneau « interdit » et il se ramasse toujours avec E (PickupFilter).
- **Voir voler les objets des amies** : les objets qu'une amie prend dans les coffres ou y range s'envolent aussi sous vos yeux, si elle a le mod à jour (ItemFlight).

## 1.0.1 — 2026-09-23

- **Fenêtre de mise à jour lisible** : fenêtre agrandie et texte à taille fixe (Updater).
- **Version du mod visible** : la fenêtre Ovomium des Paramètres a pour titre « Ovomium 1.0.0 » ; la ligne « mise à jour disponible » du menu principal est posée au-dessus de la version du jeu, elle sortait de l'écran (SettingsMenu, Updater).

## 1.0.0 — 2026-09-23

- **Rangement rapide** : inventaire ouvert, Ctrl + clic sur un objet (sans coffre ouvert) l'envoie dans le coffre le plus proche qui en contient déjà, et il y vole depuis sa case ; sans coffre qui convienne, il est jeté comme d'habitude (QuickStash).
- **Coffres manuels** : dans un coffre ouvert, le bouton « Objets similaires » devient « Coffre auto : oui / non » ; un coffre manuel est ignoré par le rangement rapide, la fabrication et le combustible depuis les coffres (réglage mémorisé sur le coffre) ; ranger les objets similaires se fait en maintenant E, sans fermer le coffre, un appui bref le ferme comme avant (ManualChest).
- **Ctrl pour l'ordre inverse** : Ctrl + clic sur Fabriquer ou pour poser une pièce, et Ctrl + E sur un feu, prennent les ingrédients dans l'ordre inverse du réglage (inventaire d'abord, ou coffres d'abord) ; le survol de Fabriquer et des feux le rappelle ; l'accroupissement (même touche) se fait au relâchement de Ctrl, et pas si Ctrl a servi (CraftFromChests).
- **Recharge des feux et fours depuis les coffres** : torches, lampes, braséros, feux de camp, fours, fourneau à charbon, fumoir et marmite se rechargent (E) avec la résine, le charbon ou le bois des coffres à portée, coffres d'abord ou inventaire d'abord selon le réglage ; le survol montre ce qu'on a sur soi et le total, l'objet vole du coffre au feu (CraftFromChests).
- **Emplacements rapides 9 et 0** : deux cases de l'inventaire s'utilisent par les touches 9 et 0 et apparaissent dans la barre d'objets ; inventaire ouvert, survoler une case et presser la touche la choisit comme cible (HotbarSlots).
- **Rangement de haut en bas** : les objets envoyés dans un coffre (Ctrl+clic, E maintenu, Tout empiler) prennent la première case libre en haut à gauche au lieu du bas, et complètent la pile la plus en haut à gauche ; dans l'inventaire aussi, la barre rapide restant servie en dernier (ChestFill).
- **Piles regroupées** : une nouvelle pile d'un objet déjà dans le coffre ou l'inventaire se pose au bout de la rangée de ses semblables (sinon de la colonne ; rangée pleine : ligne du dessous), un coffre rangé en colonnes étant traité en colonnes, la barre rapide restant à l'écart ; fonctionne aussi sans le rangement de haut en bas (ChestFill).
- **Mise à jour sans relance manuelle** : « Oui » dans la fenêtre de mise à jour télécharge puis relance le jeu de lui-même (sauvegarde faite si on est en partie, retour au menu principal), la nouvelle version étant installée à ce lancement (Updater).
- **Graphismes du jeu en direct** : dans l'onglet Graphismes du menu Paramètres, chaque réglage s'applique aussitôt pour juger de l'effet, et Annuler remet tout comme avant ; la fenêtre s'efface pendant le glissement d'un curseur (GraphicsPreview, toujours actif).
- **Fenêtre Ovomium** : la fenêtre ne s'efface plus pendant le glissement que pour les curseurs dont l'effet se voit à l'écran (SettingsMenu).

## 0.9.0 — 2026-09-20

- **Craft et construction depuis les coffres** : les ingrédients sont pris dans les coffres, chariots et bateaux à portée, le plus proche d'abord, puis dans l'inventaire (ordre inversable) ; chaque ingrédient affiche le total disponible (CraftFromChests).
- **Bouton « Continuer »** au menu principal : rejoint directement la dernière partie avec le dernier personnage (ContinueButton).
- **Carte découverte plus vite** : rayon de découverte ×3 à pied et à cheval, ×5 en bateau (MapExplore).
- **Comparaison à l'amélioration** : à la table d'amélioration, la différence de stats en vert/rouge à côté de chaque valeur (UpgradeDiff).
- **Infobulles des compétences** : effet chiffré de chaque compétence au niveau actuel et au niveau 100 (SkillTooltip).
- **Couteau de boucher plus sûr** : n'abat que la créature visée (ButcherKnife).
- **Portails plus discrets** : particules et son seulement à 2 m au lieu de 5 (PortalRange).
- **Clic de reprise de fenêtre inoffensif** : revenir dans le jeu par un clic ne déclenche plus d'attaque, de blocage, d'interaction ni de pose (FocusClick).
- **Vue première personne accroupie** : caméra abaissée à hauteur fixe avec une transition douce (FirstPerson).
- **Occlusion ambiante réglable** : intensité des ombres de contact, 0,8 par défaut (AmbientOcclusion).
- **Infobulles plus lisibles** : cadre à fond marron sombre opaque, coins arrondis, liseré (TooltipStyle).

## 0.8.0 — 2026-09-18

- **Artworks de chargement automatiques** : téléchargés une fois depuis GitHub (LoadingArt).
- **Bouton OK** dans le dialogue de mot de passe serveur (PasswordReveal).
- **Installateur** : message clair quand les mises à jour ne sont pas ouvertes.

## 0.7.0 — 2026-09-17

- **Fenêtre d'options Ovomium** dans le menu Paramètres, avec onglets et infobulles (SettingsMenu).
- **Installation Windows** : archive avec BepInEx et installateur `Installer-Ovomium.bat`.
- Mod renommé Ovomium.

## 0.6.0 — 2026-09-17

- **Vue à la première personne** (FirstPerson).
- **Démarrage accéléré** : logos et attentes sautés (StartupSkip).
- **Artworks sur les écrans de chargement** (LoadingArt).

Versions antérieures : voir l'historique git.
