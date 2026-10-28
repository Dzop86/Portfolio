# Osmose : les récompenses des succès, les suites de quêtes et les donjons (S159, D84)

> Game designer, 28 octobre 2026. Ce document fait suite à `suite-apres-d69.md` §12.4 (des succès sans puissance) : les récompenses restent de l'XP et des Éclats, jamais une caractéristique ni un objet.
> Lu avant d'écrire : `data/achievements.json` (63 succès, 810 points), `Achievements.cs`, `data/quests.json` (59 quêtes), `Progression.cs`, `Shop.cs`, `Contracts.cs`, `Dungeons.cs`, `Battles.cs`.

## 1. La formule

Chaque succès porte un **niveau de référence `L`**, un nouveau champ `level` de `achievements.json`. Ce niveau est fixe et inscrit dans les données (§1.2).

```
L'   = max(10, L)
XP     = ⌊ p × L' / 2 ⌋        (p = points du succès)
Éclats = ⌊ p × L' / 4 ⌋
```

- **À lire ainsi :** « un succès de p points vaut p/2 % d'un niveau à son niveau de référence ». Passer de L à L+1 coûte 100 × L d'XP (`XpToReach`).
- **Plancher à 10.** Sans lui, « Coup d'essai » rapporterait 2 Éclats. Ce plancher ajoute environ 460 à Σ p·L, ce qui est négligeable.
- **Une constante par monnaie** (`XpPerPointLevel = 0.5`, `ShardsPerPointLevel = 0.25`) dans `Achievements.cs`. On pourra les régler après le playtest sans toucher aux données.

### 1.1 Pourquoi ces coefficients (et pas p × L)

| Mesure | p × L (XP) | **p × L / 2 (retenu)** |
|---|---|---|
| Tous les succès (82, Σ p·L' ≈ 29 800) | 29 800 XP, 30 % de `XpToReach(45)` = 99 000 | **14 900 XP, 15 %** |
| Joueur de quêtes, ce qu'il obtient avant 45 (Σ p·L' ≈ 15 000) | +3 niveaux à 45 | **+1,4 niveau** (106 500 XP, niv. 46) |
| Pour comparer : les 59 quêtes rapportent | environ 65 000 XP | |

Pour les Éclats, le joueur de quêtes touche environ 15 000 Éclats de 1 à 45 (quêtes environ 11 000, combats environ 3 400, hors contrats).

- Avec p × L / 2, les succès lui en donneraient environ 7 500, soit **+50 %**. Ce serait de l'inflation, alors que §1.3 constatait déjà un surplus.
- Avec **p × L / 4**, ils en donnent environ 3 700, soit **+25 %**. Cela équivaut à 8 jours de contrats au niveau 30, ou à 3 h de combats sur tout le parcours.
- Le rapport Éclats/XP vaut 1/2. Il est plus généreux que celui des quêtes (1/6) : un succès se voit, et sa récompense en Éclats doit compter.

### 1.2 Le niveau de référence : tranché

| Option | Verdict |
|---|---|
| Niveau du personnage au moment de réclamer | **Non.** Attendre pour réclamer plus tard serait la stratégie dominante : un faux choix, et une file de succès non réclamés. |
| Niveau 1 pour tout succès sans zone | **Non.** « Mille fois debout » (20 pts) rapporterait 10 Éclats. |
| **Un niveau fixe dans les données**, dérivé d'une règle | **Oui.** Il est déterministe, affiché avant de réclamer et testable. |

Les règles, que les tests vérifient :

- **Suite de quêtes :** le niveau de sa quête la plus haute.
- **Donjon :** le niveau de sa quête `<id>-cleared`.
- **Famille :** le niveau de son donjon.
- **Méta-succès :** le maximum de ses sous-succès.
- **Métier N :** min(45, N/2).
- **Compteurs :** premier pas 5, palier moyen 15 à 25, dernier palier 45.
- **Arbre :** à partir de 30, son ouverture.

Les 63 succès existants (champ `level` à ajouter) :

| Succès | L |
|---|---|
| waystones-1 / -3 / -5 / -7 | 5 / 10 / 25 / 31 |
| waystone-woods / mouldy / lake / fields / ancestors | 5 / 10 / 17 / 25 / 31 |
| first-victory / victories-500 / victories-1000 | 5 / 25 / 45 |
| known-10 / known-25 / known-all | 10 / 25 / 45 |
| family-boar / wolf / mushroom / zombie / frog / crab / bee / mole / stag / golem | 9 / 10 / 15 / 17 / 24 / 25 / 29 / 29 / 44 / 45 |
| dungeon-* (mêmes familles, même ordre) | 9 / 10 / 15 / 17 / 24 / 25 / 29 / 29 / 44 / 45 |
| dungeons-all | 45 |
| job-20 / 40 / 60 / 80 / 100 | 10 / 20 / 30 / 40 / 45 |
| three-jobs-20, gatherer, maker | 10 |
| quests-10 / 25 / 50 | 5 / 15 / 35 |
| chapter-1 / chapter-2-slice | 30 / 41 |
| contracts-10 / 50 / 200 | 10 / 25 / 45 |
| first-talent / a-pair / a-heart / a-key / the-conversion | 30 / 30 / 35 / 40 / 45 |
| grouped / trader / guild-member / shared-10 / shared-50 / great-horde | 5 / 5 / 10 / 15 / 30 / 30 |

### 1.3 Exemples chiffrés

| Succès | p | L → L' | XP | Éclats | Pour situer |
|---|---|---|---|---|---|
| first-victory | 5 | 5 → 10 | 25 | 12 | le quart du niveau 1 → 2 ; un pain coûte 10 |
| clairval-first-steps (nouveau) | 10 | 10 | 50 | 25 | les 6 tutoriels rapportent 480 XP |
| dungeon-boar-wallow | 10 | 9 → 10 | 50 | 25 | la quête du donjon rapporte 900 XP et 135 Éclats |
| woods-all (méta) | 25 | 10 | 125 | 62 | 12,5 % d'un niveau à 10 |
| lake-main (nouveau) | 15 | 25 | 187 | 93 | « the-name-returned » rapporte 1 500 XP |
| dungeon-mother-tree-roots | 10 | 45 | 225 | 112 | une clef de ce donjon coûte 225 |
| ancestors-all (méta) | 25 | 45 | 562 | 281 | 12,5 % d'un niveau à 45 |
| contracts-200 (le plus gros du jeu) | 30 | 45 | 675 | 337 | 15 % d'un niveau à 45 |

**Invariant :** un succès rapporte toujours moins que la dernière quête qu'il couronne. Le plafond par réclamation vaut 675 XP et 337 Éclats.

## 2. Les nouveaux succès (19, 300 points ; total 82 succès, 1 110 points)

Deux nouveaux buts sont nécessaires, avec un nouveau champ `targets` (`string[]`) et `count = targets.length` (vérifié par un test) :

- **`Quests`** : `Reached` = le nombre de `targets` présents dans `QuestsDone`.
- **`Achievements`** (méta-succès, texte « Obtenez les succès suivants : », chaque cible cliquable vers sa tuile) : `Reached` = le nombre de `targets` obtenus.
  - Une cible n'est jamais un méta-succès, donc il n'y a pas de cycle (test).
  - Les cibles sont « obtenues », pas « réclamées ».

Tous les nouveaux succès sont dans la catégorie `Quests`, avec six nouveaux groupes : `clairval`, `woods`, `mouldy`, `lake`, `fields`, `ancestors`. Leurs libellés fr/en vont dans `Texts.cs`.

| id | groupe | goal | targets (ids de `quests.json` ou de succès) | count | p | L |
|---|---|---|---|---|---|---|
| clairval-first-steps | clairval | Quests | welcome, first-lesson, well-equipped, cellar-rats, palisade-stakes, roses-stall | 6 | 10 | 3 |
| clairval-four-trades | clairval | Quests | first-billhook, first-tunic, first-band, first-shoes | 4 | 10 | 3 |
| clairval-story | clairval | Quests | whispering-stone, the-registry | 2 | 10 | 4 |
| clairval-all | clairval | Achievements | clairval-first-steps, clairval-four-trades, clairval-story | 3 | 20 | 4 |
| woods-main | woods | Quests | edge-of-the-woods, restless-beasts, the-root-circle, rumble-under-the-wallow, four-signs | 5 | 15 | 9 |
| woods-side | woods | Quests | the-pack, winter-pelts, the-old-loner | 3 | 10 | 8 |
| woods-all | woods | Achievements | woods-main, woods-side, dungeon-boar-wallow, dungeon-wolf-den | 4 | 25 | 10 |
| mouldy-main | mouldy | Quests | what-grows-between, the-undug-crypt, down-into-the-crypt, the-cracked-seal | 4 | 15 | 16 |
| mouldy-side | mouldy | Quests | spores-for-the-stall, the-walking-dead, hermits-remedy | 3 | 10 | 13 |
| mouldy-all | mouldy | Achievements | mouldy-main, mouldy-side, dungeon-mushroom-cellar, dungeon-zombie-crypt | 4 | 25 | 17 |
| lake-main | lake | Quests | the-unlearned-lullaby, the-last-verse, the-washed-name, the-name-returned | 4 | 15 | 25 |
| lake-side | lake | Quests | frog-song, shells-and-claws, lianes-nets | 3 | 10 | 22 |
| lake-all | lake | Achievements | lake-main, lake-side, dungeon-frog-grotto, dungeon-crab-reef | 4 | 25 | 25 |
| fields-main | fields | Quests | burning-hives, under-the-hedges, the-disjoint | 3 | 15 | 30 |
| fields-side | fields | Quests | clairval-honey, under-the-furrows, harvest-bread | 3 | 10 | 30 |
| fields-all | fields | Achievements | fields-main, fields-side, dungeon-royal-hive, dungeon-mole-burrows | 4 | 25 | 30 |
| ancestors-main | ancestors | Quests | four-trails, where-roads-part, the-tree-town, the-unmapped-passage, mossy-keepers, roots-of-the-mother-tree | 6 | 15 | 41 |
| ancestors-side | ancestors | Quests | burning-ground, antler-wedges, the-bellow | 3 | 10 | 40 |
| ancestors-all | ancestors | Achievements | ancestors-main, ancestors-side, dungeon-bellowing-combe, dungeon-mother-tree-roots | 4 | 25 | 45 |

Les choix qui s'écartent de ta proposition :

- **`four-trails` et `where-roads-part` vont dans ancestors-main**, alors qu'elles n'ont pas de zone (donc Clairval). Ce sont des quêtes de niveau 30 qui mènent à Quatre-Chemins puis à la Forêt des Aïeux. Dans Clairval, elles repousseraient le méta-succès d'un village de départ au niveau 30.
- **Clairval n'a aucune quête secondaire**, donc pas de « clairval-side ». Le méta-succès de Clairval couronne 3 sous-succès.
- **`edge-of-the-woods`** (Tutorial, zone woods) va dans woods-main, pas dans « premiers pas » : ce dernier compte bien 6 tutoriels.
- **Invariant testé :** chaque quête de `quests.json` dont `kind` ≠ Dungeon figure dans **exactement un** succès `Quests`. Une quête ajoutée plus tard fait donc échouer le test tant qu'elle n'est pas rangée. Les quêtes Dungeon sont couvertes par les succès de donjon.
- Il reste des recouvrements voulus : `the-disjoint` compte pour chapter-1 et fields-main, `roots-of-the-mother-tree` pour chapter-2-slice et ancestors-main.
- Aucun nouveau titre : les 12 existants suffisent. Si on en veut, ancestors-all est le candidat.

## 3. Les donjons : réutiliser les 10 succès existants, ne pas en créer 10 de plus

**Constat :** les succès `dungeon-<id>` (but `Chief`, groupes `levels-1-15` / `16-30` / `31-45`, 10 pts) mesurent déjà « donjon fini ».

- Le chef n'apparaît que dans la 3e salle (`Dungeons.RolesIn` : `room == RoomCount` ajoute `Boss`), et aucune carte ne le place ailleurs.
- `Battles.cs:388` inscrit la victoire au bestiaire de **chaque** joueur du combat.
- Dix nouveaux succès « donjon fini » seraient donc des doublons exacts, avec deux tuiles par donjon.

**Il reste un vrai trou : le bestiaire date de S154** (migration `AddBestiary`, 27 oct.). Un personnage qui a fini un donjon avant ce sprint n'a pas le chef au bestiaire. La proposition :

- **Nouveau but `Dungeon`** (`target` = l'id du donjon), qui remplace `Chief` pour ces 10 succès. Il est atteint si le **chef est au bestiaire OU la quête `<target>-cleared` est rendue**.
  - Le bestiaire **est** la trace écrite à la fin de la 3e salle : aucune nouvelle colonne n'est nécessaire.
  - `dungeons-all` (`Chiefs`) utilise le même prédicat.
- **On garde les ids** : les titres portés (Fléau des donjons) et les succès déjà obtenus restent valides.
- Les textes deviennent « Finissez la Bauge du Roi sanglier : son chef vous attend au fond de la 3e salle. » (fr/en, par le scénariste).
- La story « finir un donjon est un succès » (1 pt) se réduit à ce changement de but et de texte, et aux tests.

## 4. Le bouton « Réclamer »

- **L'état :** une colonne `ClaimedAchievements` (liste d'ids, comme les autres listes du personnage) avec sa migration. Réclamable = **obtenu** (recalculé par le serveur) **et** absent de cette liste. Elle est vide pour tout le monde au déploiement : **les 63 succès (et les nouveaux) sont rétroactifs** sans script de migration.
- **Une seule fois, par personnage :** `POST /characters/{id}/achievements/{aid}/claim`.
  - La ligne du personnage est verrouillée, comme pour le coffre de guilde.
  - Le serveur ajoute l'XP (montées de niveau, points de caractéristiques et de sorts comme après un combat) et les Éclats, puis inscrit l'id, le tout dans une seule transaction.
  - Il répond 409 « Already claimed. » au deuxième appel et 400 « Not earned yet. » si le succès n'est pas obtenu.
  - Il refuse en combat (`InFight`).
- **« Tout réclamer »** (à ajouter, fortement recommandé) : la même transaction pour tous les succès réclamables, avec le total affiché. Sans ce bouton, un vétéran clique 60 fois.
- **L'XP est calculée au niveau de référence du succès, jamais à celui du personnage.** Le montant est affiché sur la tuile avant de réclamer (« 225 XP · 112 Éclats »). Une fois réclamé, la tuile indique « Réclamé ».
- **Un succès perdu après coup** (quitter la guilde, rendre ses talents) : ce qui a été réclamé reste acquis, et l'id inscrit empêche de réclamer deux fois. Un succès obtenu, non réclamé puis perdu redevient réclamable quand on l'obtient de nouveau.
- **Un repère dans le menu :** une pastille « n à réclamer » sur l'icône Succès.
- **Personnage au niveau maximal (100) :** l'XP est ignorée comme après un combat, les Éclats sont versés. C'est le cas de Plouf, compte d'administration, donc sans enjeu. `admin --bestiary` (bestiaire complet) rend réclamables les succès de chasse et de donjon de Plouf : c'est voulu pour les tests.

### Le vétéran de niveau 45 qui réclame tout d'un coup

Prenons un vétéran typique : 64 succès.

- Les 19 nouveaux, les 10 donjons et dungeons-all, les 2 chapitres, 3 succès de quêtes, 9 pierres de passage, 2 succès de victoires, 2 de bestiaire, 5 de l'arbre, 4 de groupe, 3 de métier et 4 de familles.
- Σ p·L' ≈ 18 900, soit **9 440 XP et 4 720 Éclats**.
- Depuis 101 000 XP (niveau 45), il arrive à 110 400 XP : **niveau 47** (`XpToReach(47)` = 108 100, `XpToReach(48)` = 112 800).

**Verdict : pas de plafond ni de conversion.**

- Une réclamation vaut au plus 675 XP, soit 15 % d'un niveau à 45.
- Le gain d'un coup (+2 niveaux) est celui qu'un nouveau joueur touche peu à peu (+1,4 niveau à 45). Il ne concerne que les personnages existants, une poignée.
- Une règle de conversion XP → Éclats ajouterait un cas, un test et une explication pour un événement unique.
- Si Charles veut quand même un garde-fou, le moins coûteux : « au-delà de 1 niveau gagné par une même réclamation groupée, le reste de l'XP est perdu ». Je ne le recommande pas.

## 5. Réserves

1. **Le coefficient XP est le plus sensible :** +1,4 niveau à 45 pour un joueur ordinaire. Les planchers et plafonds de T41 sont mesurés par niveau. Si les tests de Combe et de Racines deviennent trop faciles à 46, passer `XpPerPointLevel` à 0,25 (+0,7 niveau).
2. **Le farm sur des personnages secondaires :** les Éclats s'échangent (échange, coffre de guilde).
   - Un personnage secondaire qui fait Clairval (environ 30 min) réclame Coup d'essai, Premier passage, On compte sur vous, les 3 sous-succès de Clairval et le méta, soit environ **150 Éclats**.
   - 30 min de combats au niveau 30 en rapportent environ 640. Ce n'est pas rentable, et la règle tient tant que les succès de bas niveau restent à L' ≤ 10.
3. **Ajouter des succès plus tard redonne de l'XP aux vétérans :** c'est voulu. En revanche, **modifier les points ou le `level` d'un succès déjà réclamable** change sa valeur sans rattrapage pour ceux qui l'ont déjà réclamé. Il faut le noter dans DECISIONS.
4. **9 groupes dans la catégorie Quêtes** (general, story, contracts et les 6 zones) : la fenêtre de S158 doit les afficher sans défilement horizontal à 375 px. Un affichage replié par groupe suffit.
5. **`dungeons-all` (30 pts) fait doublon avec les 5 méta-succès de zone**, qui incluent déjà les donjons. Je le garde, parce qu'il porte un titre.
6. **Hors périmètre :** pas de vue par compte (tout est par personnage) et pas de récompense en objets. Les succès « dans un temps limité » restent exclus (§12.4 : pas de peur de manquer).

## 6. Tests (rappel des invariants)

- **`Reward(spec)`** : les exemples du §1.3 exactement ; un plafond ≤ 675 XP pour toutes les données.
- **Données :**
  - `count == targets.length` ;
  - les cibles existent ;
  - pas de méta-succès pointant vers un méta-succès ;
  - `level` respecte les règles du §1.2 ;
  - chaque quête non Dungeon apparaît dans exactement un succès `Quests`.
- **But `Dungeon`** : il est atteint par le chef au bestiaire seul, par la quête `-cleared` seule, et n'est pas atteint sans l'un ni l'autre.
- **Intégration PostgreSQL :**
  - réclamer, puis réclamer de nouveau donne 409 ;
  - réclamer un succès non obtenu donne 400 ;
  - les Éclats et l'XP sont crédités et une montée de niveau donne ses points ;
  - « Tout réclamer » est atomique ;
  - deux réclamations simultanées ne paient qu'une fois.
