# Changelog

Notes de version destinées aux joueuses : un titre en français, une phrase, le nom de la section du fichier de config entre parenthèses. Dans chaque version, du plus important au moins important. La section « Non publié » devient la version courante à la release (`tools/release.sh` la date et la publie comme notes). Règles : `CLAUDE.md` « Suivi ».

## Non publié

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
