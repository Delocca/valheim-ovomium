# Changelog

Notes de version destinées aux joueuses : un titre en français, une phrase, le nom de la section du fichier de config entre parenthèses. Dans chaque version, du plus important au moins important. La section « Non publié » devient la version courante à la release (`tools/release.sh` la date et la publie comme notes). Règles : `CLAUDE.md` « Suivi ».

## Non publié

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
