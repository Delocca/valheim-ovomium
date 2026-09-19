# Journal de session

Une entrée par jour de travail, plus récent en haut : fait, décisions (avec leurs raisons), en suspens. Ce que git dit
déjà n'y va pas. Règles : `CLAUDE.md`, section « Suivi ».

## 2026-09-19 — Boucle de test rapide, rafale de features, suivi

- **Fait** : déploiement atomique jeu lancé + relance auto (`deploy.sh --relaunch`), AutoJoin, hot reload ScriptEngine
  avec `Unload()` par feature, autocontrôle des patches au chargement, puis FocusClick, ContinueButton, MapExplore,
  PortalRange, ButcherKnife, AmbientOcclusion, UpgradeDiff, SkillTooltip, TooltipStyle, SettingsMenu en direct. Tout
  validé en jeu par Edia. Mise en place de `TODO.md`, `CHANGELOG.md` et de ce journal ; `release.sh` prend ses notes
  dans le changelog.
- **Décisions** : Edia teste sur son serveur pendant que je code et ferme le jeu elle-même (jamais moi). Pipette de
  construction abandonnée : vanilla (Maj + clic molette, `Player.CopyPiece`). Graphismes vanilla en direct reportés à
  une session neuve. Apparition au login laissée vanilla (2026-09-17). ContinueButton en texte plutôt qu'icônes du
  jeu (pas assez parlantes).
- **En suspens** : release 0.9.0 (`TODO.md`).
