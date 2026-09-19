# Changelog

Changements visibles par le joueur, par version. La section « Non publié » devient la version courante à la release
(`tools/release.sh` la date et la publie comme notes). Règles : `CLAUDE.md`, section « Suivi ».

## Non publié

- MapExplore : rayon de découverte de la carte ×3 à pied et à cheval, ×5 en bateau.
- PortalRange : les portails ne s'activent (particules, son) qu'à 2 m au lieu de 5.
- ButcherKnife : le couteau de boucher n'abat que la créature visée.
- AmbientOcclusion : intensité de l'occlusion ambiante réglable (0,8 par défaut).
- UpgradeDiff : à l'amélioration d'un objet, différence de stats en vert/rouge à côté de chaque valeur.
- SkillTooltip : infobulle des compétences avec l'effet chiffré au niveau actuel et au niveau 100.
- TooltipStyle : infobulles cadrées, fond marron sombre opaque, coins arrondis et liseré.
- SettingsMenu : chaque réglage s'applique en direct, Retour restaure, la fenêtre s'efface pendant le glissement
  d'un curseur ; sections TooltipStyle et SkillTooltip.
- FocusClick : le clic qui redonne le focus à la fenêtre ne déclenche ni attaque, ni blocage, ni interaction, ni pose.
- ContinueButton : bouton « Continuer : <partie> serveur|local » au menu principal (dernière partie, dernier personnage).
- AutoJoin : connexion automatique au lancement avec `-ovomium-autojoin`.
- FirstPerson : caméra abaissée accroupi (hauteur fixe, transition 0,1 s).
- LoadingArt : artwork affiché dès l'écran « Loading » du menu (plus d'écran blanc), une seule image par chargement.
- Autocontrôle au chargement : un ancrage disparu ne désactive que sa fonctionnalité, signalée dans le journal.

## 0.8.0 — 2026-09-18

- LoadingArt : artworks de chargement téléchargés automatiquement (release GitHub « artworks »).
- PasswordReveal : bouton OK dans le dialogue de mot de passe serveur.
- Installateur : message « Les mises à jour ne sont pas ouvertes en ce moment » quand le dépôt est privé.

## 0.7.0 — 2026-09-17

- Mod renommé Ovomium.
- SettingsMenu : fenêtre d'options Ovomium dans le menu Paramètres (onglets, infobulles).
- Distribution Windows : archive avec BepInEx et installateur `Installer-Ovomium.bat`.

## 0.6.0 — 2026-09-17

- FirstPerson : vue à la première personne.
- StartupSkip : logos et attentes du démarrage sautés.
- LoadingArt : artwork sur les écrans de chargement.

Versions antérieures : voir l'historique git.
