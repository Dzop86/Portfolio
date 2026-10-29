# Osmose : talents et spécialisations de classe *(proposition à discuter, 23 octobre 2026)*

> Demande de Charles du 23 octobre 2026 (`docs/suite-apres-d69.md` §11.8) : « un arbre de passifs et de talents, et des spécialisations par classe », par exemple « un voleur qui devient acrobate ou assassin ». Référence donnée : le guide des classes de Dragonica. On s'en inspire en principes, on n'en reprend ni noms ni contenus (D54).
>
> **Ce document est une proposition de game design, pas un plan validé.** Rien n'est implémenté. Les noms des voies sont des **noms de travail neutres** (« Sentinelle, voie mobile ») : leur nom, leur sens dans le canon et leurs textes reviennent au scénariste (`docs/talents-canon.md`).
>
> Lu avant d'écrire : `data/classes.json`, `tools/spells_gen.py` (sorts, règle de budget, passifs), `src/Rpg.Core/Progression.cs`, `Hero.cs`, `Fight.cs` (passifs), `DECISIONS.md` (T27, T29, T32, T34, T35, T41, T49, T96 à T99), `tests/Rpg.Core.Tests/BalanceTests.cs` et `TemplateTests.cs`, `docs/design-familles.md` §2.2 et §7, `docs/suite-apres-d69.md` §4 et §11.

---

## 1. Ce qui existe

### 1.1 Osmose aujourd'hui

| Élément | État | Où |
|---|---|---|
| **Classes** | 3 : Sentinelle (distance, poussées, entraves), Garde (contact, attirance, boucliers), Mage (zones, soins, invocation). Toutes à 8 PA, 4 PM, 10 d'initiative ; PV de départ 62 / 74 / 86, +5 / +3 / +3 par niveau | `data/classes.json`, T34 |
| **Sorts** | 24 par classe : 4 par élément (Terre, Feu, Eau, Air) et 8 neutres (mobilité, placement, renfort, soin). 5 au niveau 1, puis 1 tous les 5 niveaux jusqu'au 95. Dégâts **calculés par une règle de budget** : 3 dégâts moyens par PA, +60 % au niveau 100, moins pour une zone, une poussée, un retrait de PA/PM, un poison, une longue portée ; plus au contact et par tour de relance | `tools/spells_gen.py`, D62, T27, T34 |
| **Rangs** | 5 rangs par sort de dégâts (×1,2 à ×1,8). 1 point de sort par niveau, un rang coûte 1, 3, 6, 10 points en tout : au niveau 100, 99 points montent une dizaine de sorts au rang 5 | `Progression.RankCost`, T29 |
| **Caractéristiques** | 10 points par niveau, Vitalité ou un élément ; 10 points = +2 dégâts dans cet élément. Refaire ses caractéristiques coûte **10 × niveau** Éclats (gratuit jusqu'au 10) | T29, T32, `suite-apres-d69.md` §4 |
| **Le deck** | 12 sorts au plus (deux rangées de six), de n'importe quels éléments, gardé par le serveur et rejoué avec le combat ; sans choix, les 12 premiers sorts débloqués. **Dès le niveau 40, chaque classe a plus de sorts que de places** : le deck est déjà un choix | `Hero.DeckSize`, T35, T49, T96 |
| **Passifs** | Réservés aux **monstres** : 5 déclencheurs (`TurnStart`, `Hit`, `LowHp`, `AllyDies`, `Death`), des effets de sort ordinaires (état, soin, bouclier, poussée, invocation, dégâts), une file résolue à la fin de chaque action, 3 au plus par monstre. Les héros n'en ont **aucun**, mais `FighterSpec` porte déjà une liste de passifs et `Fight` les résout pour tout combattant | `data/passives.json`, `Fight.cs`, design-familles §2.2 |
| **L'équilibre seul (T41)** | Contre les petits groupes (1 à 3 monstres) : moyenne **≥ 27 sur 30**, au plus 2 groupes sous 18 ; contre les hordes (6 à 10) : **≤ 3 sur 10** nu, **≤ 2 sur 10** avec un kit. Mesuré par classe, avec le héros simulé de `Progression.Built` | `BalanceTests.cs` |
| **Après 30 (T96)** | Les mêmes bornes aux niveaux 35, 40 et 45 sur des familles portées par les gabarits : petits groupes 29,1 à 29,8 sur 30, hordes 0 à 1,4 sur 10 avec le kit `craft`. **La Sentinelle à 45 est la plus juste** (2 petits groupes sous 18) | `TemplateTests.cs`, T96 |
| **Le contenu** | Niveaux 1 à 30 (chapitre 1), 30 à 45 en cours (Forêt des Aïeux, S128 à S136). Rien de 45 à 100. Le compte de test Plouf a **trois personnages de niveau 100**, un par classe | §11.6, mémoire « Compte Plouf » |

**Ce qui manque, vu du joueur :** après 30, monter de niveau donne des caractéristiques, un sort tous les 5 niveaux et des rangs. Deux Sentinelles de niveau 45 ne diffèrent que par leurs éléments, leur deck et leur équipement. Il n'y a **aucun moment de bascule** (« je deviens quelqu'un ») et **aucun rôle de groupe choisi** (le soin du Mage est subi, pas choisi), alors que le jeu est désormais un MMORPG où l'on se groupe.

### 1.2 Ce que Dragonica fait, en principes

> La page donnée par Charles (`playdragonica.eu/guide/class`) affiche une vérification anti-robot que l'outil de lecture n'a pas passée : ce résumé vient de la connaissance générale du jeu, en principes seulement, **à confirmer par Charles** qui l'a sous les yeux.

1. **Une classe de base, puis une bifurcation.** On joue d'abord une classe large ; vers le premier tiers du jeu, on choisit une branche parmi deux, qui fixe un style (protecteur ou offensif, mobile ou lourd). Des paliers suivants approfondissent la même branche, sans nouvelle bifurcation.
2. **On garde ce qu'on avait.** Les compétences de la classe de base restent ; la branche en ajoute et en renforce.
3. **La branche est un rôle de groupe** autant qu'un style de jeu seul.
4. **Le changement est un événement** : une quête, un nouveau titre, de nouvelles compétences d'un coup, et une remise à zéro des points offerte à ce moment-là.
5. **Ce qu'on ne reprend pas** : la multiplication des paliers (chacun coûte une pleine tranche de contenu), le costume par palier (l'apparence est reportée à la fin du jeu, D76), et les branches qui rendent un personnage injouable seul (Osmose se joue beaucoup seul, T41).

---

## 2. Trois modèles possibles

Hypothèses communes : 3 classes ; 2 voies par classe (6 voies) ; le chapitre 1 (niveaux 1 à 30) ne change pas.

### 2.1 Modèle A : une voie choisie, puis un arbre de talents à points

**Principe.** Au niveau 30, le héros choisit une voie. Il gagne ensuite **1 point de talent par niveau** (70 au niveau 100) à dépenser dans l'arbre de sa voie : une vingtaine de nœuds, la plupart à plusieurs rangs (« +3 % de dégâts de zone par rang, 5 rangs »), quelques nœuds clés au bout des branches.

| Critère | Évaluation |
|---|---|
| **Expérience du joueur** | Le plaisir du « +1 point » à chaque niveau, un arbre qui se remplit. Mais beaucoup de **points morts** (+3 %, +2 PV) que personne ne remarque en combat, et un « bon arbre » vite trouvé et copié : peu de vraies décisions pour beaucoup de clics |
| **Coût moteur** | Élevé : il faut un mécanisme de **bonus permanents** du héros (pourcentages par élément, PV, portée, relance) qui n'existe pas, branché dans `Fight.Damage`, `Foresee`, la fiche de caractéristiques et `Fight.HeroTotals` ; plus les passifs des héros |
| **Équilibre** | Le plus dur des trois : 70 points × 20 nœuds font un espace de combinaisons que `Progression.Built` ne sait pas parcourir. Chaque nœud ajoute de la **puissance pure** : il pousse les plafonds de T41 (hordes ≤ 2-3 sur 10) et rend les hordes faisables seul, contre la règle de Charles. Il faudrait remonter tous les gabarits de T96 |
| **Deck de 12** | Sans effet direct : les talents renforcent des familles de sorts, le deck reste le même |
| **Contenu** | ~20 nœuds × 6 voies = **120 nœuds**, 240 textes (fr, en), une interface d'arbre en Godot (la plus chère des trois) |
| **Personnages existants** | Les points se déduisent du niveau (comme les caractéristiques) : Plouf a 70 points d'un coup à répartir, ce qui est fastidieux mais sans perte |
| **Respec** | Indispensable (on se trompe dans un arbre) ; sur le modèle des caractéristiques, 10 à 20 × niveau Éclats |
| **Taille** | Environ **22 à 25 points** (moteur des bonus 5, passifs des héros 3, interface 5, contenu 3 × 3, réglage 3) |

### 2.2 Modèle B : des talents qui transforment les sorts existants, sans voie

**Principe.** Pas de spécialisation : à chaque palier (30, 45, 60…), le héros choisit une **variante** d'un de ses sorts parmi deux ou trois. Exemple : Pieu (4 PA, −2 PM) devient « Pieu lourd » (5 PA, −3 PM) ou « Pieu lancé » (portée +2, dégâts recalculés). Les variantes sont des sorts complets **écrits par `spells_gen.py` avec la règle de budget** : chaque variante est donc payée par construction (une poussée de plus coûte ses 15 %).

| Critère | Évaluation |
|---|---|
| **Expérience du joueur** | Riche pour qui aime composer son deck (« mon Pieu à moi »), invisible pour les autres. **Aucune bascule d'identité** : ce n'est pas le « voleur qui devient assassin » de Charles |
| **Coût moteur** | Faible : une table de substitution (sort de base → variante) appliquée quand on construit les sorts du héros pour un combat. L'IA, la prévision, les fiches de sort et le rejeu marchent tels quels, puisqu'une variante est un sort ordinaire |
| **Équilibre** | Le plus sûr : la règle de budget garde chaque variante à puissance égale ; T41 bouge peu |
| **Deck de 12** | Renforce le choix du deck (une variante attire le sort qu'elle modifie), sans le changer |
| **Contenu** | Potentiellement énorme (72 sorts × 2 variantes = 144) ; raisonnable si on se limite à 3 variantes par palier et par classe. Textes générés en partie depuis les données (`Texts.SpellCard` décrit déjà un sort) |
| **Personnages existants** | Rien à migrer ; Plouf choisit ses variantes |
| **Respec** | Peut être gratuit hors combat, comme le deck : ce sont des variantes à budget égal |
| **Taille** | Environ **11 à 14 points** |

### 2.3 Modèle C : des voies qui ouvrent des sorts nouveaux

**Principe.** Au niveau 30, le héros choisit une voie qui lui donne **4 à 6 sorts exclusifs**, débloqués entre 30 et 80, en plus des 24 de sa classe.

| Critère | Évaluation |
|---|---|
| **Expérience du joueur** | La bascule la plus forte (« j'ai des sorts que l'autre Sentinelle n'a pas »), la plus proche de Dragonica |
| **Coût moteur** | Moyen à élevé : les sorts de voie appellent des **effets nouveaux** pour se distinguer (marque, invisibilité, renvoi de dégâts), chacun avec son code d'IA ; l'invisibilité est déjà prévue pour la région de l'Air (design-familles §7) |
| **Équilibre** | Tenable par la règle de budget pour les dégâts ; les effets nouveaux sont à régler un par un |
| **Deck de 12** | La pression monte : 24 + 6 = 30 sorts pour 12 places. Sans place de plus, le joueur sacrifie des sorts de classe ; avec une place de plus, il faut refaire la barre (deux rangées de six) et rééquilibrer |
| **Contenu** | **24 à 36 sorts**, leurs icônes (game-icons.net), 48 à 72 textes, les effets nouveaux ; et le gros de ces sorts tombe entre 45 et 80, où il n'y a pas encore de contenu |
| **Personnages existants** | Plouf (100) reçoit ses 6 sorts d'un coup : sans perte |
| **Respec** | Coûteux par nature (on perd des sorts et leurs rangs) ; les rangs reviennent comme pour un sort disparu (`RankList`, T34) |
| **Taille** | Environ **25 à 30 points** |

### 2.4 Synthèse

| | A : arbre à points | B : variantes de sorts | C : sorts de voie |
|---|---|---|---|
| Bascule d'identité | moyenne | nulle | forte |
| Décisions réelles | peu (beaucoup de petits nœuds) | beaucoup | une seule, forte |
| Coût moteur | élevé | faible | moyen à élevé |
| Risque sur T41 et T96 | **élevé** (puissance pure) | faible | moyen |
| Contenu | 120 nœuds | 20 à 40 variantes | 24 à 36 sorts |
| Points | 22 à 25 | 11 à 14 | 25 à 30 |

Aucun ne répond seul à la demande : A coûte cher et casse l'équilibre, B n'a pas d'identité, C demande du contenu qui n'existe pas encore au-delà de 45.

---

## 3. Recommandation : un modèle hybride, « une voie, une signature, une rangée de talents tous les 15 niveaux »

### 3.1 Le principe

- **Au niveau 30**, après « Quatre pistes » (la fin du chapitre 1, quand le joueur arrive à Quatre-Chemins, « là où les routes se séparent »), le héros choisit **une voie parmi deux**. Le thème du lieu porte déjà la bifurcation ; le scénariste dira qui l'y aide.
- **La voie donne d'un coup sa signature** (le moment « je deviens… ») :
  - **un passif de voie**, toujours actif, écrit avec le moteur des passifs des monstres (aucun déclencheur nouveau pour la tranche verticale) ;
  - **un sort de voie**, un seul, réglé par la règle de budget, qui entre dans le grimoire et se met dans le deck comme les autres (le deck reste à 12 : c'est un choix de plus, pas une place de plus) ;
  - **un titre** sous le pseudo (l'apparence attend la fin du jeu, D76).
- **Ensuite, une rangée de talents tous les 15 niveaux : 30, 45, 60, 75, 90.** Chaque rangée propose **3 talents, on en prend 1**. Un talent est soit un **passif** (modèle A, mais sans bonus permanents : des déclencheurs et des effets qui existent), soit une **variante de sort** (modèle B, à budget égal). Au niveau 100, un héros a donc sa voie, son passif, son sort et **5 talents** : 3⁵ = 243 combinaisons par voie, chacune lisible.
- **Rien avant 30.** Le chapitre 1 et tous ses tests d'équilibre restent tels quels.

**Pourquoi des rangées à choix plutôt qu'un arbre à points :** chaque choix se voit en combat (pas de +3 %), on ne peut pas tout prendre (la décision reste vraie au niveau 100), et chaque rangée se règle à part : ses trois talents doivent avoir le même poids, ce que le simulateur mesure en trois passes. C'est la forme la plus économe en contenu (3 talents par voie et par palier) et la seule qui se teste en CI sans exploser.

**Pourquoi un seul sort de voie :** il suffit à la bascule et ne gonfle pas le deck. Les sorts de voie plus nombreux (modèle C) pourront venir avec les chapitres suivants, palier par palier, si le playtest en redemande.

### 3.2 Les six voies, en intention (noms de travail)

Les voies suivent un **axe de rôle**, jamais un élément : le héros garde ses éléments, choisis par ses caractéristiques (T29). Une voie qui imposerait un élément ferait doublon avec les caractéristiques et casserait les builds existants.

| Classe | Voie (nom de travail) | Fantaisie du joueur | Passif de voie (moteur existant) | Sort de voie |
|---|---|---|---|---|
| Sentinelle | **voie mobile** (l'« acrobate » de Charles) | « on ne me touche pas » : je bondis, je repars | frappée au contact, gagne **2 PM** pendant 1 tour (`Hit`, `melee`, comme `scamper`) | **Bond** : 2 PA, portée 2 à 4, saut (`leap`), relance 1, sans dégâts |
| Sentinelle | **voie de précision** (l'« assassin ») | « un tir, une cible » | au début de son tour, **+15 % de dégâts et −1 PM** pendant 1 tour (`TurnStart` : le tireur posé, un échange, pas un gain) | **Tir visé** : 5 PA, portée 4 à 9, en vue, une cible, relance 2 ; au budget, 20 à 27 dégâts au rang 1 au niveau 30 |
| Garde | **voie du rempart** | « derrière moi » : je protège le groupe | au début de son tour, **bouclier de 6** aux alliés à 1 case (`TurnStart`, zone `Circle 1`, `Allies`) | **Interposition** : 2 PA, portée 1 à 3, échange de place avec un allié et bouclier de 10 sur soi, relance 2 (`swap` + `shield`, comme `intercept`) |
| Garde | **voie de la fureur** | « plus je saigne, plus je frappe » | sous la moitié de ses PV, **+25 % de dégâts et +1 PM** jusqu'à la fin du combat, une fois (`LowHp` 50, comme `overheat`) | **Frappe vorace** : 4 PA, contact, une cible, dégâts au budget et soin de soi de 30 % de ce budget, relance 1 |
| Mage | **voie du soin** | « tant que je suis debout, personne ne tombe » | au début de son tour, **soigne de 4** les alliés à 2 cases, lui compris (`TurnStart`, `Circle 2`, `Allies`, comme `damp-skin` en zone) | **Halo** : 4 PA, portée 0 à 4, bouclier de 15 sur un allié et +1 PM, relance 2 |
| Mage | **voie des ravages** | « la zone entière brûle » | frappée, **+10 % de dégâts** pendant 1 tour (`Hit`) ; *variante à discuter :* un déclencheur nouveau « à chaque ennemi tué, +1 PA » (S140) | **Onde** : 5 PA, portée 2 à 5, zone `Cross 2`, dégâts au budget (×0,6), relance 1 |

**Règle d'équilibre des voies :** chaque voie, **même la voie du soin**, doit tenir seule les planchers de T41 (≥ 27 sur 30 contre les petits groupes) ; aucune ne doit passer les plafonds (hordes ≤ 3 sur 10 nu, ≤ 2 avec un kit). Une voie de soin « faite pour le groupe » qui ne se joue pas seule serait un piège pour un joueur qui la choisit un soir où il est seul.

### 3.3 Exemple complet : les deux voies de la Sentinelle, rangées 30 et 45

Ce sont les talents de la tranche verticale (rangée 30) et du sprint de la rangée 45. Les chiffres de dégâts des variantes sont recalculés par la règle de budget de `spells_gen.py`.

**Voie mobile**

| Rangée | Talent | Genre | Effet |
|---|---|---|---|
| 30 | a. Dérobade longue | variante | Dérobade : +2 PM → **+3 PM**, relance 2 → **3** |
| 30 | b. Pas léger | passif | au début de son tour, **+1 PM et −5 % de dégâts** pendant 1 tour (`TurnStart`) |
| 30 | c. Tir de recul appuyé | variante | Tir de recul : poussée 1 → **2** (la base est passée de 2 à 1 en S162, T129), dégâts −10 % (la règle ne compte pas la longueur d'une poussée : on la paie à la main) |
| 45 | a. Bond réflexe | variante | Bond : 2 → **3 PA**, relance 1 → **0** |
| 45 | b. Dernier saut | passif | sous 40 % de ses PV, **+3 PM et bouclier de 15** pendant 2 tours, une fois (`LowHp` 40) |
| 45 | c. Collet large | variante | Collet : zone **`Cross 1`**, relance 2 → **3** |

**Voie de précision**

| Rangée | Talent | Genre | Effet |
|---|---|---|---|
| 30 | a. Concentration brève | variante | Concentration : +30 % pendant 2 tours → **+45 % pendant 1 tour** |
| 30 | b. Sang-froid | passif | frappée, **+10 % de dégâts** pendant 1 tour (`Hit`) |
| 30 | c. Flèche traversante | variante | Flèche perçante : ligne de 2 → **3**, dégâts au budget |
| 45 | a. Tir visé patient | variante | Tir visé : relance 2 → **3**, dégâts **+25 %** (la règle de relance) |
| 45 | b. Second souffle du tireur | passif | sous la moitié de ses PV, **+2 PA** pendant 1 tour, une fois (`LowHp` 50) |
| 45 | c. Pieu profond | variante | Pieu : −2 PM → **−3 PM**, dégâts au budget (−20 % de plus) |

**Le garde-fou de chaque rangée :** un talent qui, dans le simulateur, donne plus de 1,5 victoire sur 30 de plus que ses deux voisins contre les petits groupes, ou qui fait passer une horde au-dessus de son plafond, est retouché avant d'être livré.

### 3.4 Ce que ça change dans le moteur

| Pièce | Changement | Existe déjà |
|---|---|---|
| Données | `data/paths.json` (écrit par `tools/spells_gen.py`, qui garde la règle de budget en un seul endroit) : la voie, sa classe, son passif, son sort, ses rangées ; chaque talent est `{ passive }` ou `{ variant: { of, by } }`. Les variantes sont des sorts complets de `spells.json`, marqués `variantOf` | les passifs, la règle de budget |
| `Hero` | deux champs facultatifs : `Path` (null = sans voie) et `Talents` (un id par rangée atteinte). `Hero.Problem` vérifie : voie de sa classe, niveau ≥ 30, un talent au plus par rangée atteinte et de sa voie | `Hero.Problem`, `DeckProblem` |
| Combat | les sorts du héros passent par la table de substitution des variantes ; sa `FighterSpec` reçoit son passif de voie et ses passifs de talents (`PassivesOf` sert tel quel). La limite de 3 passifs ne vise que les monstres : un héros en a au plus 6 | `FighterSpec.Passives`, `Fight.PassivesOf`, `PassiveTriggered` (le client l'affiche déjà) |
| IA | **rien** : les passifs se déclenchent seuls, les variantes sont des sorts ordinaires. L'IA ne prévoit pas les passifs (design-familles §2.5), ce qui vaut aussi pour les héros simulés | `Ai.BestCast` |
| Deck | le sort de voie se met dans le deck comme un autre ; sans deck choisi, il prend la douzième place | `Hero.DeckOf` |
| Rejeu | le héros voyage avec le combat (comme le deck, T35) : le serveur rejoue avec la voie et les talents qu'il garde, et refuse un combat joué avec d'autres. Un combat à plusieurs écrit en base (T93) s'arrête sans perte à la mise à jour, comme à chaque déploiement | `FightRecord`, `Battles.Rebuild` |
| Serveur | deux colonnes facultatives (`Path`, `Talents`), envoyées avec les points (`PUT …/points`) ; le prix du changement de voie | `PUT …/points`, la réinitialisation payante des caractéristiques |
| Simulateur | `Progression.Built(hero, data, worn, path)` : sans voie par défaut (le chapitre 1 ne bouge pas), sinon la voie et le talent **a** de chaque rangée ; un outil hors CI mesure les trois talents de chaque rangée | `Progression.Built` |
| Client | un onglet **Voie** dans la fenêtre des caractéristiques et des sorts : les deux voies en deux cartes avant le choix, puis la signature et les rangées (trois tuiles, une allumée). Les textes des passifs suivent le format des passifs de monstres ; la fiche d'une variante dit ce qui change par rapport au sort de base | `PointsPanel`, `Texts.SpellCard`, `WindowState` |

**Déclencheurs nouveaux : aucun pour la tranche verticale.** Deux seraient utiles plus tard et coûtent chacun environ 1 point avec leurs tests : `Kill` (le porteur achève un ennemi) et `Cast` (le porteur lance un sort d'un genre donné). `AllyDies` ne marche pas pour les héros (il exige la même famille de monstres) : il faudrait l'étendre pour une voie de protection qui « venge » un allié.

### 3.5 L'effet sur l'équilibre

- **Chapitre 1 et `BalanceTests`** : aucun effet, la voie arrive à 30 et le héros simulé n'en a pas par défaut. Le test du Disjoint au niveau 30 reste sans voie.
- **T96 (`TemplateTests`, niveaux 35, 40, 45)** : la mesure passe de 3 classes à **6 voies**, avec le talent **a** de chaque rangée. Deux fois plus de combats en CI pour ces tests seulement ; les autres talents sont mesurés hors CI. Point de vigilance : la Sentinelle à 45 est déjà la classe la plus juste (2 petits groupes sous 18). La voie mobile risque de la faire passer sous le plancher (moins de dégâts contre plus de PM) ; la voie de précision risque l'inverse sur les plafonds.
- **Les plafonds de T41** : il reste une marge (hordes à 0 à 1,4 sur 10 avec le kit `craft` pour une limite de 2). Les passifs « +X % » sont tous payés par une contrepartie (−PM, −dégâts, une seule fois par combat) pour ne pas la manger. **Cible : une voie au niveau 45 vaut au plus +10 % de puissance d'un héros sans voie**, mesurée comme la hausse des victoires contre les petits groupes de 31 à 45 portés au niveau 50.
- **À plusieurs (S122, S123)** : les voies de soin et de rempart rendent les groupes plus forts, ce qui est voulu (un rôle choisi doit compter). Les bandes de §11.2 sont des planchers à 3 et 4 joueurs ; le risque est la horde de 9 ou 10 **à deux** (≤ 40 %). `PartyBalanceTests` passe sur une composition « soin + fureur » en plus des compositions par classe.
- **Le deck** : la pression monte d'un sort (le sort de voie) ; les variantes rendent certains sorts plus désirables. C'est l'effet voulu : le deck devient l'expression de la voie.

### 3.6 Les personnages existants, le respec et son prix

- **Migration** : aucune donnée à réécrire. Les deux colonnes sont nulles ; un personnage de 30 ou plus sans voie voit l'onglet Voie s'allumer et choisit quand il veut. Ses rangées se déduisent de son niveau, comme ses points de caractéristique.
- **Plouf** : ses trois personnages de niveau 100 choisissent **gratuitement** leur première voie (5 rangées d'un coup) : c'est le meilleur banc d'essai de l'interface. La commande d'administration (`Rpg.Api admin`) gagne `--path VOIE` et `--talent ID` pour restaurer le compte sans SQL (règle du compte de test).
- **Changer de talent dans une rangée : gratuit**, hors combat, **dans un village** (Clairval ou Quatre-Chemins). Les talents sont à budget égal, et essayer fait partie du plaisir. Le trajet jusqu'au village (pierres de passage, 12 à 72 Éclats) fait une petite friction naturelle sans interface de plus.
- **Changer de voie : la première fois gratuite, ensuite 30 × niveau Éclats** (900 au niveau 30, 1 350 au niveau 45, 3 000 au niveau 100), chez l'habitant de Quatre-Chemins qui garde les voies. C'est trois fois le prix des caractéristiques (10 × niveau) : une vraie décision, pas une prison. Au niveau 45, un joueur gagne environ 450 Éclats par niveau en combat (47 par monstre, une dizaine de monstres par niveau, T96) : le changement coûte trois niveaux de butin. Les talents de l'ancienne voie s'effacent, le sort de voie quitte le deck et **ses points de sort reviennent** (même règle qu'un sort disparu, `RankList`, T34).

### 3.7 Le chiffrage et la place dans le plan

**5 sprints, 17 points**, au rythme du projet (un moteur ≈ 5 points, une classe de contenu ≈ une famille ≈ 3 points).

| Sprint | Pts | Contenu | Tests | Risques |
|---|---|---|---|---|
| **T1, tranche verticale** | 5 | Le moteur des voies et des talents (§3.4) de bout en bout, **pour la Sentinelle seule** : ses deux voies, passifs, sorts, rangée 30 (6 talents). Le choix se fait dans l'onglet Voie dès 30 et « Quatre pistes » rendue ; la commande d'administration ; le changement payant | unitaires : `Hero.Problem` (voie d'une autre classe, avant 30, deux talents d'une rangée), la substitution des variantes, le passif de voie se déclenche sur un héros, le deck par défaut, le prix ; intégration par l'API : choisir une voie, jouer un combat que le serveur rejoue et accepte, refus d'un combat joué avec d'autres talents ; T41 au niveau 35 pour les deux voies (talent **a**) ; l'auto-test Godot ouvre l'onglet | une interface trop riche pour 5 points : deux cartes et une rangée de trois tuiles, rien de plus |
| **T2** | 3 | **La Garde** : deux voies, passifs, sorts, rangée 30 | T41 à 35 et 40 pour ses deux voies ; `PartyBalanceTests` avec un rempart | la voie du rempart trop faible seule (elle protège des alliés qu'elle n'a pas) : son bouclier vaut aussi pour elle |
| **T3** | 3 | **Le Mage** : deux voies, passifs, sorts, rangée 30 ; le déclencheur `Kill` si Charles le retient pour les ravages | T41 à 35 et 40 ; un `Kill` qui achève le combat ne se déclenche pas (comme `Death`) | la voie du soin qui ne gagne plus seule : régler son sort de voie avant son passif |
| **T4** | 3 | **La rangée 45** des six voies (18 talents) ; la mesure complète hors CI (3 talents × 2 rangées × 6 voies) ; T96 à 45 sur 6 voies | T41 et T96 à 45 ; chaque talent dans le garde-fou du §3.3 | un talent dominant découvert tard : le garde-fou est un test, pas un avis |
| **T5** | 3 | **L'habitant des voies** à Quatre-Chemins et sa petite quête (le choix mis en scène), le titre sous le pseudo, les textes du scénariste relus par Charles, le changement de voie chez lui ; **court playtest** (Plouf : trois voies choisies, un donjon de la Forêt) | intégration : la quête ouvre le choix ; le titre est diffusé à la carte comme l'allure | les textes attendent la relecture de Charles : les demander dès T1 |

**Les rangées 60, 75 et 90** viennent avec les chapitres qui portent ces niveaux, à raison de **3 points par rangée** (18 talents) dans le sprint de « socle » de chaque tranche, comme S128 l'a été pour 30 à 45.

**Où le placer : juste après le playtest de S136, avant les guildes et avant les Mines.**
- **Après S136**, parce que les talents de 30 et 45 ne se sentent qu'avec la Forêt jouable, et que le playtest de S136 dira si les niveaux 30 à 45 manquent de « quelque chose à construire » (si oui, c'est exactement ce que ce chantier apporte).
- **Avant les guildes**, par la logique même du plan v7 : une guilde sert quand il y a des joueurs pour la remplir (§11.3), alors que les voies servent dès ce soir à Charles seul et aux trois personnages de Plouf. Les guildes passent de S137 à S142 sans rien changer d'autre. *Alternative :* si Charles ouvre un serveur à des testeurs (question 71), garder les guildes en S137 et faire les voies en S138 à S142.
- **Avant les Mines (45 à 60)**, sans discussion : sinon les familles des Mines seraient réglées sans voies, puis à refaire.
- **Planification :** le projet passerait de **551 à 568 points** et de **137 à 142 sprints** ; Osmose de **330 à 347**. Il faudra mettre à jour `PLAN.md`, `data/scrum.json`, les fichiers de sprint, R2, R8 et `scopeDecisions`, et écrire une décision (règle « ajout = gestion de projet à jour ») : **rien de cela n'est fait ici**, en attendant le « Go » de Charles.

### 3.8 Les risques, honnêtement

- **La puissance qui monte en douce.** Chaque talent « juste un peu plus fort » mange la marge des plafonds de T41. Parade : des contreparties systématiques, la cible de +10 % au niveau 45, et les mesures en CI sur 6 voies.
- **Le talent dominant.** Avec 3 options par rangée, le joueur trouvera le meilleur en une soirée si l'écart est grand. Parade : le garde-fou chiffré du §3.3, testé.
- **La voie piège.** Une voie de soin ou de rempart qui ne se joue pas seule. Parade : la règle « toute voie tient les planchers seule ».
- **La complexité pour un nouveau joueur.** Aucune : rien avant 30, et une seule décision à ce moment-là, mise en scène par une quête.
- **Le contenu qui manque au-delà de 45.** Plouf au niveau 100 n'aura que deux rangées remplies sur cinq jusqu'aux chapitres suivants. C'est le même trou que le reste du contenu (§11.1, point 4) : on le dit, on ne le comble pas avec des talents sans monstres pour les éprouver.
- **Le texte.** 6 voies × (passif, sort, 6 talents) = 48 textes fr et en d'ici 45, plus les noms : à demander au scénariste dès la tranche verticale.

---

## 4. Questions à trancher par Charles

| # | Question | Valeur par défaut |
|---|---|---|
| 1 | Le modèle | **l'hybride du §3** : une voie à 30 (passif, sort, titre), puis une rangée de 3 talents tous les 15 niveaux, un talent pris par rangée ; *alternatives :* l'arbre à points (§2.1), les variantes seules (§2.2), les sorts de voie (§2.3) |
| 2 | Le niveau de la spécialisation | **30**, après « Quatre pistes », à Quatre-Chemins ; *alternative :* 20 (au milieu du chapitre 1, mais il faudrait refaire T41 aux Lac et Champs pour chaque voie) |
| 3 | Combien de voies par classe | **2** (6 voies) ; *alternative :* 3, soit +50 % de contenu et de mesures |
| 4 | Une quatrième classe, un vrai « voleur » | **non pour l'instant** : la voie mobile de la Sentinelle porte la fantaisie de l'acrobate, la voie de précision celle de l'assassin. Une classe nouvelle, c'est 24 sorts, environ 10 à 15 points, et un équilibre à refaire partout : après le chapitre 2 |
| 5 | Le sort de voie et le deck | **un sort de voie, le deck reste à 12** (deux rangées de six) |
| 6 | Le prix des changements | **talents : gratuit, hors combat, dans un village ; voie : la première fois gratuite, ensuite 30 × niveau Éclats** (900 à 30, 3 000 à 100) |
| 7 | Une voie doit-elle se jouer seule | **oui** : toute voie, soin compris, tient les planchers de T41 seule et aucune ne passe les plafonds |
| 8 | La place dans le plan | **juste après le playtest de S136, avant les guildes (qui passent en S142) et avant les Mines** ; *alternative :* guildes en S137 si un serveur s'ouvre à des testeurs |
| 9 | Plouf | **ses trois personnages restent sans voie et Charles les choisit en jeu** (premier choix gratuit) ; la commande d'administration gagne `--path` et `--talent` pour les restaurer |
| 10 | Le déclencheur `Kill` pour la voie des ravages | **oui, dans le sprint du Mage** (environ 1 point) ; *sinon* le passif `Hit` du tableau §3.2 |
| 11 | Ce que montre la voie | **un titre sous le pseudo**, rien sur le modèle (l'apparence reste à la fin du jeu, D76) |
| 12 | Dragonica | **le résumé du §1.2 est à confirmer** : la page n'a pas pu être lue (vérification anti-robot) ; Charles dit s'il en attendait autre chose (plus de paliers, des sorts à chaque palier) |
