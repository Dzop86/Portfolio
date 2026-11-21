# Osmose : les familles des Mines de Sous-Racine (S225 et S226)

> **Version 1, 21 novembre 2026.** Conception du game designer, à remettre à Claude Code pour les sprints 225 (les Fouille-roc) et 226 (les Vers de roche), 4 points chacun. Plan validé par Charles : D102 (Q2 a), `docs/banc-groupes.md` §5 et §6.
> Gabarit suivi : `docs/familles-foret.md` (même structure), `docs/design-familles.md` §3 (la grammaire) et §4 (les tableaux). Rédigé après lecture de : `src/Rpg.Core/Effects.cs`, `Fight.cs` (passifs, invocations, `Hurt`, `SetStatus`, `MayFire`), `Ai.cs` (valeur des invocations et des états, `NeedsRescue`), `Templates.cs`, `Monsters.cs` (`MonsterGroups`, `Depth`, `Draw`), `Progression.cs` (`Kit`, `LevelGap`), `Data.cs` (contrôle des familles, des passifs et des invocations), `tools/spells_gen.py`, `tools/sets_gen.py`, `data/monsters.json` (`stag`, `golem`), `data/passives.json`, `data/summons.json`, `godot/Creatures.cs`, `tests/Rpg.Core.Tests/BalanceTests.cs`, `PartyBalanceTests.cs`, `UnderrootTests.cs`, `docs/banc-groupes.md`, `docs/jalon-3.md`, `docs/classes-liste.md`, `docs/chapitre-2-pistes.md` §5, `DECISIONS.md` (T96 à T99, T184 à T189, D102).
> Les noms et les descriptions seront relus par le scénariste : ils restent sobres et ne reprennent rien d'Ankama (D54).

## 0. Mode d'emploi pour Claude Code

- **Sources à modifier** (comme pour la forêt) : les sorts et les passifs dans `tools/spells_gen.py` (`S(...)`, `ranks=False`, critique 10 ; `P(...)`) ; les invocations dans `data/summons.json` ; les monstres dans `data/monsters.json` ; les panoplies dans `tools/sets_gen.py` ; les cartes de combat dans `data/maps/` ; les dessins dans `godot/Creatures.cs` ; les icônes dans `tools/icons_gen.py` et `credits.json`.
- **Conventions des tableaux de sorts** (ordre des arguments de `S(...)`) : id · nom fr / en · PA · portée min-max · ligne de vue · en ligne · dégâts min-max · lancers par tour · élément · relance (`cd`) · zone · effets. Tous les sorts sont **(new)**.
- **Écriture des effets** : `status(Affects, Stat, valeur, tours[, élément])`, `summon(id, Max)`, `swap(Allies)`, `leap()`, `pull(n)`, `heal(Allies, min, max)`. Le mot **« ouvre »** dans un tableau veut dire `status(Caster, Resistance, −20, 2)` (§4.3).
- **Les chiffres sont ceux des gabarits** de `Templates.cs` au niveau de chaque monstre. Le seul écart voulu est noté (§3.2, le Coup de pic à 4 PA). Ce sont des points de départ : chaque sprint se termine par la mesure T41 à dix classes, et on règle **d'abord les PV**, puis les leviers listés dans l'ordre (§3.9, §4.9). On ne touche à une mécanique qu'en le notant dans `DECISIONS.md`.
- Les points marqués **[À TRANCHER]** ont une valeur par défaut (§8) : l'utiliser et la noter dans `DECISIONS.md`.
- **Aucune mécanique neuve n'est demandée.** Tout ce qui suit existe déjà dans le moteur (§0.1). Une seule option moteur est proposée, hors des 4 points (§8, point 8).

### 0.1 Ce que la lecture du moteur change à la conception

1. **Une invocation de monstre se pose toujours à côté de son lanceur.** L'IA ne cherche une case que parmi les voisines du lanceur (`Ai`, boucle des sorts d'invocation). Une charge de poudre ne peut donc pas être lancée sous les pieds du héros : c'est le Boutefeu, au contact, qui la pose à côté de lui. Toute la famille des Fouille-roc est construite là-dessus.
2. **L'IA d'un monstre vaut une invocation 8 points**, quel que soit le reste. Un monstre qui peut frapper deux fois préfère toujours deux coups (environ 56) à un coup et une charge (environ 36). Le Boutefeu frappe donc **une seule fois par tour** (Coup de pic à 4 PA), et ses 2 PA restants vont à la charge.
3. **Les invocations meurent avec leur lanceur, et leur passif `Death` se déclenche quand même** (`Fight.Hurt` : la liste des morts comprend les invocations, puis chaque `Death` est mis en file). Tuer le Boutefeu fait donc sauter sa charge tout de suite, sauf si cette mort finit le combat. C'est une réponse du joueur, et un piège pour qui frappe au contact.
4. **Un passif `TurnStart` sans zone frappe son porteur** (précédent : `unstable`). Une charge de 2 PV qui perd 1 PV au début de chacun de ses tours saute au début de son **second** tour. Elle joue juste après son lanceur : le héros a donc **exactement un tour** pour réagir. Les dégâts d'une invocation sont ceux du jet nu : `Power` vaut 100 par défaut et elle n'a pas de caractéristiques.
5. **Le même état du même lanceur est remplacé, pas cumulé** (`Fight.SetStatus` : même `Stat`, même élément, même source). Un passif `TurnStart` qui pose « +20 % de résistances » et un sort du même monstre qui pose « −20 % de résistances » se remplacent l'un l'autre. C'est la mécanique des Vers (§4.3), sans une ligne de code.
6. **`AllyFalls` marche pour un monstre** (le « Pas aujourd'hui » du Paladin, T151) : un allié de la même équipe, pas une invocation, qui tomberait à portée reste à 1 PV. `OncePerTurn` le limite à une fois par tour du porteur (`Fighter.MayFire`). Il ne demande pas la même famille.
7. **`Still` se déclenche au premier tour** : `WalkedLastTurn` est faux tant que le porteur n'a pas joué. Un Fouille-roc frappe donc à +20 % dès son premier tour (§3.3). C'est accepté.
8. **L'IA ne prévoit aucun passif** (`design-familles.md` §2.5, point 3), ni pour les monstres ni pour les héros. Les héros de la simulation ne s'écartent pas d'une charge, et les Fouille-roc ne s'écartent pas des leurs. La mesure T41 est donc **pessimiste pour les héros de mêlée** et optimiste sur les dégâts que les Fouille-roc se font entre eux. On le note dans les Limites (§8, point 8).
9. **L'IA d'un monstre lance un échange avec un allié** seulement si cet allié a moins de la moitié de ses PV, un héros au contact, et moins de PV que le lanceur (`NeedsRescue`). C'est la règle du Ver de galerie (§4.2).
10. **Un état de résistance sur soi ne gêne pas l'IA** : elle le compte `0,05 × valeur × tours`, soit −2 pour « ouvre ». Les vers mordent donc normalement.
11. **Le glyphe posé sous un héros l'épargne à son prochain tour** depuis S152 (`Glyph.Spared`) : la limite §0.1, point 1 de la forêt est levée. Ces familles ne posent de toute façon aucun glyphe : les glyphes sont la leçon de la forêt.
12. **Les Sylvecerfs ont fini à +16 % de PV au-dessus du gabarit** après leur mesure (72 PV pour le Daguet au lieu de 62), les Gardiens au gabarit. Ils ont été réglés pour trois classes. On part du gabarit, en sachant que la mesure à dix classes peut aussi bien les baisser (le Prêtre et le Druide sont les plus faibles seuls, §2.6).
13. **`sets_gen.py` sait déjà faire des familles de 4 pièces**, un soutien sans pièce et un butin sans clef (S132). Il suffit d'ajouter les deux familles à `SETS` et à `PIECES` (§6.1).

---

## 1. Les deux familles en une page

| Famille | Zone (niv.) | Élément | Faible à | Leçon | Passif de famille |
|---|---|---|---|---|---|
| Fouille-roc (`delver`) | Mines de Sous-Racine (46) | Feu et Terre | **Terre** | **reculer à temps** : voir venir le coup annoncé (la charge), s'écarter d'un tour d'avance, la retourner contre eux | `dogged` (`TurnStart`, `Still`) |
| Vers de roche (`borer`) | Mines de Sous-Racine (46) | Terre | **Eau** | **choisir sa cible** : frapper le ver qui vient de mordre (ouvert), pas celui qui attend (fermé), et abattre d'abord celui qui retient les autres | `rock-hide` (`TurnStart`) |

**Le contraste voulu :** les Fouille-roc posent la question **où et quand** (où me tenir au prochain tour, quand reculer) ; les Vers posent la question **qui** (lequel frapper maintenant). Les premiers font bouger le joueur, les seconds le font viser. Le deck change aussi : Terre contre les Fouille-roc, qui résistent au Feu ; Eau contre les Vers, qui résistent à la Terre.

### 1.1 Les leçons déjà enseignées

| Famille | Leçon |
|---|---|
| Sangliers | ne pas s'aligner, se méfier des murs |
| Loups | séparer la meute |
| Champignons | le poison, finir à distance |
| Zombies | varier ses éléments, ne pas rester au contact |
| Grenouilles | le burst contre la régénération, les attirances |
| Crabes | contourner une armure |
| Abeilles | le tempo, ne pas réveiller l'essaim |
| Taupes | le terrain, l'ennemi qui surgit |
| Sylvecerfs | lire le sol (glyphes ennemis) |
| Gardiens moussus | déloger (glyphes alliés) |

**Ce qui est neuf ici :**
- **Fouille-roc** : la menace **à retardement**, posée par l'ennemi, qui touche aussi l'ennemi. Le sol des cerfs reste marqué ; une charge est un compte à rebours d'un tour, qu'on peut fuir, pousser vers eux, ou faire sauter de loin en tuant son poseur. C'est de l'anticipation, pas de la lecture du sol.
- **Vers** : la cible qui **change d'état** d'un tour à l'autre. Les crabes avaient une armure fixe à contourner ; ici, l'armure s'ouvre et se ferme, et le bon choix dépend de ce que chaque ver vient de faire. Le tank ajoute la question de l'ordre : le tuer d'abord coûte cher, l'ignorer coûte un coup de plus par tour.

### 1.2 Les faiblesses

Ce qui est placé aujourd'hui, sur dix familles :

| Élément | Familles faibles | Total |
|---|---|---|
| Air | sangliers, crabes, gardiens | 3 |
| Feu | loups, champignons, sylvecerfs | 3 |
| Eau | zombies, taupes | 2 |
| Terre | grenouilles, abeilles | 2 |

- **Fouille-roc → Terre** : ils creusent la roche et c'est elle qui les tue. Leur peur, c'est l'éboulement. Ils sont de Feu (poudre, lanternes) et frappent aussi en Terre au pic (comme les zombies frappent dans les quatre éléments).
- **Vers de roche → Eau** : l'eau use la pierre, et le ver avec ; leurs galeries se noient.
- **Résultat : 3 de chaque élément sur douze familles.** La règle « chaque élément est la faiblesse de deux familles » de `design-familles.md` §3 se retrouve au niveau du jeu entier, à trois par élément.
- **Pour les huit familles des premières zones des quatre régions** (`familles-foret.md` §1), rien ne change : les Mines sont une seconde zone. Si l'on veut la même règle pour les quatre secondes zones (Mines, Grottes, Ateliers, Archipel), il restera **Terre ×1, Eau ×1, Feu ×2, Air ×2** pour les six familles des trois autres.
- **[À TRANCHER] L'alternative :** Fouille-roc faibles à l'Eau (la poudre mouillée) et Vers faibles au Feu (la roche qui éclate). Plus évident à lire, mais le compte devient Feu 4, Eau 3, Air 3, Terre 2. **Par défaut : Terre et Eau.**

**Les chiffres de grammaire** (§3) : faiblesse **−15 %** pour les monstres, **−5 %** pour les chefs ; un tank qui protège ou contrôle ; un soutien qui repositionne ou renforce ; un chef à deux phases (`LowHp` à 50 %) avec un signe narratif.

---

## 2. Choix transverses

### 2.1 Les identifiants
- **`delver`** et **`borer`** : un seul mot anglais, pour `delver-melee` (allure), `dungeon-delver-1` (S227), `family-delver` (emblème), etc.
- `miner` est écarté : c'est l'id du métier de mineur (`jobs.json`), et le mélange serait sans fin. `digger` aussi : `digger-mole`, `digger-spawn` et `digger-boots` sont des taupes. `worm` attraperait `earthworm` (ressource de Clairval) dans chaque recherche, d'où `borer`.
- Les noms vus par le joueur : « Fouille-roc / Rockdelvers » et « Vers de roche / Rock Worms ».
- Ids neufs vérifiés libres dans `data/`, `tools/` et `src/` : sorts, passifs, invocations, objets, monstres et cartes de ce document. `tremor`, `surface`, `cave-in` et `dive` (l'Archer) sont déjà pris : on ne les réutilise pas.

### 2.2 Les niveaux [À TRANCHER]
- **Ce qui contraint :** toutes les cartes des Mines sont au niveau 46 (T189), et un groupe tire sa famille et ses rôles au hasard sur n'importe quelle carte : seule la taille dépend de la profondeur.
- **Ma proposition, comme la forêt (communs de 31 à 37, chefs à 45) :** communs de **46 à 52**, chefs à **60**.
  - Fouille-roc de 46 à 49 : on les rencontre d'abord.
  - Vers de 49 à 52.
- **Ce qui porte la zone jusqu'à 60 :** les hordes du fond, les deux chefs, les donjons (S227), les ressources 50 et 55 (T189) et l'artisanat (S228).
- **L'XP reste juste :** à 59, un monstre de 46 rapporte encore tout (pas de perte avant 20 niveaux d'écart, `LevelGap`).
- **La profondeur des Mines** (calculée depuis `ancestors-2-4`, en (6, 5)) : 4 cartes de profondeur 0 (`underroot-2-0`, `1-0`, `3-0`, `2-1`), 10 de profondeur 1, **11 de profondeur 2**. La zone a plus de fond que d'entrée : beaucoup de hordes.

### 2.3 Les gabarits (T96), recopiés pour chaque monstre

| Monstre | Rôle | Niv. | PV | Sort principal (jet) | Soin | Initiative (gabarit) |
|---|---|---|---|---|---|---|
| `cinder-flinger` | Ranged | 46 | 66 | 21-27 | aucun | 182 |
| `shotfirer` | Melee | 47 | 77 | 24-32 (à 3 PA) | aucun | 152 |
| `lamp-bearer` | Support | 48 | 80 | 24-32 | 21-27 | 132 |
| `timberman` | Tank | 49 | 96 | 24-32 | aucun | 100 |
| `stonelistener` | Boss | 60 | 765 | 48-62 | aucun | 150 |
| `grit-spitter` | Ranged | 49 | 69 | 22-28 | aucun | 189 |
| `stonegnawer` | Melee | 50 | 80 | 26-34 | aucun | 157 |
| `tunnel-worm` | Support | 51 | 83 | 25-33 | 22-28 | 136 |
| `millstone-worm` | Tank | 52 | 100 | 25-33 | aucun | 103 |
| `mountain-gnawer` | Boss | 60 | 765 | 48-62 | aucun | 150 |

- Les valeurs viennent de `Templates.Monster(rôle, niveau)` (divisions entières), le jet de `Roll` (moyenne plus ou moins un septième) et l'initiative de `Templates.Initiative`, que le chargement vérifie à 10 % près.
- Résistance à l'élément propre : 10 (25 pour le tank, 30 pour le chef).
- **La faiblesse des chefs : −5**, comme les huit chefs de Clairval et les deux de la forêt (le gabarit `Boss` porte −20, `familles-foret.md` §7, point 3).
- **Le seul écart voulu :** le Coup de pic du Boutefeu coûte 4 PA et ne se lance qu'une fois par tour (§0.1, point 2). Son jet est celui du gabarit à 3 PA multiplié par 4/3 : 37 de moyenne, soit **32-42**. La moyenne de famille de `FamilyTests` compte le sort principal : noter l'écart dans le test ou y compter les dégâts par PA.

### 2.4 Les panoplies de 4 pièces
- **Comme la forêt :** une pièce par monstre commun sauf le soutien (8 %), plus l'arme du chef (30 %). Bonus à 2, 3 et 4 pièces ; la pièce de 4 est l'arme de niveau 60.
- **Les deux panoplies se chevauchent par les mains** : celle des Fouille-roc a une arme à deux mains, celle des Vers une arme à une main et un bouclier. Le joueur choisit (§4.7) :
  - **Fouille-roc 4 + Vers 2** : du Feu et **+1 PM** (la mobilité qui sert à reculer) ;
  - **Vers 4 + Fouille-roc 3** : de la Vitalité, de la Terre et un peu de Feu.
- **Différence avec la forêt :** les deux panoplies ne donnent pas le même élément (Feu d'un côté, Terre de l'autre). Le choix suit le deck du joueur, ce qui le rend plus personnel. Aucune ne donne de PA.
- **Le kit « zone » à 46** ne trouve qu'**une** pièce avec de l'initiative (les Bottes de galerie, niveau 46) : les autres pièces sont de niveau 47 et plus. C'est accepté, comme la cape à 31 dans la forêt.

### 2.5 Les cartes de combat
- **Une carte nouvelle par famille** (§5) : `underroot-gallery` (« Galerie étayée », S225) et `underroot-pillars` (« Chambres et piliers », S226, du nom de la vraie technique de mine).
- **Partagées à partir de S226**, comme la forêt : `battles` vaut `[underroot-gallery, underroot-pillars]` pour `delver` et `[underroot-pillars, underroot-gallery]` pour `borer`. En S225, les Fouille-roc n'ont que la Galerie.
- **Les cartes des chefs** (`bossMap`), toutes deux de profondeur 2 (déjà vérifié par `UnderrootTests`) : `delver` en `underroot-0-4`, `borer` en `underroot-4-4`.
- **Arriver avec S226 change les groupes de S225.** `MonsterGroups.For` tire la famille de chaque groupe parmi celles de la zone : passer d'une famille à deux change le tirage de presque tous les groupes des Mines. Ce n'est pas un problème : la mesure T41 de S226 couvre toute la zone, et aucun joueur n'y est encore. Les échantillons de `PartyBalanceTests` se choisissent **après** S226.

### 2.6 Dix classes, pas trois
- **La règle :** chaque groupe ordinaire (1 à 3 monstres) se gagne **seul et nu** par **chacune des dix classes** au niveau 46. La mesure T41 de `BalanceTests` ne boucle aujourd'hui que sur l'Archer, le Guerrier et le Mage : il faut une ligne pour les dix (§3.9).
- **Les plus fragiles seuls** (banc du jalon 3, héros sans voie à 45, `docs/jalon-3.md`) : le **Prêtre** (29,8 points), le **Druide** (32,7) et le **Guerrier** (39,8). Le **Berserker** est le plus fort (72,7) : c'est lui qui risque de passer les plafonds des hordes.
- **Le banc de groupes** (D102, Q3) : le duo fait foi, le trio est un complément. Les chefs se mesurent avec `--parties` (§3.9).

---

## 3. S225 : les Fouille-roc (`delver`), Mines de Sous-Racine

*Narratif : les fouille-roc creusaient sous l'Arbre bien avant les mineurs d'Ormeval. Petits, trapus, une lanterne vissée au front, ils suivent un son que personne d'autre n'entend : la roche qui sonne (celle de Gaspard, et de `moved-mountains`). Ils ne remontent jamais. Quand une paroi leur résiste, ils la font sauter, et tant pis pour qui se tient à côté, eux compris. Le plus vieux colle l'oreille à la pierre et sourit.* (À relire par le scénariste : ce ne sont pas les mineurs de Berthe.)

**Leçon : reculer à temps.** Le Boutefeu pose une charge à côté de lui, au contact du héros ; elle saute au début de son second tour, à 2 cases, sur tout le monde. L'Étançonneur cale le héros (−1 PM) pour l'empêcher de partir. Un Fouille-roc qui n'a pas bougé frappe plus fort. Le joueur qui a compris :
- frappe, puis recule de 3 cases hors de la charge avant la fin de son tour ;
- ou pousse la charge vers eux ;
- ou abat le Boutefeu de loin, ce qui fait sauter sa charge tout de suite, au milieu des siens ;
- et oblige les Fouille-roc à marcher (ils perdent alors leur +20 %).

### 3.1 Les monstres

| Monstre | fr / en | Rôle | Niv. | PV | PA | PM | Init. | Allure |
|---|---|---|---|---|---|---|---|---|
| `shotfirer` | Boutefeu / Shotfirer | Melee | 47 | 77 | 6 | 3 | 152 | `delver-melee` |
| `cinder-flinger` | Jeteur d'escarbilles / Cinder Flinger | Ranged | 46 | 66 | 6 | 3 | 182 | `delver-ranged` |
| `timberman` | Étançonneur / Timberman | Tank | 49 | 96 | 6 | 3 | 100 | `delver-tank` |
| `lamp-bearer` | Porte-lanterne / Lamp Bearer | Support | 48 | 80 | 6 | 3 | 132 | `delver-support` |
| `stonelistener` | Écoute-roche / Stonelistener | Boss | 60 | 765 | 8 | 3 | 150 | `delver-boss` |

| Monstre | Sorts (dans l'ordre) | Résistances | Passifs |
|---|---|---|---|
| `shotfirer` | `pick-blow`, `powder-charge` | Fire 10, Earth −15 | `dogged` |
| `cinder-flinger` | `cinder-toss`, `cinder-spray` | Fire 10, Earth −15 | `dogged` |
| `timberman` | `prop-ram`, `wedging` | Fire 25, Water 10, Earth −15 | `dogged` |
| `lamp-bearer` | `swig`, `lamp-swing` | Fire 10, Earth −15 | `dogged` |
| `stonelistener` | `great-pick`, `powder-barrel`, `lamp-hurl` | Fire 30, Water 15, Air 15, Earth −5 | `dogged`, `ringing-stone` |

- **Tank qui contrôle** : l'Étançonneur cale (−1 PM au prochain tour du héros). Il a 3 PM, pas 2 : un tank lent laisserait le héros fuir la charge sans effort, et la leçon disparaîtrait.
- **Soutien qui renforce** : la Lampée soigne et donne +15 % de dégâts à l'allié soigné.
- **Le chef pose ses barils** et, à 50 %, deux charges à ses pieds.
- **Présence de la leçon en solo :** un groupe d'un monstre est un Boutefeu une fois sur trois (`Draw` : mêlée, distance ou tank). Un groupe de 2 ou 3 en contient un environ une fois sur deux à sept sur dix. Le Cale du tank et le passif `dogged` portent la leçon le reste du temps.

### 3.2 Les sorts

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `pick-blow` | Coup de pic / Pick Blow | 4 | 1-1 | oui | non | 32-42 | 1 | Earth | 0 | Point | aucun |
| `powder-charge` | Charge de poudre / Powder Charge | 2 | 1-1 | oui | non | 0-0 | 1 | Neutral | 1 | Point | summon(powder-keg, Max 1) |
| `cinder-toss` | Escarbilles / Cinders | 3 | 2-6 | oui | non | 21-27 | 2 | Fire | 0 | Point | aucun |
| `cinder-spray` | Gerbe d'escarbilles / Cinder Spray | 3 | 2-5 | oui | non | 12-16 | 1 | Fire | 2 | Circle 1 | aucun |
| `prop-ram` | Coup d'étai / Prop Ram | 3 | 1-1 | oui | non | 24-32 | 1 | Earth | 0 | Point | aucun |
| `wedging` | Calage / Wedging | 3 | 1-2 | oui | non | 14-18 | 1 | Earth | 0 | Point | status(Enemies, Mp, −1, 1) |
| `swig` | Lampée / Swig | 3 | 0-4 | oui | non | 0-0 | 1 | Neutral | 1 | Point | heal(Allies, 21, 27) ; status(Allies, Damage, +15, 1) |
| `lamp-swing` | Coup de lanterne / Lamp Swing | 3 | 1-2 | oui | non | 24-32 | 1 | Fire | 0 | Point | aucun |
| `great-pick` | Grand pic / Great Pick | 4 | 1-1 | oui | non | 48-62 | 1 | Earth | 0 | Point | aucun |
| `powder-barrel` | Baril de poudre / Powder Barrel | 3 | 1-1 | oui | non | 0-0 | 1 | Neutral | 1 | Point | summon(powder-barrel, Max 1) |
| `lamp-hurl` | Lanterne lancée / Hurled Lantern | 3 | 2-5 | oui | non | 26-32 | 1 | Fire | 2 | Point | aucun |

**Ce que fait chaque sort, vu du joueur et de l'IA :**
- **Le tour du Boutefeu** : 4 PA de Coup de pic et 2 PA de Charge. L'IA vaut le pic environ 37 et la charge 8 : avec 6 PA, elle fait les deux. Sans héros au contact, elle pose quand même sa charge (valeur fixe), à côté d'elle : les Fouille-roc qui passent à côté en prennent leur part.
- **Une charge à la fois par Boutefeu** (`Max 1`) : comme elle saute pendant le tour qui suit, il en pose une **un tour sur deux**. La relance de 1 le dit dans la fiche ; elle ne change rien d'autre.
- **Le Calage** : posé pendant le tour du tank, il compte au prochain tour du héros, qui garde 2 PM sur 3. Assez pour sortir d'une charge à 1 ou 2 cases s'il ne s'attarde pas. L'Étançonneur frappe une fois (Coup d'étai) et cale à chaque tour : son coup principal est à un lancer par tour pour que l'IA joue le Calage (§0.1, point 2).
- **La Lampée** : la seule façon pour le soutien de « renforcer » que l'IA joue vraiment (elle soigne quand un allié est blessé). L'état de dégâts suit le soin. Un soutien qui soigne une charge lui rend son PV (§3.4).
- **La Gerbe** ne se lance presque jamais seul (un coup de 14 contre deux de 24) : elle est faite pour les groupes de héros serrés.
- **La Lanterne lancée** du chef : relance 2, sinon l'IA la préférerait toujours au baril (29 contre 8) dès qu'un héros est à 2 cases ou plus. Avec 8 PA : pic et baril, ou pic et lanterne ; avec 10 PA (seconde phase), les trois.

### 3.3 Les passifs

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `dogged` | Acharné / Dogged | TurnStart | Still | status(Caster, Damage, +20, 1) |
| `ringing-stone` | La roche qui sonne / The Ringing Rock | LowHp | Percent 50 | status(Caster, Ap, +2, 10) ; status(Caster, Resistance, −10, 10) ; summon(powder-keg, Count 2) |

Descriptions (pour la fiche) :
- `dogged` : fr « S'il n'a pas marché à son tour précédent, il frappe à +20 % ce tour-ci. » ; en "If it did not walk on its turn before, it strikes for 20% more this turn."
- `ringing-stone` : fr « Sous la moitié de ses PV, il colle l'oreille à la paroi : la roche sonne. Il allume tout ce qu'il porte : +2 PA, −10 % de résistances jusqu'à la fin du combat, et deux charges à ses pieds. » ; en "Below half its HP, it presses its ear to the wall: the rock rings. It lights everything it carries: +2 AP, −10% resistances until the end of the fight, and two charges at its feet."

**Pourquoi Acharné :**
- Il sert la même leçon que la charge : **bouger**. Le héros qui recule après avoir frappé sort de la charge et oblige le Fouille-roc à marcher, donc à perdre son +20 %. Celui qui reste planté au contact prend les deux.
- Il ne retire rien à aucune classe : il récompense le déplacement, il ne punit pas un outil.
- L'état d'un tour, posé au début du tour de son porteur, compte ce tour-là (§2.1 de `design-familles.md`).
- Il se déclenche au premier tour (§0.1, point 7) : la première volée d'un groupe est toujours à +20 %. C'est le « coup d'ouverture » des mineurs ; les PV de départ du héros l'absorbent.
- Le Jeteur d'escarbilles, qui tire de loin et marche peu, l'a souvent. C'est voulu : contre lui, le héros de distance doit le faire bouger (sortir de sa vue) ou le tuer vite.

**Le signe narratif du chef** : à 50 %, il pose son pic et colle l'oreille à la paroi. La roche sonne, comme la carrière de Gaspard. Il sourit et allume tout. Le scénariste peut y ajouter une coupe de roche « à l'envers » derrière lui, qui prépare `moved-mountains`.

### 3.4 Les invocations : la charge et le baril (format de `design-familles.md` §4.8)

| Invocation | fr / en | Allure | Échelle | PV | PA | PM | Init. | Sorts | Passifs |
|---|---|---|---|---|---|---|---|---|---|
| `powder-keg` | Charge de poudre / Powder Keg | `delver-keg` | 100 | 2 | 0 | 0 | 0 | aucun (`[]`) | `short-fuse`, `keg-blast` |
| `powder-barrel` | Baril de poudre / Powder Barrel | `delver-barrel` | 100 | 2 | 0 | 0 | 0 | aucun (`[]`) | `short-fuse`, `barrel-blast` |

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `short-fuse` | Mèche / Fuse | TurnStart | dégâts 1-1 Neutral, sans zone (le porteur) | aucun |
| `keg-blast` | Coup de mine / Blast | Death | Circle 2, dégâts 20-26 Fire | aucun |
| `barrel-blast` | Grand coup de mine / Great Blast | Death | Circle 2, dégâts 34-42 Fire | aucun |

Descriptions :
- `short-fuse` : fr « La mèche brûle : perd 1 PV au début de chacun de ses tours. » ; en "The fuse burns: loses 1 HP at the start of each of its turns."
- `keg-blast` : fr « En sautant : 20 à 26 dégâts de Feu à 2 cases, sur tout le monde, Fouille-roc compris. » ; en "As it blows: 20 to 26 Fire damage 2 cells around, to everyone, rockdelvers included."
- `barrel-blast` : la même phrase avec 34 à 42.

**Le déroulé, tour par tour** (vérifié dans `Fight.Summon`, `Fight.Hurt` et `Fight.Resolve`) :
1. Tour N : le Boutefeu frappe et pose la charge sur une case voisine. Elle est insérée juste après lui dans l'ordre de jeu et joue aussitôt : la Mèche lui retire 1 PV (2 → 1). Le nom « Mèche » flotte au-dessus d'elle : c'est le compte à rebours visible.
2. Les héros jouent : **un seul tour** pour réagir.
3. Tour N+1 : le Boutefeu rejoue d'abord (il ne peut pas poser de seconde charge). Puis la charge : la Mèche la tue, et le Coup de mine frappe les 12 cases à 2 pas ou moins, alliés compris.

**Ce que le joueur peut faire :**
- **S'écarter à 3 cases** de la charge. Le héros au contact du Boutefeu est toujours à 2 cases ou moins d'une charge posée à côté de lui : rester, c'est la prendre.
- **La pousser** : elle n'est pas ancrée, une poussée la déplace (et la collision la tue : elle saute là où elle s'arrête).
- **La faire sauter** : la frapper (2 PV), ou tuer le Boutefeu, ce qui la tue avec lui et la fait sauter **tout de suite** (§0.1, point 3), sauf si c'est le dernier monstre (le combat finit avant).
- **Les retourner contre eux** : les Fouille-roc ne s'écartent jamais (l'IA ne prévoit pas les passifs). Un Boutefeu qui a posé sa charge reste à côté d'elle et la prend.

**Les cas limites, à couvrir par un test chacun :**
- Un **poison** ou un **glyphe** du héros sur la charge la tue au début de son premier tour : elle saute alors juste après le Boutefeu, **avant** le tour du héros, sans prévenir. C'est le Voleur (poisons de zone) et le Druide (glyphes) qui le feront le plus. Accepté : c'est le héros qui l'a provoqué, et la description de la Mèche suffit à le comprendre.
- Le **Porte-lanterne** peut soigner une charge (1 PV manquant, valeur 1 pour l'IA, très rare) : elle saute un tour plus tard. Accepté et noté (« il regarnit la mèche »).
- Le **Chronomancien** (Mage) peut retarder une charge dans l'ordre de jeu (`delay`) : elle saute plus tard. Une bonne réponse, à garder.
- Une charge ne laisse pas de **cadavre** (invocation) : rien pour le Nécromancien. Le Boutefeu, si.
- La charge n'a ni sort ni PA : vérifier que l'IA passe son tour sans erreur et que `summons.json` accepte `"spells": []`.

**Les chiffres :** le Coup de mine (20-26) vaut à peu près un coup principal du gabarit (24-32), une fois tous les deux tours, évitable, et qui touche aussi les Fouille-roc. Le Grand coup (34-42) vaut 70 % du Grand pic.

### 3.5 Le butin et les ressources

| Ressource | fr / en | Icône |
|---|---|---|
| `fuse-cord` | Cordeau / Fuse Cord | une mèche, game-icons.net (CC BY 3.0), choisie par `icons_gen.py` et inscrite dans `credits.json` |
| `lode-chip` | Éclat de filon / Lode Chip | un éclat de minerai, même source ; à distinguer de `humming-debris` (`faithtoken/ore`) |

Deux ressources de type `Resource`, sans niveau, comme `stone-antler`. Elles serviront à la clef du donjon (S227) et aux recettes de 50 à 60 (S228).

**Les ressources de la zone (T189) dans le butin.** Les Fouille-roc portent du minerai : un peu de **cobalt** (`cobalt-ore`, niveau 50) sur le Boutefeu et l'Étançonneur, de l'**or** (`gold-ore`, 55) sur le chef. Les taux restent bas pour ne pas faire concurrence au métier de mineur. [À TRANCHER] Par défaut : **10 % et 100 % ×1**.

| Monstre | Butin |
|---|---|
| `shotfirer` | `fuse-cord` 40 %, `lode-chip` 40 %, `cobalt-ore` 10 %, `lantern-helmet` 8 % |
| `cinder-flinger` | `lode-chip` 60 %, `gallery-boots` 8 % |
| `timberman` | `fuse-cord` 60 %, `cobalt-ore` 10 %, `prop-pauldrons` 8 % |
| `lamp-bearer` | `fuse-cord` 50 %, `lode-chip` 50 % (pas de pièce) |
| `stonelistener` | `lode-chip` 100 % ×2, `fuse-cord` 100 %, `gold-ore` 100 %, `listener-pick` 30 %, `lantern-helmet` 8 %, `gallery-boots` 8 %, `prop-pauldrons` 8 % |

La clef du donjon (2 %, 10 % pour le chef) s'ajoute en S227.

### 3.6 La panoplie `delver` : « Panoplie du fouille-roc » / "Rockdelver set"

| Pièce | fr / en | Emplacement | Niv. | Monstre | Caractéristiques en jeu | Budget (+1) |
|---|---|---|---|---|---|---|
| `gallery-boots` | Bottes de galerie / Gallery Boots | Boots | 46 | `cinder-flinger` | Feu 10, Vitalité 8, Initiative 30 | 27/27 |
| `lantern-helmet` | Casque à lanterne / Lantern Helmet | Hat | 47 | `shotfirer` | Feu 15, Vitalité 13 | 28/28 |
| `prop-pauldrons` | Épaulières d'étai / Prop Pauldrons | Shoulders | 49 | `timberman` | Vitalité 16, Feu 13 | 29/29 |
| `listener-pick` | Pic de l'Écoute-roche / Stonelistener's Pick | TwoHanded | 60 | `stonelistener` | Feu 40, Vitalité 28 | 68/68 |

| Pièces | Bonus en jeu |
|---|---|
| 2 | Feu 10 |
| 3 | Feu 10, Vitalité 10 |
| 4 | Feu 15, Vitalité 15, **+1 PM** |

- **Les règles appliquées :** `Templates.Budget` (moitié de 8 + niveau, le double à deux mains), un point d'initiative compte 0,3, l'initiative seulement sur les bottes, les capes et les anneaux.
- **Les Épaulières** : premier emplacement `Shoulders` d'une panoplie de famille, ce qui laisse la ceinture libre pour les autres.
- **Dans `sets_gen.py`** (`SCALE = 0.5`) : écrire le **double** des valeurs en jeu, sauf l'arme à deux mains (déjà comptée double par `scaled(..., two_handed)`) ; l'initiative telle quelle (§6.1).

### 3.7 La carte, les allures et le chef
- **Carte de combat** : `underroot-gallery` (§5.1). `battles: ["underroot-gallery"]` en S225, les deux cartes en S226. `bossMap: "underroot-0-4"`.
- **Allures** (`Creatures.cs`, fonction nouvelle `Delver(size, coat, …)`) : un petit corps trapu sur deux jambes, bâti sur le modèle de `Zombie` (tronc, tête, bras, `Legs`, `TwoLegs`), un casque avec une lanterne jaune sur le front, des yeux sombres.
  - `delver-melee` : 1,0, brun ocre, un pic (manche sombre, fer gris) ;
  - `delver-ranged` : 0,9, gris cendre, un sac d'escarbilles rouge sombre ;
  - `delver-tank` : 1,3, brun sombre, une poutre d'étai sur l'épaule ;
  - `delver-support` : 0,95, ocre clair, une grosse lanterne tenue devant lui ;
  - `delver-boss` : 1,6, presque noir, un grand pic, la couronne comme les autres chefs.
- **La charge et le baril** (fonction nouvelle `Keg(big)`) : un tonnelet brun cerclé de deux bandes sombres, une mèche rouge sur le dessus ; 0,4 de haut pour la charge, 0,6 pour le baril. Pas de pattes : `Animations([])` ne garde que la respiration du corps (la boucle des jambes ne tourne pas). `Kinds` reçoit `"delver"`, et les deux allures commencent par `delver-`, donc `Knows` les accepte.

### 3.8 Ce qu'il faut surveiller, classe par classe

| Classe | Ce qui l'aide | Ce qui la gêne | À surveiller dans la mesure |
|---|---|---|---|
| Guerrier | les poussées (pousser la charge), la Terre | il est au contact : charges et Calage ; l'IA ne recule pas | le plus exposé des trois mêlées sans soin |
| Paladin | sa solidité, ses protections | même chose que le Guerrier | rien de particulier |
| Berserker | ses gros coups tuent le Boutefeu vite ; le Drain | il paie en PV, et les charges s'ajoutent | les hordes (il est le plus fort seul, jalon 3) |
| Archer | la distance : les charges ne l'atteignent presque jamais | le Jeteur d'escarbilles toujours à +20 % | le plus à l'aise ; plafond des hordes |
| Mage | la distance ; le Chronomancien retarde une charge ; les Images attirent les coups | peu de PV | rien de particulier |
| Prêtre | ses soins absorbent les explosions ; la purge ôte l'Acharné et la Lampée | le plus faible seul (jalon 3) | **en premier** : ≥ 27 sur 30 ? |
| Voleur | la discrétion contre les tirs ; ses poisons tuent les charges | la discrétion ne protège pas d'une explosion (une zone de passif ne vise pas) ; ses poisons font sauter une charge un tour plus tôt | les groupes avec Boutefeu |
| Invocateur | ses créatures prennent les explosions à sa place | ses créatures au contact sautent | les groupes avec Boutefeu (créatures perdues) |
| Nécromancien | les Fouille-roc laissent des cadavres | la charge n'en laisse pas | rien de particulier |
| Druide | ses glyphes et ses formes | faible seul ; un glyphe sous une charge la fait sauter plus tôt | **en premier**, avec le Prêtre |

### 3.9 Fini quand (S225)
- **xUnit (CI) :**
  - le chargement accepte la famille (initiative, passifs, invocations sans sort ni PA) ; `FamilyTests` : `InlineData("delver", "dogged", Element.Earth, …)` (la seconde carte vient en S226) ; la moyenne de famille avec l'écart du Coup de pic noté ;
  - `APowderKeg_LaidBesideTheShotfirer_LosesAHitPoint_ThenBlowsAtItsSecondTurn` : 2 → 1 PV à son premier tour, explosion au second, 20 à 26 de Feu à 2 cases, Boutefeu compris ;
  - `APowderKeg_BlowsAtOnce_WhenItsShotfirerFalls_UnlessThatEndsTheFight` ;
  - `APowderKeg_Pushed_BlowsWhereItStops` et `APowderKeg_StruckOrPoisoned_BlowsAtOnce` ;
  - `Dogged_FiresOnTheFirstTurn_AndAfterAStillTurn_NotAfterAWalk` ;
  - `Wedging_TakesAMovementPoint_AtTheHerosNextTurn` ; `Swig_HealsAndStrengthensTheAlly` ;
  - `TheStonelistener_BelowHalf_GainsTwoActionPoints_LaysTwoKegsAtItsFeet` ;
  - `ItemBudgetTests` sur les 4 pièces ; les bonus à 2, 3 et 4 ; le soutien sans pièce, le chef avec les quatre et l'or ;
  - un combat avec charges qui se rejoue à l'identique (`FightRecord`).
- **T41 à dix classes** (`BalanceTests`, explicite, hors CI) : une théorie nouvelle `TheMines_SmallGroups_AreWonByEachOfTheTenClasses_TheHordesAreNot`, au niveau 46, héros nus :
  - **petits groupes** (1 à 3) : ≥ 27 sur 30 en moyenne **pour chaque classe**, au plus 2 groupes sous 18 par classe ;
  - **hordes** (6 et plus) : ≤ 3 sur 10 nus, ≤ 2 avec les kits « zone » et « craft », pour chaque classe ;
  - à 46, le kit « zone » ne trouve qu'une pièce avec de l'initiative (§2.4) : accepté, noté.
- **Un échantillon en CI** (comme `TheWallowsGreatHall`) : le petit groupe le plus dur des Fouille-roc, `Share(400)` combats répartis sur les dix classes, ≥ 80 % pour chacune.
- **Le chef :**
  - ≤ 10 % seul, pour chaque classe ;
  - `Rpg.Sim --parties` (100 combats par composition) : à trois ≥ 70 % sans composition sous 50 %, à quatre ≥ 90 % ;
  - la ligne `group:underroot-0-4.0` dans `PartyBalanceTests` (le chef est le groupe 0 de sa carte) ; les hordes de l'échantillon se choisissent après S226 (§2.5).
- **Godot :** l'auto-test dessine chaque allure de `monsters.json` et `summons.json`, charge et baril compris.
- **Leviers, dans l'ordre**, si la mesure sort des bornes :
  1. les PV ;
  2. les dégâts du Coup de mine (20-26 vers 16-20) ;
  3. le Calage (−1 PM vers aucun retrait, dégâts seuls) ;
  4. la portée du Coup de mine (2 cases vers 1, soit 4 cases au lieu de 12).
  
  Si les hordes deviennent trop faciles parce que les Fouille-roc se font sauter entre eux : la résistance au Feu de la famille de 10 à 25, **avant** de toucher aux PV.
- **Playtest** (question de `design-familles.md` §6) : « Il recule d'un tour d'avance quand une charge est posée, pousse une charge vers eux ou abat le Boutefeu de loin, et ne reste plus planté au contact d'un Fouille-roc. »

---

## 4. S226 : les Vers de roche (`borer`), Mines de Sous-Racine

*Narratif : les vers de roche mangent la pierre et rendent du gravier. Leurs galeries traversent les Mines dans tous les sens, et les mineurs s'en servent sans le dire. Leur cuir est de roche ; il ne s'ouvre que lorsqu'ils mordent. On dit que le plus grand a mangé le dessous d'une montagne : sa peau en porte les couches, à l'envers.*

**Leçon : choisir sa cible.** Au début de son tour, un ver se referme (+20 % de résistances). Quand il frappe, il s'ouvre (−20 %) jusqu'à son tour suivant : un écart de 40 points, la moitié des dégâts en plus. Le Ver-meule retient une fois par tour l'allié qui tomberait, et le Ver de galerie échange sa place avec un ver blessé. Le joueur qui a compris :
- frappe le ver qui vient de mordre, pas celui qui a soigné, plongé ou attendu ;
- tue le Ver-meule d'abord quand il est ouvert, ou finit les autres loin de lui (à 3 cases ou plus) ;
- et sort ses sorts d'Eau.

### 4.1 Les monstres

| Monstre | fr / en | Rôle | Niv. | PV | PA | PM | Init. | Allure |
|---|---|---|---|---|---|---|---|---|
| `stonegnawer` | Ronge-pierre / Stonegnawer | Melee | 50 | 80 | 6 | 3 | 157 | `borer-melee` |
| `grit-spitter` | Cracheur de gravier / Grit Spitter | Ranged | 49 | 69 | 6 | 3 | 189 | `borer-ranged` |
| `millstone-worm` | Ver-meule / Millstone Worm | Tank | 52 | 100 | 6 | 2 | 103 | `borer-tank` |
| `tunnel-worm` | Ver de galerie / Tunnel Worm | Support | 51 | 83 | 6 | 3 | 136 | `borer-support` |
| `mountain-gnawer` | Ronge-montagne / Mountain Gnawer | Boss | 60 | 765 | 8 | 3 | 150 | `borer-boss` |

| Monstre | Sorts (dans l'ordre) | Résistances | Passifs |
|---|---|---|---|
| `stonegnawer` | `stone-bite`, `burrow-dive` | Earth 10, Water −15 | `rock-hide` |
| `grit-spitter` | `grit-spit`, `burrow-dive` | Earth 10, Water −15 | `rock-hide` |
| `millstone-worm` | `millstone-crush`, `snatch` | Earth 25, Fire 10, Water −15 | `rock-hide`, `coils` |
| `tunnel-worm` | `rock-slime`, `tunnel-swap`, `tunnel-nip` | Earth 10, Water −15 | `rock-hide` |
| `mountain-gnawer` | `mountain-maw`, `engulf`, `deep-dive` | Earth 30, Fire 15, Air 15, Water −5 | `rock-hide`, `coils`, `strata` |

- **Tank qui protège** : le Ver-meule retient l'allié qui tomberait (`coils`). Il a 2 PM, la seule lenteur de la famille : il reste au milieu des siens, là où il protège.
- **Soutien qui repositionne** : la Galerie échange le Ver de galerie avec un ver blessé au contact d'un héros. Le ver blessé (souvent ouvert) part à l'abri ; le soutien, fermé s'il n'a pas mordu, prend sa place.
- Les résistances ci-dessus sont celles de la fiche ; `rock-hide` ajoute +20 ou −20 à **tous** les éléments selon l'état.
- **Le chef n'a pas de phase qui baisse ses résistances** : un état de résistance du chef serait remplacé à chaque début de tour par `rock-hide` (même source, §0.1, point 5). Sa seconde phase porte sur les dégâts et deux vermisseaux.

### 4.2 Les sorts

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `stone-bite` | Morsure de pierre / Stone Bite | 3 | 1-1 | oui | non | 26-34 | 2 | Earth | 0 | Point | ouvre |
| `burrow-dive` | Plongée / Burrow Dive | 2 | 2-4 | non | non | 0-0 | 1 | Neutral | 2 | Point | leap |
| `grit-spit` | Crachat de gravier / Grit Spit | 3 | 2-6 | oui | non | 22-28 | 2 | Earth | 0 | Point | ouvre |
| `millstone-crush` | Écrasement / Millstone Crush | 3 | 1-1 | oui | non | 25-33 | 2 | Earth | 0 | Point | ouvre |
| `snatch` | Happe / Snatch | 3 | 2-4 | oui | oui | 14-18 | 1 | Earth | 1 | Point | pull(Enemies, 2) ; ouvre |
| `rock-slime` | Bave de roche / Rock Slime | 3 | 0-4 | oui | non | 0-0 | 1 | Neutral | 1 | Point | heal(Allies, 22, 28) |
| `tunnel-swap` | Galerie / Tunnel | 2 | 1-5 | non | non | 0-0 | 1 | Neutral | 2 | Point | swap(Allies) |
| `tunnel-nip` | Morsure de galerie / Tunnel Nip | 3 | 1-2 | oui | non | 25-33 | 1 | Earth | 0 | Point | ouvre |
| `mountain-maw` | Gueule de montagne / Mountain Maw | 4 | 1-1 | oui | non | 48-62 | 1 | Earth | 0 | Point | ouvre |
| `engulf` | Engloutir / Engulf | 3 | 2-5 | oui | oui | 18-23 | 1 | Earth | 1 | Point | pull(Enemies, 2) ; ouvre |
| `deep-dive` | Plongée profonde / Deep Dive | 2 | 2-5 | non | non | 0-0 | 1 | Neutral | 2 | Point | leap |

« ouvre » = `status(Caster, Resistance, −20, 2)`, élément neutre (tous les éléments). Écriture dans `spells_gen.py` : `effects=[status("Caster","Resistance",-20,2)]`, après le `pull` quand il y en a un.

**Ce que fait chaque sort, vu du joueur et de l'IA :**
- **La règle est une seule phrase : un ver qui frappe s'ouvre.** Chaque sort de dégâts de la famille ouvre son lanceur. Le soin, la Galerie et les plongées ne l'ouvrent pas.
- **La Plongée** : le ver s'enfonce et ressort jusqu'à 4 cases plus loin, sans ligne de vue (comme `surface` des taupes). L'IA la prend si le meilleur plan depuis la case d'arrivée vaut 1,5 fois le plan sans sauter (`design-familles.md` §2.5). Un ver qui a plongé sans mordre reste **fermé** : c'est le cas que le joueur apprend à repérer.
- **Le Happe** du Ver-meule ramène un héros de 2 à 4 cases (en ligne) contre lui, au milieu de ses anneaux. Il ne sert que si personne n'est au contact (portée minimale 2) ; sinon le Ver-meule écrase deux fois.
- **La Galerie** : 8 points pour l'IA, lancée seulement si l'allié a moins de la moitié de ses PV, un héros au contact, et moins de PV que le Ver de galerie (§0.1, point 9). Avec 6 PA : Galerie et Morsure de galerie (5 PA), ou Bave et Morsure (6 PA).
- **Le chef** : Gueule (4) et Engloutir (3) dans le même tour, ou Gueule et Plongée profonde. Il est ouvert presque tout le temps : le chef est un mur qu'on attaque quand il vient de frapper, c'est-à-dire souvent, mais sa résistance de base (Terre 30) reste haute.

### 4.3 Les passifs

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `rock-hide` | Cuir de roche / Rock Hide | TurnStart | aucun | status(Caster, Resistance, +20, 2) |
| `coils` | Anneaux / Coils | AllyFalls | Range 2, OncePerTurn | aucun (l'allié reste à 1 PV) |
| `strata` | Couches à l'envers / Upturned Strata | LowHp | Percent 50 | status(Caster, Damage, +20, 10) ; summon(wormling, Count 2) |

Descriptions (pour la fiche) :
- `rock-hide` : fr « Au début de son tour, son cuir se referme : +20 % de résistances. Quand il frappe, il s'ouvre : −20 % jusqu'à son tour suivant. » ; en "At the start of its turn, its hide closes: +20% resistances. When it strikes, it opens: −20% until its next turn."
- `coils` : fr « Une fois par tour, un allié à 2 cases qui tomberait reste à 1 PV : le ver l'abrite dans ses anneaux. » ; en "Once a turn, an ally within 2 cells who would fall stays at 1 HP: the worm shelters it in its coils."
- `strata` : fr « Sous la moitié de ses PV, son cuir se fend sur des couches de roche à l'envers ; deux vermisseaux en sortent. +20 % de dégâts jusqu'à la fin du combat. » ; en "Below half its HP, its hide splits on layers of rock laid upside down; two wormlings crawl out. +20% damage until the end of the fight."

**Comment marche le cuir, sans code** (§0.1, point 5) :
1. Début du tour du ver : après les poisons et les glyphes, `rock-hide` pose +20 (2 tours). Il remplace l'état « ouvert » s'il y en avait un.
2. Le ver mord : le sort pose −20 (2 tours), qui **remplace** le +20 (même état, même élément neutre, même source).
3. Fin de son tour : il reste 1 tour à l'état. Les héros jouent contre un ver ouvert.
4. Début de son tour suivant : `rock-hide` le referme.

Un ver qui n'a pas mordu garde son +20 pendant les tours des héros (2 tours, renouvelés à chaque début de tour). **Les poisons et les glyphes frappent avant que le cuir se referme** : un ver qui a mordu les prend ouvert. Le Voleur et le Druide y gagnent.

**Ce que ça donne en chiffres** (le Ronge-pierre, Terre 10 et Eau −15) :

| État | Terre | Eau | Feu, Air |
|---|---|---|---|
| fermé | 30 % | 5 % | 20 % |
| ouvert | −10 % | −35 % | −20 % |

Un coup d'Eau sur un ver ouvert fait 135 % ; un coup de Terre sur un ver fermé, 70 %. Le choix de la cible pèse presque du simple au double.

**Pourquoi Anneaux sur le tank et le chef seulement :**
- Sur toute la famille, chaque ver retiendrait les autres : plus de PV pour tous, sans décision. Sur le tank, c'est une **question d'ordre** : le tuer d'abord (100 PV, Terre 25), ou payer un coup de plus par tour sur les autres, ou les finir à 3 cases de lui.
- `OncePerTurn` : une fois par tour **du Ver-meule**. Le héros seul joue une fois par tour : son premier coup mortel près du tank laisse la cible à 1 PV, le suivant la tue.
- Les invocations (vermisseaux, créatures) ne sont jamais retenues (`AllyFalls` exclut les invocations).
- Il ne peut pas se retenir lui-même.
- Les héros ont la même mécanique (« Pas aujourd'hui » du Paladin) : le joueur la reconnaît.

**Le signe narratif du chef** : à 50 %, son cuir se fend et montre des couches de roche posées à l'envers, comme les trois coupes de `moved-mountains`. Deux vermisseaux en sortent. Le joueur a déjà vu ces couches, ou les verra : on a posé une montagne sur quelque chose.

### 4.4 L'invocation : le vermisseau
Invocation `wormling` : Vermisseau / Wormling, allure `borer-melee`, scale 60, PV 50, PA 6, PM 3, initiative 100, sorts [`stone-bite`], sans passif.
- Il mord, donc il s'ouvre, mais il n'a pas de cuir pour se refermer : une fois qu'il a mordu, il reste à −20. C'est une cible facile, voulue.
- Il n'est jamais retenu par les Anneaux (invocation) et meurt avec le chef.
- Le levier de `design-familles.md` §9, question 3, s'applique : si le chef dépasse 10 % seul, on baisse ses PV, on ne retire pas les vermisseaux.

### 4.5 Le butin et les ressources

| Ressource | fr / en | Icône |
|---|---|---|
| `rock-slough` | Mue de roche / Rock Slough | une peau ou une écaille, game-icons.net (CC BY 3.0), choisie par `icons_gen.py` et inscrite dans `credits.json` |
| `borer-tooth` | Dent de ver / Worm Tooth | une dent, même source |

Pas de ressource de zone dans leur butin : le minerai reste aux Fouille-roc, et les vers ne gardent rien de ce qu'ils mangent.

| Monstre | Butin |
|---|---|
| `stonegnawer` | `borer-tooth` 40 %, `rock-slough` 40 %, `ringed-amulet` 8 % |
| `grit-spitter` | `rock-slough` 60 %, `slough-cape` 8 % |
| `millstone-worm` | `borer-tooth` 60 %, `millstone-shield` 8 % |
| `tunnel-worm` | `borer-tooth` 50 %, `rock-slough` 50 % (pas de pièce) |
| `mountain-gnawer` | `borer-tooth` 100 % ×2, `rock-slough` 100 %, `gnawer-mandible` 30 %, `ringed-amulet` 8 %, `slough-cape` 8 %, `millstone-shield` 8 % |

### 4.6 La panoplie `borer` : « Panoplie du ver de roche » / "Rock Worm set"

| Pièce | fr / en | Emplacement | Niv. | Monstre | Caractéristiques en jeu | Budget (+1) |
|---|---|---|---|---|---|---|
| `slough-cape` | Cape de mue / Slough Cape | Cape | 49 | `grit-spitter` | Terre 11, Vitalité 9, Initiative 30 | 29/29 |
| `ringed-amulet` | Amulette annelée / Ringed Amulet | Amulet | 50 | `stonegnawer` | Terre 16, Vitalité 13 | 29/29 |
| `millstone-shield` | Bouclier-meule / Millstone Shield | Shield | 52 | `millstone-worm` | Vitalité 17, Terre 13 | 30/30 |
| `gnawer-mandible` | Mandibule du Ronge-montagne / Mountain Gnawer's Mandible | OneHanded | 60 | `mountain-gnawer` | Terre 21, Vitalité 13 | 34/34 |

| Pièces | Bonus en jeu |
|---|---|
| 2 | Vitalité 10 |
| 3 | Vitalité 10, Terre 10 |
| 4 | Vitalité 30, Terre 15 |

Le bouclier porte plus de Vitalité que de Terre, comme celui des Gardiens.

### 4.7 Pourquoi les deux voies de panoplie se valent
Le calcul vaut une fois les deux panoplies livrées (S226), pièces et bonus compris :
- **Fouille-roc 4 + Vers 2** (cape et amulette ; l'arme à deux mains prend les deux mains) : **Feu 93, Terre 27, Vitalité 112, Initiative 60, +1 PM**, sur 6 pièces.
- **Vers 4 + Fouille-roc 3** (bottes, casque, épaulières) : **Terre 76, Feu 48, Vitalité 129, Initiative 60**, sur 7 pièces.

L'une est mono-Feu et mobile, l'autre bi-élément et solide. Dans une zone où l'on recule (Fouille-roc), le PM compte : c'est voulu. Si la mesure montre qu'une voie domine (le kit « fast » de la simulation prend toujours la même), on ajuste la Vitalité du bonus à 4 pièces, jamais le PM.

### 4.8 La carte, les allures et le chef
- **Carte de combat** : `underroot-pillars` (§5.2). `battles: ["underroot-pillars", "underroot-gallery"]`, et `underroot-pillars` s'ajoute aux Fouille-roc (§2.5). `bossMap: "underroot-4-4"`.
- **Allures** (`Creatures.cs`, fonction nouvelle `Borer(size, rock, …)`) : un corps de 5 anneaux en blocs, du plus gros (la tête, une gueule sombre devant) au plus petit (la queue), alignés le long de +z, chaque anneau un peu plus clair que le précédent. Pas de pattes : `Animations([])`, le corps respire et ondule.
  - `borer-melee` : 1,0, gris brun ;
  - `borer-ranged` : 0,9, gris clair, la tête relevée (le premier anneau plus haut) ;
  - `borer-tank` : 1,3, gris sombre, des anneaux larges et plats comme des meules ;
  - `borer-support` : 0,95, beige pâle, luisant (une bande plus claire sur le dos) ;
  - `borer-boss` : 1,6, presque noir, des bandes de couleurs en couches sur les anneaux, la couronne.
- `Kinds` reçoit `"borer"`. Le vermisseau reprend `borer-melee` à l'échelle 60.
- **[À TRANCHER] Un signe visible de l'état** (la tête relevée quand le ver est ouvert, par exemple) : ce serait du client, hors des 4 points. Par défaut : **la fiche au survol suffit** (l'état « Résistance −20 % » y paraît), et l'événement `StatusAdded` fait déjà flotter la valeur au-dessus du ver.

### 4.9 Ce qu'il faut surveiller, classe par classe

| Classe | Ce qui l'aide | Ce qui la gêne | À surveiller dans la mesure |
|---|---|---|---|
| Guerrier | les vers ouverts viennent au contact | les Anneaux (un coup de plus par tour) | rien de particulier |
| Paladin | sa solidité | même chose | rien de particulier |
| Berserker | ses gros coups sur un ver ouvert ; il finit à travers les Anneaux | rien | les hordes (plafond) |
| Archer | le Cracheur est ouvert dès qu'il tire | les Plongées rapprochent les vers ; le Happe le tire au contact | le Happe |
| Mage | la distance ; plusieurs éléments | même chose que l'Archer | rien de particulier |
| Prêtre | **la purge ôte le Cuir de roche** (un bonus d'ennemi) : elle ouvre un ver fermé | le plus faible seul | **en premier** ; vérifier que l'IA du Prêtre purge un ver fermé |
| Voleur | **ses poisons frappent avant que le cuir se referme** | la discrétion ne gêne pas un ver qui plonge à côté de lui | rien de particulier |
| Invocateur | ses créatures frappent les vers ouverts | les Anneaux ne retiennent pas ses créatures, mais retiennent les vers | rien de particulier |
| Nécromancien | des cadavres | les Anneaux retardent les morts, donc les cadavres | le tempo de ses premiers tours |
| Druide | ses glyphes frappent aussi avant le cuir | faible seul | **en premier**, avec le Prêtre |

**L'élément du héros pèse plus que d'habitude** : un héros dont le deck est surtout de Terre frappe les vers à 70 % quand ils sont fermés. C'est la leçon « changer de deck » de la forêt ; la mesure à 46 dira si un deck automatique de Terre tombe sous 27 sur 30.

### 4.10 Fini quand (S226)
- **xUnit :** comme au §3.9, plus :
  - `FamilyTests` : `InlineData("borer", "rock-hide", Element.Water, "underroot-gallery")`, et la seconde carte des Fouille-roc (`underroot-pillars`) ;
  - `AWorm_ClosesAtItsTurnStart_OpensWhenItStrikes_UntilItsNextTurn` (+20, puis −20, puis +20) ; `AWorm_ThatHealsOrDives_StaysClosed` ;
  - `APoisonOrAGlyph_StrikesAWormStillOpen_BeforeItsHideCloses` ;
  - `APurge_TakesAWormsHide` ;
  - `TheMillstoneWorm_HoldsOneFallingAllyATurn_WithinTwoCells_NotASummon_NotItself` ;
  - `TheTunnelWorm_SwapsOnlyWithAHurtAllyBesideAHero` ;
  - `TheMountainGnawer_BelowHalf_StrikesHarder_AndTwoWormlingsCrawlOut` ;
  - un combat avec vers, échanges et retenues qui se rejoue à l'identique.
- **T41 à dix classes sur toute la zone** (les deux familles, les deux cartes) : mêmes bornes qu'au §3.9. Le chef des Vers : ≤ 10 % seul, `--parties` à trois ≥ 70 % sans composition sous 50 %, à quatre ≥ 90 %, la ligne `group:underroot-4-4.0` dans `PartyBalanceTests`.
- **`PartyBalanceTests`** : l'échantillon des hordes des Mines (une de 6, une de 7 ou 8, une de 9 ou 10), choisi après la génération de S226.
- **Leviers, dans l'ordre** :
  1. les PV ;
  2. le Cuir de roche, de ±20 à ±15 ;
  3. les Anneaux, d'une fois par tour à une fois par combat (`PerFight 1`) ;
  4. la relance de la Galerie, de 2 à 3.
- **Playtest** : « Il frappe le ver qui vient de mordre plutôt que celui qui attend, tue le Ver-meule d'abord ou finit les autres loin de lui, et sort ses sorts d'Eau. »

---

## 5. Les cartes de combat (11 × 11, 4 places A, 10 places B, toutes reliées)

Légende du format existant : `.` sol, `#` obstacle, `~` trou (on tire par-dessus, on ne passe pas), `A` place des héros, `B` place des monstres. Comptes vérifiés à la main ; **à revérifier par le script** de `design-familles.md` §5.2.

### 5.1 `underroot-gallery` : « Galerie étayée » / « Propped Gallery » (S225)
Deux rangées d'étais (colonnes 4 et 6) tracent une galerie étroite au milieu, coupée en son centre par un puisard. On recule par les quatre passages (rangées 0, 3, 6, 9 et 10). Une charge posée dans la galerie la ferme ; un héros qui y recule mal s'y fait coincer. Les étais gênent les poussées, ce qui oblige à choisir l'angle pour renvoyer une charge.
```
...........
.A..#.#..B.
....#.#...B
.........B.
.A..#~#...B
....#~#..B.
.A........B
....#.#..B.
....#.#...B
.A.......B.
.........B.
```

### 5.2 `underroot-pillars` : « Chambres et piliers » / « Rooms and Pillars » (S226)
Onze piliers de roche isolés, en quinconce, comme une mine exploitée par chambres. Ils coupent les lignes de vue sans fermer les chemins : les vers plongent derrière, le Cracheur cherche son angle, la Galerie du soutien passe sans ligne de vue. Pour les Fouille-roc, beaucoup de place pour reculer, mais des piliers qui arrêtent une poussée de charge.
```
..#......B.
.A....#...B
...#.....B.
......#...B
.A.#.....B.
.....#....B
.A.#.....B.
......#...B
...#.....B.
.A....#...B
..#........
```

**Pour S227 (les donjons) :** la salle 2 de chaque donjon reprend la carte variante de sa famille, comme à Clairval et dans la forêt (`design-familles.md` §5.3).

---

## 6. Ce que les outils doivent apprendre

### 6.1 `tools/sets_gen.py`
Rien à changer dans le code : il sait faire 4 pièces, un soutien sans pièce et un butin sans clef depuis S132. Ajouter à `SETS` (le double des valeurs en jeu, l'arme à deux mains telle quelle) :
```python
    # Sprint 225: the Underroot Mines' rockdelvers, four pieces (docs/familles-mines.md §3.6); twice the values worn, the two-handed weapon once.
    ("delver", (text("Panoplie du fouille-roc", "Rockdelver set"),
                [({"fire": 20}, 0, 0, 0), ({"fire": 20, "vitality": 20}, 0, 0, 0), ({"fire": 30, "vitality": 30}, 0, 1, 0)],
                [piece("gallery-boots", "Bottes de galerie", "Gallery Boots", "Boots", 46, "cinder-flinger", {"fire": 20, "vitality": 16}, 30),
                 piece("lantern-helmet", "Casque à lanterne", "Lantern Helmet", "Hat", 47, "shotfirer", {"fire": 30, "vitality": 26}),
                 piece("prop-pauldrons", "Épaulières d'étai", "Prop Pauldrons", "Shoulders", 49, "timberman", {"vitality": 32, "fire": 26}),
                 piece("listener-pick", "Pic de l'Écoute-roche", "Stonelistener's Pick", "TwoHanded", 60, "stonelistener", {"fire": 40, "vitality": 28})])),
    # Sprint 226: the rock worms (docs/familles-mines.md §4.6), twice the values worn.
    ("borer", (text("Panoplie du ver de roche", "Rock Worm set"),
               [({"vitality": 20}, 0, 0, 0), ({"vitality": 20, "earth": 20}, 0, 0, 0), ({"vitality": 60, "earth": 30}, 0, 0, 0)],
               [piece("slough-cape", "Cape de mue", "Slough Cape", "Cape", 49, "grit-spitter", {"earth": 22, "vitality": 18}, 30),
                piece("ringed-amulet", "Amulette annelée", "Ringed Amulet", "Amulet", 50, "stonegnawer", {"earth": 32, "vitality": 26}),
                piece("millstone-shield", "Bouclier-meule", "Millstone Shield", "Shield", 52, "millstone-worm", {"vitality": 34, "earth": 26}),
                piece("gnawer-mandible", "Mandibule du Ronge-montagne", "Mountain Gnawer's Mandible", "OneHanded", 60, "mountain-gnawer", {"earth": 42, "vitality": 26})])),
```
et `PIECES = {"stag": 4, "golem": 4, "delver": 4, "borer": 4}`. Les fichiers des autres familles doivent ressortir à l'identique (`git diff` vide sur leurs entrées).

### 6.2 Les autres outils et données
- **`tools/spells_gen.py`** : les 22 sorts des §3.2 et §4.2 à la suite des Gardiens, sous deux commentaires `# The Underroot Mines' rockdelvers (sprint 225, docs/familles-mines.md §3)` et `# The rock worms (sprint 226, §4)`. Aucune fonction nouvelle : `summon`, `status`, `heal`, `pull`, `leap` et `swap` existent.
- **`data/passives.json`** : `dogged`, `ringing-stone`, `short-fuse`, `keg-blast`, `barrel-blast` (S225) ; `rock-hide`, `coils`, `strata` (S226). Les noms `explosion` et `unstable` existent déjà : ne pas les réutiliser.
- **`data/summons.json`** : `powder-keg`, `powder-barrel` (S225), `wormling` (S226).
- **`tools/icons_gen.py`** et `credits.json` : 4 ressources et 8 pièces.
- **`tools/item_art.py`** : une couleur par famille (par exemple ocre de lanterne pour `delver`, gris de pierre pour `borer`).
- **`tools/ui_art.py`** : l'emblème `family-delver` et `family-borer` (grisé tant qu'aucun n'est rencontré).
- **`data/achievements.json`** : `family-<id>-100` et `hunter-<id>`, comme pour `stag` et `golem`.
- **Le bestiaire** (`lore`) : une phrase par monstre et un paragraphe par famille, à relire par le scénariste. Propositions :
  - famille `delver` : fr « Les fouille-roc creusaient sous l'Arbre bien avant les mineurs d'Ormeval. Petits, trapus, une lanterne vissée au front, ils suivent un son que personne d'autre n'entend : la roche qui sonne. Quand une paroi leur résiste, ils la font sauter, et tant pis pour qui se tient à côté, eux compris. » ; en "The rockdelvers were digging under the Tree long before Ormeval's miners came. Small and stocky, a lantern screwed to the brow, they follow a sound no one else hears: the ringing of the rock. When a wall will not give, they blow it up, and too bad for whoever stands beside it, themselves included."
  - `shotfirer` : « Il ne regarde jamais la mèche : il écoute la roche. » / "It never watches the fuse: it listens to the rock." ; `cinder-flinger` : « Il jette ce qui reste au fond de sa lanterne. » / "It throws what is left at the bottom of its lantern." ; `timberman` : « Il cale tout ce qui bouge, la roche comme vous. » / "It wedges whatever moves, the rock as well as you." ; `lamp-bearer` : « Autour de sa lanterne, les autres creusent plus fort. » / "Around its lantern, the others dig harder." ; `stonelistener` : « Il colle l'oreille à la paroi et sourit : là-dessous, quelque chose sonne. » / "It presses its ear to the wall and smiles: beneath, something is ringing."
  - famille `borer` : fr « Les vers de roche mangent la pierre et rendent du gravier. Leurs galeries traversent les Mines dans tous les sens, et les mineurs s'en servent sans le dire. Leur cuir est de roche ; il ne s'ouvre que lorsqu'ils mordent. » ; en "Rock worms eat stone and leave gravel behind. Their tunnels cross the Mines every way, and the miners use them without saying so. Their hide is rock; it opens only when they bite."
  - `stonegnawer` : « Il ne mord que ce qui bouge ; le reste, il le mange. » / "It bites only what moves; the rest it eats." ; `grit-spitter` : « Il crache ce qu'il n'a pas digéré, assez fort pour fendre un casque. » / "It spits what it could not digest, hard enough to crack a helmet." ; `millstone-worm` : « Ses anneaux sont si lourds qu'un ver blessé s'y abrite. » / "Its coils are so heavy that a wounded worm shelters inside them." ; `tunnel-worm` : « Il connaît toutes les galeries, et y fait passer ceux qui saignent. » / "It knows every tunnel, and takes those who bleed through them." ; `mountain-gnawer` : « On dit qu'il a mangé le dessous d'une montagne ; sa peau en a les couches, à l'envers. » / "They say it ate the underside of a mountain; its hide has the layers, upside down."
- **`godot/Creatures.cs`** : `Delver`, `Keg` (S225), `Borer` (S226), et `Kinds` complété. Les portraits des monstres (T130) se refont avec les dessins.

---

## 7. Le compte des points

| Sprint | Contenu | Points |
|---|---|---|
| **S225, les Fouille-roc** | 11 sorts, 5 passifs, 2 invocations, 5 monstres, butin, panoplie, `underroot-gallery`, dessins `Delver` et `Keg`, icônes, tests ; mesure T41 à dix classes et réglage des PV | 4 |
| **S226, les Vers de roche** | 11 sorts, 3 passifs, 1 invocation, 5 monstres, butin, panoplie, `underroot-pillars`, dessin `Borer`, tests ; mesure T41 de toute la zone à dix classes, échantillons de `PartyBalanceTests` | 4 |

**Aucune mécanique neuve** : la charge à mèche (invocation, `TurnStart` sur soi, `Death`), le cuir qui s'ouvre (remplacement d'un état de même source), les Anneaux (`AllyFalls`), la Galerie (`swap`) et les Plongées (`leap`) existent et sont déjà testés ailleurs. Le risque des deux sprints est la **mesure à dix classes** (environ 9 000 combats pour les petits groupes de la zone, hors CI), pas le code.

---

## 8. Points incertains et valeurs par défaut

| # | Point | Valeur par défaut |
|---|---|---|
| 1 | Les faiblesses : Fouille-roc **Terre**, Vers **Eau** (3 de chaque élément sur douze familles, §1.2), ou l'alternative Eau et Feu, plus évidente à lire | **Terre et Eau** |
| 2 | L'élément des Fouille-roc : Feu (résistance Feu 10) et Terre au pic, alors que la région est de Terre | **comme écrit** ; si Charles veut des Fouille-roc de Terre, prendre l'alternative du point 1 |
| 3 | Les niveaux : communs de 46 à 52, chefs à 60 (§2.2) | **46 à 52 et 60** ; T41 mesuré au niveau des cartes (46) |
| 4 | Les ids `delver` et `borer` (§2.1), plutôt que `miner` (pris) et `worm` (mêlé à `earthworm`) | **comme écrit** |
| 5 | La faiblesse des chefs : −5 (comme les dix autres) ou −20 (gabarit `Boss`) | **−5** |
| 6 | Le Coup de pic à 4 PA, un lancer par tour, 32-42 : le seul écart au gabarit (§2.3) | **gardé** ; sinon l'IA ne pose jamais de charge (§0.1, point 2) |
| 7 | Les ressources de la zone dans le butin : cobalt 10 % sur deux Fouille-roc, or 100 % ×1 sur le chef (§3.5) | **oui** ; si un test des métiers exige qu'une ressource récoltée ne tombe d'aucun monstre, les retirer et le noter |
| 8 | **L'IA des héros ne voit pas les charges** (§0.1, point 8) : la mesure T41 est pessimiste pour la mêlée. Option moteur (environ 1 point, hors des 4) : un `KegRisk` sur le modèle de `GlyphRisk`, qui compte comme dangereuses, pour le tour suivant, les cases à 2 pas d'une invocation dont la Mèche est à 1 PV | **ne pas toucher à l'IA** : on règle sur la mesure telle quelle, avec les leviers du §3.9, et on note la limite dans `DECISIONS.md`. Reparler de l'option si le playtest trouve les Fouille-roc trop faciles une fois la leçon comprise |
| 9 | Les Fouille-roc se font sauter entre eux (friendly fire voulu) : les hordes peuvent devenir trop faciles | **gardé** ; levier : résistance Feu de la famille de 10 à 25, avant les PV (§3.9) |
| 10 | Une charge qu'un poison ou un glyphe tue saute un tour plus tôt, sans prévenir ; une charge soignée saute un tour plus tard (§3.4) | **acceptés** ; un test chacun |
| 11 | Acharné se déclenche au premier tour (§0.1, point 7) | **accepté** |
| 12 | Le Cuir de roche à ±20 | **±20** ; levier ±15 (§4.10) |
| 13 | Les Anneaux sur le tank et le chef seulement, une fois par tour (§4.3) | **comme écrit** ; levier une fois par combat |
| 14 | Le vermisseau (50 PV, `stone-bite`, sans cuir) | **comme écrit** |
| 15 | Les panoplies : 4 pièces, sans pièce pour le soutien, bonus à 2, 3 et 4 ; le kit « zone » ne trouve qu'une pièce avec de l'initiative à 46 (§2.4) | **comme écrit** et **accepté** |
| 16 | Trois dessins nouveaux (`Delver`, `Keg`, `Borer`) dans des sprints de 4 points | **des formes simples** sur le modèle de `Zombie` et `Golem` ; si le temps manque, reprendre `zombie-*` teinté (Fouille-roc) et `mole-*` teinté (Vers), et le noter |
| 17 | Un signe visible du ver ouvert dans le client (§4.8) | **la fiche et la valeur flottante suffisent** ; demander au client un signe si le playtest ne voit pas l'état |
| 18 | Les cartes partagées à partir de S226, qui changent les groupes de S225 (§2.5) | **comme écrit** ; échantillons de `PartyBalanceTests` choisis après S226 |
| 19 | La mesure T41 à dix classes (environ 9 000 combats) hors CI, avec un échantillon en CI par famille | **comme écrit** (§3.9) |
| 20 | Les clefs des donjons | **en S227**, avec les donjons (2 % sur les communs, 10 % sur les chefs) |
| 21 | Les textes (noms, descriptions, bestiaire) | **relus par le scénariste** avant d'entrer dans les données ; les ids ne changent pas |
