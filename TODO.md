# TODO

Chantiers restants, par priorité. Une entrée par chantier avec le contexte utile (décisions, ancrages dans `build/decompiled/`). Règles de maintenance : `CLAUDE.md`, section « Suivi ».

1. **Refermer le dépôt** (`tools/repo-visibility.sh private`, hors bac à sable) une fois les amies passées en 0.9.0 (publiée le 2026-09-20, dépôt public depuis).

2. **Deux emplacements rapides de plus** (Edia, 2026-09-19) : touches 9 et 0 en plus des 1 à 8 vanilla (barre d'objets `HotkeyBar`, `Player.UseHotbarItem`, cases de l'inventaire ; regarder les mods existants, ex. AzuExtendedPlayerInventory, QuickSlots).

3. **Inventaire / coffres**, trois features (Edia, 2026-09-19). Regarder les mods existants avant de coder (licences, ancrages) :
   - filtre du ramassage automatique (choisir quels objets sont ramassés) ;
   - rangement automatique vers les coffres proches contenant déjà l'objet (type QuickStack) ;
   - craft et construction puisant les ingrédients dans les coffres proches : **livré (CraftFromChests), validé en solo le 2026-09-19 (chaudron, marteau, piles bas-droite ; ordre coffres puis inventaire par défaut depuis le 2026-09-19, option `ChestsFirst`) ; réservation anticipée des coffres livrée (`ChestReservation`, demande `RPC_RequestOpen` vanilla à la propriétaire, réponse avalée), à valider sur le serveur avec une amie : coffre posé par elle, elle l'ouvre pendant que je crafte (le coffre doit être ignoré, sans message « en cours d'utilisation » ni fenêtre chez elle), on crafte toutes les deux depuis le même coffre (journal : lignes « repli, prise directe » à surveiller)**. Décisions d'Edia : rayon autour du joueur (50 m par défaut, max 150 : limite de chargement des zones), affichage « requis (total) », chariots et bateaux inclus. Limites assumées : recettes « un seul ingrédient au choix » (`m_requireOnlyOneIngredient`, `DoCrafting` choisit l'objet dans l'inventaire joueur) non couvertes sans exemplaire en inventaire ; deux crafteuses puisant dans le même coffre dans la même seconde (chacune réserve à son tour, la seconde retire sur une copie qui peut être vieille d'une seconde) ; fours, feux, fermenteurs non couverts (ancrages : `Smelter.OnAddOre` / `OnAddFuel`, `Fireplace.Interact`, `Fermenter.UseItem`, `CookingStation.OnAddFuelSwitch`, `Turret.UseItem`).

4. **FirstPerson phase 2 : corps visible** : caméra sur l'os tête, tête en ShadowsOnly ou os rétréci. Références : Landoria.FirstPerson (MIT), ImmersiveFirstPerson (GPL).
