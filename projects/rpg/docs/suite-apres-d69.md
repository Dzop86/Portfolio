# Osmose : la suite après D69

> **Version 3, 15 octobre 2026.**
> - **Version 1** (même jour) : les chantiers 0 à 4, l'initiative par l'équipement. **Charles a validé toutes les valeurs par défaut** de la v1 (questions 1 à 11 du §9).
> - **Version 2 :** ajoute, à sa demande, **les métiers** (récolte et fabrication, ateliers, ressources récoltables, craft). C'est le chantier 3, au §5. Le plan, les numéros des chantiers, le total de points et les questions sont mis à jour.
> - **Version 2.1 :** précision de Charles, il faut **au moins quatre métiers de récolte de base : bûcheron, mineur, pêcheur, paysan**. Ils sont au §5.2, un par élément, et l'herboriste reste en cinquième métier de récolte.
> - **Version 3 :** trois demandes de Charles.
>   - Une **fenêtre « Métiers »** (§5.9), sur le modèle d'une capture d'un jeu du genre.
>   - Des **métiers de 1 à 100** et **une nouvelle ressource tous les 5 niveaux** pour chaque métier de récolte (§5.3 et §5.4) : les paliers de 10 de la v2 sont remplacés. Les métiers de fabrication font les équipements, les clefs et les consommables.
>   - De **vraies icônes pour les objets** (§3 bis, nouveau sprint S98 : Rose était déjà faite au S97).
>   Le plan passe à **58 points** (§10).
> - **Version 4, 16 octobre 2026 :** règle voulue par Charles, **la vie se garde entre les combats** et se restaure avec le temps (§5 bis, sprint S101, après la fenêtre Métiers faite au S100). Elle redonne un usage à la nourriture : le **boulanger** rejoint l'alchimiste (S107 depuis la v5). Le plan passe à **65 points** (§10).
> - **Version 5, 16 octobre 2026 :** demandes de Charles (§5 ter).
>   - **Clairval en 12 cartes** derrière une palissade de bois, sans la porte d'entraînement.
>   - **Un fil conducteur** : une chaîne principale, des quêtes ouvertes l'une après l'autre, **au plus 2 quêtes proposées par habitant**, un tuto au village jusqu'au niveau 5, des donneurs dans chaque zone, quatre objectifs nouveaux dont le butin de quête.
>   - **Une mini-carte** déplaçable et zoomable.
>   - Les icônes d'ingrédients de l'établi, faites au S101.
>   L'histoire passe **avant** le jeu à plusieurs. Le plan passe à **78 points** (§10).
> - **Version 6, 17 octobre 2026 :** demande de Charles (§5 quater). **Quatre métiers d'équipement**, un pour chaque emplacement sauf le familier et la monture : forgeron, tailleur (qui fait aussi coiffes et capes), **bijoutier** et **cordonnier**. Les pièces fabriquées n'ont **jamais d'initiative**, au même budget que le butin. **32 recettes** des niveaux 1 à 35, le §5.6 et l'équilibre T41 revus, deux ateliers et deux artisans de plus dans la rue des artisans. **S109 (5 points) forgeron et tailleur, S110 (3 points) bijoutier et cordonnier**, la suite décalée d'un cran jusqu'à S118 : **5 points de plus** (§10, planification v6). Les tableaux de sprints des versions précédentes gardent leur numérotation d'origine ; celle qui fait foi est au §10.
> - **Version 7, 21 octobre 2026 :** la suite après S121 (§11). Le plan v6 est fini (S122 n'existe pas encore). Elle propose **16 sprints, S122 à S137, 62 points** : finir le jeu à plusieurs (une IA de héros qui joue en équipe, l'équilibre à plusieurs réglé, la minuterie, les absents, des combats qui survivent au serveur), les amis et le voyage, une **tranche du chapitre 2** (Quatre-Chemins et la Forêt des Aïeux, niveaux 30 à 45), puis les guildes. Elle répond aux deux questions d'équilibre de T89 (§11.2). **Rien de la v7 n'est à implémenter avant l'accord de Charles.**
>
> Rédigée après lecture du dépôt au commit `08cb31c`, revérifiée pour la v4 (`classes.json`, `Fight.cs` : `heroHp`, `StartHeroHp`, `MaxHp` ; `FightRecord.HeroHp` ; `Entities.cs` : `DungeonHp` ; `Shop.cs`) :
> - `DECISIONS.md` (T29 à T62), `docs/design-familles.md`, `docs/trame-saga-1.md`, `docs/bible.md`, `PLAN.md` (D54 à D70) ;
> - les données `data/items.json`, `sets.json`, `monsters.json`, `quests.json`, `jobs.json`, `classes.json`, `data/towns/clairval.json`, `data/towns/woods-2-2.json` ;
> - le code `src/Rpg.Core/Fight.cs`, `Items.cs`, `Jobs.cs`, `Progression.cs`, `Monsters.cs`, `FightRecord.cs`, `Data.cs`, `Town.cs`, `src/Rpg.Client/ItemStyle.cs`, `GameMenu.cs` (`WindowState.Bar`), `godot/SpellIcon.cs`, `godot/SideBar.cs`, `godot/assets/icons/spells/credits.json`, `src/Rpg.Api/Endpoints/CharacterEndpoints.cs`, `src/Rpg.Api/Data/Entities.cs`, et l'outil `tools/world_gen.py`.
>
> Les points **[À TRANCHER]** ont une **valeur par défaut** (§9), à utiliser tant que Charles n'a pas dit autre chose. **Rien de la v5 n'est à implémenter avant son accord** (la v4, la vie, est en cours au S101).

---

## 0. La réponse courte

| Rang | Chantier | Le trou qu'il bouche | Taille | Dépend de |
|---|---|---|---|---|
| 0 | **Correctif** : la quête principale bloquée à la Bauge | la chaîne d'Ysaline exige au niveau 5 un donjon fait pour plusieurs | ≤ 1 pt, dans S95 | rien |
| 1 | **Le butin de Clairval : panoplies et initiative** | il n'y a presque rien à looter, et l'initiative ne compte pas | 2 sprints (5 + 3) | rien |
| 1 bis | **Des icônes d'objets** *(v3)* | les glyphes dessinés par le jeu sont laids, et 150 objets nouveaux arrivent | 1 sprint (3) | 1 (la liste des nouveaux objets) |
| 2 | **Les Éclats servent à quelque chose** | la monnaie s'accumule sans aucun usage | 1 sprint (3) | 1 (les prix des objets) |
| 3 | **Les métiers : récolte, fenêtre Métiers, ateliers, fabrication** *(v2, v3, v4)* | les cartes ne donnent rien hors des combats, et l'établi ne fait que des clefs | 5 sprints (5 + 3 + 3 + 3 + 5) | 1 (budget des objets), 1 bis (icônes), 2 (prix de Rose) |
| 3 quater | **Un métier pour chaque pièce d'équipement** *(v6)* : forgeron, tailleur, bijoutier, cordonnier | des emplacements sans plancher garanti (coiffe, cape, anneaux, amulette, bottes, ceinture ne viennent que du butin), et aucun métier d'équipement n'existe encore | 2 sprints (5 + 3), S109 et S110 | 3 (récolte, fenêtre Métiers), 3 ter (le quartier des artisans) |
| 3 bis | **La vie se garde entre les combats** *(v4)* | chaque combat repart à pleine vie : une victoire serrée ne coûte rien, et soins et nourriture n'ont pas d'usage hors combat | 1 sprint (5), S101 | rien ; le boulanger (S107) s'appuie dessus |
| 3 ter | **Clairval en 12 cartes, le fil conducteur des quêtes, la mini-carte** *(v5)* | trop de quêtes d'un coup chez chaque habitant (le dialogue déborde), aucun ordre, un village d'une seule carte, rien pour se repérer | 7 sprints (5 + 5 + 3 + 3, puis 3 + 3 + 3), S102 à S105 et S108 à S110 | 3 (récolte, ateliers), 3 bis (la vie) ; il remplace le chantier 5 |
| 4 | **Jouer ensemble** | les hordes, les chefs, les salles 3 et les quêtes de donjon sont faits pour plusieurs, mais le jeu ne connaît qu'un héros par combat | 4 sprints (3 + 5 + 5 + 5) | 1 ; 3 pour l'échange entre joueurs |
| 5 | ~~**La fin du chapitre 1**~~ *(remplacé par 3 ter en v5)* | l'histoire s'arrête au niveau 8, alors que 94 % de l'XP jusqu'au niveau 30 vient après | 3 sprints (3 + 3 + 3) | 0 |
| 6 | Le monde après 30 (concept de `design-familles.md` §7) | il n'y a plus rien après le niveau 30 | plusieurs mois | 1 à 5 et une décision de Charles |

**Ma recommandation (v5) :** soit **78 points, sprints 95 à 114**, dans cet ordre :
- les chantiers 0 à 3 bis ;
- **le chantier 3 ter** : le village, le moteur des quêtes, le tuto, la mini-carte, puis les quêtes des zones jusqu'au Disjoint ;
- **le jeu à plusieurs en dernier**.

Un playtest a lieu après le chantier 1, un autre après l'alchimie (S107). Le chantier 5 est remplacé par le 3 ter.

- **Les métiers passent avant le jeu à plusieurs.** Ils complètent la boucle d'un joueur seul (combattre, récolter, fabriquer, vendre) pour un risque technique faible : l'établi, les recettes et le niveau de métier existent déjà (T48). Ils donnent aussi au jeu à plusieurs sa première raison d'échanger.
- **Le jeu à plusieurs reste le trou le plus grand.** Mais une boucle de récompense vide reste vide à quatre. Il faut d'abord que le butin, la monnaie et l'artisanat aient un sens, et que l'équilibre soit mesuré avec l'équipement, que le multijoueur viendra multiplier.

---

## 1. Le constat : où ça coince pour un joueur qui monte de 1 à 30

D69 a réglé la boucle **micro** (le combat) : huit familles qui se jouent chacune autrement. Les boucles **méso** (une sortie dans une zone) et **macro** (la progression d'une session à l'autre) ont des trous. Les voici, du plus gênant au moins gênant pour un joueur seul aujourd'hui.

### 1.1 Le butin ne nourrit pas la progression
- `data/items.json` compte **22 équipements**. 14 viennent du jeu de départ (entraînement, braconnier, orque), **8 seulement viennent des familles**, un par famille (`tusk-amulet`, `wolf-cape`, `cap-hat`, `grave-belt`, `lily-ring`, `shell-shield`, `stinger-dagger`, `digger-boots`). Chacun tombe du tank à 5 % et du chef à 25 %.
- `data/sets.json` contient **une seule panoplie**, celle du braconnier, qui vient du combat d'entraînement.
- Les ressources ne servent qu'au serrurier (les clefs) et à deux quêtes.
- **Conséquence :** dans un genre dont le moteur de motivation est « ce monstre peut me laisser la pièce qui me manque », un joueur traverse quatre zones en ramassant surtout des ressources. Le pilier « progression par l'équipement » du questionnaire (« XP + caractéristiques ET équipement ET niveaux de sort ») n'est pas tenu.
- **Fait important pour l'équilibre :** les simulations T41 jouent des héros **nus**. `Progression.Built` répartit les points et les rangs, mais ne met aucun équipement. Tout l'équipement est donc aujourd'hui un bonus par-dessus un équilibre réglé sans lui.

### 1.2 L'initiative n'existe pas pour le joueur
- Les trois classes ont 10 d'initiative (`classes.json`), et aucun objet n'en donne (`Item` n'a pas de champ pour elle).
- Les monstres de Clairval ont de 40 à 100 : Sangliers 50-80, Loups 60-100, Champignons 50-80, Zombies 40-75, Grenouilles 50-85, Crabes 40-75, Abeilles 60-100, Taupes 40-70.
- L'ordre se calcule une fois pour tout le combat (`Fight` : initiative décroissante, puis l'ordre du scénario ; le héros est le premier du scénario, il gagne donc les égalités). **Le héros joue toujours en dernier.** La frise (T45) montre un ordre que le joueur ne peut pas changer.

### 1.3 Les Éclats n'ont aucun usage
- Les Éclats tombent à chaque victoire : 2 + niveau par monstre (`Progression.MonsterShards`). Les quêtes de Clairval en donnent en tout 2 122.
- **Rien ne les consomme.** Il n'y a ni marchand (T54, Limites), ni vente, ni coût de fabrication en Éclats (T48).
- Estimation : un joueur arrive au niveau 30 avec 5 000 à 7 000 Éclats, et rien à en faire. Rose a le dialogue `merchant`, mais elle ne vend rien.

### 1.4 Les cartes ne se récoltent pas, l'artisanat se limite aux clefs *(v2)*
- Un seul métier existe, le **serrurier** (`data/jobs.json`, T48). Il a un seul établi, à Clairval (`workshops` de `clairval.json`, case (11, 9)), et 8 recettes : les clefs, faites des deux ressources de chaque famille (6 + 4).
- Le niveau de métier va de 1 à 100 (`Crafting.XpToReach` = 10 × (N − 1) × N). Une recette rapporte 20 + 5 × son niveau. Le serveur garde les niveaux (colonne `Jobs` de `Character`, « id:xp,… ») et vérifie la fabrication (`POST …/craft`).
- **Aucune ressource ne se récolte.** Les 16 ressources (`kind: Resource`) tombent toutes des monstres. Les 101 cartes ont pourtant leurs arbres (`T`), leurs roches (`R`), leur eau (`~`), leurs champignons géants (`M`) et leurs fleurs (`,`), posés par `tools/world_gen.py` (`STYLES`).
- **Conséquence :** une carte sans monstre libre est un couloir. La boucle « je sors récolter entre deux combats » et l'arbitrage « je fabrique ou je cherche en butin » n'existent pas. C'était pourtant un souhait de Charles du 9 octobre (« métiers », onglet « Métiers » de l'interface).

### 1.5 Un tiers du contenu attend un multijoueur qui n'existe pas
- La règle de Charles (T41) réserve à plusieurs joueurs les groupes de 6 et plus, les chefs et la salle 3 des donjons. D'après les tailles de `MonsterGroups.Sizes`, cela fait environ **un quart des groupes du fond de chaque zone** et **5 % de ceux du milieu**, plus les **8 chefs**, les **8 salles 3** et donc les **8 quêtes « Au fond : … »**.
- Le moteur ne connaît **qu'un héros par combat** : `Fight(…, Hero? hero, Hero? rival, …)`, `FightRecord.Hero`, et `Progression.FightXp` est calculé pour un seul héros. On ne voit pas non plus les autres joueurs sur la carte : le chat de proximité les lit sans les montrer (T51). Pourtant, les cartes de combat ont quatre places A, le bonus de groupe existe (+10 % d'XP par allié) et le canal de groupe du chat est prévu dans le protocole (T51).

### 1.6 L'histoire s'arrête au niveau 8, et elle bloque au niveau 5
- La chaîne d'Ysaline s'arrête avec « Quatre pistes », au niveau 8. Il faut `XpToReach(8)` = 2 800 XP pour l'atteindre et 43 500 pour le niveau 30 : **94 % de la montée vers 30 se fait sans fil narratif**.
- **Défaut bloquant :** `rumble-under-the-wallow` (niveau 5) demande de **finir** la Bauge (`goal: Dungeon`), donc de battre sa salle 3. D'après T47, cette salle est faite pour plusieurs, et la Bauge ne se gagnait seul qu'au niveau 13. Cette mesure date d'avant D69 et serait à refaire. La bible l'interdit : « aucun blocage possible ».

### 1.7 Ce qui va bien et qu'on ne touche pas
Le combat, les familles, le monde en cartes, les donjons où l'on marche, les quêtes en données, la courbe d'XP. Elle demande environ 10 monstres de la zone par niveau, soit 4 à 5 combats de 2 ou 3 monstres. Les quêtes donnent environ 23 % de l'XP jusqu'au niveau 30, hors quêtes de donjon. Les règles du serrurier sont saines et servent de base au chantier 3.

---

## 2. Chantier 0 : la quête principale ne bloque plus (à glisser dans S95, ≤ 1 point)

**Règle de design :** une quête de la chaîne principale doit pouvoir se faire **seul, à son niveau**. Les donjons entiers restent l'affaire des quêtes annexes « Au fond : … » et du multijoueur.

- `QuestObjective` gagne un champ optionnel `Room` (1 à 3, 0 par défaut = finir le donjon). Avec `Room: 2`, l'objectif est atteint dès que la salle 2 est gagnée (`DungeonRun` sait où en est le personnage).
- `rumble-under-the-wallow` passe à `{ goal: Dungeon, target: boar-wallow, room: 2 }`. Le texte change à peine : la pierre est « dans la grande salle de la bauge » (relecture par l'agent `scenariste`, puis par Charles).
- **Clef offerte une fois :** nouveau champ `grants` sur la quête, des objets donnés **à l'acceptation**. Ici, `boar-wallow-key` × 1.
- **Mesure :** la salle 2 de la Bauge, au niveau 5, pour chaque classe, nue. Cible : au moins 70 % seul. Si la cible n'est pas atteinte, monter le niveau de la quête plutôt que d'affaiblir la salle.
- **Tests :** l'objectif `Room` compte à la bonne salle, pas avant ; la clef n'est donnée qu'une fois ; `FirstQuestsTests` vérifie que toute quête de la chaîne principale a des objectifs de donjon avec `Room` ≤ 2.

---

## 3. Chantier 1 : le butin de Clairval, les panoplies et l'initiative (S95 à 5 points, S96 à 3 points)

### 3.1 L'expérience visée
« Je sors du Bois avec trois pièces de la meute : je joue avant le vieux solitaire, et ça change mon premier tour. » L'initiative devient **un choix de construction** : elle se paie par des caractéristiques en moins, elle se trouve **en butin** (direction donnée par Charles), et elle se lit dans la frise.

### 3.2 Règles moteur (S95)
- `Item` et `SetBonus` gagnent `int Initiative = 0`. `Equipment.Total` renvoie aussi l'initiative portée, et `Fight.HeroTotals` calcule `c.Initiative + initiative portée`. La fiche (`CharacterSheet.Initiative` existe déjà) et la fiche d'objet l'affichent.
- **Aucune caractéristique ne donne d'initiative**, et la base des classes reste à 10. L'initiative vient de l'équipement **laissé par les monstres** et des panoplies, et de rien d'autre. Le craft n'en donne jamais (§5.6).
- **L'ordre du tour ne change pas de nature.** Il reste trié une fois par initiative décroissante, et à égalité les héros passent d'abord.
- **Frise :** au survol d'un portrait, afficher l'initiative. Sur la fiche, ajouter la ligne « Vous jouez avant : … ».
- **Simulation :** `Rpg.Sim` gagne `--kit none|zone|fast` (§3.5), et `Progression.Built` accepte un équipement.

### 3.3 Combien d'initiative, à quels niveaux
Une pièce qui porte de l'initiative en donne selon son niveau, à peu près **10 + 0,6 × niveau, arrondi à 5** :

| Niveau de la pièce | 1-6 (Bois) | 9-13 (Bois moisi) | 16-20 (Lac) | 24-28 (Champs) |
|---|---|---|---|---|
| Initiative par pièce | 10 | 15 | 20 | 25 |

- **Coût dans le budget de l'objet.** Le budget des pièces existantes vaut environ **8 + niveau** points de caractéristiques (`stinger-dagger`, niveau 26 : 30 ; `lily-ring`, niveau 18 : 24 ; `tusk-amulet`, niveau 3 : 10). **Un point d'initiative coûte 0,6 point de budget.** Exemple : des bottes de niveau 26 à 25 d'initiative gardent 19 points de caractéristiques, contre 34 pour des bottes « pleines ».
- **Emplacements qui portent l'initiative :** les **pièces légères**, c'est-à-dire bottes, cape et anneaux.
- **Ce que ça donne au niveau de chaque zone** (base 10) :

| Zone (niv.) | Kit « zone » : 2 pièces rapides | Kit « fast » : tout ce qui est rapide et portable | Monstres de la zone |
|---|---|---|---|
| Bois (3) | 30 | 50 (3 pièces de la meute + bonus 10) | 50-100 |
| Bois moisi (10) | 40 | 70 | 40-80 |
| Lac (18) | 50 | 95 | 40-85 |
| Champs (26) | 60 | 120 | 40-100 |

### 3.4 Les huit panoplies de Clairval (données, S95 pour le Bois et le Bois moisi, S96 pour le Lac et les Champs)
Chaque famille a **une panoplie de 5 pièces**. Chacun des 4 monstres communs en laisse une, et le chef laisse l'arme.

- **Emplacements :** les panoplies tombées prennent les pièces légères et les bijoux (coiffe, cape, bottes, anneau, amulette, ceinture), plus l'arme du chef. **Le plastron et les épaulettes sont réservés à l'artisanat** (§5.6).
- Les 8 objets de famille existants y entrent **sans changer d'identifiant**, avec leurs chiffres revus au budget. Le bouclier de carapace (`shell-shield`) reste la pièce des crabes : boucliers et armes peuvent venir des deux sources.

| Famille | Élément | Initiative | Pièces qui en portent | Bonus de panoplie (2 / 3 / 5 pièces) |
|---|---|---|---|---|
| Sangliers | Terre, Vitalité | faible | bottes (10) | +10 Vit / +10 Terre / +15 Vit, +10 init |
| **Loups** | Air | **forte** | cape (`wolf-cape`), bottes, anneau (10 chacune) | +10 init / +10 Air / +1 PM |
| Champignons | Eau, Vitalité | nulle | — | +10 Vit / +10 Eau / +20 Vit |
| Zombies | Feu (et les quatre) | faible | anneau (15) | +10 Feu / +10 Vit / +10 dans chaque élément |
| **Grenouilles** | Eau, Air | **forte** | bottes, anneau (20 chacune) | +15 init / +15 Eau / +1 PM |
| Crabes | Terre, Vitalité | nulle | — | +15 Vit / +15 Terre / +30 Vit |
| **Abeilles** | Air, Feu | **forte** | cape, bottes, anneau (25 chacune) | +15 init / +15 Air / +1 PA |
| Taupes | Terre | nulle | — | +15 Terre / +15 Vit / +1 PM |

- **Part de l'initiative :** 11 pièces sur 40. Trois panoplies sur huit en ont fait leur identité, chacune celle d'une famille **rapide**.
- Les bonus de PA et de PM n'arrivent qu'à **5 pièces**, chef compris.
- **Taux de butin** : la pièce d'un monstre commun tombe à **8 %**. L'arme et une pièce au hasard de la panoplie tombent chacune à **30 %** sur le chef, deux fois dans un donjon. Ordre de grandeur : en passant environ 35 combats dans une zone, un joueur seul en sort avec **la moitié des deux panoplies environ**.

### 3.5 Effet sur l'équilibre T41 et règle de mesure
- **Les planchers (petits groupes ≥ 96 %) se mesurent nus**, comme aujourd'hui (le pire cas).
- **Les plafonds (hordes et chefs ≤ 10 % seul) se mesurent avec le kit « fast »** de la zone (le meilleur cas). Depuis la v2, ce kit comprend aussi les pièces d'artisan de la zone (§5.7). La Sentinelle, qui gagne déjà jusqu'à 21 % des hordes sur les Mares et les Pontons (accepté par Charles, T59 et T60), est la classe à surveiller.
- **Si un plafond saute, on règle les objets**, jamais les monstres. Les hordes sont faites des mêmes monstres que les petits groupes : leur toucher casserait les planchers.
- **Après 30 (concept, ne pas implémenter) :** les monstres des nouvelles régions auront une initiative qui croît avec leur niveau, à peu près base du rôle × (1 + niveau / 40), avec comme bases mêlée 70, distance 85, tank 45, soutien 60 et chef 60.

### 3.6 Tests et fin du chantier
- **Tests unitaires :** initiative portée et bonus de panoplie dans `HeroTotals` ; ordre du tour à égalité ; validation au chargement (initiative seulement sur bottes, cape et anneau, budget respecté : `ItemBudgetTests`) ; chaque famille a 5 pièces, une par monstre.
- **Test d'intégration :** l'API équipe des bottes à initiative, et le combat rejoué par le serveur a le même ordre que le client.
- **Fin :** T41 remesuré dans les quatre zones avec `--kit none` et `--kit fast`, entrées T63 et T64. Ensuite, un **playtest**.

---

## 3 bis. Chantier 1 bis : des icônes d'objets (S98, 3 points) *(v3)*

### 3 bis.1 Ce qui existe
- **Les sorts** ont de vraies icônes : un dessin de **game-icons.net** (CC BY 3.0) en SVG, dans `godot/assets/icons/spells/<id>.svg`. Il est posé **en blanc sur une tuile arrondie à la couleur de l'élément** (`SpellIcon`). Le champ `icon` de chaque sort est écrit par `tools/spells_gen.py` (« lorc/flame-spin »…), et les auteurs sont listés dans `credits.json`. Un sort sans icône montre sa première lettre. La barre latérale fait de même (`assets/icons/bar/credits.json`).
- **Les objets** n'ont qu'un **glyphe géométrique dessiné par le jeu** : `ItemStyle.GlyphOf` choisit parmi 16 formes selon l'emplacement ou le genre, et `ItemIcon` le teint de la couleur de la caractéristique la plus forte. Il y a une seule forme par emplacement, si bien que tous les anneaux se ressemblent et toutes les ressources sont une feuille. C'est ce que Charles trouve laid. Le défaut vient de la forme, pas de la couleur.

### 3 bis.2 La direction recommandée : game-icons.net, comme les sorts
- **Source : game-icons.net (CC BY 3.0).** C'est déjà la source des sorts et de la barre, la chaîne et le mécanisme de crédits existent, et la bibliothèque couvre largement ce qu'il faut : armes, armures, bijoux, bottes, potions, minerais, bûches, poissons, épis, plantes, clefs. Avec **une seule source, le style reste cohérent** : silhouettes pleines au même trait.
  - *Kenney (CC0)* : ses packs d'icônes sont surtout des icônes d'interface, pas un inventaire de RPG complet. Je le garde en appoint, au cas par cas.
  - *Les packs d'objets en pixel art d'OpenGameArt* : ils jureraient avec la 3D « cartoon peint » de la bible, et leurs licences sont à vérifier une par une. Écartés.
  - *Des icônes générées par IA* : la licence est floue pour un dépôt public. Écartées pour l'instant.
- **Le rendu, pour ne pas copier celui des sorts.** Un sort est un dessin blanc sur une tuile colorée. Un objet sera **un dessin coloré sur une tuile sombre** :
  - **la silhouette** prend la couleur de l'élément de l'objet (`ItemStyle.Colour`, inchangée). Les ressources ont une couleur par métier, celle de son élément (§5.2). Les objets de quête restent or ;
  - **un liseré et un léger dégradé** donnent du relief à la silhouette. Ils sont dessinés par `ItemIcon` (contour plus foncé, reflet en haut), sans changer le SVG ;
  - **le cadre de la tuile dit la provenance**, une notion de rareté propre à Osmose, sans nouvelle statistique :
    - gris : commun (ressources, objets de départ) ;
    - brun chocolat : artisan (fabriqué) ;
    - vert : panoplie (butin des monstres) ;
    - or : chef (arme ou pièce du chef).
- **Une icône par objet, mais des dessins partagés.** On compte environ 150 objets à terme : les 54 actuels, les 32 pièces de panoplie, les 40 ressources de Clairval (§5.3), les 16 pièces d'artisan et les 8 consommables. Les déclinaisons d'un même genre partagent un dessin (le cuivre et le fer : un minerai) et se distinguent par la couleur et le cadre. Il faut environ 60 dessins.
- **Remplaçable sans toucher au code**, comme Charles le demande pour les visuels (11 octobre) :
  - un champ `icon` sur chaque objet de `data/items.json` (« lorc/ore »), écrit par l'outil qui écrit les objets ;
  - le SVG copié dans `godot/assets/icons/items/<icon>.svg`, et l'auteur ajouté à `credits.json` ;
  - le **glyphe actuel reste le repli** si le fichier manque ;
  - un test vérifie que chaque objet a son fichier, et que chaque fichier a son crédit.
- **Les métiers** ont aussi leur icône (9), dans la fenêtre Métiers et la barre latérale.

### 3 bis.3 Sprint, tests
- **S98 (3 points)**, juste après les panoplies et l'étal de Rose (fait au S97 avant la v3), pour que tous les objets du chantier 1 arrivent avec leur icône. Les objets des métiers (S99 à S107) reçoivent la leur en même temps qu'ils sont écrits.
- **Tests :**
  - chaque objet a une icône ou un repli, et chaque icône a son crédit ;
  - le cadre dépend de la provenance (`ItemStyle.Frame`, sans moteur) ;
  - l'auto-test Godot dessine l'inventaire, la fin de combat et la fenêtre Métiers.
- Le README garde la mention des crédits.

---

## 4. Chantier 2 : les Éclats servent à quelque chose (S97, 3 points, fait)

### 4.1 L'expérience visée
« J'ai 800 Éclats : je refais mes caractéristiques, ou je garde de quoi acheter la clef de la ruche ? »

### 4.2 Contenu
- **Rose devient marchande** (son dialogue `merchant` existe).
  - Elle **achète** les ressources (1 + niveau de la ressource / 10, soit 1 à 4 Éclats à Clairval, jusqu'à 10 au niveau 95) et les équipements (2 × leur niveau).
  - Elle **vend** les ressources à 5 fois leur prix de rachat : celles des monstres comme celles de la récolte (§5).
  - Les potions viennent de l'alchimiste (§5.5). Rose les vend à 10 + niveau Éclats, plus cher que leur fabrication.
- **Puits 1 : refaire ses caractéristiques** coûte **10 × niveau** Éclats, gratuit jusqu'au niveau 10.
- **Puits 2 : les clefs du serrurier** coûtent aussi **5 × le niveau du donjon** en Éclats.
- **Puits 3 : les potions en combat.** Il n'est plus en option : il arrive avec l'alchimiste, en S107 (§5.5).
- Le marchand se fait **au village seulement**, et le serveur vérifie tout.

**Tests :** prix calculés par les règles ; achat refusé sans Éclats ; vente d'un objet porté refusée ; réinitialisation facturée au bon prix ; intégration API achat → inventaire → Éclats.

---

## 5. Chantier 3 : les métiers (S99 et S100, faits ; S106 à 3 points ; S107 à 5 points ; les ateliers vont dans le nouveau village, S102) *(v2 à v5, demandes de Charles)*

### 5.1 L'expérience visée
« En traversant le Lac pour aller aux crabes, je coupe deux saules et je pêche une anguille. Ce soir, je forge le bouclier de mon palier ; la panoplie des grenouilles, je l'attends du butin. »

Les métiers ajoutent **une boucle méso sans combat**, avec sa propre progression (motivation de **compétence** et de **collection**). Ils donnent aussi un **plancher d'équipement garanti**, face au butin qui reste aléatoire. Trois principes :
1. **Le butin reste la source des pièces aspirationnelles** (panoplies, initiative, bonus de PA et de PM). Le craft donne les pièces de base, fiables, **sans panoplie ni initiative**. On ne casse ni la direction de Charles (l'initiative par le butin) ni T41.
2. **Chaque recette lie les deux boucles** : des ressources récoltées et des ressources de monstres. Le récolteur va au combat, le combattant va à la récolte.
3. **Périmètre réaliste.** Chaque métier va de 1 à 100 (`Crafting.MaxLevel`, déjà dans le code), et chaque métier de récolte a une ressource tous les 5 niveaux, soit 20 par métier. **Maintenant**, on fait ce que Clairval peut porter : les niveaux de métier 1 à 35, soit 8 ressources par métier de récolte et 8 recettes par métier de fabrication. **Plus tard**, les niveaux 40 à 95 arrivent avec le monde après 30 (§5.3). Tout est en données : ajouter un palier ne demande aucun code.

### 5.2 Les métiers au départ

**Les quatre métiers de base, un par élément.** C'est une lecture de la bible, qui ne se voit que dans les textes des artisans et dans la couleur de l'icône de chaque métier (`ElementStyle`). Aucune règle n'en dépend.
- **Mineur, Terre (Dhoram)** : la pierre garde la mémoire, le mineur la lit. Outil : le **pic**.
- **Pêcheur, Eau (Névara)** : il prend à l'eau ce qu'elle veut bien rendre. Outil : la **nasse**.
- **Bûcheron, Feu (Kaor)** : son bois nourrit la forge. Outil : la **hachette**.
- **Paysan, Air (Élyon)** : le vent sème et mûrit ses récoltes. Outil : la **faucille**.

Les outils donnent leur nom et leur geste à l'animation de récolte. Ils ne sont pas exigés (§5.3).

| Métier | Type | Récolte ou fabrique | Pour qui | Atelier |
|---|---|---|---|---|
| **Bûcheron** | récolte (base) | bois (arbres marqués) | forgeron (manches, hampes, boucliers) | aucun |
| **Mineur** | récolte (base) | minerais (roches veinées) | forgeron (lames), tailleur (boucles, rivets) | aucun |
| **Pêcheur** | récolte (base) | poissons (eau) | alchimiste (huiles des potions) | aucun |
| **Paysan** | récolte (base) | fibres et céréales, en parcelles sauvages ou cultivées | tailleur (plastrons, épaulettes) | aucun |
| **Herboriste** | récolte (en plus) | plantes médicinales (sous-bois, champignons, fleurs, eau) | alchimiste | aucun |
| **Forgeron** | fabrication (équipement) | armes à une et à deux mains, boucliers | tous les combattants | forge |
| **Tailleur** | fabrication (équipement) | plastrons, épaulettes ; **coiffes et capes depuis la v6** (§5 quater) | tous les combattants | atelier du tailleur |
| **Bijoutier** *(v6)* | fabrication (équipement) | anneaux, amulettes (§5 quater) | tous les combattants | établi de bijouterie |
| **Cordonnier** *(v6)* | fabrication (équipement) | bottes, ceintures (§5 quater) | tous les combattants | banc de cordonnerie |
| **Alchimiste** | fabrication (consommables) | potions de soin et huiles d'élément (utilisables en combat) | tous, surtout en donjon | alambic |
| Serrurier (existant) | fabrication (clefs) | clefs des donjons | tous | établi (existant) |
| **Boulanger** *(v4)* | fabrication (consommables hors combat) | pains, qui rendent de la vie hors combat (§5 bis.5) | tous, entre deux combats | four |

Comme le veut Charles, **les métiers de fabrication font les équipements (forgeron, tailleur), les clefs de donjon (serrurier) et les consommables (alchimiste)**. Tous les métiers, de récolte comme de fabrication, vont de 1 à 100.

- **Écartés pour l'instant :**
  - ~~**bijoutier et cordonnier** : ils feraient anneaux, amulettes et bottes, les pièces des panoplies tombées et de l'initiative~~ : **ils entrent au plan en v6** (§5 quater), sans jamais d'initiative ; le butin garde l'initiative et les panoplies ;
  - ~~la cuisine~~ : écartée en v2 parce que les PV revenaient pleins après chaque combat. **Depuis la v4, la vie se garde (§5 bis) : le boulanger entre au plan**, avec les céréales du paysan. Une cuisine plus riche (plats à effet durable) reste pour plus tard ;
  - **l'herboriste fusionné dans le paysan** : un seul métier pour les fibres et les plantes médicinales nourrirait à la fois le tailleur et l'alchimiste, ce qui en ferait un passage obligé. Séparés, chaque métier de fabrication a son récolteur principal ;
  - **les produits intermédiaires** (planches, lingots) : un étage de plus pour le même plaisir.
- Un personnage peut **apprendre tous les métiers**, sans limite. Le jeu se joue encore surtout seul : une limite à la Dofus obligerait à trouver d'autres joueurs pour s'équiper.

### 5.3 Les ressources récoltables
**Une ressource tous les 5 niveaux de métier** (1, 5, 10, …, 95), soit **20 par métier de récolte et 100 en tout** sur 1-100. **Deux par zone** : la première sur les cartes proches de l'entrée de la zone, la seconde sur les cartes du fond (la même profondeur que les groupes, `MonsterGroups.Depth`). Le milieu de la zone a les deux. Clairval porte les niveaux **1 à 35, soit 40 ressources** (objets `kind: Resource` nouveaux).

| Niv. de métier | Zone (cartes) | Bûcheron | Mineur | Pêcheur | Paysan | Herboriste |
|---|---|---|---|---|---|---|
| 1 | Bois (entrée) | Chêne | Cuivre | Goujon | Lin sauvage | Ortie |
| 5 | Bois (fond) | Noisetier | Étain | Écrevisse | Avoine folle | Menthe des bois |
| 10 | Bois moisi (entrée) | Bois noir | Fer | Anguille des mares | Chanvre des mares | Mousse-lumière (sur les `M`) |
| 15 | Bois moisi (fond) | If | Soufre | Tanche | Seigle | Pied-de-loup |
| 20 | Lac (entrée) | Saule | Argile bleue | Perche | Laîche des rives | Nénuphar |
| 25 | Lac (fond) | Aulne | Zinc | Carpe | Riz des joncs | Prêle |
| 30 | Champs (entrée) ; Lac (îlots) pour le pêcheur | Frêne | Argent | Brochet | Blé de Clairval | Fleur de ruche (sur les `,`) |
| 35 | Champs (fond) ; Lac (pontons) pour le pêcheur | Tilleul | Quartz | Sandre | Sarrasin | Lavande |

- Les Champs n'ont pas d'eau (`STYLES` de `world_gen.py`). Les poissons des niveaux 30 et 35 se pêchent donc **au Lac**, en eau profonde (îlots et pontons), ce qui fait revenir le pêcheur au Lac. Le Bois n'a de l'eau que sur environ 40 % de ses cartes : ses deux poissons y sont plus rares, ce qui est voulu pour un premier palier.
- **Le monde après 30 (concept, avec le §8.1)** : les 60 ressources des niveaux 40 à 95 (12 par métier) se placent sur les régions de `design-familles.md` §7, toujours deux par zone. Niveaux 40-45 : zones 30-45 ; 50-55-60 : zones 45-60 ; 65-70 : Marches disjointes ; 75-80 : Terres de la Fracture ; 85-90 : Désert des Noms perdus ; 95 : le Cœur. Dans les quatre régions élémentaires, chaque ressource a une variante par région, avec le même niveau et les mêmes recettes (le bois de la Forêt des Aïeux, le bois flotté des Brisants…). Le joueur récolte ainsi dans la région qu'il a choisie. Rien de cela n'est à faire maintenant.
- **Le paysan cueille, il ne sème pas.** Ses parcelles sont des carrés de 2 × 2 cases de culture sur le sol, où l'on marche : lin et avoine des clairières, chanvre et seigle au bord des mares, laîche et riz des rives. Aux Champs, ce sont de vraies parcelles cultivées : le blé et le sarrasin de Clairval, que les paysans du village laissent glaner. Il n'y a ni semis, ni arrosage, ni attente de pousse. Planter serait un système à part entière (des parcelles à soi, des temps de pousse), à garder pour le jour où il y aura des maisons ou des enclos.
- **Le lien avec le niveau des zones est volontairement décalé** : la zone de niveau 26 porte les ressources de métier 30 et 35. Le métier est une progression à part, pas un sous-produit du niveau du personnage : un herboriste qui arrive aux Champs doit avoir monté son métier en chemin. Les clefs du serrurier gardent leurs niveaux de recette (1, 10, 20, 30, T48).

**Les points de récolte :**
- **Pas tous les arbres ni toutes les roches.** Une carte du Bois compte une cinquantaine d'arbres : tout récolter rendrait chaque clic ambigu et la carte illisible. On pose des **points de récolte nommés**, un nouveau champ `nodes` des cartes : `[{ "at": {x, y}, "resource": "oak-log" }]`, validé au chargement (ressource connue, case voisine d'une case atteignable). Le client les dessine distinctement : arbre marqué, roche veinée, remous sur l'eau, touffe ; souche ou roche grise pendant la repousse.
- **Combien :** **7 à 10 par carte de zone** (environ 850 en tout), tirés par `tools/world_gen.py` à partir du nom de la carte, comme les groupes. Ils sont placés sur le décor de la bonne nature (`T`, `R`, bord de `~`, `M` ; parcelles et herbes sur le sol). Ils restent à 3 cases au moins des passages, des sorties et de l'arrivée (règle `Distance` des groupes), et ne bloquent jamais un chemin (`KeepsWaysOpen`).
- **Répartition par carte** (arbres, roches, eau, parcelles, herbes) :

| Zone | Arbres | Roches | Eau | Parcelles | Herbes |
|---|---|---|---|---|---|
| Bois | 2 | 2 | 1 si la carte a de l'eau | 2 | 1 |
| Bois moisi | 2 | 2 | 2 | 1 | 2 |
| Lac | 1 | 1 | 3, plus 1 sur l'îlot | 2 | 2 |
| Champs | 2 | 2 | 0 | 4 | 2 |

Chaque métier de base a au moins un point sur presque chaque carte où il a quelque chose à récolter. Sur une carte, chaque point porte la ressource de la profondeur de la carte : la première près de l'entrée de la zone, la seconde au fond, l'une ou l'autre (tirée) au milieu. Le nombre de points ne change pas avec la v3 : il y a plus de ressources, pas plus de points. Le village n'en a aucun.
- **Partagés entre les joueurs** (un MMO : on voit l'autre récolter). Un point récolté se **repousse en 2 + niveau de la ressource / 10 minutes**, soit 2 à 5 minutes à Clairval et 11 minutes au niveau 95, pour tout le monde. Avec peu de joueurs aujourd'hui, la concurrence restera rare, et la repousse courte limite la frustration.

**Comment on récolte :**
- **Clic** sur le point, le personnage **marche jusqu'à une case voisine**, puis une **action de 3 secondes** avec une barre. Bouger l'annule.
- **Aucun outil exigé** : un outil à acheter ou à équiper serait une porte de plus sans décision intéressante. Le pic, la nasse, la hachette et la faucille apparaissent dans la main du personnage pendant l'animation, et ils donnent leur nom au geste. Ils pourront devenir plus tard des objets à améliorer (cosmétique ou bonus de quantité).
- Le serveur vérifie :
  - le niveau de métier ≥ niveau de la ressource ;
  - le point disponible ;
  - le personnage sur cette carte, à une case du point de sa dernière place enregistrée ;
  - une seule récolte par 3 secondes par personnage.
  Il tire ensuite la quantité avec son propre générateur et donne la ressource et l'XP.
- **Quantité :** 1 + (niveau de métier − niveau de la ressource) / 20, bornée à 3. Un bûcheron de niveau 41 coupe 3 chênes d'un coup : l'avance se récompense sans rendre le bas niveau inutile.

### 5.4 La progression des métiers
- **Niveaux 1 à 100, courbe inchangée** (`Crafting.XpToReach`, 10 × (N − 1) × N) : 900 XP pour le niveau 10, 8 700 pour le 30, 99 000 pour le 100. Passer du niveau N à N + 5 demande **100 × N + 200** XP.
- **XP de récolte :** **25 + 5 × niveau de la ressource** par action. En récoltant la ressource la plus haute qu'on peut, cela fait **environ 10 à 20 récoltes par tranche de 5 niveaux**, à peu près constant de 1 à 100 :

| Tranche | XP à gagner | Ressource récoltée (XP par action) | Récoltes |
|---|---|---|---|
| 1 → 5 | 200 | niv. 1 (30) | 7 |
| 10 → 15 | 1 200 | niv. 10 (75) | 16 |
| 30 → 35 | 3 200 | niv. 30 (175) | 18 |
| 50 → 55 | 5 200 | niv. 50 (275) | 19 |
| 90 → 95 | 9 200 | niv. 90 (475) | 19 |

  Avec 1 à 3 points du métier par carte, une tranche de 5 niveaux prend **10 à 15 minutes** de récolte, mêlées aux combats.
- **Pas d'XP pour une ressource trop basse :** comme pour les monstres (`Progression.LevelGap`), une ressource de **20 niveaux ou plus sous le métier** ne donne plus d'XP (on la récolte toujours, en plus grande quantité). Le contenu de Clairval (ressources jusqu'à 35) permet donc de monter un métier **jusqu'au niveau 55 environ**, pas jusqu'à 100. Les niveaux 55 à 100 attendent le monde après 30.
- **XP de fabrication :** la règle actuelle (20 + 5 × niveau de la recette) convient aux clefs et aux consommables. Pour l'**équipement**, il faut un champ optionnel `xp` sur `Recipe`, à **150 + 15 × niveau de recette** (165 au niveau 1, 675 au niveau 35). Sinon, monter un métier demanderait des dizaines de pièces par tranche, comme les 36 clefs du serrurier pour le niveau 10 (T48). Avec ce champ, il faut **3 à 6 fabrications par tranche de 5 niveaux**. La même règle des 20 niveaux s'applique aux recettes.

### 5.5 Le craft
- **Recettes déterministes**, ce qui garde la ligne du projet (le serveur rejoue tout, rien n'est caché au joueur) :
  - **réussite à 100 %** : un échec ne ferait que punir le joueur qui a déjà payé ses ingrédients ;
  - **aucun jet aléatoire de qualité** : l'objet fabriqué a les chiffres de sa fiche. Des jets rendraient l'équilibre T41 impossible à garantir et pousseraient à fabriquer en boucle pour un « parfait ».
- **Une recette tous les 5 niveaux pour chaque métier de fabrication**, comme les ressources. Clairval en porte **8 par métier** (niveaux 1 à 35) : 8 au forgeron, 8 au tailleur, 8 à l'alchimiste, plus les 8 clefs existantes du serrurier.
- **Recette type d'équipement :** 2 ressources récoltées de son niveau + 1 ressource de monstre de la zone où ces ressources se trouvent. Exemples :

| Niv. de recette | Objet (niv. de l'objet) | Métier | Ingrédients |
|---|---|---|---|
| 1 | Plastron de lin (3, Vitalité + Terre) | tailleur | 6 lin sauvage, 2 cuivre, 3 peaux de loup |
| 5 | Hache de noisetier (6, 2 mains, Air) | forgeron | 8 noisetier, 4 étain, 3 défenses de sanglier |
| 10 | Bouclier de bois noir (10, Vitalité + Eau) | forgeron | 8 bois noir, 5 fer, 3 chapeaux de champignon |
| 25 | Épaulettes de riz tressé (22, Vitalité + Eau) | tailleur | 8 riz des joncs, 4 zinc, 4 bave |
| 30 | Lame de frêne (26, 1 main, Vitalité + Air) | forgeron | 10 frêne, 6 argent, 4 ailes d'abeille |
| 35 | Plastron de sarrasin (30, Vitalité + Feu) | tailleur | 10 sarrasin, 4 quartz, 4 fourrures de taupe |
| 1, 10, 20, 30 | Potion de soin (4 tailles) | alchimiste | 2 plantes + 1 poisson de son niveau |
| 5, 15, 25, 35 | Huile d'élément (4 tailles) | alchimiste | 2 plantes + 1 ressource de monstre de la zone |

- **Le niveau d'un objet fabriqué** est celui de la zone de ses ressources : 3 et 6 au Bois, 10 et 14 au Bois moisi, 18 et 22 au Lac, 26 et 30 aux Champs. On peut fabriquer une pièce d'avance (le plastron de niveau 30 avant d'avoir le niveau 30).
- **Les 16 pièces d'artisan de Clairval** : le forgeron alterne bouclier, arme à une main et arme à deux mains, le tailleur alterne plastron et épaulettes. Chaque pièce donne de la Vitalité et un élément, et **les 4 pièces d'une zone couvrent les quatre éléments**. Elles respectent le **budget 8 + niveau**, sans initiative, sans PA ni PM, et sans panoplie.
- **Les consommables de l'alchimiste** : une nouvelle action de combat, `UseItem`, avec **un consommable par combat**, quel qu'il soit. On choisit donc entre soigner et frapper plus fort.
  - **Potion de soin** (4 tailles) : 2 PA, soigne 20 + 2 × niveau.
  - **Huile d'élément** (4 tailles, une par élément à chaque taille) : 2 PA, +10 % à +25 % de dégâts de son élément pendant 3 tours.
  Un consommable décrit ses effets comme un sort (`SpellEffect` : soin, état). Le moteur n'a donc qu'une action de plus, sans nouvel effet. L'objet est retiré de l'inventaire, ce que le serveur vérifie au rejeu. C'était l'option S98 de la v1.
- **Les ateliers : un quartier des artisans.** Clairval fait 16 × 12 cases, et T44 le dit « petit à côté des autres ».
  - Une **nouvelle carte du village, « Clairval, quartier des artisans »**, en [1, 0] à l'est (case libre du monde), de 24 × 16 cases. On y entre par la porte est, dont la sortie d'entraînement de Garance est déplacée d'une case.
  - Elle porte **la forge, l'atelier du tailleur, l'alambic et le four du boulanger** (v4), et accueille l'établi du serrurier, déplacé.
  - Un artisan PNJ par atelier, qui explique son métier et donne une quête d'initiation : récolter 5, fabriquer 1 (50 XP de métier).
  - `Town.Workshops` sert déjà pour n'importe quel métier. L'ancien panneau de l'établi (T48) est remplacé par la fenêtre Métiers (§5.9), qui s'ouvre sur le bon métier, en mode « Fabriquer ».
  - *Alternative :* les ateliers dans les zones (la forge au Bois moisi). Écartée : le craft est un moment social, au même endroit que Rose ; les zones gardent la récolte.

### 5.6 Quelles pièces tombent, lesquelles se fabriquent

> **v6 :** ce tableau est remplacé par celui du §5 quater.6 (coiffe, cape, anneaux, amulette, bottes et ceinture deviennent butin **et** artisanat). Il reste ici comme trace de la v2.

| Emplacement | Source | Initiative possible |
|---|---|---|
| Coiffe, cape, bottes, anneaux, amulette, ceinture | **butin** (panoplies des familles) | oui sur cape, bottes, anneaux |
| Arme | butin (l'arme du chef, dans sa panoplie) **et** craft (forgeron) | non |
| Bouclier | craft (forgeron) ; le bouclier de carapace des crabes en butin | non |
| Plastron, épaulettes | **craft seulement** (tailleur) | non |
| Familier, monture | ni l'un ni l'autre pour l'instant (l'âne et le chaton existants restent) | non |
| Potions et huiles | craft (alchimiste) ; Rose revend les potions plus cher | non |

L'arme fabriquée et l'arme du chef ont le **même budget**. Celle du chef complète sa panoplie (bonus de 5 pièces), celle du forgeron s'obtient à coup sûr. C'est un vrai choix, sans arme dominante.

### 5.7 L'économie et l'équilibre
- **Rose (§4)** rachète les ressources récoltées à 1 à 4 Éclats et les revend à 5 fois ce prix.
  - Pour le joueur pressé, acheter le bois d'une hache coûte environ 40 Éclats au palier 1 et environ 300 au palier 30 : un puits utile.
  - Pour le récolteur, revendre rapporte environ 3 Éclats par action, **bien moins qu'un combat** (28 Éclats par monstre au niveau 26). La récolte n'est donc pas une ferme à Éclats.
- **Revendre une pièce fabriquée** (2 × niveau, 52 au niveau 26) rapporte moins que ses ingrédients au prix de Rose. Il n'y a **pas de boucle infinie** acheter → fabriquer → revendre.
- **T41** *(précisé en v6 au §5 quater.8 : kits `zone` et `craft` bloquants à 20 %, `fast` + artisan + consommable en indicateur)* **:** les planchers restent mesurés **nus, sans consommable**. Les plafonds sont mesurés avec le **kit « fast » + les 4 pièces d'artisan de la zone + le meilleur consommable** (potion ou huile, le pire des deux pour la règle). Si un plafond saute, on baisse d'abord les chiffres des pièces d'artisan : elles sont garanties, donc elles pèsent plus sur l'équilibre que le butin.
- **Initiative :** inchangée par le craft (§3.2). Aucune pièce d'artisan et aucun consommable n'en donne.

### 5.8 Dépendances avec le jeu à plusieurs
- Les points partagés n'ont pas besoin du chantier 4. Le serveur garde leur état. Le client le lit en entrant sur une carte, et un refus « déjà récolté » le met à jour. Quand le chantier 4 diffuse les présences (S111), il diffuse aussi « point récolté » et « point repoussé ».
- **L'échange entre joueurs** (deux joueurs sur la même carte, les deux valident) entre dans le chantier 4, au sprint du groupe (S112, 5 points). C'est ce qui donne un sens économique aux métiers à plusieurs.

### 5.9 La fenêtre « Métiers » *(v3, d'après la capture de Charles)*

**Touche J**, la seule lettre libre et parlante (C, S, I, M et Q sont prises dans `WindowState.Bar`). Elle a une icône dans la barre latérale, après le journal. C'est une fenêtre mobile comme les autres (T52).

**À gauche, la liste des métiers :**
- les 9 métiers, rangés en deux blocs, « Récolte » puis « Fabrication » ;
- chaque ligne porte l'icône du métier (§3 bis), son nom, son niveau, et une barre d'XP **dans le niveau** : « 710 / 2 840 », c'est-à-dire l'XP gagnée depuis le niveau atteint sur l'XP qu'il faut pour le suivant, calculées par `Crafting.XpToReach` ;
- un métier jamais pratiqué est au niveau 1, en grisé ;
- un clic choisit le métier.

**À droite, deux onglets pour le métier choisi :**
- **Recettes.** Pour un métier de fabrication, ses recettes. Pour un métier de récolte, **les recettes qui utilisent ses ressources** : « à quoi sert mon chêne ? ».
  - Chaque ligne montre l'icône de l'objet, son nom, le niveau de la recette (en rouge s'il est trop haut), les ingrédients en icônes avec **possédé / requis** (en rouge s'il en manque), et l'XP gagnée.
  - Au survol, la fiche de l'objet, la même que dans l'inventaire.
- **Ressources.** Pour un métier de récolte, ses 20 ressources : icône, nom, niveau, zone où elle se trouve (« Bois, cartes du fond »), quantité possédée. Celles qui ne sont pas encore dans le jeu (40 à 95) apparaissent grisées, avec « bientôt ». Pour un métier de fabrication, les ingrédients de toutes ses recettes, avec possédé et provenance (récolte, monstre, Rose).

**Les outils de la liste.**

On garde, comme sur la capture :
- la **recherche** (`ItemStyle.Matches`, déjà écrit) ;
- le **filtre de niveau** (de … à …, de 1 à 100 par défaut) ;
- le **filtre par catégorie**, réduit aux catégories du métier (armes, boucliers ; plastrons, épaulettes ; potions, huiles ; clefs) ;
- le **tri** (niveau le plus élevé par défaut, niveau le plus bas, nom) ;
- la case **« Recettes possibles seulement »** (`Crafting.Problem` est nul).

On simplifie :
- le filtre **« ingrédients manquants »** de la capture devient un affichage : chaque ingrédient manquant est en rouge avec son compte. Une deuxième case cocher-décocher ferait doublon avec « possibles seulement » ;
- pas de case séparée « afficher le niveau trop haut » : le filtre de niveau suffit.

**Fabriquer.** Ailleurs qu'à un atelier, la fenêtre se **consulte seulement**, comme les fenêtres C et I en combat (T61). Devant l'atelier du métier, elle s'ouvre sur ce métier, et chaque recette gagne « Fabriquer » et « Tout (n) » (20 au plus, `Crafting.MostAtOnce`). **Elle remplace le panneau de l'établi** (T48) : il n'y a plus qu'un seul écran pour l'artisanat.

**Code :** la logique (filtres, tri, barre d'XP, possédé / requis) va dans `Rpg.Client.JobBook`, testé sans moteur comme `SpellBook`. Godot ne fait que dessiner.

### 5.10 Découpage et tests

> **v6 :** les sprints réels ont été renumérotés par D75, puis par la v6. Le forgeron et le tailleur sont au **S109 (5 points)**, le bijoutier et le cordonnier au **S110 (3 points)**, l'alchimiste et le boulanger au **S111** (§5 quater.11 et §10).

| Sprint | Pts | Contenu |
|---|---|---|
| S99 | 5 | **La récolte.** `Job` gagne `Kind` (récolte ou fabrication) et `Harvests` (ressource, niveau, nature du point : arbre, roche, eau, parcelle, herbe) ; champ `nodes` des cartes, écrit par `world_gen.py` (ressource selon la profondeur de la carte) ; table serveur des points (carte, case, prêt à) et `POST …/harvest` ; règle des 20 niveaux pour l'XP ; client : points dessinés (les parcelles en carrés de culture), clic, marche, barre de 3 s avec l'outil en main, « +2 Chêne » flottant ; les **40 ressources** (niveaux 1 à 35) et les 5 métiers de récolte. |
| (S100) | (5) | *La vie qui se garde : chantier 3 bis, §5 bis.* |
| S101 | 3 | **La fenêtre Métiers** (§5.9), touche J, `JobBook` ; elle remplace le panneau de l'établi. |
| ~~S102~~ | — | ~~Le quartier des artisans à part~~ : **fondu dans le nouveau Clairval** (§5 ter, S102, carte `clairval-3-1`). |
| S106 | 3 | **Forgeron et tailleur** : les 16 pièces d'artisan et leurs recettes (niveaux 1 à 35), `Recipe.Xp`, `ItemBudgetTests` étendu ; T41 remesuré (kit « fast » + artisan). |
| S107 | 5 | **L'alchimiste, `UseItem` et le boulanger** : 4 potions, 4 huiles (une par élément à chaque taille), un consommable par combat, rejoué par le serveur, l'IA ne s'en sert pas ; Rose revend les potions ; les 8 pains du boulanger, mangés hors combat (§5 bis.5) ; T41 remesuré. |

**Tests :**
- **Unitaires :**
  - quantité récoltée selon l'écart de niveau ;
  - repousse ;
  - refus : palier trop haut, point pas prêt, trop loin, trop tôt ;
  - chaque point posé sur le bon décor, voisin d'une case atteignable, à distance des passages, sans couper la carte ;
  - chaque métier de récolte a une ressource tous les 5 niveaux jusqu'au niveau atteint par les données ;
  - pas d'XP pour une ressource ou une recette 20 niveaux sous le métier ;
  - chaque recette d'équipement dans le budget, sans initiative ;
  - les 4 pièces d'artisan d'une zone couvrent les quatre éléments ;
  - `UseItem` une seule fois par combat, consommable retiré, combat rejoué à l'identique ;
  - `JobBook` : barre « 710 / 2 840 », filtres, tri, « possibles seulement », possédé / requis.
- **Intégration :** récolter par l'API, le point indisponible pour un second personnage, l'XP de métier gardée ; fabriquer une hache de bout en bout (récolte, butin, forge, équiper) ; acheter une potion à Rose et l'utiliser dans un combat rejoué par le serveur.
- **Auto-test Godot :** une récolte sur la carte du milieu du Bois, la fenêtre Métiers ouverte par J, une fabrication au quartier.

### 5.11 Risques
- **La corvée.** 10 à 20 récoltes par tranche de 5 niveaux, c'est la limite. Au playtest, la question sera : « La récolte en passant vous a-t-elle semblé un plaisir ou un détour ? » Si c'est un détour, on monte la quantité, pas le nombre de points.
- **Les bots.** Une seule récolte par 3 secondes, la vérification de la place, et pas de récolte hors ligne. Une vraie modération viendra avec le jeu à plusieurs.
- **Le volume de données.** 40 ressources, 16 pièces, 8 consommables, environ 850 points, sans compter les 60 ressources de plus tard. Tout est écrit par les outils (`world_gen.py` et un nouveau `craft_gen.py`, qui écrit les ressources, les recettes, les objets et leur `icon`), jamais à la main.
- **La promesse « de 1 à 100 ».** La fenêtre montre 20 ressources par métier alors que Clairval n'en a que 8. Les 12 autres sont grisées avec « bientôt » : c'est un horizon, mais il ne faut pas qu'il dure trop longtemps.

---

## 5 bis. Chantier 3 bis : la vie se garde entre les combats (S101, 5 points) *(v4, règle de Charles)*

### 5 bis.1 Ce qui existe
- **Hors donjon, chaque combat commence à pleine vie.** `Fight` ne prend des PV de départ (`heroHp`) que pour les salles de donjon.
- **En donjon**, les PV restants passent de salle en salle (`Character.DungeonHp`, `FightRecord.HeroHp`, vérifié au rejeu, T53), et la fontaine les rend tous.
- **Perdre ne coûte aucune vie.** Un ticket de combat jamais rendu ne coûte rien non plus : c'est une limite connue de T29 (« abandonner un combat mal parti »).
- **Les PV max** valent les PV de la classe + PV par niveau × (niveau − 1) + la Vitalité, un point de Vitalité valant un PV (`Fight.HeroTotals`, `Fighter.MaxHp`). Classes : Sentinelle 62 + 5 par niveau, Garde 74 + 3, Mage 86 + 3. Avec les points répartis comme dans les simulations (`Progression.Built`, moitié en Vitalité), cela donne environ **60 à 90 PV au niveau 1, 300 à 350 au niveau 30, 870 à 1 050 au niveau 100**, et jusqu'à 1 550 avec tous les points en Vitalité, sans compter l'équipement.

### 5 bis.2 L'expérience visée
« J'ai gagné à 20 PV. Je mange un pain et je repars, ou je marche jusqu'au groupe suivant le temps de remonter ? »

La vie devient une **ressource méso**, entre deux combats.
- Une victoire serrée **coûte** quelque chose.
- Jouer proprement (finir sans perdre de vie) **rapporte** du temps.
- Les soins, les potions et la nourriture **comptent**.
- Le temps d'attente doit rester court, sinon on retombe dans le « je regarde ma barre remonter », qui est de l'ennui pur. **Cible : le repos ne dépasse pas la moitié de la durée d'un combat** (2 à 3 minutes), et la marche vers le groupe suivant en couvre la plus grande partie.

### 5 bis.3 La règle

**1. Le rythme de récupération.** Le rythme exact de Charles (1 PV par seconde) vaut pour 100 PV. Appliqué tel quel au niveau 100, il donnerait plus de 15 minutes pour remonter de zéro : c'est trop. Il faut donc un rythme qui suit les PV max :
- **un pour cent des PV max par seconde, au moins 1 PV par seconde**, soit `max(1, PV max / 100)` par seconde, en valeur exacte et pas arrondie ;
- ce qui donne :
  - 1 PV/s jusqu'à 100 PV max, donc la règle de Charles telle quelle pour un héros de 100 PV ;
  - environ 3,3 PV/s au niveau 30 ;
  - environ 10 PV/s au niveau 100 ;
- **de 0 à pleine vie, il faut 60 à 100 secondes, à tous les niveaux.** Le confort ne dépend pas du niveau ;
- **au village** (Clairval et le quartier des artisans), le rythme est **doublé**. C'est une raison de rentrer vendre et fabriquer, et un lieu sûr.
- *S'asseoir pour récupérer plus vite* (comme dans les jeux du genre) est laissé pour plus tard : c'est un état et une animation de plus, pour un gain que le village donne déjà.

**2. Le calcul se fait à partir d'un horodatage, sans tic.**
- Le serveur garde, pour chaque personnage, sa **vie** (`Hp`), le **moment** où elle a été fixée (`HpAt`) et son **état** (hors combat ou en combat).
- La vie au moment t vaut :
  > min(PV max, Hp + (t − HpAt) × rythme × multiplicateur du lieu)
  
  Le calcul est dans `Rpg.Core` (`Life.Now`). Le client utilise le même code pour animer la barre, sans rien demander au serveur.
- **La vie est figée à nouveau** (on écrit la valeur du moment, puis on repart de là) à chaque événement qui change la vie, le rythme ou l'état :
  - un **changement de carte** (le village double le rythme) ;
  - un **équipement** changé (les PV max changent) ;
  - un **niveau** gagné ;
  - l'**entrée en combat** et la **fin** du combat ;
  - un **pain** mangé.
- **Pas de récupération en combat.** Le ticket de combat fige la vie, qui ne bouge plus jusqu'au rapport.
- **La vie remonte aussi hors ligne**, toujours au rythme normal (le personnage n'est pas au village pour le serveur s'il s'est déconnecté dehors). On revient donc à pleine vie : aucune raison de punir l'absence.

**3. Le combat.**
- **Tout combat commence avec la vie actuelle du héros**, calculée par le serveur au moment du ticket. Le paramètre `heroHp` de `Fight`, qui ne servait qu'en donjon, sert désormais partout. `FightRecord.HeroHp` le garde, et le serveur le compare au rejeu, comme en donjon aujourd'hui.
- **À la fin**, la vie est celle du héros à la dernière action, bornée à ses PV max hors combat (l'érosion ne vaut que pendant le combat).
- Les potions de l'alchimiste (S107) soignent pendant le combat, donc **elles comptent aussi pour la suite**.
- **Monter de niveau rend toute la vie.** C'est le moment fort de la progression, il doit faire du bien, comme dans les jeux du genre.
- **Changer d'équipement** ne soigne pas : la vie est seulement ramenée aux nouveaux PV max si elle les dépasse.

**4. La défaite.**
- **Le héros reste où il était, à 1 PV.** En donjon, il ressort devant la porte, comme aujourd'hui, à 1 PV.
- Le coût d'une défaite est donc le temps : 60 à 100 secondes, deux fois moins au village, ou un pain.
- *Alternative écartée :* le retour au village. La marche depuis le fond des Champs (une dizaine de cartes) est longue, et elle punit par l'ennui plutôt que par un choix.
- **Un ticket jamais rendu compte comme une défaite.** Au prochain ticket demandé, ou après 30 minutes, la vie passe à 1 PV. Cela ferme la limite de T29 (abandonner un combat mal parti en gardant sa vie). Le risque : un plantage du jeu en plein combat coûte la vie. On l'accepte et on le dit dans les Limites. Le combat à plusieurs (S113) saura reprendre un combat interrompu.

### 5 bis.4 Les donjons et la fontaine
- **`DungeonHp` disparaît.** La vie du personnage est la même partout : en donjon, elle passe d'une salle à l'autre parce qu'elle se garde, tout simplement. Une migration copie `DungeonHp` dans `Hp` pour les personnages en cours de donjon.
- **Dans les salles, la vie remonte au rythme normal**, hors combat. On ne peut pas l'interdire sans rendre l'attente absurde.
- **La fontaine rend toute la vie d'un coup** (`POST …/dungeon/rest`, inchangé). Elle fait gagner du temps, et rien de plus : la difficulté d'un donjon reste celle de chaque salle, et la règle des PV gardés en donjon (T53) devient celle du monde entier.

### 5 bis.5 Ce qui fait remonter la vie plus vite
- **Le pain** (le boulanger, S107) se mange **hors combat seulement**. Il rend tout de suite **30 + 10 × son niveau** PV : 40 au niveau 1, 380 au niveau 35. Une recette tous les 5 niveaux, comme les autres métiers, avec 3 céréales du paysan + 1 ressource de l'herboriste ou du pêcheur de son niveau.
  - **Rose vend le pain de Clairval** (l'objet `bread` qui existe déjà, niveau 1, 40 PV) à 10 Éclats. Le joueur a de quoi manger dès le début, sans métier.
  - Il n'y a pas de délai entre deux pains : le prix est l'objet. Le serveur vérifie qu'on n'est pas en combat (`POST …/use`).
- **Les potions et les huiles de l'alchimiste se boivent en combat seulement** (une par combat, `UseItem`, S107). Les rôles restent clairs : le pain entre les combats, la potion pendant.
- **Le village double le rythme** (§5 bis.3), et la fontaine des donjons soigne tout.
- Plus tard, des plats à effet durable (un rythme doublé pendant 10 minutes, par exemple) pourront venir d'une vraie cuisine.

### 5 bis.6 L'équilibre T41
- **Les planchers (petits groupes ≥ 96 % seul) restent mesurés à pleine vie.** Le joueur peut toujours attendre une minute ou manger : la règle mesure ce que le jeu demande, pas l'impatience. Les hordes et les chefs restent mesurés à pleine vie aussi : c'est le meilleur cas.
- **Nouvelle mesure, la vie restante** : la moyenne des PV restants à la fin des victoires contre les petits groupes, au niveau de la zone, nu, par classe. **Cible : au moins 50 % des PV max.**
  - Au-dessus, le joueur enchaîne : la marche vers le groupe suivant (20 à 40 secondes) rend 20 à 40 % de la vie.
  - En dessous, chaque combat impose une pause : on regarde alors les dégâts des monstres concernés, jamais la règle de récupération.
  - Cette mesure est un **indicateur**, notée dans `DECISIONS.md` à chaque remesure, pas un test bloquant.
- **Les donjons** se mesurent comme aujourd'hui (la vie passe d'une salle à l'autre).

### 5 bis.7 Le client
- **Une barre de vie hors combat**, sous le nom du personnage, en haut à gauche : « 20 / 100 », qui se remplit en continu, avec le rythme au survol (« +1 PV/s, ×2 au village »). Elle apparaît aussi dans la fiche C.
- **Attaquer en dessous de 30 % de vie** ouvre un avertissement, pas un refus : « Vous n'avez que 20 PV sur 100. Attaquer quand même ? » (« Attaquer » ou « Plus tard »). C'est aussi le cas pour « Rejoindre » un combat à plusieurs.
- **Le combat montre la vie de départ** du héros dans la frise et sur sa barre : `Fight` le fait déjà en donjon.
- **La fenêtre de fin de combat** affiche la vie restante, « 20 / 100 PV », sur la ligne du joueur.

### 5 bis.8 Sprint S101 (5 points) et tests
**Contenu :**
- `Rpg.Core.Life` : le rythme, `Now`, les règles de défaite, de montée de niveau et de bornes.
- Le serveur : colonnes `Hp` et `HpAt` et l'état « en combat », migration de `DungeonHp`, la vie figée à chaque changement, le ticket qui fige et porte la vie de départ, le rapport qui la rend, le ticket non rendu compté comme défaite.
- La route `POST …/use` (manger un pain), et le pain de Rose.
- Le client : la barre, l'avertissement, la fin de combat.

**Tests :**
- **Unitaires :**
  - le rythme (1 PV/s jusqu'à 100 PV max, puis 1 %) ;
  - le doublement au village ;
  - la borne aux PV max ;
  - la vie figée puis reprise sans perte (deux changements de carte d'affilée donnent la même vie qu'un seul calcul) ;
  - pas de récupération en combat ;
  - défaite à 1 PV ;
  - montée de niveau à pleine vie ;
  - équipement : vie bornée sans soin.
- **Intégration :**
  - un combat gagné à 20 PV, puis un second demandé tout de suite qui part à 20 PV environ (± ce que les secondes ont rendu) ;
  - un ticket abandonné, puis un nouveau ticket à 1 PV ;
  - un pain mangé, refusé en combat ;
  - un donjon dont la vie passe de salle en salle sans `DungeonHp` ;
  - la fontaine.
- **Auto-test Godot :** la barre hors combat se remplit, et l'avertissement s'ouvre sous 30 %.

**Risques :**
- Le rejeu des anciens combats : `FightRecord.HeroHp` est nul pour eux, ce qui veut dire pleine vie, et ils se rejouent comme avant.
- Le compte d'essai de Charles (Plouf) part à pleine vie.
- Les auto-tests qui enchaînent des combats doivent attendre ou partir d'une vie fixée par la commande d'administration : ajouter `--hp N` à `admin`.

---

## 5 ter. Chantier 3 ter : Clairval en douze cartes, un fil conducteur, la mini-carte (S102 à S105, puis S108 à S110) *(v5, demande de Charles)*

### 5 ter.1 Le constat, vérifié dans le dépôt

**Le « bug » d'interface n'en est pas vraiment un : c'est un dialogue qui déborde.**
- `QuestBook.Offers` propose **toutes** les quêtes disponibles d'un habitant, et `Conversation` les ajoute **toutes** comme réponses, devant les réponses du dialogue.
- Toutes les quêtes du jeu sont données au village par 4 personnes. Au niveau 30, Garance en propose jusqu'à 7 (`the-pack`, `the-walking-dead`, `under-the-furrows` et quatre quêtes « Au fond : … ») et Aubin 6.
- Le panneau de dialogue (`TalkPanel`) est **ancré en bas et grandit vers le haut, sans hauteur maximale ni défilement** : au-delà de 6 à 8 réponses, il sort de l'écran par le haut.
- **Seules les touches 1 à 4** répondent (`Main.Town`, `Key1` à `Key4`).

**Le jeu ne raconte pas de fil.** Les quêtes de toutes les zones sont disponibles dès leur niveau, au même endroit, sans ordre. La chaîne d'Ysaline s'arrête au niveau 8, et les quêtes des donjons (« Au fond : … ») sont proposées en même temps que tout le reste.

**Clairval est une seule carte de 16 × 12** (`data/towns/clairval.json`, à la main) :
- 4 habitants (Aubin, Rose, Garance, Ysaline) ;
- l'établi du serrurier ;
- une sortie est vers le combat d'entraînement (`training`) ;
- un passage ouest vers `woods-4-2` (`tools/world_gen.py`, `outsides`).

Les zones sont placées autour, sur la carte du monde : Bois en x −5 à −1 et y −2 à 2, Lac en y −7 à −3, Champs en y 3 à 7, Bois moisi en x −10 à −6. **Tout ce qui est à l'est de x = 0 est libre.**

### 5 ter.2 L'expérience visée
« Je sors de chez moi à Clairval : on m'apprend à me battre, à porter mon anneau, à couper du bois pour la palissade, à vendre à Rose. Au niveau 5, Garance m'ouvre la porte de l'Ouest. Dans le Bois, un campement de bûcherons a besoin de moi, et la pierre d'Ysaline me tire toujours plus loin. »

Il faut **une seule question à la fois dans la tête du joueur** (« que veut dire ce cinquième signe ? »), et à chaque endroit **une ou deux choses à faire, pas quinze**. Les quêtes secondaires nourrissent le fil sans le noyer.

### 5 ter.3 Le nouveau Clairval : 4 × 3 cartes derrière une palissade

**Placement :** les cartes vont de **x = 0 à 3, y = −1 à 1** sur la carte du monde. Le côté ouest du village touche les trois cartes du bord est du Bois (`woods-4-1`, `woods-4-2`, `woods-4-3`, en x = −1). Rien d'autre ne bouge sur la carte du monde.

**Taille :** **48 × 32 cases** par carte, comme les zones. Les passages de bord à bord (T50), la carte du monde et la mini-carte fonctionnent alors sans cas particulier, et le village remplit enfin ses cases sur la carte du monde (limite de T44).

**Identifiants :** **la place garde l'identifiant `clairval`.** Les donneurs « clairval/… », les places enregistrées et les tests continuent de marcher. Les autres cartes s'appellent `clairval-<col>-<row>` (col 0 à 3, row 0 à 2).

**La palissade.** Une nouvelle tuile, la palissade de pieux en bois, fait le tour extérieur des 12 cartes. Elle bloque le passage comme un arbre, et elle est dessinée en pieux taillés, avec des tours de guet aux angles. Elle a **trois portes**, de 3 cases chacune, gardées, toutes à l'ouest, vers le Bois :
- **Porte de l'Ouest** (`clairval-0-1`, vers `woods-4-2`) : la route principale, déjà existante ;
- **Porte du Nord** (`clairval-0-0`, vers `woods-4-1`) : « chemin du Lac » ;
- **Porte du Sud** (`clairval-0-2`, vers `woods-4-3`) : « chemin des Champs ».

Entre deux cartes du village, les bords sont largement ouverts (rues, places). **La porte est d'entraînement disparaît** : le combat `training` reste, mais il est lancé par Garance dans sa conversation (« Première leçon », §5 ter.4).

| Carte | Nom (proposition) | Contenu |
|---|---|---|
| `clairval-0-0` | Porte du Nord, les vergers | la porte, un garde ; **la charpentière** (palissade, tuto bûcheron) ; vergers : 3 chênes (bûcheron niv. 1) |
| `clairval-1-0` | Les potagers | parcelles de lin sauvage (paysan niv. 1), orties (herboriste niv. 1), maisons |
| `clairval-2-0` | Le moulin et l'étang | le **moulin** et son meunier ; un étang : goujons (pêcheur niv. 1) |
| `clairval-3-0` | La carrière | une petite carrière dans la palissade : 3 roches de cuivre (mineur niv. 1) ; le **mineur retraité** (tuto mineur, optionnel) |
| `clairval-0-1` | Porte de l'Ouest | la grande porte, la cour d'armes : **Garance** (déplacée), ses mannequins ; deux gardes |
| **`clairval`** (1-1) | La place | **la vieille pierre gravée** et **Ysaline** ; **Aubin** ; la fontaine du village ; l'arrivée des nouveaux personnages |
| `clairval-2-1` | Le marché | **l'étal de Rose** (déplacé) ; deux étals vides (pour plus tard) ; une auberge (décor) |
| `clairval-3-1` | Le quartier des artisans | **forge, atelier du tailleur, alambic, four du boulanger, établi du serrurier** (déplacé), un artisan par atelier (§5.5). Cela remplace la carte [1, 0] prévue en S102 |
| `clairval-0-2` | Porte du Sud | la porte, un garde ; une écurie (décor, pour les montures plus tard) |
| `clairval-1-2` | Les maisons du sud | maisons, greniers : le **grenier infesté de rats** (combat du tuto) ; la fermière du village |
| `clairval-2-2` | La chapelle et le cimetière | la chapelle, le cimetière (il annonce la Crypte oubliée) ; le **copiste du registre** (Odalric dans la trame, à valider) |
| `clairval-3-2` | Les pâturages | un enclos, quelques moutons (décor), parcelles et touffes de niveau 1 |

- **Des spots de niveau 1 seulement** au village (2 à 4 par carte qui en a), pour apprendre sans sortir. Les ressources de niveau 5 et plus restent dans les zones (§5.3).
- **Pas de monstres errants dans le village** : c'est la raison d'être de la palissade. Les combats du tuto sont des **scénarios de quête**, comme l'entraînement :
  - le combat de Garance contre un mannequin animé et son élève ;
  - les **rats du grenier**, deux ou trois rats de niveau 1, lancés en parlant au meunier ou en ouvrant la trappe du grenier.
  Ces créatures ne sont pas une famille (la règle des 5 rôles d'une même espèce vaut pour le monde, pas pour un scénario de tuto).
- **Rythme de récupération ×2** dans tout le village (§5 bis.3).

**Comment c'est fabriqué :**
- un nouvel outil, **`tools/village_gen.py`**, lit 12 plans ASCII de 48 × 32 dessinés à la main, dans `tools/village/<carte>.txt` ;
- il ajoute la palissade, les portes, les passages entre les cartes du village et vers les trois cartes du Bois, puis écrit `data/towns/clairval*.json`, avec les habitants, les ateliers et les points de récolte (`nodes`) ;
- **`world_gen.py`** ouvre le bord est de `woods-4-1` et de `woods-4-3` en face des portes nouvelles (et garde celui de `woods-4-2`). Les groupes de ces deux cartes sont retirés au sort (ils dépendent du plan de la carte) : on remesure seulement la règle des petits groupes sur ces cartes.

**La migration des données :**
- les personnages enregistrés sur l'ancien `clairval` (coordonnées de 16 × 12) sont replacés à l'arrivée de la nouvelle place ;
- l'établi du serrurier change de carte (`workshops`).

### 5 ter.4 Le fil conducteur des quêtes

**Les règles :**
1. **Une chaîne principale**, celle d'Ysaline et de la pierre, de 1 à 30. Elle est la seule à ouvrir la zone suivante dans l'histoire. Chaque quête principale terminée ouvre (`After`) **la suivante et une ou deux quêtes secondaires** de l'endroit où l'on est.
2. **Des quêtes secondaires en petites suites** de 2 ou 3 quêtes par donneur, chacune ouverte par la précédente (`After`).
3. **Un habitant ne propose jamais plus de 2 quêtes à la fois.**
   - `QuestBook.Offers` les classe (la quête principale d'abord, puis le niveau le plus bas, puis l'ordre des données) et **n'en garde que 2**.
   - Un test vérifie, en simulant un joueur qui prend tout ce qu'on lui propose de niveau en niveau, qu'aucun habitant n'en a jamais plus de 2 sous la main.
4. **Les donneurs vivent là où se font les quêtes.** Le village pour le tuto, puis un habitant par zone, sur une carte de l'entrée de la zone. Le champ `npcs` des cartes de zone est écrit par `world_gen.py` à partir d'une petite table.
   - Bois : **le campement des bûcherons**, avec le chef des bûcherons (`woods-3-2`).
   - Bois moisi : **l'ermite des mares**.
   - Lac : **Liane, la passeuse**, sur les pontons.
   - Champs : **la fermière des Champs**, dans sa ferme.
   - Ysaline voyage : elle donne la quête principale de chaque zone **depuis le donneur de la zone**, ou depuis la place.
5. **On voit où aller.**
   - Un « ! » au-dessus d'un habitant qui a une quête à proposer, un « ? » s'il en attend une.
   - Le journal (Q) indique pour chaque quête en cours la carte où se trouve son objectif ou son donneur (« Bois de Clairval, campement des bûcherons »).
   - La mini-carte (§5 ter.6) montre les « ! » et les « ? ».

**Les objectifs.** Quatre existent : parler (`Talk`), vaincre (`Defeat`, une famille, N monstres), rapporter (`Bring`), donjon (`Dungeon`, avec `Room`). Il en faut quatre de plus :
- **`Interact` : trouver des indices sur une carte.** Il était prévu en S109 et **avance en S103**. Ce sont des objets posés (`Town.Props` : id, case, apparence), cliquables à une case : une trace de griffes, un nœud de mycélium, un sceau, un roseau, une galerie. Chaque objet ne compte que pour la quête qui le demande, et il est dessiné avec un éclat discret tant que la quête est en cours.
- **`Win` : gagner un scénario de quête.** C'est le mécanisme de « Première leçon » (champ `scenario`), qui devient un objectif comme les autres. Le combat est lancé depuis une conversation (réponse `Fight`, qui existe déjà) ou un objet `Interact` (la trappe du grenier).
- **`Wear` : porter un objet** (le tuto de l'inventaire). Le serveur le sait déjà (`Worn`).
- **Le butin de quête** : un objet de quête (`kind: Quest`) qui **ne tombe que si la quête est en cours** et que son objectif `Bring` n'est pas atteint.
  - Nouveau champ `quest` sur `LootDrop` (« croc marqué, 35 %, seulement pendant `restless-beasts` »).
  - Le serveur tire le butin en connaissant les quêtes en cours du héros. Le tirage reste déterministe : graine du combat et journal de quêtes.
  - Les objets de quête ne se vendent pas et disparaissent quand la quête est rendue.

Les ressources de métiers se rapportent avec `Bring` (déjà là), les objets fabriqués aussi (« rapporte un pieu de palissade que tu as taillé »), et « farmer des monstres » est `Defeat`.

**Les récompenses :**
- **XP :** quête principale **60 × niveau**, secondaire **40 × niveau**, donjon entier **100 × niveau**. Un test vérifie que les quêtes faisables seul d'une zone donnent **35 à 60 % de l'XP** qu'il faut pour la traverser, sans les donjons entiers (le reste vient des combats).
- **Éclats :** 10 × niveau, 15 × niveau pour un donjon entier.
- **Objets :**
  - le **tuto donne l'équipement de départ** : anneau de cuivre, dague ou bouclier de bois selon la classe, ceinture de cuir, 3 pains ;
  - les quêtes principales donnent la **clef** de leur donjon (`Grants`) ;
  - les secondaires donnent des consommables (pains, potions) et des ressources.
  Pas de pièce de panoplie en récompense : le butin des monstres en reste la source (§3.4).

**Les quêtes, zone par zone.** Il y en a 45 :

| Zone | Niveaux | Principales (Ysaline, la pierre) | Secondaires | Total |
|---|---|---|---|---|
| Clairval (tuto) | 1 → 5 | 2 | 7 | 9 |
| Bois | 5 → 10 | 4 | 5 (dont 2 donjons entiers) | 9 |
| Bois moisi | 10 → 18 | 4 | 5 (dont 2 donjons entiers) | 9 |
| Lac | 18 → 26 | 4 | 5 (dont 2 donjons entiers) | 9 |
| Champs | 26 → 30 | 4 (dont le Disjoint et la fin du chapitre) | 5 (dont 2 donjons entiers) | 9 |

**Clairval, le tuto (niveaux 1 à 5).** Une quête = un système appris. On ne peut jamais en avoir plus de deux devant soi.

| # | Identifiant | Niv. | Donneur | Objectifs | Ce qu'elle apprend | Ouverte par |
|---|---|---|---|---|---|---|
| C1 | `welcome` (nouv.) | 1 | Aubin (place) | `Talk` Garance (porte de l'Ouest) | marcher, changer de carte, parler | — |
| C2 | `first-lesson` (gardée) | 1 | Garance | `Win` training (lancé par sa conversation) | le combat, la frise, les sorts | C1 |
| C3 | `well-equipped` (nouv.) | 1 | Garance | `Wear` anneau de cuivre (donné par C2) ; récompense : dague ou bouclier selon la classe | l'inventaire, l'équipement, les caractéristiques (le texte invite à placer ses points) | C2 |
| C4 | `cellar-rats` (nouv.) | 2 | le meunier (moulin) | `Win` « rats du grenier » ; `Bring` 3 queues de rat (**butin de quête**) | le deck, le butin de quête, la vie qui se garde (on sort du combat blessé) | C3 |
| C5 | `palisade-stakes` (nouv.) | 2 | la charpentière (porte du Nord) | `Bring` 6 chênes ; `Bring` 1 pieu de palissade (fabriqué à la forge : 3 chênes, 2 cuivres, recette de tuto) | la récolte, la fenêtre Métiers (J), un atelier | C3 |
| C6 | `roses-stall` (nouv.) | 3 | Rose (marché) | `Bring` 4 lins ; `Bring` 1 pain (acheté chez elle) | vendre, acheter, manger hors combat | C5 |
| C7 | `whispering-stone` (gardée) | 3 | Ysaline (place) | `Interact` la pierre ; `Talk` Aubin ; `Talk` le copiste | les indices, le journal ; **début du fil** | C4 et C6 |
| C8 | `the-registry` (nouv., principale) | 4 | Ysaline | `Interact` 2 tombes marquées du cimetière ; `Bring` 3 orties (pour l'encre du copiste) | `Interact` sur une autre carte ; l'herboriste | C7 |
| C9 | `edge-of-the-woods` (gardée, donneur changé : Garance) | 4-5 | Garance | `Defeat` 3 sangliers juste derrière la porte de l'Ouest ; `Bring` 2 soies | le premier vrai combat dehors ; **ouvre le Bois** | C8 |

**Le Bois (5 à 10) : les monstres et le premier donjon.**
- **Principales** (Ysaline, puis le chef des bûcherons qui relaie) :
  - `restless-beasts` (gardée, niv. 5) : `Defeat` 5 loups, avec 3 crocs marqués (**butin de quête**), et `Interact` 2 traces ;
  - `the-root-circle` (nouv., 7) : `Interact` le cercle de racines ; `Talk` le chef des bûcherons ;
  - `rumble-under-the-wallow` (gardée, 8) : `Dungeon` bauge, `Room: 2`, clef offerte ;
  - `four-signs` (nouv., 9) : `Talk` Ysaline ; elle ouvre le Bois moisi.
- **Secondaires** (le chef des bûcherons, en deux suites) :
  - `the-pack` (gardée, 6) puis `winter-pelts` (nouv., 7) : `Bring` 4 peaux de loup et 6 noisetiers ;
  - `the-old-loner` (nouv., 8) : `Defeat` 3 vieux solitaires.
  - Les donjons entiers `boar-wallow-cleared` (9) et `wolf-den-cleared` (10) sont ouverts après `rumble-under-the-wallow`, avec « groupe conseillé ».

**Le Bois moisi (10 à 18).**
- **Principales :**
  - « Ce qui pousse entre » (10) : `Interact` 3 nœuds de mycélium ; `Defeat` 5 champignons ;
  - « La crypte que personne n'a creusée » (12) : `Talk` Aubin et le copiste ; `Bring` 3 pages de registre (**butin de quête** des zombies) ;
  - « Crypte oubliée » (14) : `Dungeon` crypte, `Room: 2`, puis `Interact` le sceau ; clef offerte ;
  - « Le sceau fissuré » (16) : `Talk` Ysaline ; elle ouvre le Lac.
- **Secondaires** (l'ermite des mares) :
  - `spores-for-the-stall` (gardée, donneur changé, 11) puis « Remède de l'ermite » (13) : `Bring` 2 anguilles et 3 pieds-de-loup ;
  - `the-walking-dead` (gardée, 12) ;
  - donjons entiers `mushroom-cellar-cleared` (15) et `zombie-crypt-cleared` (17).

**Le Lac (18 à 26).**
- **Principales :**
  - « La berceuse que personne n'a apprise » (18) : `Talk` Liane et 2 villageois ; `Interact` 3 roseaux chantants ;
  - « Le dernier couplet » (20) : `Dungeon` grotte, `Room: 2` ; `Bring` le couplet (**butin de quête** du crapaud-roi) ;
  - « La passeuse au nom lavé » (23) : `Bring` 6 tessons gravés (**butin de quête** des bernard-l'ermite) ; `Talk` le copiste ;
  - « Le nom rendu » (25) : `Talk` Aubin ; elle ouvre les Champs.
- **Secondaires** (Liane) :
  - `frog-song` (gardée, 19) puis « Les filets de Liane » (22) : `Bring` 5 perches et 8 laîches ;
  - `shells-and-claws` (gardée, 21) ;
  - donjons entiers `frog-grotto-cleared` (24) et `crab-reef-cleared` (25).

**Les Champs (26 à 30).**
- **Principales :**
  - « Les ruches brûlantes » (26) : `Defeat` 8 abeilles ; `Talk` Rose ;
  - « Sous les haies » (28) : `Interact` 3 galeries ; `Bring` le débris (**butin de quête** de la taupe-roi) ;
  - « Le Disjoint » (30) : `Win` le scénario du Disjoint (`design-familles.md` §4.9, gagnable seul à 50 à 70 %) ;
  - `four-trails` (**gardée, déplacée du niveau 8 au niveau 30**) : les quatre pistes deviennent la fin du chapitre et ouvrent les arcs de la bible.
- **Secondaires** (la fermière des Champs) :
  - `clairval-honey` (gardée, 27) puis « Le pain de la moisson » (28) : `Bring` 10 blés et 2 pains fabriqués ;
  - `under-the-furrows` (gardée, 27) ;
  - donjons entiers `royal-hive-cleared` (29) et `mole-burrows-cleared` (29).

**Les quêtes existantes :** **les 21 identifiants sont gardés.** Les personnages en ont dans leur journal, et les quêtes « Au fond : … » ou de famille restent justes. Changent :
- les **donneurs** : vers Garance, ou vers les donneurs des zones ;
- les **`After`**, pour enchaîner ;
- les **niveaux** ;
- l'**XP** selon la règle ci-dessus ;
- les **textes**, réécrits par le scénariste.

`four-trails` passe du niveau 8 au niveau 30. Aucune quête n'est supprimée. Il y a **24 quêtes nouvelles** : 6 au tuto, 18 dans les zones.

### 5 ter.5 Ce que le scénariste devra écrire (le cadre est ci-dessus, lui écrit les mots)
- **Les noms et les fiches** des nouveaux habitants :
  - au village : la charpentière, le meunier, la fermière, le copiste, les 5 artisans (forgeron, tailleur, alchimiste, boulanger, serrurier), 3 gardes ;
  - dans les zones : le chef des bûcherons, l'ermite des mares, Liane, la fermière des Champs.
  Pour chacun : rôle, ton, un trait, son lien avec le fil de la pierre. **Prénoms interdits : Margaux et Camille.**
- **Les noms des 12 cartes du village** (ceux du tableau sont des propositions).
- **Pour les 45 quêtes :** le titre, le texte d'offre, le texte de remerciement, l'intitulé de chaque objectif dans le journal, et **en fr et en en**. Il réécrit les 21 existantes et écrit les 24 nouvelles.
- **Les descriptions des objets `Interact`** (ce qu'on voit en cliquant : 1 ou 2 phrases) et des **objets de quête** (queues de rat, crocs marqués, pages de registre, couplet, tessons, débris…).
- **Les répliques de base** des habitants (hors quêtes), 1 à 3 lignes chacun, et les mots de Garance qui « ouvre la porte de l'Ouest ».
- **Contraintes :**
  - 3 à 5 lignes par texte ;
  - les anomalies se montrent avant d'être expliquées (trame §7) ;
  - le fil « relation ≠ fusion » ;
  - rien sur l'Accord avant la Saga I tardive ;
  - aucun nom ni contenu d'Ankama ;
  - relecture par Charles avant intégration.

### 5 ter.6 La mini-carte
- **Touche N**, ou une icône de la barre latérale. Elle est **visible par défaut**, en haut à droite, au-dessus de la barre latérale.
- **Déplaçable** par son cadre comme les fenêtres (T52), avec **trois tailles** (petite, moyenne, grande). Sa place, sa taille et son zoom sont gardés entre deux parties (`WindowState`).
- **Zoom** à la molette au-dessus d'elle et avec des boutons + et − : de la **carte entière** (48 × 32) **jusqu'à 3 × 3 cartes autour** au plus large, et jusqu'à environ 16 cases au plus serré. Elle est centrée sur le joueur et suit ses pas.
- **Ce qu'elle montre**, en couleurs simples avec une légende au survol :
  - le décor simplifié (sol, obstacles, eau, palissade) ;
  - **le joueur** (point et direction) ; les autres joueurs quand le jeu à plusieurs viendra (S111) ;
  - **les groupes de monstres** (points rouges, plus gros pour une horde, couronne pour un chef) ;
  - **les points de récolte** (couleur du métier, grisés si le métier est trop bas, cachés pendant la repousse) ;
  - **les habitants** (jaune, avec « ! » et « ? ») ;
  - **les passages** vers les cartes voisines (flèches au bord, nom de la zone quand on en change), les portes de donjon et les ateliers ;
  - **les objectifs de la quête suivie** (un cercle).
- **Clic** sur un point de la carte actuelle : le personnage y marche, comme un clic sur le sol. Elle se cache en combat.
- **Code :** le calcul (ce qu'on montre, où, à quel zoom) va dans `Rpg.Client.MiniMap`, testé sans moteur. Le dessin reprend celui de la carte du monde vue du ciel (`WorldPicture`, T38).

### 5 ter.7 Découpage

| Sprint | Pts | Contenu |
|---|---|---|
| S102 | 5 | **Le nouveau Clairval** : `village_gen.py` et les 12 plans, la palissade, les 3 portes et les bords ouverts du Bois, les habitants déplacés, le quartier des artisans (forge, tailleur, alambic, four, établi), les points de niveau 1, la porte est retirée, la migration des places. **Correctif du dialogue** : au plus 2 quêtes proposées par habitant, le panneau a une hauteur maximale et défile, les touches 1 à 9. Remplace l'ancien S102 (le quartier des artisans à part). |
| S103 | 5 | **Le moteur des quêtes** : `Interact` (`Town.Props`), `Win`, `Wear`, le butin de quête (`LootDrop.Quest`), les habitants des zones, « ! » et « ? », le journal qui dit où aller, le test « jamais plus de 2 ». Avancé depuis l'ancien S109. |
| S104 | 3 | **Le tuto de Clairval** : les 9 quêtes, les rats du grenier, la recette du pieu, l'équipement de départ (textes du scénariste). |
| S105 | 3 | **La mini-carte.** |
| S108 | 3 | **Les quêtes du Bois et du Bois moisi** : 18 quêtes, le campement des bûcherons, l'ermite. |
| S109 | 3 | **Les quêtes du Lac et des Champs**, sauf la fin : 16 quêtes, Liane, la fermière. |
| S110 | 3 | **Le Disjoint et `four-trails`** : la fin du chapitre (ancien S111). |

**Tests :**
- au plus 2 offres par habitant à chaque niveau, en simulant un joueur qui prend tout ;
- chaque quête a un chemin d'`After` depuis C1 ;
- chaque objectif a sa cible (habitant, objet `Interact`, scénario, objet porté, butin de quête qui tombe bien d'un monstre de la zone) ;
- le butin de quête ne tombe que pendant sa quête ;
- l'XP des quêtes d'une zone reste entre 35 et 60 % de ce qu'il faut pour la traverser ;
- le village : 12 cartes reliées, palissade fermée sauf aux portes, toutes les cases importantes atteignables ;
- l'auto-test Godot fait C1 à C3 et ouvre la mini-carte.

---

## 5 quater. Chantier 3 quater : un métier pour chaque pièce d'équipement (S109 et S110) *(v6, demande de Charles du 17 octobre 2026)*

### 5 quater.1 Ce que Charles a tranché, et ce que j'ai vérifié dans le dépôt

**Tranché par Charles (17 octobre) :**
1. **Quatre métiers d'équipement** : le **forgeron** (armes à une et à deux mains, boucliers), le **tailleur** (plastron, épaulettes, et désormais **coiffe et cape**), le **bijoutier** (anneaux, amulette), le **cordonnier** (bottes, ceinture). Chaque emplacement, sauf le familier et la monture, a son métier.
2. **Une pièce fabriquée ne donne jamais d'initiative, ni PA ni PM**, au même budget que le butin. L'initiative reste le privilège des pièces tombées et des panoplies.
3. **Deux sprints** : S109 le forgeron et le tailleur, S110 le bijoutier et le cordonnier, avec leurs deux ateliers et leurs deux artisans dans la rue des artisans (`clairval-3-1`), textes du scénariste. L'alchimiste et le boulanger passent en S111, toute la suite est décalée d'un cran.
4. **Pas de nouveaux modèles 3D.** Une pièce fabriquée a son icône game-icons.net, comme les autres.

**Vérifié dans le dépôt (commit `bdc1567`) :**
- `data/jobs.json` compte le serrurier (8 clefs, et le pieu de palissade du tuto « en attendant la forge », T75) et les 5 métiers de récolte avec leurs 40 ressources (niveaux 1 à 35). **Aucun métier d'équipement n'existe encore.**
- **Le budget réel des pièces est la moitié de 8 + niveau**, pas 8 + niveau. T63 l'a divisé par deux après la première mesure (la Sentinelle gagnait seule 43 % des hordes du Bois moisi), et `InitiativeTests` vérifie : somme des caractéristiques + 0,6 × initiative / 2 ≤ (8 + niveau) / 2 + 1. Le « budget 8 + niveau » du §3.3 et du §5.5 est le chiffre d'avant T63. **Les pièces d'artisan suivent le budget réel du butin**, comme Charles le veut (« même budget que le butin »).
- **Une arme ne porte que des caractéristiques** : aucune n'a de dégâts propres (`Item` : `Stats`, `Ap`, `Mp`, `Initiative`), et `Equipment.Problem` interdit une arme à deux mains avec une arme à une main ou un bouclier. **Au même budget, une arme à deux mains vaut donc la moitié d'une arme à une main avec un bouclier** : c'est un choix dominé. C'est déjà vrai pour les 4 armes à deux mains des panoplies (`king-tusk`, `elder-staff`, `figurehead-axe`, `debris-pick`).
- **Des objets de départ sont hors budget, et l'entraînement se rejoue** chez Garance (`dialogues/trainer.json`, « Contre l'orque et le braconnier ! », à volonté). Le **plastron d'orque** (niveau 5, 15 Vitalité + 5 Terre = 20 points, 10 % sur l'orque) bat tous les plastrons fabriqués jusqu'au niveau 30 (19 points). Le **bouclier de bois** (niveau 1, 12 Vitalité) et la **ceinture de cuir** (niveau 1, 10 Vitalité), donnés par le tuto, battent les boucliers et les ceintures d'artisan jusqu'au niveau 14. Sans correction, la ligne des plastrons du tailleur ne sert à rien (§5 quater.7).
- `Recipe` n'a pas de champ `Xp`. `Crafting.Xp` vaut 20 + 5 × niveau pour toute recette, et la règle « pas d'XP 20 niveaux sous le métier » n'existe que pour la récolte (`Harvesting`), pas pour la fabrication.
- **Ce qui vient gratuitement :** le filtre de la fenêtre Métiers range les recettes par emplacement (`JobBook.KindOf`) ; le cadre chocolat « artisan » est donné à tout ce qu'un métier fabrique (`ItemStyle.ProvenanceOf`, T66) ; un atelier est dessiné par un anneau chocolat et son nom (`TownView`), sans modèle 3D, et la mini-carte le montre déjà (`MiniMarkKind.Workshop`).
- `ItemBudgetTests`, cité au §3.6 et au §5.10, **n'existe pas** : le budget n'est vérifié que pour les panoplies des familles (`InitiativeTests`). Il est créé au S109, pour tout l'équipement.

### 5 quater.2 L'expérience visée
« Il me manque des bottes. Le cordonnier me les fait à coup sûr, 5 de Terre et 4 de Vitalité. Les bottes de loup, elles, me feraient jouer avant la meute, si elles tombent un jour. »

- **Un plancher garanti dans chaque emplacement** : un joueur seul et malchanceux n'a plus de trou dans son équipement. C'est la motivation de **compétence** (je prépare mon personnage) et d'**autonomie** (je choisis entre le sûr et le rare).
- **Le butin garde ce que le craft n'a pas** : l'initiative, les bonus de panoplie, les PA et PM des 5 pièces. À niveau égal, la pièce fabriquée a **plus de caractéristiques** qu'une pièce rapide (l'initiative coûte du budget) ; la pièce tombée **joue plus tôt** ou complète une panoplie. Aucun des deux ne domine.
- **La boucle méso** : on récolte en traversant une zone, on rapporte les ressources de ses monstres, on fabrique au village en vendant le reste à Rose.

### 5 quater.3 Les quatre métiers

**Le principe des récolteurs.** Chaque métier de fabrication a **un récolteur principal** (le gros des quantités) et **un récolteur secondaire**, plus une ressource de monstre de la zone. Avec 5 métiers de récolte et 6 métiers de fabrication (serrurier à part), un récolteur doit en nourrir deux ; je l'ai réparti pour que **chaque récolteur soit principal au moins une fois et secondaire au moins une fois**, et qu'aucun ne devienne un passage obligé pour tout :

| Récolteur | Principal pour | Secondaire pour |
|---|---|---|
| Bûcheron | forgeron (manches, hampes, boucliers) | cordonnier (semelles, talons, écorce à tanner) |
| Mineur | bijoutier (métaux, cristaux) | forgeron (lames, fers, boucles) |
| Pêcheur | **cordonnier (cuir et colle de poisson)** | bijoutier (écailles et colle de poisson pour sertir), alchimiste (huiles) |
| Paysan | tailleur (fibres, pailles), boulanger (céréales) | — |
| Herboriste | alchimiste (plantes) | tailleur (teintures, fibres d'ortie), boulanger |

- **Le pêcheur devient le récolteur principal du cordonnier.** Jusqu'ici il n'était le principal de personne. Le cuir de poisson (anguille, carpe, brochet) est souple, solide et réel ; c'est une identité propre à Osmose, et elle fait revenir le pêcheur au Lac pour les bottes et ceintures des niveaux 30 et 35 (les Champs n'ont pas d'eau, §5.3).
- **Le tailleur prend l'herboriste en secondaire** (teintures, fibres d'ortie), et non plus le mineur comme dans l'exemple de la v2 (« 6 lin sauvage, 2 cuivre, 3 peaux de loup ») : le mineur aurait été le secondaire de trois métiers.

| Métier (`id`) | Fabrique | Principal | Secondaire | Atelier (fr / en) | Artisan | Sprint |
|---|---|---|---|---|---|---|
| Forgeron / Smith (`smith`) | arme à une main ×3, bouclier ×3, arme à deux mains ×2 | bûcheron | mineur | Forge / Forge | Anselme (existe) | S109 |
| Tailleur / Tailor (`tailor`) | plastron, épaulettes, coiffe, cape : ×2 chacun | paysan | herboriste | Atelier de couture / Tailor's workshop | Perrine (existe) | S109 |
| Bijoutier / Jeweller (`jeweller`) | anneau ×4, amulette ×4 | mineur | pêcheur | Établi de bijouterie / Jeweller's bench | nouveau (scénariste) | S110 |
| Cordonnier / Cobbler (`cobbler`) | bottes ×4, ceinture ×4 | pêcheur | bûcheron | Banc de cordonnerie / Cobbler's bench | nouveau (scénariste) | S110 |

Les noms d'ateliers sont neutres pour ne pas présumer du genre des deux nouveaux artisans. L'anglais suit l'orthographe britannique du code (`Sulphur`, `Armoured`).

**La progression de 1 à 100**, la même pour les quatre :
- **Courbe inchangée** (`Crafting.XpToReach`). **Une recette tous les 5 niveaux**, 8 à Clairval (niveaux 1 à 35), 12 de plus avec le monde après 30 (§8.1), en données seulement.
- **XP d'une pièce d'équipement : 150 + 15 × niveau de la recette** (165 au niveau 1, 675 au niveau 35), comme le prévoyait le §5.4. Je recommande une **règle dans `Crafting.Xp`** (l'équipement d'un côté, le reste à 20 + 5 × niveau) plutôt qu'un champ `xp` dans les données : une seule source de vérité, rien à recopier dans 32 recettes.
- **Pas d'XP pour une recette 20 niveaux ou plus sous le métier**, comme la récolte. À ajouter dans `Crafting.Xp`, qui reçoit le niveau du métier.
- **Le rythme**, en fabriquant la recette la plus haute qu'on peut :

| Tranche | XP à gagner | XP par pièce | Pièces |
|---|---|---|---|
| 1 → 5 | 200 | 165 | 2 |
| 5 → 10 | 700 | 225 | 4 |
| 10 → 15 | 1 200 | 300 | 4 |
| 15 → 20 | 1 700 | 375 | 5 |
| 20 → 25 | 2 200 | 450 | 5 |
| 25 → 30 | 2 700 | 525 | 6 |
| 30 → 35 | 3 200 | 600 | 6 |

  **32 pièces pour monter un métier de 1 à 35**, soit environ 530 ingrédients, dont une centaine de ressources de monstres. Avec la recette du niveau 35, le métier monte jusqu'à 55 environ (20 pièces de plus), comme la récolte. Ce n'est pas une corvée pour un métier ; c'en serait une pour les quatre à la fois, et ce n'est pas demandé : on fabrique d'abord ce qu'on porte.

### 5 quater.4 Les règles communes des pièces d'artisan
- **Le niveau de l'objet** suit celui de la recette, comme au §5.5 : recette 1 → objet 3, 5 → 6, 10 → 10, 15 → 14, 20 → 18, 25 → 22, 30 → 26, 35 → 30.
- **Le budget B** = (8 + niveau de l'objet) / 2, arrondi au supérieur : **6, 7, 9, 11, 13, 15, 17, 19**. C'est le budget du butin (T63) : `king-tusk` (niveau 6) a 7 points, `mole-hat` (25) 17, `debris-pick` (30) 19.
  - **Vitalité + un élément.** L'élément prend la moitié haute, la Vitalité la moitié basse (niveau 6 : 4 dans l'élément, 3 de Vitalité). **Un bouclier fait l'inverse** (la Vitalité d'abord) : c'est la pièce qui protège.
  - **Une arme à deux mains compte pour deux pièces : budget 2 × B**, moitié élément, moitié Vitalité. Elle vaut alors une arme à une main et un bouclier ; elle concentre tout dans un élément, quand la paire en mélange deux. C'est un vrai choix, plus un piège. *[À TRANCHER, question 53]* Je recommande d'appliquer la même règle aux 4 armes à deux mains des panoplies (`sets_gen.py`) ; cela ne change pas le kit « fast » des mesures, qui prend partout l'arme à une main de la famille rapide.
  - **Jamais** d'initiative, de PA, de PM ni de panoplie. Le chargement le refuse (`GameData`).
- **Les éléments.** Chaque zone porte **8 pièces, deux par élément** ; les deux pièces d'un même métier dans une zone ont deux éléments différents. Sur les 35 niveaux, les **4 anneaux, les 4 amulettes, les 4 bottes et les 4 ceintures couvrent chacun les quatre éléments**, et les trois armes à une main comme les trois boucliers en couvrent trois. Il n'y a pas de variante par élément d'une même pièce (×4 objets) : c'est pour plus tard, s'il le faut (question 59).
- **Les ingrédients** : 2 ressources récoltées **du niveau de la recette**, du récolteur principal puis du secondaire, et 1 ressource de monstre **de la zone de ce niveau**. Les quantités montent avec le niveau :

| Niv. de recette | Principal | Secondaire | Monstre | Éclats |
|---|---|---|---|---|
| 1 | 6 | 2 | 2 | 3 |
| 5 | 6 | 3 | 2 | 6 |
| 10 | 8 | 3 | 3 | 10 |
| 15 | 8 | 4 | 3 | 14 |
| 20 | 10 | 4 | 3 | 18 |
| 25 | 10 | 5 | 3 | 22 |
| 30 | 12 | 5 | 4 | 26 |
| 35 | 12 | 6 | 4 | 30 |

  - **Arme à deux mains** : principal et secondaire × 1,5, une ressource de monstre de plus, Éclats × 2.
  - **Une exception** : la ceinture du cordonnier de niveau 5 demande 4 écrevisses au lieu de 6. L'eau du Bois n'est que sur 40 % de ses cartes, et l'écrevisse est au fond (§5.3).
  - Les poissons des niveaux 30 et 35 se pêchent au Lac, alors que leurs recettes prennent une ressource des Champs : c'est déjà la règle du §5.3.
- **Les Éclats = le niveau de l'objet** (le charbon de la forge, le fil, la poix). C'est un petit puits, et il garantit que fabriquer puis revendre ne rapporte jamais plus que revendre ses ingrédients (§5 quater.9).
- **L'icône** : un dessin **déjà téléchargé** (`tools/icons_gen.py`, table `ICONS`), choisi par emplacement, sans `--fetch` ; la couleur de l'élément et le cadre chocolat font la différence. Les pièces d'un même emplacement se ressemblent ; quelques dessins nouveaux pourront venir plus tard (question 61).

### 5 quater.5 Les 32 recettes de Clairval

Notation des caractéristiques : **Vit** Vitalité, **Te** Terre, **Fe** Feu, **Ea** Eau, **Ai** Air. Tous les identifiants de ressources ont été vérifiés dans `data/items.json`. Le niveau d'une ressource de monstre est celui du plus faible monstre qui la laisse (`Shop.Level`) : Bois 1 à 2, Bois moisi 8 à 9, Lac 16 à 17, Champs 24 à 25.

**Forgeron (`smith`), S109.** Bûcheron + mineur + monstre.

| Rec. | `id` | Nom fr / en | Emplacement | Obj. | Caractéristiques | Ingrédients | Éclats | Icône |
|---|---|---|---|---|---|---|---|---|
| 1 | `copper-billhook` | Serpe de cuivre / Copper Billhook | OneHanded | 3 | Ai 3, Vit 3 | `oak-log` 6, `copper-ore` 2, `wolf-fang` 2 | 3 | `lorc/plain-dagger` |
| 5 | `hazel-pavise` | Pavois de noisetier / Hazel Pavise | Shield | 6 | Vit 4, Te 3 | `hazel-log` 6, `tin-ore` 3, `boar-tusk` 2 | 6 | `delapouite/viking-shield` |
| 10 | `blackwood-maul` | Merlin de bois noir / Blackwood Maul | TwoHanded | 10 | Fe 9, Vit 9 | `blackwood-log` 12, `iron-ore` 5, `old-bone` 4 | 20 | `delapouite/wood-club` |
| 15 | `yew-targe` | Targe d'if / Yew Targe | Shield | 14 | Vit 6, Ea 5 | `yew-log` 8, `sulphur` 4, `mushroom-cap` 3 | 14 | `delapouite/viking-shield` |
| 20 | `willow-cutlass` | Coutelas de saule / Willow Cutlass | OneHanded | 18 | Ea 7, Vit 6 | `willow-log` 10, `blue-clay` 4, `crab-claw` 3 | 18 | `lorc/meat-cleaver` |
| 25 | `alder-greataxe` | Grande hache d'aulne / Alder Greataxe | TwoHanded | 22 | Te 15, Vit 15 | `alder-log` 15, `zinc-ore` 8, `crab-shell` 4 | 44 | `lorc/battle-axe` |
| 30 | `ash-shield` | Écu de frêne / Ash Heater Shield | Shield | 26 | Vit 9, Ai 8 | `ash-log` 12, `silver-ore` 5, `mole-fur` 4 | 26 | `delapouite/viking-shield` |
| 35 | `linden-falchion` | Fauchon de tilleul / Linden Falchion | OneHanded | 30 | Fe 10, Vit 9 | `linden-log` 12, `quartz` 6, `honeycomb` 4 | 30 | `lorc/meat-cleaver` |

L'argile bleue du coutelas sert à tremper la lame (on l'enduit d'argile avant la trempe) : le mineur garde un rôle au niveau 20. **Si Charles refuse la règle des deux mains** (question 53), le merlin passe à Fe 5, Vit 4 et la grande hache à Te 8, Vit 7, avec les quantités d'une arme à une main.

**Tailleur (`tailor`), S109.** Paysan + herboriste + monstre. Il alterne par zone : plastron et coiffe au Bois, épaulettes et cape au Bois moisi, plastron et coiffe au Lac, épaulettes et cape aux Champs. Le plastron et les épaulettes, qui ne tombent jamais, changent ainsi tous les 15 niveaux environ, en décalé (plastron 3 et 18, épaulettes 10 et 26). Le Bois, dont les panoplies n'ont pas de coiffe, a la sienne.

| Rec. | `id` | Nom fr / en | Emplacement | Obj. | Caractéristiques | Ingrédients | Éclats | Icône |
|---|---|---|---|---|---|---|---|---|
| 1 | `linen-tunic` | Tunique de lin / Linen Tunic | Chest | 3 | Ea 3, Vit 3 | `wild-flax` 6, `nettle` 2, `wolf-pelt` 2 | 3 | `lorc/breastplate` |
| 5 | `oat-straw-hat` | Chapeau de paille d'avoine / Oat-Straw Hat | Hat | 6 | Fe 4, Vit 3 | `wild-oats` 6, `wood-mint` 3, `boar-bristle` 2 | 6 | `delapouite/outback-hat` |
| 10 | `hemp-shoulders` | Épaulières de chanvre / Hemp Shoulder Guards | Shoulders | 10 | Te 5, Vit 4 | `pool-hemp` 8, `glowmoss` 3, `rotten-cloth` 3 | 10 | `skoll/pauldrons` |
| 15 | `rye-pelerine` | Pèlerine de seigle / Rye Pelerine | Cape | 14 | Ai 6, Vit 5 | `rye` 8, `clubmoss` 4, `spore-dust` 3 | 14 | `delapouite/cape` |
| 20 | `sedge-gambeson` | Gambison de laîche / Sedge Gambeson | Chest | 18 | Fe 7, Vit 6 | `shore-sedge` 10, `water-lily` 4, `crab-shell` 3 | 18 | `lorc/breastplate` |
| 25 | `rice-sunhat` | Capeline de riz des joncs / Reed-Rice Sunhat | Hat | 22 | Ai 8, Vit 7 | `reed-rice` 10, `horsetail` 5, `slime` 3 | 22 | `delapouite/flower-hat` |
| 30 | `wheat-spaulders` | Épaulières de blé tressé / Braided Wheat Spaulders | Shoulders | 26 | Ea 9, Vit 8 | `clairval-wheat` 12, `hive-flower` 5, `bee-wing` 4 | 26 | `skoll/pauldrons` |
| 35 | `harvest-mantle` | Mante de moisson / Harvest Mantle | Cape | 30 | Te 10, Vit 9 | `buckwheat` 12, `lavender` 6, `mole-fur` 4 | 30 | `lucasms/cloak` |

**Bijoutier (`jeweller`), S110.** Mineur + pêcheur + monstre. Un anneau puis une amulette dans chaque zone.

| Rec. | `id` | Nom fr / en | Emplacement | Obj. | Caractéristiques | Ingrédients | Éclats | Icône |
|---|---|---|---|---|---|---|---|---|
| 1 | `copper-band` | Jonc de cuivre / Copper Band | Ring | 3 | Te 3, Vit 3 | `copper-ore` 6, `gudgeon` 2, `boar-tusk` 2 | 3 | `delapouite/ring` |
| 5 | `tin-pendant` | Pendentif d'étain / Tin Pendant | Amulet | 6 | Ea 4, Vit 3 | `tin-ore` 6, `crayfish` 3, `wolf-fang` 2 | 6 | `delapouite/tribal-pendant` |
| 10 | `blackened-iron-ring` | Anneau de fer noirci / Blackened Iron Ring | Ring | 10 | Fe 5, Vit 4 | `iron-ore` 8, `pool-eel` 3, `old-bone` 3 | 10 | `delapouite/power-ring` |
| 15 | `sulphur-medallion` | Médaillon de soufre / Sulphur Medallion | Amulet | 14 | Ai 6, Vit 5 | `sulphur` 8, `tench` 4, `spore-dust` 3 | 14 | `lorc/gem-pendant` |
| 20 | `blue-clay-ring` | Anneau d'argile bleue / Blue Clay Ring | Ring | 18 | Ea 7, Vit 6 | `blue-clay` 10, `perch` 4, `crab-claw` 3 | 18 | `delapouite/frozen-ring` |
| 25 | `zinc-torc` | Torque de zinc / Zinc Torc | Amulet | 22 | Te 8, Vit 7 | `zinc-ore` 10, `carp` 5, `frog-leg` 3 | 22 | `delapouite/primitive-necklace` |
| 30 | `silver-signet` | Chevalière d'argent / Silver Signet | Ring | 26 | Ai 9, Vit 8 | `silver-ore` 12, `pike` 5, `honeycomb` 4 | 26 | `delapouite/globe-ring` |
| 35 | `quartz-pendant` | Pendeloque de quartz / Quartz Drop Pendant | Amulet | 30 | Fe 10, Vit 9 | `quartz` 12, `zander` 6, `bee-wing` 4 | 30 | `lorc/gem-pendant` |

Le jonc de cuivre ne remplace pas l'**anneau de cuivre** du tuto (`copper-ring`, niveau 1, Vit 5) : deux noms distincts, deux objets distincts.

**Cordonnier (`cobbler`), S110.** Pêcheur + bûcheron + monstre. Des bottes puis une ceinture dans chaque zone ; les bottes du Bois moisi comblent le seul emplacement léger que ses panoplies n'ont pas.

| Rec. | `id` | Nom fr / en | Emplacement | Obj. | Caractéristiques | Ingrédients | Éclats | Icône |
|---|---|---|---|---|---|---|---|---|
| 1 | `gudgeon-shoes` | Souliers en peau de goujon / Gudgeon-Skin Shoes | Boots | 3 | Ai 3, Vit 3 | `gudgeon` 6, `oak-log` 2, `wolf-pelt` 2 | 3 | `lorc/leather-boot` |
| 5 | `crayfish-belt` | Ceinture à écailles d'écrevisse / Crayfish-Scale Belt | Belt | 6 | Fe 4, Vit 3 | `crayfish` 4, `hazel-log` 3, `boar-bristle` 2 | 6 | `lucasms/belt` |
| 10 | `eel-skin-boots` | Bottes en peau d'anguille / Eel-Skin Boots | Boots | 10 | Te 5, Vit 4 | `pool-eel` 8, `blackwood-log` 3, `mushroom-cap` 3 | 10 | `delapouite/rubber-boot` |
| 15 | `tench-girdle` | Ceinturon de tanche / Tench-Leather Girdle | Belt | 14 | Ea 6, Vit 5 | `tench` 8, `yew-log` 4, `rotten-cloth` 3 | 14 | `delapouite/belt-armor` |
| 20 | `perch-waders` | Cuissardes en peau de perche / Perch-Skin Waders | Boots | 18 | Fe 7, Vit 6 | `perch` 10, `willow-log` 4, `frog-leg` 3 | 18 | `delapouite/rubber-boot` |
| 25 | `carp-belt` | Ceinture en cuir de carpe / Carp-Leather Belt | Belt | 22 | Ai 8, Vit 7 | `carp` 10, `alder-log` 5, `slime` 3 | 22 | `lucasms/belt` |
| 30 | `pike-skin-boots` | Bottes en peau de brochet / Pike-Skin Boots | Boots | 26 | Ea 9, Vit 8 | `pike` 12, `ash-log` 5, `mole-fur` 4 | 26 | `delapouite/fur-boot` |
| 35 | `zander-baldric` | Baudrier de sandre / Zander Baldric | Belt | 30 | Te 10, Vit 9 | `zander` 12, `linden-log` 6, `honeycomb` 4 | 30 | `delapouite/belt-armor` |

**Contrôle par zone** (les 8 pièces d'une zone, deux par élément) :

| Zone (objets) | Terre | Feu | Eau | Air | Ressources de monstres (fois) |
|---|---|---|---|---|---|
| Bois (3, 6) | pavois, jonc | chapeau d'avoine, ceinture d'écrevisse | tunique, pendentif | serpe, souliers | défense 2, croc 2, peau de loup 2, soies 2 |
| Bois moisi (10, 14) | épaulières de chanvre, bottes d'anguille | merlin, anneau de fer | targe, ceinturon | pèlerine, médaillon | vieil os 2, chapeau de champignon 2, tissu pourri 2, spores 2 |
| Lac (18, 22) | grande hache, torque | gambison, cuissardes | coutelas, anneau d'argile | capeline, ceinture de carpe | pince 2, carapace 2, bave 2, cuisse 2 |
| Champs (26, 30) | mante, baudrier | fauchon, pendeloque | épaulières de blé, bottes de brochet | écu, chevalière | fourrure de taupe 3, rayon de miel 3, aile 2, ver de terre 0 |

Le ver de terre reste aux galeries (clef du serrurier) et à l'alchimiste (huile de Terre, S111) : un ver de terre dans une botte, personne n'y croirait.

**Le pieu de palissade** (tuto C5) passe du serrurier au **forgeron**, au niveau 1, avec les mêmes ingrédients (3 chênes, 2 cuivres) : le texte du scénariste dit déjà « à la forge » (T75). Il reste une ressource, à 25 XP.

### 5 quater.6 Quelles pièces tombent, lesquelles se fabriquent *(remplace le tableau du §5.6)*

| Emplacement | Butin (panoplies des familles) | Artisanat (niveaux des objets) | Initiative |
|---|---|---|---|
| Coiffe | oui (toutes les zones sauf le Bois) | tailleur : 6, 22 | non |
| Cape | oui | tailleur : 14, 30 | **oui, butin seulement** |
| Anneaux (2) | oui | bijoutier : 3, 10, 18, 26 | **oui, butin seulement** |
| Amulette | oui | bijoutier : 6, 14, 22, 30 | non |
| Bottes | oui (toutes sauf le Bois moisi) | cordonnier : 3, 10, 18, 26 | **oui, butin seulement** |
| Ceinture | oui | cordonnier : 6, 14, 22, 30 | non |
| Arme à une main | l'arme du chef | forgeron : 3, 18, 30 | non |
| Arme à deux mains | l'arme du chef | forgeron : 10, 22 | non |
| Bouclier | le bouclier de carapace (crabes) | forgeron : 6, 14, 26 | non |
| Plastron, épaulettes | **jamais** (le plastron et les épaulettes d'orque de l'entraînement, remis au budget) | tailleur : plastron 3, 18 ; épaulettes 10, 26 | non |
| Familier, monture | ni l'un ni l'autre (l'âne et le chaton restent) | — | — |
| Potions, huiles, pains | — | alchimiste, boulanger (S111) | — |

- **Ce qui reste au butin :** l'initiative (bottes, cape, anneaux), les bonus de panoplie, les PA et PM des 5 pièces. **Rose ne vend toujours aucun équipement** (T65) : sinon on achèterait le craft.
- **Pas de pièce de panoplie en récompense de quête** (§5 ter.4) : inchangé.

### 5 quater.7 Les objets de départ remis au budget (S109)
Sans cela, deux lignes du craft sont mortes dès le premier jour. Les objets gardent leur identifiant ; leurs chiffres sont dans les données, donc les exemplaires déjà possédés changent sans migration.

| Objet | Niveau | Aujourd'hui | Proposé | Pourquoi |
|---|---|---|---|---|
| `orc-breastplate` (l'orque, 10 %, rejouable) | 5 | Vit 15, Te 5 (20) | Vit 4, Te 3 (7) | battait tous les plastrons fabriqués jusqu'au niveau 30 |
| `orc-club` (l'orque, 5 %) | 8 | Te 15 | Te 16 (deux mains, 2 × 8) ; Te 8 si la question 53 est refusée | presque inchangé avec la règle des deux mains |
| `wooden-shield` (tuto) | 1 | Vit 12 | Vit 5 | battait les boucliers fabriqués jusqu'au niveau 14 |
| `leather-belt` (tuto) | 1 | Vit 10 | Vit 5 | battait les ceintures fabriquées jusqu'au niveau 6 |
| `poacher-hat` | 1 | Vit 8 | Vit 5 | au-dessus du budget de 5,5 |
| `poacher-cape` | 1 | Ai 6 | Ai 5 | idem |

`orc-pauldrons` (Te 6), `copper-ring`, `pebble-amulet`, `dagger`, `poacher-boots` et `ember-ring` sont déjà dans le budget. **Le bonus de la panoplie du braconnier** (+10 Air dès deux pièces, au niveau 1) est à ramener à +5, comme les familles l'ont été (T63). Le familier et la monture restent hors de la règle.

### 5 quater.8 L'équilibre T41

**Combien de pièces d'artisan par zone.** Chaque zone a **8 pièces fabricables**. Dans le kit « fast » (le meilleur cas du butin, `Progression.Kit`), les panoplies remplissent déjà la plupart des places ; l'artisan n'y ajoute que **les places vides** :

| Zone | Places vides du kit « fast » | Pièces d'artisan ajoutées |
|---|---|---|
| Bois | coiffe, plastron, épaulettes, bouclier | 3 (pas d'épaulettes avant le niveau 10) |
| Bois moisi | bottes, plastron, épaulettes, bouclier | 4 |
| Lac | plastron, épaulettes (le bouclier de carapace y est) | 2 |
| Champs | plastron, épaulettes, bouclier | 3 |

**Ce que ça pèse** (estimation à la main, à remesurer ; au niveau de fin de chaque zone, en points de caractéristiques) :

| Zone (niveau) | Points du héros | Kit « fast » (butin) | « fast » + artisan | Kit « craft » (que de l'artisan) |
|---|---|---|---|---|
| Bois (6) | 50 | ≈ 33, + 50 d'initiative | ≈ 53 (+24 % sur héros + kit) | ≈ 52, initiative 10 |
| Bois moisi (13) | 120 | ≈ 67 | ≈ 98 (+17 %) | ≈ 78 |
| Lac (22) | 210 | ≈ 119 | ≈ 141 (+7 %) | ≈ 143 |
| Champs (30) | 290 | ≈ 130 | ≈ 177 (+11 %) | ≈ 185 |

**Ce que j'en tire :**
- Le kit « craft » a **à peu près les caractéristiques du kit « fast »**, un peu plus, mais **sans initiative ni PA ni PM** : il joue toujours en dernier. Les mesures « fast » de T63 et T64 en donnent l'ordre de grandeur : sous 15 % dans le Bois, au Bois moisi et au Lac, hors des cartes à eau déjà acceptées (T59, T60), et au-dessus aux Champs (taupes lanceuses, acceptées par Charles, T64).
- « fast » + artisan monte surtout **dans le Bois et au Bois moisi** (+17 à +24 %), parce qu'à bas niveau le héros a peu de points et que chaque pièce pèse lourd. C'est là qu'un plafond peut sauter.
- **Je ne baisse rien d'avance.** Charles veut le même budget que le butin ; la mesure tranche.

**La règle de mesure, v6 :**
- **Planchers** (petits groupes ≥ 96 %) : **nus**, sans consommable, comme toujours.
- **Plafonds**, trois kits :
  - **`zone`** (deux pièces rapides) : le test bloquant de T64, **≤ 20 %**, inchangé ;
  - **`craft`** (nouveau) : les deux pièces rapides de `zone` et, dans toutes les autres places, **la meilleure pièce fabriquée de niveau ≤ au héros, toutes zones**. C'est ce qu'un joueur seul a à coup sûr. **Nouveau test bloquant, ≤ 20 %**, avec les mêmes cartes exclues que T64 ;
  - **`fast`** : il prend désormais aussi la meilleure pièce fabriquée dans les places vides, et, à partir du S111, le meilleur consommable (le §5.7). C'est un **indicateur**, noté dans `DECISIONS.md`, pas un test : il dépassait déjà aux Champs et sur les cartes à eau, et Charles l'a accepté.
- **Si `craft` dépasse 20 %**, dans l'ordre : enlever 1 point de Vitalité aux pièces d'artisan de niveau ≤ 14 (celles qui pèsent le plus) ; puis, **avec l'accord de Charles seulement**, passer toutes les pièces d'artisan à 80 % du budget du butin. **Jamais les monstres** (§3.5).
- **L'initiative** : inchangée par le craft, aucune pièce d'artisan n'en donne.

### 5 quater.9 L'économie avec Rose
- **Rose rachète une pièce fabriquée 2 × son niveau** (T65), de 6 à 60 Éclats. Elle ne vend aucun équipement.
- **Pas de boucle infinie.** Ce que valent les ingrédients d'une recette chez Rose, en rachat (1 + niveau / 10 par ressource) et à l'achat (5 fois plus), comparé au rachat de la pièce :

| Recette | Ingrédients revendus à Rose | Ingrédients achetés à Rose | Éclats de la recette | Pièce revendue |
|---|---|---|---|---|
| 1 (objet 3) | 10 | 50 | 3 | 6 |
| 5 (objet 6) | 11 | 55 | 6 | 12 |
| 15 (objet 14) | 27 | 135 | 14 | 28 |
| 25, deux mains (objet 22) | 77 | 385 | 44 | 44 |
| 35 (objet 30) | 84 | 420 | 30 | 60 |

  - **Acheter, fabriquer, revendre** coûte toujours 4 à 9 fois ce que la pièce rapporte.
  - **Récolter, fabriquer, revendre** rapporte moins que revendre ses ingrédients bruts, **grâce aux Éclats de la recette** : sans eux, les recettes 5 et 15 rapporteraient un ou deux Éclats de plus que le brut. C'est la raison de ce coût.
  - **Fabriquer reste rentable autrement** : l'XP du métier, et la pièce qu'on porte.
- **Le joueur pressé** achète ses ingrédients : environ 50 Éclats pour la serpe, 420 pour le fauchon, soit treize monstres au niveau 30 (2 + niveau par monstre). C'est un puits utile, pas une porte.
- **Le récolteur** revend toujours 1 à 4 Éclats par ressource, bien moins qu'un combat : la récolte n'est pas une ferme à Éclats (§5.7, inchangé).

### 5 quater.10 Les ateliers et les artisans dans la rue des artisans (`clairval-3-1`)

**Le plan actuel** (`tools/village_gen.py`) : la carte a deux routes, **nord-sud en x = 24** (bords nord et sud) et **vers l'ouest en y = 16**, avec une case libre de chaque côté (x 23 à 25, y 15 à 17). L'est est la palissade. Les artisans existants sont **à l'est de la route** (x = 26 à 28), le serrurier **à l'ouest**, au nord :

| Qui | Maison (coin haut-gauche, porte au sud) | Habitant | Atelier | Sprint |
|---|---|---|---|---|
| Gédéon, serrurier | (19, 8) | (21, 11) | établi (19, 10), existe | — |
| Anselme, forgeron | (27, 4) | (26, 5) | **forge (27, 6)** | S109 |
| Perrine, tailleuse | (27, 9) | (26, 10) | **atelier de couture (27, 11)** | S109 |
| Sidonie, alchimiste | (27, 20) | (26, 21) | alambic (27, 22), réservé | S111 |
| Basile, boulanger | (27, 25) | (26, 26) | four (27, 27), réservé | S111 |
| **le bijoutier** (nouveau) | **(19, 19)** | **(21, 22)** | **établi de bijouterie (19, 21)** | S110 |
| **le cordonnier** (nouveau) | **(19, 25)** | **(21, 28)** | **banc de cordonnerie (19, 27)** | S110 |

```
        x=19     x=24 (route N-S)   x=27
y=4                  =             [A]  Anselme, forge (27,6)
y=8    [G]           =
y=9                  =             [P]  Perrine, couture (27,11)
y=10   établi        =
y=16  ===============+                  route vers la place (ouest)
y=19   [B]           =             [S]  Sidonie (y=20), alambic (27,22)
y=21   bijouterie    =
y=25   [C]           =             [Bo] Basile, four (27,27)
y=27   cordonnerie   =
```

- **Le même gabarit que Gédéon** : la maison, l'atelier juste sous sa porte, l'artisan deux cases à droite et trois en dessous du coin de la maison. Le côté ouest, au sud de la route de l'ouest, fait pendant à Sidonie et Basile. Toutes les cases sont hors des routes et de leur bordure, ce que `village_gen.py` vérifie déjà (`assert` des maisons et des habitants), comme le chemin depuis l'arrivée vers chaque habitant et chaque atelier (`reachable`).
- **Ce qui bouge** : `HOMES[(3, 1)]` gagne deux maisons, `PEOPLE[(3, 1)]` deux habitants, `BENCHES[(3, 1)]` quatre ateliers (forge et couture au S109, les deux autres au S110), **ajoutés après l'établi du serrurier** : l'auto-test prend aujourd'hui `Workshops[0]` (`Main.Town.cs`), qui doit rester l'établi, ou mieux, chercher l'atelier par métier.
- **Un effet de bord** : les 6 maisons tirées au hasard de la carte changent de place quand on ajoute des maisons fixes. Un personnage enregistré sur `clairval-3-1` peut se retrouver sur une case devenue maison. Le S109 vérifie que le serveur replace à l'arrivée un personnage sur une case bloquée ; sinon, une migration comme `MoveToTwelveMapVillage`.
- **Le décor** : aucun modèle nouveau. Un atelier est un anneau chocolat et son nom (`TownView`), une maison est la maison du village.

**Pour le scénariste** (lui écrit les mots, comme au §5 ter.5) : les deux nouveaux artisans, leur nom, leur ton, un trait, leur lien avec le fil de la pierre, 1 à 3 répliques de base, en fr et en en. Pistes, à prendre ou à laisser : le bijoutier sertit des pierres de la petite carrière et remarque qu'elles ont toutes quatre veines qui ne se touchent jamais ; le cordonnier tanne le cuir des poissons du lac et dit que les semelles de Clairval s'usent toutes du côté de l'ouest. **Prénoms interdits : Margaux et Camille.** Rien d'Ankama.

**Une quête d'initiation par artisan** *[À TRANCHER, question 60]* : le §5.5 en prévoyait une par atelier. Je propose, en données seulement (le moteur existe) : chaque artisan propose, après « Des pieux pour la palissade » (C5), « Ta première pièce » : `Bring` sa pièce de niveau 1 (serpe, tunique, jonc, souliers), niveau 3, récompense 40 × niveau d'XP et 30 Éclats. Au plus 2 offres par habitant : chacun n'en a qu'une.

### 5 quater.11 Le découpage : S109 et S110

Le plan donnait **3 points** à « forgeron et tailleur, 16 pièces ». Réestimé honnêtement : il manquait le budget réel, la règle des deux mains, les objets de départ, les deux kits de mesure et la remesure de T41, et le tailleur passe de 2 à 4 emplacements. **32 pièces sur deux sprints, 8 points.**

| Sprint | Pts | Contenu |
|---|---|---|
| **S109** | **5** | **Forgeron et tailleur.** Un nouvel outil **`tools/craft_gen.py`** (annoncé au §5.11) écrit les métiers d'équipement (`smith`, `tailor`, atelier et recettes), leurs 16 pièces (chiffres calculés par la règle du §5 quater.4, pas écrits à la main) et leurs icônes, en gardant les autres métiers ; il se lance après `trades_gen.py` et avant `icons_gen.py`. Le pieu de palissade passe à la forge. `Crafting.Xp` : 150 + 15 × niveau pour l'équipement, rien 20 niveaux sous le métier, appliqué par le serveur. **`ItemBudgetTests`** pour tout l'équipement ; la règle des deux mains (artisanat et butin, `sets_gen.py`) ; les objets de départ remis au budget (§5 quater.7). `Progression.Kit` : le kit `craft`, et `fast` complété par l'artisan ; `Rpg.Sim --kit craft`. **T41 remesuré** dans les quatre zones, trois classes, kits `none`, `zone`, `craft`, `fast` (entrée T77). La forge et l'atelier de couture au quartier. |
| **S110** | **3** | **Bijoutier et cordonnier.** `craft_gen.py` gagne `jeweller` et `cobbler` et leurs 16 pièces ; les deux maisons, les deux artisans et leurs dialogues (`village_dialogues.py`, textes du scénariste), les deux ateliers ; un même anneau ne se porte pas deux fois (question 54) ; **T41 remesuré** (le kit `craft` gagne anneaux, amulette, bottes et ceinture) ; les quêtes d'initiation si Charles les veut (question 60). |

**Tests attendus :**
- **Unitaires (`Rpg.Core.Tests`) :**
  - `ItemBudgetTests` : chaque pièce d'équipement (sauf familier et monture) a somme + 0,6 × initiative / 2 ≤ B + 1, B doublé pour une arme à deux mains ; **chaque pièce fabriquée** a B − 1 ≤ somme ≤ B + 1, Vitalité et un seul élément, aucune initiative, aucun PA ni PM, aucune panoplie ; le chargement refuse une pièce fabriquée avec de l'initiative ;
  - chaque métier d'équipement a **une recette tous les 5 niveaux de 1 à 35**, chacune pour un emplacement de son métier ;
  - chaque recette d'équipement a **trois ingrédients** : deux récoltés **au niveau de la recette**, du récolteur principal puis du secondaire du métier, et une ressource de monstre de niveau compris entre (niveau de l'objet − 10) et le niveau de l'objet ;
  - les 8 pièces d'une zone ont **deux pièces par élément**, et les deux pièces d'un métier dans une zone deux éléments différents ; les anneaux, amulettes, bottes et ceintures couvrent les quatre éléments ;
  - `Crafting.Xp` : 165 au niveau 1, 675 au niveau 35, 25 pour le pieu, 0 vingt niveaux sous le métier ;
  - le prix des Éclats : égal au niveau de l'objet, double pour une arme à deux mains ; **pour chaque recette, ingrédients revendus + Éclats > pièce revendue** (pas de boucle) ;
  - `Progression.Kit("craft", …)` est portable (`Equipment.Problem` nul), sans initiative au-delà des deux pièces de `zone` ; `fast` remplit ses places vides avec l'artisan ;
  - (S110) deux anneaux identiques sont refusés, si la question 54 est acceptée.
- **Intégration (`Rpg.Api.Tests`) :** de bout en bout, récolter du chêne et du cuivre, gagner des crocs de loup, fabriquer la serpe à la forge, l'équiper, la revendre à Rose au bon prix ; refus sans les Éclats de la recette ; l'XP du métier gardée. Au S110, la même chose pour le jonc de cuivre à l'établi de bijouterie.
- **Village :** la forge, l'atelier de couture (S109), l'établi de bijouterie et le banc de cordonnerie (S110) existent, hors des routes, atteignables depuis l'arrivée ; chaque artisan est sur sa carte avec son dialogue.
- **T41 :** les planchers inchangés, `zone` et `craft` ≤ 20 % hors des cartes acceptées.
- **Auto-test Godot :** à la forge, la fenêtre Métiers s'ouvre sur le forgeron et fabrique la serpe ; au S110, la même chose chez le cordonnier.

### 5 quater.12 Risques
- **Le plafond du Bois** : à bas niveau, une pièce pèse lourd (§5 quater.8). La mesure du S109 le dira ; les leviers sont prêts et ne touchent pas aux monstres.
- **Le volume de monstres** : environ 100 ressources de monstres pour monter un métier de 1 à 35, en concurrence avec les clefs du serrurier. Rose les vend ; au playtest du S111, demander « avez-vous manqué de ressources de monstres ? ». Si oui, on baisse la colonne « Monstre », pas les taux de butin.
- **Les textes du scénariste** pour les deux nouveaux artisans **doivent être prêts avant le S110**, sinon le sprint glisse.
- **Des icônes qui se ressemblent** : 32 pièces sur une quinzaine de dessins. La couleur de l'élément et le cadre suffisent pour un premier jet.
- **Les noms** des 32 pièces sont des propositions, à relire par le scénariste et par Charles.

---

## 6. Chantier 4 : jouer ensemble (S111 à S114, 18 points ; S115 à S118 depuis la v6)

### 6.1 L'expérience visée
« Je vois Plop passer sur la carte. On se groupe, il attaque la horde du fond, je le rejoins, et la reine tombe à deux. Après, je lui échange mes saules contre son arme de reine. »

### 6.2 Le choix d'architecture : un combat en pas à pas, servi par le serveur
Le moteur est déjà **déterministe et piloté par les actions** (T4).
- Le serveur garde le `Fight` en mémoire et reçoit les actions du joueur dont c'est le tour, qu'il vérifie avec les mêmes règles. Il joue les monstres avec `Ai`, puis diffuse chaque action acceptée, numérotée. Chaque client rejoue ces actions dans son propre `Fight`. Le transport passe par une connexion WebSocket comme celle du chat (T51).
- **Tour limité à 30 secondes.** Un joueur déconnecté voit son héros passer ses tours, et reçoit l'historique en revenant.
- La récompense est donnée **pour chaque héros** par le serveur. Le combat seul garde le chemin actuel (ticket + enregistrement rejoué).

### 6.3 Découpage
| Sprint | Pts | Contenu |
|---|---|---|
| S111 | 3 | **Se voir.** Le serveur diffuse aux joueurs d'une carte l'arrivée, la case visée et le départ de chacun, ainsi que l'état des points de récolte. **Les groupes de monstres deviennent communs** : leur errance se calcule à partir de l'horloge, et « battu » et « en combat » passent côté serveur. |
| S112 | 5 | **Le groupe et l'échange.** Inviter au groupe (4 au plus), un chef de groupe, le canal de groupe du chat, le bandeau du groupe ; **l'échange entre deux joueurs** de la même carte (objets et Éclats, chacun valide, le serveur fait tout ou rien). |
| S113 | 5 | **Le combat à plusieurs : règles et serveur.** `Fight` prend une liste de héros ; `FightRecord` passe à une nouvelle version ; XP, Éclats, butin (tiré pour chacun), quêtes et **vie de départ et de fin** par héros (§5 bis) ; un consommable par héros ; le hub de combat, le tour de 30 s, la reconnexion ; le simulateur fait jouer plusieurs héros par l'IA. |
| S114 | 5 | **Le combat à plusieurs : client et donjons.** « Rejoindre » pendant 15 s ; la frise et la fin de combat à plusieurs lignes ; les donjons à plusieurs (chacun use sa clef) ; l'équilibre à plusieurs (§6.4). |

### 6.4 Équilibre à plusieurs (validé par Charles)
Mesuré avec des héros joués par l'IA, en compositions de classes mélangées, au niveau de la zone, avec le kit « zone » :
- hordes : **2 joueurs ≈ 50 %, 3 joueurs ≥ 80 %** ;
- chefs et salles 3 : **3 joueurs ≥ 70 %, 4 joueurs ≥ 90 %**.

On règle les PV du chef seul. **XP :** la règle actuelle reste (XP entière pour chacun, +10 % par allié).

### 6.5 Risques
C'est le chantier le plus risqué : il touche le moteur, le serveur (un état en mémoire, un seul processus), le client et l'auto-test. Il faut **un test d'intégration à deux clients WebSocket** dès S113, et accepter qu'un sprint glisse.

---

## 7. Chantier 5 : la fin du chapitre 1 *(remplacé en v5 par le §5 ter)*

> **Depuis la v5**, la chaîne principale de 1 à 30, les objectifs `Interact` et le Disjoint sont décrits au §5 ter.4 et découpés au §5 ter.7 (S103, S108 à S110). Ce qui suit reste comme source pour le scénariste (titres, idées d'objectifs), mais le découpage ci-dessous n'est plus le plan.

- **(ancien) S109 (3) :** l'objectif `Interact` (un objet posé sur une carte, `Town.Props`) ; la chaîne d'Ysaline après « Quatre pistes », en **option A** de la trame (chapitre 1 = niveaux 1 à 30) : « Ce qui pousse entre » (10), « La crypte que personne n'a creusée » (12), « Crypte oubliée » (14, `Room: 2` puis `Interact` sur le sceau).
- **(ancien) S110 (3) :** le Lac et les Champs, de « La berceuse que personne n'a apprise » (18) à « Sous les haies » (28). **Liane** est la seule nouvelle PNJ (v5 : d'autres habitants s'ajoutent, §5 ter.5).
- **(ancien) S111 (3) :** le **Disjoint** (`design-familles.md` §4.9), **gagnable seul au niveau 30 (50 à 70 %)**, « groupe conseillé » ; puis « Quatre routes » (30). Tant que le chantier 6 n'existe pas, le chapitre se termine sur une promesse, sans contenu fantôme.
- **Écriture :** agent `scenariste`, puis relecture de Charles. **Récompenses :** environ 100 × niveau en XP, pas d'équipement (le butin et l'artisanat restent ses sources). Une quête de la chaîne peut demander une ressource récoltée (« trois joncs pour Liane ») : c'est un clin d'œil aux métiers, jamais une porte (Rose la vend).

**Tests :** chaque quête de la chaîne se prend à son niveau et se finit seul (objectifs de donjon ≤ salle 2) ; `Interact` refuse un objet d'une autre carte ; le Disjoint perd 15 PV par tour et explose ; intégration : toute la chaîne de 1 à 30 jouée par l'API.

---

## 8. Chantier 6 et ce qui reste hors classement

### 8.1 Le monde après 30 (conditionnel)
`design-familles.md` §7 reste la référence. Proposition minimale quand Charles le décidera : Quatre-Chemins et **une seule région** de deux zones, avec une seule mécanique nouvelle, soit environ 6 sprints. Les métiers y gagnent leurs ressources des niveaux 40 et 45 (et la région suivante les niveaux 50 à 60), en données (§5.3), ainsi que les recettes correspondantes. Il faut d'abord l'initiative des monstres qui monte avec leur niveau (§3.5) et l'équilibre de haut niveau (T29).

### 8.2 Petits chantiers à glisser si la place le permet
| Chantier | Pourquoi | Taille |
|---|---|---|
| **L'IA voit les passifs** | les leçons de D69 comptent plus à plusieurs | 3 pts |
| Tutoriel de première connexion | demandé le 9 octobre, après les quêtes | 3 pts |
| Apparence en pièces séparées, puis barbier (puits d'Éclats) | souhait de Charles, puits purement cosmétique | 5 pts, en attente de l'essai des modèles |
| Bestiaire | rend lisibles les leçons, les taux de butin et les ressources | 3 pts |
| Guildes | après le groupe | 5 pts |
| ~~Bijoutier, cordonnier, paysan et cuisine~~ | ~~élargir l'artisanat quand le butin aura ses panoplies 30+~~ : le paysan est fait (S99), le bijoutier et le cordonnier entrent au plan en v6 (S110) ; reste une cuisine à effet durable | 3 pts |

---

## 9. Questions à trancher par Charles

### 9.1 Déjà tranchées (v1, validées par Charles le 15 octobre)
1. L'ordre des chantiers : 0, puis le butin, puis les Éclats, puis le jeu à plusieurs, puis l'histoire. Les métiers s'y insèrent, voir 9.2, question 12.
2. Le correctif de la Bauge : salle 2, clef offerte, la chaîne principale toujours faisable seul.
3. L'ordre du tour : trié par initiative, à égalité les héros d'abord.
4. L'initiative : seulement l'équipement et les panoplies, base des classes à 10.
5. Les valeurs : 10 / 15 / 20 / 25 par pièce, sur bottes, cape et anneaux, coût de 0,6 ; panoplies rapides Loups, Grenouilles, Abeilles.
6. Le butin : 8 % sur les monstres communs, 30 % sur le chef, double en donjon.
7. Les Éclats : Rose achète et vend, réinitialisation à 10 × niveau, clefs à 5 × niveau. La potion en combat n'est plus en option : elle arrive avec l'alchimiste.
8. Le jeu à plusieurs : 4 joueurs, tour de 30 s, 15 s pour rejoindre, butin pour chacun, une clef chacun, XP entière + 10 %.
9. L'équilibre à plusieurs : 2 joueurs ≈ 50 %, 3 ≥ 80 % (hordes) ; 3 ≥ 70 %, 4 ≥ 90 % (chefs).
10. Le chapitre 1 couvre les niveaux 1 à 30, et le Disjoint se gagne seul.
11. Liane seule comme nouvelle PNJ.

### 9.2 Nouvelles (v2, les métiers)
| # | Question | Valeur par défaut |
|---|---|---|
| 12 | Place des métiers dans le plan | **Après les Éclats, avant le jeu à plusieurs** (chantier 3) |
| 13 | Quels métiers au départ | **Récolte : les quatre de base demandés par Charles (bûcheron, mineur, pêcheur, paysan, un par élément), plus l'herboriste pour l'alchimie. Fabrication : forgeron, tailleur, alchimiste**, en plus du serrurier ; **le boulanger depuis la v4** (question 35). Bijoutier, cordonnier et cuisine à effet durable plus tard |
| 13 bis | Le paysan sème-t-il ? | **Non : il cueille** des parcelles sauvages ou cultivées, partagées comme les autres points ; planter viendra avec des parcelles à soi |
| 13 ter | Les noms des métiers, des outils et des ressources | ceux du §5.2 et du §5.3 (pic, nasse, hachette, faucille ; lin, chanvre, laîche, blé de Clairval…), à faire relire par Charles et l'agent `scenariste` |
| 14 | Combien de métiers par personnage | **Tous**, sans limite |
| 15 | Points de récolte partagés ou par joueur | **Partagés**, repousse de 2 + niveau de la ressource / 10 minutes (2 à 5 à Clairval) |
| 16 | Combien de points | **7 à 10 par carte de zone**, aucun au village ; la ressource d'un point dépend de la profondeur de la carte |
| 17 | Comment on récolte | **Clic, marche jusqu'au point, 3 secondes**, l'outil en main sans être exigé ; 1 à 3 ressources selon l'avance du métier |
| 18 | ~~Paliers 1, 10, 20, 30~~ (v2, remplacé par 23) | — |
| 19 | Réussite et qualité | **100 % de réussite, pas de jets aléatoires** : l'objet a les chiffres de sa fiche |
| 20 | Ce qui se fabrique | **Plastron et épaulettes (craft seulement), boucliers et armes (craft et butin), consommables (potions, huiles), clefs.** Jamais d'initiative, de PA ou de PM, ni de panoplie sur une pièce fabriquée |
| 21 | Où sont les ateliers | **Un nouveau quartier des artisans à l'est de Clairval**, avec forge, tailleur, alambic et l'établi du serrurier déplacé |
| 22 | L'échange entre joueurs | **Oui, avec le groupe (S112)** |

### 9.3 Nouvelles (v3)
| # | Question | Valeur par défaut |
|---|---|---|
| 23 | Ressources et recettes par niveau | **Une ressource par métier de récolte tous les 5 niveaux** (20 par métier, 100 en tout sur 1-100), **deux par zone** (entrée et fond) ; une recette tous les 5 niveaux par métier de fabrication ; **maintenant, les niveaux 1 à 35** (40 ressources, 24 recettes en plus des clefs), les niveaux 40 à 95 avec le monde après 30 |
| 24 | XP des métiers | courbe inchangée ; récolte **25 + 5 × niveau** (10 à 20 actions par tranche de 5 niveaux) ; équipement fabriqué **150 + 15 × niveau** ; **pas d'XP 20 niveaux sous le métier** (Clairval permet de monter jusqu'au métier 55 environ) |
| 25 | Consommables | potions de soin **et** huiles d'élément (+10 à +25 % de dégâts d'un élément, 3 tours), **un consommable par combat**, 2 PA |
| 26 | Fenêtre Métiers | **touche J**, icône dans la barre ; la liste et la barre « 710 / 2 840 » ; onglets Recettes et Ressources ; recherche, niveau de … à …, catégorie, tri, « possibles seulement » ; « ingrédients manquants » affichés en rouge plutôt que filtrés ; elle remplace le panneau de l'établi et ne fabrique que devant l'atelier |
| 27 | Source des icônes d'objets | **game-icons.net (CC BY 3.0)**, comme les sorts ; Kenney (CC0) en appoint ; pas de pixel art ni d'IA |
| 28 | Style des icônes | **silhouette colorée par l'élément sur tuile sombre** (l'inverse des sorts), **cadre selon la provenance** : gris commun, chocolat artisan, vert panoplie, or chef |
| 29 | Place des icônes | **un sprint dédié, S98**, juste après les panoplies et Rose |

### 9.4 Nouvelles (v4, la vie qui se garde)
| # | Question | Valeur par défaut |
|---|---|---|
| 30 | Le rythme de récupération | **1 % des PV max par seconde, au moins 1 PV/s** (la règle de Charles pour 100 PV ; de 0 à pleine vie en 60 à 100 s à tous les niveaux) ; **×2 au village** ; s'asseoir plus tard ; la vie remonte aussi hors ligne |
| 31 | Le calcul | **par le serveur, à partir d'un horodatage** (vie, moment, état), sans tic ; la même formule dans `Rpg.Core` pour la barre du client ; pas de récupération en combat |
| 32 | La défaite | **sur place à 1 PV** (devant la porte en donjon) ; un ticket non rendu compte comme une défaite |
| 33 | Monter de niveau | **rend toute la vie** ; changer d'équipement ne soigne pas |
| 34 | Les donjons | **`DungeonHp` fusionne dans la vie du personnage** ; la vie remonte dans les salles hors combat ; la fontaine rend tout d'un coup |
| 35 | Les consommables | **pain hors combat** (boulanger, 30 + 10 × niveau PV, Rose vend le pain de Clairval à 10 Éclats) ; **potions et huiles en combat seulement** (alchimiste) |
| 36 | L'équilibre T41 | **planchers et plafonds toujours à pleine vie** ; nouvel indicateur, la vie restante après un petit groupe, cible ≥ 50 % |
| 37 | Le client | **barre de vie hors combat** qui se remplit ; **avertissement sous 30 %**, jamais de refus ; vie restante dans la fin de combat |
| 38 | La place dans le plan | **S101 (5 points)**, après la fenêtre Métiers ; le boulanger rejoint l'alchimiste (S107, 5 points) |

### 9.5 Nouvelles (v5 : le village, le fil conducteur, la mini-carte)
| # | Question | Valeur par défaut |
|---|---|---|
| 39 | Taille du village | **4 × 3 cartes de 48 × 32** (x 0 à 3, y −1 à 1), la place garde l'identifiant `clairval` ; *alternative :* 3 × 3 cartes |
| 40 | Les portes | **3 portes, toutes à l'ouest vers le Bois** (Nord : chemin du Lac ; Ouest : route principale ; Sud : chemin des Champs) ; palissade partout ailleurs ; **la porte est d'entraînement disparaît**, le combat d'entraînement est lancé par Garance |
| 41 | Monstres au village | **aucun groupe errant** ; les combats du tuto sont des scénarios de quête (le mannequin de Garance, les rats du grenier) |
| 42 | Quêtes proposées par habitant | **au plus 2 à la fois** (la principale d'abord, puis le niveau le plus bas) ; le dialogue défile et répond aux touches 1 à 9 |
| 43 | L'enchaînement | **une chaîne principale** (Ysaline, la pierre) qui ouvre chaque zone ; des **suites secondaires de 2 ou 3 quêtes** par donneur, ouvertes l'une après l'autre ; les donjons entiers ouverts après la quête principale du donjon |
| 44 | Où sont les donneurs | **au village pour le tuto, puis un donneur par zone** sur une carte d'entrée : le chef des bûcherons, l'ermite des mares, Liane, la fermière des Champs |
| 45 | Les objectifs nouveaux | **`Interact`** (indices, avancé en S103), **`Win`** (gagner un scénario), **`Wear`** (porter un objet), **butin de quête** (ne tombe que pendant la quête) |
| 46 | Le nombre de quêtes | **45** : 9 par zone, village compris (2 principales et 7 secondaires au tuto, 4 et 5 dans chaque zone) ; 21 identifiants gardés, 24 nouveaux, **aucune supprimée** ; `four-trails` passe du niveau 8 au niveau 30 |
| 47 | Les récompenses | XP **60 × niveau** (principale), **40 ×** (secondaire), **100 ×** (donjon entier) ; Éclats 10 × niveau (15 × pour un donjon) ; l'équipement de départ par le tuto ; les clefs par la chaîne principale ; jamais de pièce de panoplie |
| 48 | Les repères | « ! » et « ? » au-dessus des habitants, le journal dit où aller, la mini-carte montre les objectifs de la quête suivie |
| 49 | La mini-carte | **touche N, visible par défaut en haut à droite**, déplaçable, 3 tailles, zoom de 16 cases à 3 × 3 cartes ; elle montre le joueur, les groupes, les points de récolte, les habitants (! ?), les passages, les portes de donjon, les ateliers, les objectifs ; un clic fait marcher ; cachée en combat |
| 50 | Les nouveaux habitants | le copiste (Odalric dans la trame) **gardé** ; le reste selon §5 ter.5 ; **Margaux et Camille interdits** |
| 51 | L'ordre du plan | **l'histoire avant le jeu à plusieurs** : le village, le moteur des quêtes, le tuto et la mini-carte (S102 à S105), puis la forge et l'alchimie (S106, S107), puis les quêtes des zones (S108 à S110), puis le jeu à plusieurs (S111 à S114) |

### 9.6 Nouvelles (v6 : un métier pour chaque pièce d'équipement)
Les quatre métiers, l'absence d'initiative sur le craft, les deux sprints et l'absence de nouveaux modèles sont **tranchés par Charles** (17 octobre). Restent :

| # | Question | Valeur par défaut |
|---|---|---|
| 52 | Le budget des pièces d'artisan | **celui du butin réel : (8 + niveau) / 2**, arrondi au supérieur (T63), et non 8 + niveau (le chiffre d'avant T63) |
| 53 | L'arme à deux mains | **elle compte pour deux pièces** (budget × 2, ingrédients × 1,5, Éclats × 2), sinon elle est dominée par une main + bouclier ; **la même règle pour les 4 armes à deux mains des panoplies** |
| 54 | Deux fois le même anneau | **interdit** (`Equipment.Problem`, S110) : le craft rend le doublon trivial, et choisir son second anneau est une décision |
| 55 | Les récolteurs | forgeron : bûcheron + mineur ; tailleur : paysan + **herboriste** ; bijoutier : mineur + pêcheur ; cordonnier : **pêcheur** (cuir de poisson) + bûcheron. Chaque récolteur est principal au moins une fois |
| 56 | Le coût en Éclats | **le niveau de l'objet** (3 à 30, double pour une arme à deux mains) : un petit puits, et la garantie que fabriquer pour revendre ne rapporte jamais plus que le brut |
| 57 | L'XP de fabrication | **150 + 15 × niveau pour l'équipement, par une règle dans `Crafting.Xp`** (pas un champ de données) ; **pas d'XP 20 niveaux sous le métier**, comme la récolte |
| 58 | La mesure T41 | planchers nus ; plafonds : **`zone` et le nouveau `craft` bloquants à 20 %** (mêmes cartes exclues que T64), **`fast` + artisan (+ consommable au S111) en indicateur** ; si `craft` saute : −1 Vitalité aux pièces d'artisan de niveau ≤ 14, puis 80 % du budget **seulement avec l'accord de Charles** ; jamais les monstres |
| 59 | Variantes par élément | **non pour l'instant** : une pièce, un élément ; les 8 pièces d'une zone couvrent les quatre éléments deux fois |
| 60 | Une quête d'initiation par artisan | **oui**, en données seulement : « Ta première pièce », `Bring` la pièce de niveau 1, après C5, une par artisan (textes du scénariste) ; *alternative :* aucune, la fenêtre Métiers et C5 suffisent |
| 61 | Les icônes | **les dessins déjà téléchargés**, par emplacement, sans `--fetch` ; quelques dessins nouveaux plus tard si les pièces se confondent |
| 62 | Les objets de départ | **remis au budget au S109** : plastron d'orque 7 points (au lieu de 20), bouclier de bois et ceinture de cuir 5 Vitalité, chapeau et cape du braconnier 5, massue d'orque 16 (deux mains) ; le bonus de la panoplie du braconnier à +5 Air |
| 63 | La répartition du tailleur | **deux de chaque** (plastron 3 et 18, coiffe 6 et 22, épaulettes 10 et 26, cape 14 et 30) ; *alternative :* 3 plastrons, 3 épaulettes, 1 coiffe, 1 cape, puisque plastron et épaulettes ne tombent jamais |
| 64 | Les noms | ceux du §5 quater.5 et des ateliers (forge, atelier de couture, établi de bijouterie, banc de cordonnerie), à relire par le scénariste et Charles ; les deux nouveaux artisans nommés par le scénariste |
| 65 | La place dans le plan | **S109 (5 points) et S110 (3 points)**, l'alchimiste et le boulanger en S111, la suite décalée d'un cran jusqu'à S118 ; **5 points de plus** |

---

## 10. Planification proposée (si Charles dit « Go » sur la v5)

Quatre décisions couvrent ensemble les sprints **95 à 114**, soit **78 points** :
- **D71** (v1 et v2, 52 points) ;
- **D72** (v3 : les icônes, la fenêtre Métiers, une ressource tous les 5 niveaux ; 6 points de plus) ;
- **D73** (v4 : la vie qui se garde, 5 points, et le boulanger, 2 points de plus) ;
- **D74** (v5 : le village, le fil conducteur, la mini-carte).

**Le bilan de D74 :**
- **Retirés** : l'ancien S102 (le quartier des artisans, 3 points), les anciens S109 à S111 (la chaîne et le Disjoint, 9 points).
- **Ajoutés** : S102 le village (5), S103 le moteur des quêtes (5), S104 le tuto (3), S105 la mini-carte (3), S108 et S109 les quêtes des zones (3 + 3), S110 le Disjoint et la fin (3).
- Soit 25 points ajoutés et 12 retirés : **13 de plus**.
- Les icônes d'ingrédients de l'établi sont faites dans S101.
- Le projet passe de **381 à 459 points**, et rpg de **160 à 238**.
- Il faut mettre à jour `PLAN.md`, `data/scrum.json`, les fichiers `scrum/sprint-NN.md`, ainsi que R2, R8 et `scopeDecisions` (règle « ajout = gestion de projet à jour »).
- Les entrées de `DECISIONS.md` commencent à **T63**.

| Sprint | Pts | Chantier | Contenu |
|---|---|---|---|
| S95 | 5 | 0 et 1 | Correctif de la Bauge ; moteur de l'initiative ; panoplies du Bois et du Bois moisi ; `--kit` |
| S96 | 3 | 1 | Panoplies du Lac et des Champs ; T41 remesuré ; **playtest** |
| S97 | 3 | 2 | Rose marchande, puits d'Éclats (fait, T65) |
| S98 | 3 | 1 bis | Les icônes d'objets (game-icons.net, cadre selon la provenance) |
| S99 | 5 | 3 | La récolte : 5 métiers (bûcheron, mineur, pêcheur, paysan, herboriste), 40 ressources (niveaux 1 à 35), les points des cartes (fait) |
| S100 | 3 | 3 | La fenêtre Métiers (touche J), qui remplace le panneau de l'établi (fait, T68) |
| S101 | 5 | 3 bis | **La vie qui se garde** : vie et horodatage sur le serveur, 1 % des PV max par seconde (×2 au village), combats à la vie actuelle, défaite à 1 PV, `DungeonHp` fusionné, barre hors combat, avertissement, pain de Rose ; icônes d'ingrédients de l'établi (fait, T69) |
| S102 | 5 | 3 ter | **Le nouveau Clairval** : 12 cartes, palissade, 3 portes, habitants déplacés, quartier des artisans (5 ateliers), points de niveau 1, porte est retirée ; correctif du dialogue (2 offres au plus, défilement, touches 1 à 9) (fait, T70, sans les nouveaux habitants ni les ateliers à venir) |
| S103 | 5 | 3 ter | **Le moteur des quêtes** : `Interact`, `Win`, `Wear`, butin de quête, habitants des zones, « ! » et « ? », journal « où aller » |
| S104 | 3 | 3 ter | **Le tuto de Clairval** : 9 quêtes (niveaux 1 à 5), rats du grenier, équipement de départ |
| S105 | 3 | 3 ter | **La mini-carte** (N), déplaçable et zoomable |
| S106 | 3 | 3 | Forgeron et tailleur : 16 pièces d'artisan ; T41 remesuré |
| S107 | 5 | 3 | Alchimiste (potions et huiles en combat) et boulanger (pains hors combat) ; T41 et vie restante remesurées ; **playtest** |
| S108 | 3 | 3 ter | Quêtes du Bois et du Bois moisi (18) ; campement des bûcherons, ermite des mares |
| S109 | 3 | 3 ter | Quêtes du Lac et des Champs (16) ; Liane, la fermière des Champs |
| S110 | 3 | 3 ter | Le Disjoint et `four-trails` (niveau 30), fin du chapitre 1 |
| S111 | 3 | 4 | Se voir sur la carte, des monstres et des points communs |
| S112 | 5 | 4 | Le groupe, son canal de chat, l'échange entre joueurs |
| S113 | 5 | 4 | Le combat à plusieurs : règles, serveur, simulation |
| S114 | 5 | 4 | Le combat à plusieurs : client, donjons, équilibre |

### Planification v6 (17 octobre 2026, si Charles dit « Go »)

Le tableau ci-dessus est celui de la v5. Depuis, **D75** (retours de Charles après le S102 : la récolte qu'on voit, un vrai village, suivre une quête) a décalé la suite de trois crans, et le plan comptait **470 points sur 117 sprints, Osmose 249** (`PLAN.md`). La v6 (décision **D76**) :
- **Retiré :** le S109 de D75, « forgeron et tailleur, 16 pièces », 3 points.
- **Ajoutés :** S109 forgeron et tailleur (5), S110 bijoutier et cordonnier (3).
- **Soit 5 points et un sprint de plus** : le projet passe de **470 à 475 points sur 118 sprints**, Osmose de **249 à 254**.
- Il faut mettre à jour `PLAN.md`, `data/scrum.json`, les fichiers `scrum/sprint-109.md` à `sprint-118.md` (un de plus), R2, R8 et `scopeDecisions` (règle « ajout = gestion de projet à jour »).
- Les entrées de `DECISIONS.md` reprennent à **T77**.

| Sprint | Pts | Chantier | Contenu | État |
|---|---|---|---|---|
| S95 | 5 | 0 et 1 | Correctif de la Bauge ; initiative ; panoplies du Bois et du Bois moisi | fait (T63) |
| S96 | 3 | 1 | Panoplies du Lac et des Champs ; T41 ; playtest | fait (T64) |
| S97 | 3 | 2 | Rose marchande, puits d'Éclats | fait (T65) |
| S98 | 3 | 1 bis | Les icônes des objets | fait (T66) |
| S99 | 5 | 3 | La récolte : 5 métiers, 40 ressources | fait (T67) |
| S100 | 3 | 3 | La fenêtre Métiers (J) | fait (T68) |
| S101 | 5 | 3 bis | La vie qui se garde | fait (T69) |
| S102 | 5 | 3 ter | Le nouveau Clairval en 12 cartes ; le dialogue réparé | fait (T70) |
| S103 | 3 | D75 | La récolte qu'on voit | fait (T71) |
| S104 | 5 | D75 | Un vrai village | fait (T72) |
| S105 | 5 | 3 ter | Le moteur des quêtes | fait (T73) |
| S106 | 3 | D75 | Suivre une quête | fait (T74) |
| S107 | 3 | 3 ter | Le tuto de Clairval | fait (T75) |
| S108 | 3 | 3 ter | La mini-carte | fait (T76) |
| **S109** | **5** | **3 quater** | **Forgeron et tailleur** : 16 pièces, `craft_gen.py`, XP de fabrication, `ItemBudgetTests`, règle des deux mains, objets de départ au budget, kits `craft` et `fast` + artisan, T41 remesuré, forge et atelier de couture | à faire |
| **S110** | **3** | **3 quater** | **Bijoutier et cordonnier** : 16 pièces, deux ateliers, deux artisans (textes du scénariste), un même anneau une seule fois, T41 remesuré | à faire (nouveau) |
| S111 | 5 | 3 | L'alchimiste et le boulanger ; T41 (+ consommable) et vie restante remesurées ; **playtest** | à faire (ancien S110) |
| S112 | 3 | 3 ter | Les quêtes du Bois et du Bois moisi (18) | à faire (ancien S111) |
| S113 | 3 | 3 ter | Les quêtes du Lac et des Champs (16) | à faire (ancien S112) |
| S114 | 3 | 3 ter | Le Disjoint et `four-trails`, fin du chapitre 1 | à faire (ancien S113) |
| S115 | 3 | 4 | Se voir sur la carte | à faire (ancien S114) |
| S116 | 5 | 4 | Le groupe et l'échange | à faire (ancien S115) |
| S117 | 5 | 4 | Le combat à plusieurs : règles et serveur | à faire (ancien S116) |
| S118 | 5 | 4 | Le combat à plusieurs : client et donjons | à faire (ancien S117) |

> Depuis, D77 et D78 ont ajouté trois sprints (S115 à S117) et décalé le jeu à plusieurs en S118 à S121. Le plan v6 est **entièrement fait** au commit `6f779a0` : 489 points, 121 sprints, Osmose 268.

---

## 11. La suite après S121 *(v7, 21 octobre 2026)*

> Rédigée après lecture de `DECISIONS.md` (D75 à D78), `projects/rpg/DECISIONS.md` (T77 à T89), `PLAN.md`, `docs/design-familles.md` §2.5 et §7, `data/monsters.json` (famille `bee`), `data/spells.json` (`royal-jelly`, `foraging`, `egg-laying`, `swarm`), `src/Rpg.Core/Monsters.cs` (`Sizes`, `Draw`) et `src/Rpg.Core/Ai.cs` (`BestCast`, `Value`).

### 11.1 Le constat après S121

Le chapitre 1 est complet (niveaux 1 à 30, 49 quêtes, le Disjoint), les sept métiers de fabrication et les cinq de récolte tournent, la vie se garde, et le jeu à plusieurs existe de bout en bout : se voir, se grouper, échanger, combattre à quatre, descendre en donjon ensemble. Les trous, du plus gênant au moins gênant :

1. **L'équilibre à plusieurs n'est pas tenu, et il est mal mesuré.** T89 laisse deux questions ouvertes (§11.2). Surtout, les héros du simulateur `--party` sont joués par une IA qui **ignore les rôles et les passifs** : `Ai.BestCast` prend la plus grande valeur, puis la cible la plus faible, et `design-familles.md` §2.5 l'avait accepté (« les passifs ne sont pas prévus par l'IA »). Trois joueurs humains tuent d'abord la nourrice ; l'IA, non. Avec 20 combats par cas, l'incertitude est de ±20 points autour de 50 % : la reine de `fields-4-3` à 75 % peut aussi bien être à 55 %.
2. **Le combat à plusieurs se lit mal et casse facilement** (limites de T86 à T89) : pas de minuterie affichée, un absent fait attendre 30 s à chaque tour, pas d'exclusion du groupe, un échange ouvert survit à la déconnexion, l'allure d'un joueur ne change pas quand il s'équipe, et **un redémarrage du serveur perd tous les combats en cours** (`Battles` en mémoire).
3. **Rien ne retient deux joueurs l'un à l'autre** d'une soirée à l'autre : ni amis, ni message privé, ni guilde (souhaits du 9 octobre : « contacts », « guilde »).
4. **Il n'y a rien après le niveau 30.** Les personnages de Plouf, au niveau 100, n'ont aucun contenu à leur niveau ; un joueur qui finit « Quatre pistes » n'a plus que le farm des Champs.
5. **On se déplace seulement à pied**, alors que Charles veut des téléportations (D77) et que le chapitre 2 doublera les distances.

**Hors de cette liste, volontairement :** l'hôtel des ventes (§11.8), l'apparence (reportée à la fin du jeu, D76), le JcJ (personne ne l'a demandé).

### 11.2 Les deux questions d'équilibre de T89

**Principe commun :** on ne règle pas un monstre contre une IA qui joue mal. On rend d'abord l'IA des héros capable de jouer en équipe (S122), on remesure avec **100 combats par cas**, et seulement ensuite on touche aux données (S123), avec le levier le moins invasif d'abord. Chaque levier a une **condition d'arrêt** : dès que la cible est tenue, on s'arrête.

#### 11.2.1 La reine des abeilles de `fields-0-4.0` (5 % à trois, 100 % à quatre)

**Diagnostic.** Le groupe compte 7 monstres : la reine (380 PV, 8 PA, `overheat` : +30 % de dégâts sous la moitié de ses PV), **deux nourrices** (Gelée royale, 13 à 18 PV, un tour sur deux chacune ; Butinage, +1 PA à un allié) et quatre abeilles qui déclenchent `alarm`. Deux nourrices rendent environ 15 PV par tour à la reine : sur un combat de 10 tours, 150 PV, ce qui explique les « 380 → 150 » de T89. La **salle 3 de la Ruche royale**, qui n'a qu'une nourrice (un monstre de chaque rôle), passe à 100 % à trois : le problème est la deuxième nourrice, pas la reine. La règle de génération l'autorise (`Draw` : soutiens `< size / 3`, soit 2 pour un groupe de 7, 3 pour un groupe de 9 ou 10). Le saut de 5 % à 100 % entre trois et quatre héros dit la même chose : c'est une course entre les dégâts et les soins, gagnée ou perdue d'un bloc.

**À vérifier d'abord :** que `--party` mesure les chefs au niveau de **leur carte** et non de la zone (Champs 26) : la reine est niveau 30.

**Recommandation, dans l'ordre :**

| Étape | Levier | Où | S'arrêter si |
|---|---|---|---|
| 1 | L'IA des héros vise d'abord les soigneurs et concentre ses coups (§11.4, S122) | `Ai`, combats à plusieurs seulement | ≥ 70 % à trois sur les 10 compositions de classes, aucune sous 50 % |
| 2 | **Un groupe de chef compte au plus un soutien**, comme la salle 3 de son donjon. Le soutien en trop devient un mêlée, tiré **après** les autres pour ne pas changer la suite des tirages du reste du monde | `MonsterGroups.For` | idem |
| 3 | PV de la reine : 380 → 330, puis **300 au plus bas** (−21 %) | `monsters.json` | idem, et la salle 3 de la Ruche royale reste ≤ 10 % seul (règle de `design-familles.md` §9, question 3) |
| 4 | Retour à Charles avec les chiffres (par exemple : un chef ne reçoit que la moitié des soins) | — | — |

Mon pronostic : l'étape 2 suffit (elle rend le groupe identique, au mêlée près, à la salle 3 qui passe déjà à 100 %). Elle touche **tous les groupes de chef du monde**, mais seulement pour les rendre plus faciles ; ils sont tous à 100 % à trois sauf les deux reines. C'est une règle de composition, pas une règle sur les PV : Charles avait validé « on règle les PV du chef seul » (§6.4), d'où la question 66. La reine de `fields-4-3` (75 % sur 20 combats) suit les mêmes étapes.

#### 11.2.2 Les hordes à deux joueurs (de 0 à 100 %)

**Diagnostic.** « 2 joueurs ≈ 50 % » s'appliquait à toutes les hordes, de 6 à 10 monstres. Or une horde de 6 contre deux héros fait 3 monstres par héros, la limite du « facile seul » de Charles ; une horde de 10 en fait 5. Un écart de 0 à 100 % est donc **en partie normal** : la cible doit dépendre de la taille. La règle de Charles du 14 octobre (« un joueur bat 1 à 3 monstres facilement ») donne une lecture simple : **un joueur par tranche de 3 monstres**.

**Cible recommandée** (elle précise celle de §6.4 sans la contredire) :

| Taille de la horde | 2 joueurs | 3 joueurs | 4 joueurs |
|---|---|---|---|
| 6 | 50 à 90 % | ≥ 90 % | — |
| 7 et 8 | 20 à 70 % | ≥ 80 % | ≥ 95 % |
| 9 et 10 | ≤ 40 % (0 % permis) | ≥ 60 % | ≥ 90 % |
| **Médiane des hordes d'une zone** | **35 à 65 %** (le « ≈ 50 % » de Charles) | **≥ 80 %** | — |

Les hordes de taupes lanceuses de la Prairie restent à part, comme en T64 et T77. T89 ne donne pas la mesure à trois joueurs : elle est à faire.

**Leviers, dans l'ordre, jamais les caractéristiques des monstres :**
1. L'IA des héros en équipe (S122), puis remesure.
2. **Au plus deux soutiens par groupe** (aujourd'hui trois pour 9 et 10 monstres) : trois soigneurs empilés rendent une horde imprenable à deux comme à trois.
3. Si la médiane d'une zone reste sous 35 % à deux : les tirages de 9 et 10 monstres au fond passent de 3 + 3 à 2 + 1 sur 100, reportés sur 6 (`Sizes[2]` : `[15, 20, 20, 10, 10, 8, 6, 5, 3, 3]` → `[15, 20, 20, 10, 10, 11, 6, 5, 2, 1]`). Ce levier touche toutes les zones, d'où la mesure de toutes avant et après.
4. Une horde trop **facile** (au-dessus de sa bande) est acceptée : on ne durcit rien.

### 11.3 Les chantiers dans l'ordre

| Rang | Chantier | Le trou qu'il bouche | Sprints | Points |
|---|---|---|---|---|
| 7 | **Finir le jeu à plusieurs** | §11.1, points 1 et 2 | S122 à S125 | 3 + 3 + 5 + 3 = 14 |
| 8 | **Les amis et le voyage** | points 3 (en partie) et 5 | S126, S127 | 3 + 3 = 6 |
| 9 | **Le chapitre 2, une tranche** : Quatre-Chemins et la Forêt des Aïeux (30 à 45) | point 4 | S128 à S136 | 37 |
| 10 | **Les guildes** | point 3 | S137 | 5 |
| | **Total** | | **16 sprints** | **62** |

**Pourquoi cet ordre.**
- **Le jeu à plusieurs d'abord**, parce qu'il vient d'arriver : ses défauts se voient au premier essai à deux, et un combat perdu au redémarrage du serveur coûte la confiance d'un joueur. Le playtest de S125 décide du reste.
- **Les amis et le voyage avant le chapitre 2** : 6 points peu risqués. Le voyage est un prérequis du chapitre 2 (Quatre-Chemins est loin), les amis rendent le playtest à plusieurs utilisable d'une soirée à l'autre.
- **Une tranche du chapitre 2, pas une région.** `design-familles.md` §7 prévoit quatre régions de deux zones entre 30 et 60, soit des mois. Une seule zone (deux familles, deux donjons, une mécanique) teste toute la chaîne de fabrication du contenu à haut niveau en 37 points, et donne aux niveaux 30 à 45 de quoi jouer. La zone suivante (les Mines de Sous-Racine, 45 à 60) se décide après y avoir joué.
- **Les guildes en dernier** : une guilde sert quand il y a assez de joueurs pour en remplir une. Si Charles veut inviter des testeurs tôt (question 71), elles passent avant le chapitre 2 sans rien changer d'autre.

### 11.4 Chantier 7 : finir le jeu à plusieurs (S122 à S125, 14 points)

#### S122 (3) : une IA de héros qui joue en équipe, des mesures fiables
- **Expérience visée :** aucune pour le joueur ; elle sert à régler les combats contre la façon dont des humains les jouent : « on tue d'abord la nourrice, on tape tous la même ».
- **Contenu :**
  - Dans `Ai`, **seulement quand le combat compte au moins deux héros** (le combat seul et T41 ne bougent pas), trois multiplicateurs sur `Value` :
    - ×1,5 contre un monstre qui a un sort de soin ;
    - ×1,25 contre le monstre qu'un autre héros a frappé depuis le début du tour de jeu ;
    - −10 % de la valeur par monstre qu'un passif `Hit` à zone (comme `alarm`) renforcerait.
  - `--party` : **100 combats par cas**, toutes les compositions de classes (6 à deux, 10 à trois, 15 à quatre), au niveau de la carte, un tableau par taille de groupe, et l'intervalle de confiance à 95 % (Wilson) à côté de chaque taux.
  - La remesure complète des hordes à deux et à trois, des chefs et des salles 3, consignée en T90.
- **Tests :**
  - unitaires : un héros entre une nourrice et une ouvrière de même valeur frappe la nourrice ; deux héros finissent la même cible ; un combat seul enregistré avant S122 se rejoue à l'identique (le multiplicateur ne s'applique pas) ;
  - intégration : `--party` sur `fields-0-4.0` avec une graine fixe donne deux fois le même tableau.
- **Risques :**
  - le temps du simulateur (×5 combats) : les mesures complètes restent hors de la CI ;
  - les taux montent partout : c'est l'effet voulu, et les cibles à quatre (≥ 90 %) ne peuvent pas être dépassées dans le mauvais sens.

#### S123 (3) : l'équilibre à plusieurs réglé
- **Expérience visée :** « à trois, la reine se mérite mais tombe » ; « une horde de 7 à deux, c'est un pari ; à trois, ça passe ».
- **Contenu :** les étapes 2 et 3 du §11.2.1 et les leviers 2 et 3 du §11.2.2, chacun appliqué **seulement si la mesure de S122 l'exige**, dans l'ordre. T91 consigne avant et après.
- **Tests :**
  - unitaires : `MonsterGroups.For` ne met jamais plus d'un soutien dans un groupe de chef, ni plus de deux ailleurs ; les autres groupes du monde gardent leur composition (instantané des groupes de toutes les cartes, comparé hors des groupes touchés) ;
  - intégration : `PartyBalanceTests`, un échantillon fixe par zone (une horde de chaque bande de taille, les deux reines, deux salles 3), 30 combats, bandes du §11.2 avec ±10 points de tolérance ;
  - les tests T41 seuls restent verts (planchers nus, plafonds `zone` et `craft`).
- **Risques :**
  - un groupe battu qui change de composition sous les yeux d'un joueur connecté : déployer serveur et jeu ensemble ;
  - les quêtes qui comptent des soutiens (« 3 nourrices ») : à vérifier dans `quests.json`, la quête reste faisable puisque les soutiens existent toujours dans les petits groupes.

#### S124 (5) : le combat à plusieurs qui se lit, le groupe qui se tient
- **Expérience visée :** « je vois qu'il me reste 8 secondes ; Plop est parti chercher un café, son tour ne nous bloque plus ; je vois l'épée sur le groupe que Rosalie attaque et je la rejoins à temps ».
- **Contenu :**
  - **Minuterie du tour** :
    - le serveur donne l'échéance de chaque tour (`TurnStarted { fighter, deadline }`, heure du serveur) ; le jeu corrige l'écart de son horloge à la connexion ;
    - dans la frise, un anneau autour du portrait de celui qui joue, et les secondes ; les 10 dernières en couleur d'alerte, un son à 5 s ;
    - rien dans le combat seul, qui n'a pas de limite de temps.
  - **Les absents** : deux tours passés de suite par la minuterie, et le tour de ce héros ne dure plus que **10 s** jusqu'à sa prochaine action ; son portrait est marqué « absent » ; il retrouve 30 s dès qu'il agit.
  - **Rejoindre** : les 15 s se voient aussi hors de la question, avec deux épées croisées et un compte à rebours au-dessus du groupe de monstres, et une ligne dans le bandeau du groupe (« Rosalie attaque : rejoindre (12 s) ») ; un clic rejoint même si la question a été fermée. **Pas de « rejoindre en cours »** (question 69).
  - **Le groupe** (limites de T86 et T87) :
    - le chef exclut un membre ;
    - un membre hors ligne depuis 10 minutes quitte le groupe ;
    - un échange s'annule quand l'un des deux ferme sa connexion ;
    - l'allure d'un joueur est rediffusée quand il change d'équipement.
- **Tests :**
  - unitaires : `BattleClock` (30, 30, puis 10 s ; 30 de nouveau après une action), la correction d'horloge, les règles d'exclusion ;
  - intégration à deux clients WebSocket, sur une horloge simulée : un joueur muet voit ses tours passer en 30, 30, puis 10 s ; l'exclusion ; l'échange annulé à la fermeture ;
  - l'auto-test Godot ouvre un combat à plusieurs factice et vérifie que la minuterie s'affiche.
- **Risques :** une horloge du client en avance ou en retard (d'où l'échéance du serveur, jamais une durée lancée côté client) ; une frise surchargée à 4 héros et 10 monstres : l'anneau seulement sur celui qui joue.

#### S125 (3) : un combat qui survit au serveur, le premier vrai playtest à plusieurs
- **Expérience visée :** « le serveur a redémarré pendant la reine : on a reperdu dix secondes, pas le combat ».
- **Contenu :**
  - Chaque combat à plusieurs s'écrit en base **au fil de l'eau**. Une table `battles` (le départ : graine, groupe, héros, vies) et une table `battle_actions` (les actions numérotées, en ajout seul). Au démarrage, le serveur reconstruit les combats en cours en rejouant leurs actions (le moteur est déterministe, T4) et rend 30 s à celui dont c'est le tour. `Hunts` se déduit des combats ; les groupes de joueurs restent en mémoire (T87 : un groupe vit une soirée), on se regroupe après un redémarrage.
  - **Playtest à plusieurs**, 45 minutes guidées : se grouper, une horde à deux, un chef à trois, un donjon, un échange. Charles et un à trois joueurs, ou Charles avec trois jeux ouverts si personne n'est disponible (question 71). Les retours deviennent D79.
- **Tests :**
  - unitaires : reconstruire un combat depuis la base donne le même état que celui en mémoire ;
  - intégration : ouvrir un combat, jouer trois actions, arrêter et relancer l'hôte (nouveau `WebApplicationFactory` sur la même base), `GET /battles/{id}` rend le même combat et le tour continue.
- **Risques :** une écriture par action (quelques millisecondes, sans effet sur un tour de 30 s) ; une version du moteur qui change entre deux démarrages rendrait le rejeu faux : le combat garde la version du jeu qui l'a ouvert, et en cas d'écart il s'arrête **sans perte**, chacun garde sa vie de départ.

### 11.5 Chantier 8 : les amis et le voyage (S126, S127, 6 points)

#### S126 (3) : les amis et les messages privés
- **Expérience visée :** « Plop est en ligne, aux Champs ; je lui écris, je l'invite au groupe d'un clic ».
- **Contenu :**
  - l'onglet **Contacts** (souhait du 9 octobre) : ajouter par nom de personnage, liste en ligne ou hors ligne, niveau, **zone** (pas la carte), inviter au groupe, retirer ;
  - **ignorer** : ni messages, ni invitations, ni échanges de cette personne ;
  - `/w Nom message` partout dans le monde, et « Nom n'est pas en ligne » sinon ;
  - un avis discret quand un ami se connecte, en option ;
  - les listes sont en base, par compte : un ami se suit d'un personnage à l'autre.
- **Tests :**
  - unitaires : l'analyse de `/w`, les règles d'ignorer, la limite de 100 contacts ;
  - intégration à deux clients sur deux cartes : le message privé arrive ; un ignoré ne peut ni écrire ni inviter.
- **Risques :** le harcèlement. Ignorer suffit pour un jeu de test ; un signalement viendra avec un public. Ne jamais montrer la carte exacte (la zone suffit).

#### S127 (3) : le rappel et les pierres de passage
- **Expérience visée :** « sac plein au fond du Lac : je lis un parchemin, je suis sur la place » ; « je touche la pierre des Champs une fois, puis j'y reviens d'un clic ».
- **Contenu :**
  - **Le parchemin de rappel** : consommable hors combat et hors donjon, comme le pain (route `/use`). Il ramène à la place de Clairval, puis à la pierre choisie comme foyer. Odalric le copiste le vend 15 Éclats (c'est un copiste : il écrit des parchemins).
  - **Les pierres de passage** : une à Clairval et une à l'entrée de chaque zone, soit 5, puis une par zone du chapitre 2. Une pierre se découvre en passant à 3 cases, et le personnage garde celles qu'il a découvertes. Le voyage coûte **10 + 2 × niveau de la zone d'arrivée** Éclats (12 à 62 à Clairval) : un puits régulier.
  - La fenêtre de la pierre montre l'image du monde vu du ciel (`WorldPicture`, T38) et les pierres connues.
- **Tests :**
  - unitaires : la découverte, le coût, le refus d'une pierre inconnue, en combat ou en donjon ;
  - intégration par l'API : le voyage retire les Éclats et place le personnage ; le rappel est consommé.
- **Risques :** tuer l'exploration. Les pierres sont aux **entrées** des zones : le fond se rejoint toujours à pied.

### 11.6 Chantier 9 : le chapitre 2, une tranche (S128 à S136, 37 points)

**Expérience visée :** « après le Disjoint, je prends la route de Quatre-Chemins. L'érudit me montre quatre pistes ; trois sont barrées, la Terre est ouverte. Dans la Forêt des Aïeux, des cerfs de pierre posent des glyphes sur le plateau, et je ne marche plus n'importe où. Mon bûcheron trouve un bois nouveau, ma forge une recette nouvelle. »

**Pourquoi la Terre et ses glyphes :** c'est la mécanique de §7 la plus lisible (une case marquée, un effet) et la moins coûteuse pour l'IA (un coût de chemin). La copie de sort (Eau) et l'invisibilité (Air) demandent bien plus de travail au moteur et à l'IA. Le rythme mesuré (une famille ≈ un sprint de 3, S72 à S76 ; une zone de Clairval ≈ un sprint de quêtes) donne les chiffres ci-dessous.

| Sprint | Pts | Contenu | Tests | Risques |
|---|---|---|---|---|
| **S128** | 3 | **Le socle après 30** : l'initiative des monstres monte avec leur niveau (§3.5) ; les gabarits PV, dégâts et résistances par rôle de 30 à 45 ; le budget des objets de 31 à 45 ; le rythme d'XP (environ 10 monstres de la zone par niveau, comme à Clairval) ; la liste des sorts de classe qui s'ouvrent entre 30 et 45 (s'il n'y en a aucun, le dire à Charles : la montée ne donnerait que des caractéristiques) ; les kits `zone` et `craft` aux niveaux 35, 40 et 45 | T41 aux niveaux 35 et 45 sur une famille de synthèse ; `ItemBudgetTests` jusqu'à 45 | découvrir que les classes sont déséquilibrées à haut niveau (T29) : on le consigne, on ne règle pas les classes dans ce sprint |
| **S129** | 5 | **Quatre-Chemins** : 2 × 2 cartes au style de Clairval (S104), une route depuis Clairval ouverte par « Quatre pistes », l'érudit et une étape de la chaîne principale, 6 à 8 habitants (scénariste), un marchand, une pierre de passage ; les trois autres routes **barrées dans le monde** (éboulis, gué en crue, brasier), avec un habitant qui dit pourquoi, sans contenu fantôme | cartes valides (`KeepsWaysOpen`), passages et routes barrées, la quête ouvre la route ; auto-test du village | un hub vide : les métiers restent à Clairval, Quatre-Chemins a ses habitants et sa pierre, et c'est assez |
| **S130** | 5 | **Le moteur des glyphes** : un effet `glyph` posé sur des cases pour N tours, déclenché à l'entrée sur la case et au début du tour de celui qui s'y trouve (dégâts ou état), dessiné sur le plateau, montré au survol ; l'IA l'évite (coût de chemin) et sait en poser ; la fiche de sort le décrit | règles de déclenchement, rejeu identique par le serveur, l'IA contourne un glyphe, `FightRecord` v2 suffit (l'effet vit dans le sort) | la file des passifs et des glyphes au même moment : un ordre écrit et testé (glyphe, puis passifs) |
| **S131** | 5 | **La Forêt des Aïeux** : 25 cartes (`world_gen.py`, un style nouveau), ses passages vers Quatre-Chemins, **10 ressources** (niveaux 40 et 45 des cinq récolteurs), leurs points, leurs icônes, une pierre de passage | cartes valides, ressources par profondeur, icônes présentes (`credits.json`) | des cartes qui ressemblent au Bois : un style de décor différent (pierre moussue, troncs géants), sans nouveaux modèles (Kenney, D76) |
| **S132** | 3 | **Les Sylvecerfs** : 5 monstres et leur chef, la leçon (les glyphes), le passif, la faiblesse, le butin, la panoplie de 4 pièces | T41 seul (planchers et plafonds), `--party` pour le chef | une leçon trop punitive : le glyphe montré avant qu'il ne frappe |
| **S133** | 3 | **Les Gardiens moussus** : idem | idem | idem |
| **S134** | 5 | **Les deux donjons** (les Racines de l'Arbre-Mère et un second, nommé par le scénariste), leurs clefs chez le serrurier, leurs trois salles | salle 2 gagnable seul à son niveau (la règle du §2), salle 3 au §6.4 avec `--party` | la salle 3 réglée avant S123 : faire S134 après |
| **S135** | 3 | **Les recettes 40 et 45** : forgeron, tailleur, bijoutier et cordonnier (16 pièces par `craft_gen.py`), deux potions, deux pains, les clefs | `MadePieces`, `ItemBudgetTests`, kit `craft` au niveau 45 ≤ 20 % des hordes seul | rien de nouveau dans les règles : le risque est faible |
| **S136** | 5 | **Les quêtes de la Forêt** : 9 quêtes comme une zone de Clairval (4 principales, 5 secondaires en petites suites), un donneur, des indices, un butin de quête, la fin de tranche qui promet les Mines ; textes du scénariste relus par Charles ; **playtest** | chaque étape principale se fait seul à son niveau (donjons ≤ salle 2) ; intégration : la chaîne de « Quatre pistes » à la fin de la tranche jouée par l'API | l'écriture attend la relecture de Charles : la demander dès S129 |

**Tranché par défaut, à confirmer (question 70) :** la tranche couvre **30 à 45**, et les niveaux 45 à 100 restent sans contenu. Les familles de la zone ont les niveaux 31 à 45, leur chef 45.

### 11.7 Chantier 10 : les guildes (S137, 5 points)
- **Expérience visée :** « je porte le nom de ma guilde au-dessus de ma tête, je vois qui est en ligne et je parle à tous sur /gu ».
- **Contenu :**
  - Créer une guilde chez un habitant de la mairie de Clairval, pour **1 000 Éclats** (un puits ; un joueur au niveau 30 en a plusieurs milliers, §1.3). Un nom de 3 à 20 caractères, unique, filtré comme les pseudos.
  - Trois rangs (meneur, officier, membre) et **50 membres** au plus.
  - Inviter (meneur et officiers), quitter, exclure, transmettre le titre de meneur.
  - Le canal `/gu`.
  - L'onglet **Guilde** : membres, rang, niveau, en ligne, dernière connexion.
  - Le nom de la guilde sous le pseudo sur la carte.
  - Tout est en base.
  - **Pas** de niveau de guilde, de percepteur, de blason ni de banque de guilde dans ce sprint.
- **Tests :**
  - unitaires : les droits par rang, le nom unique, la transmission quand le meneur part ;
  - intégration à deux clients : invitation, `/gu`, exclusion, création refusée sans les Éclats.
- **Risques :** une guilde n'a de sens qu'avec des joueurs. C'est pourquoi elle est en dernier, sauf si Charles ouvre le serveur à des testeurs (question 71).

### 11.8 Hors classement (à garder en tête)

| Chantier | Pourquoi pas maintenant | Taille | Quand |
|---|---|---|---|
| **Hôtel des ventes** | avec quelques joueurs, il reste vide, et un marché vide dit « jeu mort ». L'échange (T87) et Rose suffisent | 5 pts (dépôt, recherche, taxe de 2 % comme puits, 20 offres par joueur) | quand une dizaine de joueurs réguliers jouent sur un serveur ouvert |
| **Rejoindre en cours** | il casse le réglage de §11.2 : une horde faite pour deux, rejointe par deux de plus au tour 3. S124 règle la vraie gêne (rater les 15 s) | 3 pts (au début d'un tour de jeu, sur une case de départ libre) | si Charles le veut après le playtest (question 69) |
| Banque du compte | utile pour Plouf et ses trois personnages, pas pour le jeu | 3 pts | à glisser si un sprint finit tôt |
| Fenêtre Bestiaire, succès | souhaits du 9 octobre (onglets « comme Dofus vraiment ») | 3 + 5 pts | après le chapitre 2 |
| Les Mines de Sous-Racine (45 à 60) | la suite de la tranche | environ 30 pts | après le playtest de S136 |
| Apparence, barbier | reportée par Charles à la fin du jeu (D76) | 5 pts et plus | à la fin |
| **Arbre de talents et spécialisations de classe** | demande de Charles du 23 octobre 2026 : un arbre de passifs et de talents, et des spécialisations par classe (exemple donné : un voleur qui devient acrobate, assassin…). Référence donnée par Charles le même jour, pour la discussion : le guide des classes de Dragonica, https://playdragonica.eu/guide/class (on s'en inspire, on n'en reprend ni noms ni contenus, D54). À discuter d'abord avec le scénariste (le sens de chaque voie dans le canon) et le game designer (quand on s'y spécialise, ce que coûte un talent, l'effet sur l'équilibre de T41 et de T96, le paquet de 12 sorts). Rien n'est conçu ni chiffré | à chiffrer avec le game designer | à discuter avant d'être planifié |

### 11.9 Questions à trancher par Charles (v7)

| # | Question | Valeur par défaut |
|---|---|---|
| 66 | La reine des abeilles | **dans l'ordre du §11.2.1** : l'IA en équipe, puis **un soutien au plus dans un groupe de chef** (une règle de composition, pas de PV), puis ses PV jusqu'à **300 au plus bas** ; au-delà, retour à Charles |
| 67 | La cible des hordes à plusieurs | **par taille, un joueur par tranche de 3 monstres** (tableau du §11.2.2), et le « ≈ 50 % » de Charles comme **médiane de chaque zone à deux joueurs (35 à 65 %)** ; **au plus deux soutiens par groupe** |
| 68 | L'ordre des chantiers | **finir le jeu à plusieurs (S122 à S125), les amis et le voyage (S126, S127), la tranche du chapitre 2 (S128 à S136), les guildes (S137)** ; *alternative :* le chapitre 2 d'abord, si Charles joue surtout seul pour l'instant |
| 69 | Rejoindre un combat déjà lancé | **non** : les 15 s rendues visibles (S124) ; un absent passe ses tours en 10 s au bout de deux tours manqués |
| 70 | Le chapitre 2 | **une zone, pas une région** : Quatre-Chemins et **la Forêt des Aïeux (30 à 45, Terre, les glyphes)**, les trois autres routes barrées dans le monde ; *alternative :* une autre région (Eau, Feu, Air), plus chère au moteur |
| 71 | Jouer avec d'autres | **playtest de S125 avec Charles seul sur trois jeux** si personne n'est disponible ; **un serveur ouvert à des testeurs** (hébergement, coût, compte de test) est une décision à part : elle avance les guildes et rend l'hôtel des ventes utile |
| 72 | Les prix | parchemin de rappel 15 Éclats, voyage 10 + 2 × niveau de la zone d'arrivée, guilde 1 000 Éclats et 50 membres |

### 11.10 Planification v7 (si Charles dit « Go »)

- **Ajoutés :** S122 à S137, soit 16 sprints et **62 points**. Le projet passe de **489 à 551 points**, et de **121 à 137 sprints** ; Osmose passe de **268 à 330**.
- Il faut mettre à jour `PLAN.md`, `data/scrum.json`, les fichiers `scrum/sprint-122.md` à `sprint-137.md`, R2, R8 et `scopeDecisions` (règle « ajout = gestion de projet à jour »), et écrire une décision D79.
- Les entrées de `projects/rpg/DECISIONS.md` reprennent à **T90**.
- **Deux playtests** : S125 (à plusieurs) et S136 (la tranche).
- **Point d'arrêt prévu après S127** : si le playtest de S125 change les priorités (par exemple, ouvrir un serveur à des testeurs), le chantier 9 se replanifie sans rien perdre.

| Sprint | Pts | Chantier | Contenu |
|---|---|---|---|
| S122 | 3 | 7 | Une IA de héros qui joue en équipe ; `--party` à 100 combats, toutes les compositions, intervalles de confiance |
| S123 | 3 | 7 | L'équilibre à plusieurs réglé : la reine, les hordes (§11.2) |
| S124 | 5 | 7 | La minuterie du tour, les absents, rejoindre qui se voit, exclure, l'échange et l'allure à jour |
| S125 | 3 | 7 | Les combats à plusieurs écrits en base et repris au redémarrage ; **playtest à plusieurs** |
| S126 | 3 | 8 | Les amis, ignorer, les messages privés |
| S127 | 3 | 8 | Le parchemin de rappel, les pierres de passage |
| S128 | 3 | 9 | Le socle après 30 : initiative, gabarits, budget, XP, sorts, kits |
| S129 | 5 | 9 | Quatre-Chemins et ses routes barrées |
| S130 | 5 | 9 | Le moteur des glyphes |
| S131 | 5 | 9 | La Forêt des Aïeux : 25 cartes, 10 ressources |
| S132 | 3 | 9 | Les Sylvecerfs |
| S133 | 3 | 9 | Les Gardiens moussus |
| S134 | 5 | 9 | Les deux donjons de la Forêt |
| S135 | 3 | 9 | Les recettes 40 et 45 |
| S136 | 5 | 9 | Les quêtes de la Forêt ; **playtest** |
| S137 | 5 | 10 | Les guildes |

---

## 12. La suite après S151 *(v8, 27 octobre 2026)*

> Rédigée après lecture de `PLAN.md` (D79 à D82), `projects/rpg/DECISIONS.md` (T96 à T118), `docs/talents-arbre.md` (§7 et §10), `docs/talents-arbre-questions.md` (réponses et arbitrages), `docs/elements.md`, `docs/mesures-arbre.md`, `docs/design-familles.md` §7, `docs/chapitre-2-pistes.md`, `docs/textes-v8.md`, `data/reactions.json`, `data/talents.json` et `scrum/sprint-136.md`, `sprint-144.md` à `sprint-151.md`. Rien n'est planifié : c'est une proposition, à valider par Charles. Les entrées techniques reprendraient à **T119**, la décision serait **D83**.

### 12.1 Le bilan après S151 (build 1.0.137)

**Ce qui est solide.**
- **Une montée complète de 1 à 45.** Le chapitre 1 : 4 zones, 8 familles, le Disjoint, 49 quêtes. La tranche du chapitre 2 : Quatre-Chemins, la Forêt des Aïeux, deux familles à glyphes, deux donjons, 9 quêtes.
- **Une économie qui tourne.** Cinq récolteurs et sept artisans, des recettes jusqu'à 45. Les Éclats ont leurs puits : le voyage, les clefs, la guilde, le changement de talents.
- **Le jeu à plusieurs de bout en bout.** Groupe, échange, combat à quatre avec minuterie, combats repris après un redémarrage, amis, messages privés, guildes.
- **Un arbre des talents qui est enfin un arbre.** Tronc, fourche, tresse de Foudre, six styles, explications, planification, partage. Il a son outil de mesure hors CI (`Rpg.Sim --talents`).
- **Une méthode de réglage qui tient.** Gabarits par rôle, T41, `PartyBalanceTests`, 100 combats par cas et intervalles de Wilson.

**Ce qui manque pour qu'un joueur ait envie de revenir**, du plus grave au moins grave :
1. **Le contenu s'arrête juste quand le build s'accomplit.** La Conversion et les clés des styles s'ouvrent à 45, le chef de la Forêt est de niveau 45, et au-delà il n'y a rien. Le joueur reçoit sa plus belle récompense au moment où il n'a plus rien à en faire. Les trois personnages de Plouf, au niveau 100, n'ont toujours aucun contenu à leur niveau.
2. **Rien ne donne rendez-vous pour le lendemain.** Les 58 quêtes ne se font qu'une fois. Le reste, c'est du farm sans but. Il n'y a ni objectif répétable ni collection : le bestiaire et les succès, souhaités le 9 octobre, n'existent toujours pas.
3. **La guilde est une coquille vide.** Elle a un nom, un canal et une liste de membres, mais aucun but commun, rien à partager et rien qui progresse ensemble (T112).
4. **En solo, le build ne se voit pas.** Contre les petits groupes, les 18 combinaisons gagnent à 100 % (`mesures-arbre.md`). C'est la règle de Charles, mais les décisions de l'arbre ne se sentent donc que dans les hordes et les donjons, c'est-à-dire à plusieurs. Les objectifs répétables doivent y conduire.
5. **L'arbre est asymétrique.** Une seule paire sur six a sa tresse, la Foudre. C'est la première cause probable de l'avance d'Électrisation (§12.3).
6. **Des retours restent en attente.** Charles doit encore faire trois playtests : la tranche (S136), les guildes (S145) et le playtest extérieur de l'arbre (S151). Il doit aussi relire `textes-v7.md`, `textes-v8.md` et `textes-v9.md`, le texte d'Edmée, les noms des couches, les 13 bandeaux et les 56 lignes d'ambiance. Les sprints proposés ici écrivent beaucoup de textes (contrats, succès, titres) : mieux vaut solder ces retours d'abord (question 88).

**En une phrase :** le combat (boucle micro) et la montée (boucle méso) tiennent jusqu'à 45. Il manque la boucle macro : rien ne relie une soirée à la suivante, ni un joueur à sa guilde.

### 12.2 Les chantiers dans l'ordre

| Rang | Chantier | Le trou qu'il bouche | Sprints | Points |
|---|---|---|---|---|
| 11 | **Mesurer avant de régler** | point 5, reliquats de T100 et T112 | S152 | 3 |
| 12 | **La boucle de retour** : contrats, bestiaire, succès, guilde qui sert | points 2, 3 et 4 | S153 à S156 | 5 + 3 + 5 + 5 = 18 |
| 13 | **Le socle 45-60 et les spécialisations** | point 1, en partie | S157 à S160 | 3 + 6 + 3 + 3 = 15 |
| 14 | **La deuxième tresse : la Glace** | point 5 | S161 | 4 |
| | **Total** | | **10 sprints** | **40** |

**Pourquoi cet ordre.**
- **Mesurer d'abord** (3 points). On tranche Électrisation avant que les builds suggérés, Plouf et les joueurs ne s'y installent.
- **La boucle de retour avant une nouvelle zone.** Une zone coûte environ 30 points et se consomme en quelques soirées. La boucle de retour coûte 18 points et rend rejouables les 45 niveaux qui existent déjà : 10 donjons, 5 zones, 8 + 2 familles. Une zone sans boucle de retour ne se joue qu'une fois. Les contrats font aussi le pont de 45 à 50 en attendant les Mines.
- **Les spécialisations avant les Mines.** D80 les range « avec les chapitres ». `talents-specialisations.md` disait déjà qu'il fallait régler les familles des Mines *avec* les voies, sinon on les règle deux fois. Le socle 45-60 (S157) est le « sprint de socle du chapitre » que prévoyait D80.
- **La Glace en dernier.** C'est l'ordre de livraison de `elements.md` §4.3 (Foudre, puis Glace), et c'est l'exemple de Charles. Elle répond aussi à la vraie question que pose la mesure : une tresse de **contrôle** (Morsure, −1 PA) peut-elle tenir face à une tresse de **dégâts** (l'Arc) ?
- **Les Mines de Sous-Racine (45 à 60) viendraient en v9**, pour environ 30 points sur le modèle de la tranche S129 à S136, sans hameau ni moteur de glyphes : 25 cartes, 2 familles, 2 donjons, des ressources 50 et 55, des recettes et 9 quêtes. `textes-v8.md` les promet déjà.
- **Point d'arrêt après S156.** Le playtest de la boucle décide si l'on passe à S157 ou si l'on reprend d'abord la boucle.

### 12.3 Électrisation devant contre les hordes : mon avis

**Les chiffres disent vrai.** Les trois classes réunies, Électrisation gagne **93 hordes sur 300 (31 %)** contre **299 sur 1 500 (19,9 %)** pour les cinq autres paires. L'écart est de **+11 points**, environ 3,9 écarts-types : ce n'est pas du bruit. Par classe, l'écart à la médiane des autres paires est de +14 (Sentinelle), +10 (Garde) et +10 (Mage). Qu'une même paire soit première dans les trois classes par hasard n'a qu'une chance sur 36.

**Pourquoi elle est devant : trois causes que la mesure confond.**
1. **C'est la seule réaction qui fait des dégâts.** Les cinq autres font du contrôle : −2 PM, −2 PA, −15 % de résistance, −25 % de dégâts, −2 PO. Contre une horde, faire sauter 35 % d'un coup sur le voisin revient à une petite zone gratuite. Or le taux de victoire récompense directement les dégâts, et ne récompense le contrôle que si l'IA du héros simulé s'en sert bien. Il y a probablement un biais de mesure en faveur des dégâts.
2. **C'est la seule paire qui a sa tresse.** Les builds de Foudre tirés par `TreeDraws.Random` peuvent viser la Transmutation, Signature appuyée, le Prisme, puis la Conversion à 45 : +15 %, 2 × la moyenne des caractéristiques, Arc à 52,5 %. Les autres paires mettent ces points dans les styles. La mesure mélange donc la réaction et la tresse.
3. **Électrisé profite des coups de tous les alliés.** À plusieurs, la réaction vaut davantage, et le rapport, qui ne mesure que le solo, ne le voit pas.

**Est-ce un problème ?** Pas en solo tel quel. À 31 %, un joueur seul perd encore deux hordes sur trois (règle de Charles), la règle J3 tient, et c'est l'identité voulue de la Foudre (« le feu qui court dans l'air »). Électrisé a aussi sa contrepartie naturelle : contre un chef seul ou une cible isolée, il ne vaut rien. Il devient un problème dans deux cas :
- **(a)** la réaction seule, sans la tresse, garde l'avance. Ce serait alors le choix dominant de quiconque joue des hordes, donc de tout le contenu à plusieurs ;
- **(b)** à trois ou quatre, chaque groupe voudrait son joueur de Foudre.

**Recommandation : mesurer décomposé en S152, puis appliquer une règle écrite d'avance.**

| Étape | Mesure | Si… | Alors |
|---|---|---|---|
| 1 | 300 combats par classe et par cas, au niveau 45, contre les hordes de la Forêt : (i) Électrisation **sans tresse** ; (ii) Électrisation **avec tresse** ; (iii) les cinq autres paires ; (iv) une ligne « meilleur héros nu » (T110) | — | `mesures-arbre.md` v2 |
| 2 | (i) contre la médiane des autres paires, trois classes réunies | **≤ +8 points** | **on ne touche pas Électrisé.** L'avance vient de la tresse et se refermera avec les autres tresses ; on remesure après la Glace (S161) |
| 3 | idem | **> +8 points** | **Électrisé passe de 35 à 25 %**, et rien d'autre ne change (1 tour, une cible par lanceur, l'Arc reste à 35 %). Le saut vaut 29 % de moins ; si la réaction porte les 11 points, il en reste environ 8. Si l'écart reste au-dessus de 8 à 25 %, on revient vers Charles plutôt que de descendre sous 20 %, en dessous de quoi la réaction ne se lit plus à l'écran |
| 4 | Trio avec un joueur de Foudre (réaction seule) contre le meilleur trio sans, hordes de 7 à 10, 100 combats par composition | **> +10 points** | **Au plus 2 sauts par tour de jeu** depuis une même cible électrisée. La synergie de groupe reste, la chaîne à quatre disparaît |

**Pourquoi 8 points.** Au-delà, un joueur le sent : une horde sur douze en plus. Avec 300 combats par classe, le bruit d'une classe (environ 3,5 points d'écart-type sur une différence) ne l'explique plus. Et 8 points restent sous la tolérance de l'arbre contre le héros nu (+1,5 horde sur 10, soit 15 points, T110).

**Mon pronostic.** La moitié de l'écart vient de la tresse, la réaction seule est autour de +5 à +7, et Électrisé reste à 35 %. **J'en profite pour corriger la règle des 5 %** : compter la part d'un nœud parmi les builds qui **visent son style**, pas parmi les 1 800. Le seuil passe alors à 20 %, sinon les clés et la tresse resteront sous 5 % quoi qu'on fasse, pour une simple raison d'arithmétique du tirage (`mesures-arbre.md`, « Lecture »).

### 12.4 Les sprints proposés

#### S152 (3) : mesurer avant de régler
- **Objectif :** trancher Électrisation sur des chiffres qui séparent la réaction de la tresse, et solder les petites limites restées ouvertes.
- **Stories :**
  - (1,5) `Rpg.Sim --talents` gagne `--no-braid`, `--fights 300`, la ligne « meilleur héros nu » et `--party 3` (un héros dont la paire est imposée, deux tirés). La règle des 5 % devient la règle des 20 % par style. Le rapport passe en v2 dans `mesures-arbre.md`.
  - (0,5) La règle du §12.3 appliquée, seulement si la mesure l'exige : `reactions.json`, ou le plafond de 2 sauts dans `Fight` ; consignée en T119.
  - (0,5) Les restes des guildes (T112) : un verrou sur le compte des membres (deux acceptations simultanées ne font plus 51) et son propre nom de guilde visible sous son personnage.
  - (0,5) Si Charles le veut (question 85) : un glyphe neuf épargne, au début de son prochain tour, celui qui se trouvait déjà sur ses cases (limite de T100).
- **Critère de réussite :** le rapport v2 donne (i) à (iv) et le tableau du trio. La décision est écrite en T119, avec son chiffre. Après la correction éventuelle, aucune mesure de T41, J3 ni `PartyBalanceTests` ne sort de ses bandes.
- **Tests :** unitaires pour le plafond de sauts (s'il est retenu), pour le tirage `--no-braid` (aucun nœud de tresse) et pour le glyphe qui épargne. Intégration : 20 acceptations parallèles sur une guilde de 45 membres en laissent entrer 5.

#### S153 (5) : le tableau des contrats
- **Expérience visée :** « je me connecte ; au tableau de Quatre-Chemins, trois avis m'attendent : six biches graveuses, dix brins de sauge, et une horde de la Futaie à faire avec ma guilde ».
- **Stories :**
  - Un **tableau d'avis** (un objet à examiner, pas un nouveau PNJ) sur la place de Clairval et sur celle de Quatre-Chemins.
  - **3 contrats par jour et par personnage**, renouvelés à 4 h (heure du serveur) et **cumulables jusqu'à 9**. Trois jours d'absence ne font rien perdre : pas de peur de manquer. Un changement de contrat est gratuit chaque jour, les suivants coûtent 5 × niveau Éclats (un puits).
  - **Six types**, tirés parmi les familles de niveau [niveau − 10, niveau] et dans les zones que le personnage peut atteindre (`QuestToReach`) : chasse (6 monstres d'une famille), récolte (10 ressources d'un métier du joueur), fabrication (une pièce ou un consommable de son métier), donjon (la salle 2), horde (6 monstres et plus) et chef. Les deux derniers sont marqués « groupe » : **au plus un sur trois**, échangeable gratuitement.
  - **Récompense :** 25 × niveau en XP, soit un quart de niveau (750 XP à 30), et 5 × niveau Éclats (150 à 30). Un contrat de groupe vaut le double. Aucune nouvelle monnaie.
  - Les contrats sont des quêtes générées (`QuestKind.Contract`), avec un onglet « Contrats » dans le journal. Les phrases viennent de modèles, un par type, écrits par le scénariste.
- **Critère de réussite :**
  - sur 1 000 tirages par niveau (10, 20, 30, 40 et 45), 100 % des contrats sont faisables dans les zones ouvertes au personnage ;
  - à 30, une journée de contrats (450 Éclats) vaut **25 à 40 %** des Éclats d'une heure de combats (environ 20 combats de 2 monstres à 32 Éclats, soit 1 280) ;
  - en playtest, Charles fait ses trois contrats en 30 à 45 minutes au niveau 30.
- **Tests :** unitaires pour le tirage (la graine vient du personnage et du jour : deux appels donnent le même tableau), le cumul à 9 et le prix du changement. Intégration : un contrat de chasse est rendu après les combats, et un contrat de groupe compte pour chaque membre qui l'a.
- **Risques :** la corvée (d'où le cumul) ; un contrat impossible (d'où le tirage limité aux zones atteintes) ; l'inflation des Éclats (d'où le critère chiffré et le prix du changement).

#### S154 (3) : le bestiaire
- **Expérience visée :** « avant la horde, j'ouvre le bestiaire : les gardiens craignent l'Air et leur Cairn protège les voisins ; je change mon paquet ».
- **Stories :**
  - La fenêtre **Bestiaire** (panneau Menus ; touche B si elle est libre) montre les familles par zone. Un monstre reste en silhouette tant qu'il n'a pas été combattu.
  - **Des paliers de connaissance par monstre :**
    - rencontré : nom, rôle, niveau, zone ;
    - 1 victoire : PV, résistances, faiblesse ;
    - 10 victoires : ses sorts et la leçon de la famille ;
    - 25 victoires : le butin déjà vu tomber (question 80).
  - Le serveur garde un compteur de victoires par monstre et par personnage, incrémenté à la fin du combat pour chaque héros présent.
  - On y arrive aussi depuis l'onglet « bestiaire » de la fiche d'objet (T117) et par un clic droit sur un monstre de la frise.
  - Pour Plouf : `admin … --bestiary full`.
- **Critère de réussite :** chaque monstre de `monsters.json` a sa fiche (test). En playtest, un joueur trouve la faiblesse d'une famille en moins de 20 secondes depuis un combat.
- **Tests :** unitaires pour les paliers ; intégration : un combat à deux incrémente les deux compteurs.

#### S155 (5) : les succès et les titres
- **Expérience visée :** « il me manque le donjon de la Combe à quatre et 40 Gardiens pour le titre de Tailleur de pierre ».
- **Stories :**
  - Environ **60 succès** en données (`data/achievements.json`), en sept catégories :
    - Exploration : zones, pierres de passage ;
    - Chasse : paliers du bestiaire, 10, 50 et 200 victoires par famille ;
    - Donjons : chaque donjon, à plusieurs, sans tomber ;
    - Métiers : niveaux 20, 40, 60, 80 et 100 ;
    - Quêtes : chapitre 1, tranche, 10, 50 et 200 contrats ;
    - Arbre : une paire, une clé, chaque style essayé ;
    - Ensemble : grouper, entrer dans une guilde, une horde de 9 ou plus à quatre.
  - Des **points de succès** et une **douzaine de titres** cosmétiques ; un titre choisi s'affiche sous le pseudo (question 84).
  - **Les compteurs sont tenus par le serveur**, pas par le client. Ce qui se déduit de la base (quêtes rendues, niveaux, métiers, donjons finis) est rétroactif, calculé au premier chargement.
  - **Aucun succès ne se perd pour toujours** et aucun n'exige de jouer un jour donné.
  - Une fenêtre **Succès** et une annonce discrète quand on en obtient un.
- **Critère de réussite :**
  - un test vérifie que chaque succès est atteignable dans le contenu actuel (ids existants, seuils possibles) ;
  - un personnage qui a fini le chapitre 1, rejoué par l'API, a **entre 20 et 35 %** des succès ;
  - au moins 3 succès « Ensemble » demandent de jouer à plusieurs.
- **Tests :** unitaires pour les compteurs et la rétroactivité ; intégration : finir un donjon à deux donne le succès aux deux.

#### S156 (5) : la guilde qui sert, playtest de la boucle
- **Expérience visée :** « ce soir, à cinq, on a fait passer la guilde au niveau 2 ; j'ai déposé mes surplus de granit au coffre pour le forgeron de la guilde ».
- **Stories :**
  - **Le niveau de guilde, de 1 à 10.**
    - Points : un contrat rendu par un membre rapporte 10 (20 pour un contrat de groupe), un succès 5, une salle 3 finie par au moins deux membres 30.
    - Passer du niveau n à n + 1 coûte **150 × n** : le niveau 2 en une soirée à cinq, le niveau 10 (6 750 points) en six à sept semaines à cinq joueurs actifs.
    - **Aucun bonus de combat** (question 79).
  - **Le coffre de guilde**, chez Edmée.
    - Des onglets de 40 cases : 1 au niveau 1, 2 au niveau 3, 3 au niveau 6, 4 au niveau 9.
    - Des droits par rang (question 81) et un journal des 100 derniers mouvements.
  - **Le blason** (question 82) : une forme parmi 6, un pictogramme game-icons.net parmi 30 et deux couleurs des jetons de la charte. Il s'affiche dans la fenêtre et à côté du nom de guilde. Il passe par un identifiant, donc on pourra le remplacer à la direction artistique sans toucher au code.
  - Un message du meneur et l'objectif de la semaine (« 120 points : 85 »).
  - **Le playtest de la boucle :** une semaine, Charles et 2 à 5 joueurs si possible (question 75). Le serveur tient des compteurs anonymes par personnage (jours joués, durée, contrats faits), lisibles par l'admin (question 89).
- **Critère de réussite :** en intégration à trois clients, le dépôt, le retrait refusé à un membre, le journal et la montée de niveau passent. En playtest, sur 7 jours, chaque testeur revient au moins 3 jours et fait au moins 60 % des contrats offerts. Sinon, on reprend la boucle avant S157 (point d'arrêt).
- **Risques :**
  - le coffre vidé par un membre fâché : droits par rang et journal ;
  - une guilde d'un seul joueur : la boucle marche quand même, puisque les contrats et les succès sont aussi solo.

#### S157 (3) : le socle 45-60
- **Objectif :** des règles pour 45 à 60, avant tout contenu : les spécialisations s'y règlent, les Mines suivront.
- **Stories :**
  - les gabarits par rôle prolongés jusqu'à 60 (pentes de T96, revues si la mesure l'exige) ;
  - le budget des objets jusqu'à 60 et les kits « zone » et « craft » à 50, 55 et 60 ;
  - le rythme d'XP (environ 10 monstres de son niveau par niveau) ;
  - la liste des sorts de classe ouverts entre 50 et 60, et ce que devient le paquet de 12 (T96 : déjà 13 sorts et plus dès 40).
  - La question restée ouverte en T110 (un héros d'un seul élément passe trop de hordes) mesurée à 50 et à 60.
- **Critère de réussite :** T41 sur une famille de synthèse à 50, 55 et 60 : petits groupes ≥ 29 sur 30 en moyenne, hordes ≤ 2 sur 10 sans équipement, pour les trois classes.
- **Tests :** `ItemBudgetTests` jusqu'à 60, T41 aux trois niveaux.

##### Décision du game designer : le paquet de 12 après 45 *(29 octobre 2026, sprint 161 du dépôt)*
- **Expérience visée :** « à 50, mon nouveau sort arrive dans ma barre à la place d'un vieux sort du même élément ; si j'ai déjà composé mon paquet, rien ne bouge sans moi ».
- **1. La taille reste 12** jusqu'à 60 (à revoir au plus tôt avec l'approfondissement à 75). À 60, une classe a 17 sorts (20 avec une spécialisation) pour 12 places : choisir, c'est le jeu. Avec 8 PA, un héros lance 2 ou 3 sorts par tour et en utilise 6 à 8 par combat. Un paquet plus grand ajouterait de la puissance pure contre les hordes (plafond T41). Il casserait aussi les deux rangées de six (D62) et le prix de Deck serré et d'Économie (10 et 8 places). *Écartés :* 14 places à 50 (puissance et interface) ; tous les sorts, à la Dofus (le paquet ne serait plus un choix). *Gardé en réserve :* une 13e place réservée aux sorts de la voie, seulement si S158 à S160 montrent qu'aucun build ne les prend.
- **2. Le paquet par défaut (`Deck == null`) évolue, en place.** Soit L la liste des sorts débloqués (`grimoire.Spells`, dans l'ordre d'aujourd'hui) et N = `DeckLimit`. On part des N premiers de L. On parcourt ensuite les sorts suivants de L, dans l'ordre :
  - **un sort de dégâts** (`DamageMax > 0`) ne prend une case que **s'il vaut plus que le sort qu'il remplace** (*révisé le 29 octobre, voir ci-dessous*). On regarde les sorts de dégâts **de même élément** du paquet, du plus ancien au plus récent (le niveau le plus bas, puis le premier dans L), en sautant deux sortes de sorts : ceux de niveau 1 (le sort de départ de l'élément, pas cher, souvent 2 fois par tour) et ceux dont le rang dépasse 1 (des points y sont placés). Le nouveau sort prend la case **du premier qu'il bat**. S'il n'en bat aucun, il n'entre pas ;
  - **un sort utilitaire** (sans dégâts) n'entre jamais tout seul, et aucun utilitaire n'est retiré.
  - **La valeur d'un sort (révision du 29 octobre).** La première règle disait « le plus ancien est le plus faible », et la mesure l'a démentie. Châtiment (5 PA, contact, 1 fois par tour) remplaçait Fente ardente (4 PA, portée 1 à 3, 2 fois par tour), et la Garde tombait à 66 sur 100 dans la grande salle de la Combe à 41 (seuil 70). Après 35, le barème fait des nouveaux sorts **des outils de côté** (zones, entraves, gros coups à relance), pas des améliorations. La valeur, au rang 1, se calcule ainsi :
    - V = **moyenne × n + poison**, divisé par **(1 + relance)** ;
    - moyenne = (min + max) / 2 ;
    - n = min(fois par tour, ⌊8 / PA⌋) ;
    - poison = valeur × tours, **compté une fois**, car un poison du même lanceur se renouvelle sans s'empiler (`Fight.SetStatus`) ;
    - la relance compte, parce qu'un sort à relance 2 ne se lance qu'un tour sur trois (`Spell.Cooldown`) ;
    - la zone et la portée ne comptent pas, à une exception près : **un sort de contact seul (portée maximale 1) ne remplace jamais un sort qui porte à 2 ou plus**. Ce garde-fou ne joue pour aucun sort d'aujourd'hui, mais il protège les sorts de voie à venir ;
    - le nouveau sort doit être **strictement** meilleur.
  - **Les valeurs** (anciens → nouveaux) :
    - Sentinelle : Salve ardente 12 → Tir brûlant 24 ; Flèche de marée 21 → Flèche empoisonnée 24 ; Flèche perçante 19 → Rafale 12 ; Tir lourd 10,5 et Éboulis 11,5 → Pieu 5,8.
    - Garde : Fente ardente 26 → Châtiment 20,5 ; Ressac 17 → Vague de fond 10,5 ; Lame du vent 21 → Lame cyclone 11 ; Fauchage 11,5 → Coup de titan 13,3.
    - Mage : Brûlure 22 → Météore 14 ; Raz-de-marée 11,5 → Gel 7,75 ; Trait de vent 25 → Cyclone 6 ; Racines 6,5 → Faille 16,5.
  - **Les paquets automatiques qui en résultent** (de 35 à 39 : les 12 premiers sorts, comme avant) :
    - **Sentinelle :**
      - à 40, Tir brûlant prend la case de Salve ardente ;
      - à 45, Flèche empoisonnée prend celle de Flèche de marée ;
      - à 50, 55 et 60, rien ne change : Rafale, Concentration et Pieu attendent un choix.
      - À 60 : stone-arrow, fire-arrow, frost-arrow, recoil-shot, sidestep, grapple, heavy-shot, **searing-shot**, **venom-arrow**, piercing-arrow, hawk, rockfall.
    - **Garde :**
      - de 40 à 55, les 12 premiers sorts : Châtiment, Vague de fond, Lame cyclone et Cri de guerre attendent ;
      - à 60, Coup de titan prend la case de Fauchage.
      - À 60 : shield-bash, burning-blade, ice-blade, slash, taunt, bulwark, **titan-blow**, fire-lunge, undertow, wind-blade, stone-skin, quake.
    - **Mage :**
      - de 40 à 55, les 12 premiers sorts : Météore, Gel, Cyclone et Renouveau attendent ;
      - à 60, Faille prend la case de Racines.
      - À 60 : pebble, flare, ice-shard, gust, mend, frost-ward, **fault-line**, burn, tidal-wave, bolt, elemental, landslide.
  - **L'annonce** qui accompagne chaque sort refusé : « Nouveau sort : Rafale. Ton paquet automatique ne le prend pas : ajoute-le toi-même si tu veux ». C'est le moment où l'on apprend que le paquet est un choix.
  - **Écartés :**
    - *Le plus ancien remplacé sans condition :* c'est la règle mesurée, qui affaiblit la Garde.
    - *Le seul calcul de V, sans relance :* Coup de titan (40) et Pieu (17,5) écraseraient leurs rivaux, alors qu'ils ne se lancent qu'un tour sur trois.
    - *Les dégâts par PA :* Châtiment (4,1) battrait Fente ardente (3,25), c'est-à-dire le cas même qui a échoué.
    - *« Ne remplacer que si la portée maximale ne baisse pas » :* trop strict. Tir brûlant (portée 6) ne remplacerait jamais Salve ardente (portée 7) alors qu'il vaut le double.
    - *Compter la zone :* il faudrait supposer un nombre de cibles. C'est l'affaire des hordes et de `OfOne`, pas du paquet par défaut.
  - **Rien ne change en dessous de 40.** Une barre qui change sans le joueur est acceptable **seulement** pour ce paquet automatique, si trois conditions tiennent. Le changement se fait en place : les autres cases ne bougent pas. Il protège les sorts qui ont des points. Il est annoncé à la montée de niveau (« Paquet automatique : Tir brûlant remplace Salve ardente »). Un paquet enregistré une fois, même vide (D66), n'est plus jamais touché. Aujourd'hui, l'éditeur enregistre le paquet avec les points : ce cas ne concerne que les joueurs qui n'ont jamais rien enregistré.
  - **Le rejeu (tel que codé) :** le relevé garde `Deck = null`, parce que l'API compare `record.Hero` au héros enregistré. Il passe en version 3. Un relevé de version 1 ou 2 se rejoue avec la règle des N premiers. La révision du 29 octobre change la règle de la version 3 :
    - si aucun relevé de version 3 avec un paquet `null` au niveau 40 ou plus n'a été stocké (API locale, Plouf), on corrige la version 3 sur place ;
    - sinon, la règle révisée passe en version 4, et le rendement des éléments en version 5.
- **3. Le héros des simulations** applique **la même fonction** : `Built` reçoit `Deck = null` et des rangs vides. Tous les sorts sont donc au rang 1, et la protection des rangs ne joue pas. Ce sont les paquets listés au point 2, sans code de plus dans `Progression`. Avec la révision, la Garde et le Mage gardent leurs 12 premiers sorts jusqu'à 55 : la grande salle de la Combe à 41 retrouve le paquet qui passait les 70 sur 100. La Sentinelle change à 40 et à 45 ; on remesure T41 à 40 et à 45 et ses grandes salles à 41. Les points de sort vont ensuite, comme aujourd'hui, sur ses sorts de dégâts.
- **4. Les seuils s'ajoutent, ils ne remplacent rien.** T41 (≥ 27 de moyenne, au plus 2 groupes sous 18, hordes ≤ 3 sur 10 nu et ≤ 2 avec les kits) reste la règle partout, et pour tout build avec une spécialisation. À 50, 55 et 60, **le héros sans spécialisation** doit aussi tenir ≥ 29 sur 30 de moyenne et ≤ 2 sur 10 nu. Cet écart (2 victoires sur les petits groupes, 1 sur les hordes) est la marge laissée aux voies de S158 à S160, qui ajoutent de la puissance (bande de ±8 points). Les niveaux 35 à 45 ne changent pas. Si le seuil ≤ 2 nu bascule d'une exécution à l'autre (bruit d'environ 1 victoire sur 10 combats), on passe à 20 combats par horde à ces trois niveaux plutôt que de relâcher le seuil.
- **5. `OfOne` est plafonné à `DeckLimit`**, par cohérence : un joueur ne peut pas faire mieux, et `Hero.Problem` refuserait le paquet. Cela ne change rien aujourd'hui : un élément a 4 sorts et la classe 8 neutres, soit au plus 12 sorts au niveau 95 et 7 ou 8 à 60. Si les sorts de voie le font déborder : d'abord les sorts de dégâts de l'élément, puis les neutres dans l'ordre de la classe. `OfOne` reste la borne haute de la question de T110, mesurée à 50 et 60.
- **Limites :**
  - Le Mage perd Racines (−2 PM) dans son paquet automatique à 60, car Faille vaut plus selon V.
  - V ignore la zone, la portée et les entraves. Un joueur qui compose son paquet fait mieux contre les hordes, et c'est voulu.
  - Le paquet automatique grandit peu après 35 : c'est le signe que les sorts de 40 à 60 sont des choix, pas des améliorations.

##### Décision du game designer : le héros d'un seul élément et les hordes *(29 octobre 2026)*
- **Le constat.** Le héros d'un seul élément (`OfOne`) gagne 1,2 à 7,7 hordes sur 10. Le héros mixte en gagne 0 à 2,9. Le plafond T41 est de 3 nu et de 2 avec un kit.
  - **La cause est la règle des caractéristiques, pas les monstres.** `Characteristics.Bonus` donne +2 dégâts fixes par 10 points, sans limite.
  - À 50, la moitié des 490 points va à la Vitalité. Les 245 autres donnent :
    - **+49 par coup** au héros pur ;
    - environ **+12** au mixte, qui les répartit sur quatre éléments.
  - Un sort de 50 jette 15 à 25 : le pur frappe donc environ 2,2 fois plus fort. L'écart grandit avec le niveau, puisque les points montent plus vite que le barème des sorts.
- **1. Oui, c'est un problème.**
  - Un vrai joueur se concentre sur un ou deux éléments. L'éditeur le lui apprend (« 10 points = +2 dégâts dans cet élément »), et refaire ses points coûte 10 × niveau Éclats.
  - Le héros mixte de T41 sous-estime donc le joueur, avec un écart de dégâts du simple au double. Le jeu de l'IA, meilleur ou moins bon, ne compense pas un tel écart.
  - Un joueur seul qui farme des hordes de 6 à 10 monstres casse la règle de Charles (« les hordes sont faites pour le multi »), et donc la raison de se grouper.
- **2. Ce qu'on change : un rendement décroissant des points d'élément**, dans `Characteristics.Bonus`, qui prend désormais le niveau du combattant.
  - **Le seuil :** K = **2 × niveau** points.
  - **La formule :** Bonus(p, niveau) = 2 × ⌊min(p, K) / 10⌋ + 1 × ⌊max(0, p − K) / 10⌋. Jusqu'au seuil, rien ne change ; au-delà, chaque point vaut moitié moins.
  - **Où :** sur la valeur que rend `Characteristics.For`, équipement compris. Un composé converti, qui prend la somme de ses parents, passe par la même courbe.
  - **Effet à 50 :**
    - le mixte, avec environ 60 points par élément, n'est pas touché, et les planchers de T41 non plus, à tous les niveaux ;
    - le pur passe de +49 à **+34** (20 + 14), soit environ −22 % par coup.
  - **Réglage dans cet ordre, puis on s'arrête.**
    - Si `OfOne` dépasse encore le critère du point 3, l'au-delà du seuil passe à 1 par 20 points (pur à +27).
    - Si ça ne suffit toujours pas, on revient vers Charles. On n'aplatit pas davantage : un élément pur doit rester le build qui frappe le plus fort.
  - **Les monstres :** si l'un d'eux a plus de 2 × niveau points dans un élément, la courbe ne s'applique qu'aux héros.
  - **Le rejeu :** `FightRecord` passe en version 4. Un relevé de version 3 ou moins se rejoue avec l'ancien bonus linéaire.
  - **Écartés :**
    - *Remonter les gabarits ou les pentes de T96 en mesurant le pur.* Un même gabarit sert aux petits groupes et aux hordes. Des monstres qui arrêtent le pur feraient tomber le mixte, et le joueur qui débute, sous 27 contre les petits groupes. Et le Champs et la Forêt (31-34) restent tels quels.
    - *Seulement changer de héros de référence.* La mesure deviendrait juste, mais le défaut resterait.
    - *Multiplier les PV des hordes* (×k à partir de 6 monstres). Ça touche le multi réglé par `PartyBalanceTests` et les hordes du chapitre 1, alors que le défaut est l'écart entre joueurs.
    - *Des hordes aux résistances mêlées.* C'est un bon levier de contenu, gardé pour les Mines, mais il ne règle pas la règle.
  - **Réglage après la mesure de l'étape 2 (S162)** *(29 octobre 2026, choix de Charles : « garder la courbe, viser les sorts »)*
    - **Où on en est.** L'étape 2 est en place : points efficaces = K + (p − K) / 4, soit +26 au pur de 50 (245 points, 136 efficaces) et +32 à 60 (295 points, 163 efficaces). Quatre cas sur 72 dépassent encore 3 sur 10 : Sentinelle Terre à 50 nue et « zone » (3,9), Sentinelle Air « craft » (3,4 à 50, 3,7 à 60), Garde Air « craft » (3,2 à 60, 3,0 à 50). La Mage reste partout à 1,1 ou moins.
    - **La cause, en une phrase.** Chaque cas repose sur **un sort bon marché qui ferme un tour de 8 PA** et compte le bonus plat (+26 à +32, deux fois la base d'un sort de rang 5) à chaque coup ou à chaque cible : sans Tir de recul, la Sentinelle Air tombe de 3,7 à 0,0 ; sans Taillade, la Garde Air de 3,2 à 0,0 ; sans Flèche de pierre **ou** sans Éboulis, la Sentinelle Terre de 3,9 à 0,0, parce que Éboulis (5 PA) et Flèche de pierre (3 PA) remplissent exactement les 8 PA.
    - **Ce que la mesure dit des leviers.** Le bonus est un levier faible : l'étape 2 a retiré 8 points de bonus à **tous** les coups (−24 %), et la Sentinelle Terre n'a perdu que 0,5 horde. Toucher les dégâts ou le bonus d'un seul sort ne suffira donc pas. Le levier fort, c'est **le nombre de coups par tour** : on retire un coup aux tours qui font le dépassement, sans toucher la courbe.
    - **Le réglage, trois lignes de `data/spells.json`, appliquées ensemble** (chaque ligne vise un cas différent) :

      | Sort | Avant | Après | Cas visé | Niveau du sort |
      |---|---|---|---|---|
      | Éboulis (`rockfall`, Sentinelle, Terre) | `apCost` 5 | **`apCost` 6**, dégâts et rangs inchangés | Sentinelle Terre 50 | 35 |
      | Tir de recul (`recoil-shot`, Sentinelle, Air) | `perTurn` 2 | **`perTurn` 1**, dégâts et poussée de 2 inchangés | Sentinelle Air « craft » | 1 |
      | Taillade (`slash`, Garde, Air) | `perTurn` 3 | **`perTurn` 2**, dégâts inchangés | Garde Air « craft » | 1 |

      - **Éboulis à 6 PA** casse le tour « Éboulis + Flèche de pierre ». Il reste « Éboulis + Grappin » ou « Éboulis + Dérobade », et le tour perd un coup de Flèche de pierre (environ 40 dégâts à 50). Il passe un cran sous le barème, et c'est voulu : il frappe sans ligne de vue jusqu'à 8 cases. Sa valeur V ne bouge pas (n = min(1, ⌊8 / 6⌋) = 1, toujours 11,5), donc **les paquets automatiques du point 2 du paquet de 12 restent les mêmes** (Pieu, à 5,8, ne le bat toujours pas). Éboulement, le sort jumeau de la Mage, reste à 5 PA, parce que la Mage Terre est à 1,1 ou moins.
      - **Tir de recul une fois par tour.** La Sentinelle Air garde une poussée de 2 par tour au lieu de deux. Le tour devient « Tir de recul + Flèche perçante + Dérobade » (8 PA). La Flèche perçante, en ligne et à 2 cases minimum, ne sauve pas un tir à bout portant : c'est la raison du 0,0 de la sonde.
      - **Taillade deux fois par tour.** La Garde Air perd un coup par tour (un tiers des coups de Taillade). Les 2 PA libérés vont à Provocation ou à Rempart, ou bien, avec Lame du vent, le tour devient « 2 Taillades + Lame du vent ».
      - **Ce qu'on attend (estimation, sans mesure) :** Sentinelle Terre 50 vers 2,5 à 3 ; Sentinelle Air « craft » vers 1,5 à 2,5 ; Garde Air « craft » vers 2 à 2,5. **Les cas visés doivent tenir 2,7 ou moins**, la marge contre le bruit de 40 combats par horde. Les autres des 72 cas ne peuvent que baisser.
      - **Rien d'autre ne change :** ni la courbe, ni K, ni `Fight.Damage`, ni la version des relevés. Les dégâts des trois sorts restent les mêmes, pour ne pas affaiblir le héros mixte et le joueur qui débute, que les petits coups font vivre.
    - **Si la remesure échoue encore, dans cet ordre, une étape par remesure, puis on s'arrête :**
      1. **Le cas en faute seulement :**
         - Sentinelle Terre : Éboulis prend **une relance de 1** (un tour sur deux). V passe à 5,75, et Pieu (5,8) le remplace dans le paquet automatique de 60. On l'accepte et on l'écrit au point 2 du paquet de 12.
         - Sentinelle Air : la poussée de Tir de recul passe de **2 à 1** (comme Bourrasque).
         - Garde Air : Lame du vent passe à **`perTurn` 1**.
      2. **Une règle par le coût, réservée aux héros :** le bonus d'élément d'un sort vaut bonus × min(PA, 4) / 4. Un sort de 2 PA reçoit la moitié du bonus, un sort de 3 PA les trois quarts, et rien ne change à partir de 4 PA. Elle vise tous les petits sorts, ceux des voies compris, mais touche aussi le mixte et le joueur qui débute : c'est pour ça qu'elle vient en second. Il faut ajouter le texte dans la fiche (fr et en) et passer les relevés en version 5.
      3. **Si un cas dépasse encore, on revient vers Charles**, avec le tableau des 72 cas. On n'aplatit toujours pas la courbe.
      - **Si c'est un plancher qui casse** (le mixte sous 29 sur 30 à 50 ou 60, T41 sous 27 entre 1 et 45, `PartyBalanceTests`) : on annule le réglage du sort en cause, on garde les autres, et on passe pour ce cas à l'étape de repli suivante.
    - **Écartés :**
      - *Le bonus une seule fois par tour et par sort au-delà du seuil :* la part au-delà du seuil n'est que de 6 (à 50) à 8 (à 60) sur 26 à 32, et le tour de la Sentinelle Terre ne relance aucun sort.
      - *La règle par le coût en PA, dès la première étape :* elle ne touche pas Éboulis (5 PA) et ne retire à Tir de recul que 25 % de son bonus. Or −24 % de bonus n'a valu que 0,5 horde. Elle reste en repli.
      - *Baisser les dégâts des petits sorts :* −1 ou −2 sur une base de 8 à 15 ne change presque rien au pur (+26 par coup), mais touche le mixte et le niveau 1 en proportion. C'est le sens inverse de ce qu'on veut.
      - *Flèche de pierre une fois par tour :* c'est le sort de départ de la Terre, et le tour fautif est « Éboulis + Flèche de pierre », pas deux Flèches de pierre.
      - *Retirer ou affaiblir Rafale :* sans Rafale, la Sentinelle Air monte (4,2 au lieu de 3,7).
      - *Aplatir la courbe ou baisser K :* refusé par Charles. Un élément pur doit rester le build qui frappe le plus fort.
    - **Ce que ça casse ou risque de casser (à remesurer dans la même story) :**
      - **T41 du chapitre 1 (`BalanceTests`, niveaux 1 à 30) :** Tir de recul et Taillade sont des sorts de niveau 1. Le risque est faible pour le mixte, qui change d'élément d'un coup à l'autre et a toujours un autre sort de 3 ou 4 PA pour finir son tour. On remesure la Sentinelle et la Garde à 1, 10, 20 et 30 : moyenne ≥ 27.
      - **Le ressenti du joueur Air de 1 à 24 :** une Sentinelle tout Air n'a qu'un sort d'Air (Tir de recul) jusqu'à la Flèche perçante (25), et elle ne pourra plus le lancer qu'une fois par tour. C'est le vrai coût de ce réglage. On le surveille en playtest. Si quelqu'un s'en plaint, on avance la Flèche perçante, plutôt que de rendre le deuxième tir.
      - **T41 de 35 à 45 et les grandes salles :** Éboulis arrive à 35. On remesure la Sentinelle à 35, 40 et 45 et ses grandes salles de la Combe à 41.
      - **`PartyBalanceTests` :** les duos et les trios avec une Sentinelle ou une Garde perdent un peu contre les hordes. C'est le sens voulu (« les hordes sont faites pour le multi »), mais il faut vérifier les planchers du multi.
      - **L'arbre (`mesures-arbre.md`) :** le build de Manière de la Sentinelle (Poussée lourde) vit des poussées, et en aura deux fois moins par tour. Les jumeaux de Tir de recul et de Taillade (sprint 138) reprennent leur `perTurn` : on vérifie qu'ils suivent. La remesure de l'arbre est déjà prévue dans le 1,5 point du point 4.
      - **Les relevés :** les réglages de données ne changent pas de version. Un relevé stocké en version 4 qui lance deux Tirs de recul dans un tour serait refusé au rejeu. On vérifie qu'aucun relevé de ce type n'existe (API locale, Plouf) ; sinon, on les remplace.
      - **Rien ne change pour :** le paquet automatique (V inchangé), les textes (le livre de sorts lit les PA et le `perTurn` dans les données, pas de nouvelle clé fr ou en), les monstres (les sorts de synthèse viennent des monstres, pas des classes).
    - **Limite de la mesure :** le kit « craft » de synthèse donne des points d'Air (le reste du budget dans `Synthetic.Pieces`), mais aucun point de Terre. « Craft Air » est donc le seul cas où le pur porte des pièces de son élément. La Sentinelle Terre bien équipée serait au-dessus de son 2,9 mesuré. Ce réglage la vise déjà par Éboulis, mais on le note pour les Mines, où les pièces de chaque élément existeront.
    - **Ce que la remesure a donné, et ce qui est appliqué (S162, T129)** *(29 octobre 2026)* :
      - **Éboulis à 6 PA : gardé.** Sentinelle Terre de 3,9 à 0,0 (nue et « zone » à 50), 0,1 à 1,0 ailleurs. Le barème T27 de `tools/spells_gen.py` calcule les dégâts par PA : Éboulis passe à 12-16, mêmes dégâts par PA.
      - **Tir de recul `perTurn` 1 et Taillade `perTurn` 2 : annulés.** Aucun effet sur les cas « craft » (3,4, 3,7, 3,0 et 3,2, au dixième près) : avec ce kit, ces héros ne lançaient déjà qu'un Tir de recul ou une Taillade par tour. Les garder n'aurait pénalisé que le joueur Air débutant.
      - **Repli 1, Sentinelle Air : poussée de Tir de recul 2 → 1, puis annulée.** 3,4 et 3,7 deviennent 2,8 à 50 et à 60, mais quatre planchers cassent (duel du niveau 1 Sentinelle-Mage à 27,8 %, hordes des champs à 26, styles de 45, grande salle du golem à 41) : Tir de recul est un sort de niveau 1. Remis à 2, ces tests repassent.
      - **Repli 1, Garde Air (Lame du vent `perTurn` 1) : écarté sans essai.** La sonde montrait déjà que retirer Lame du vent ne change rien (3,2 → 3,1).
      - **La vraie cause des cas « craft », sur 160 combats par horde :** c'est tout ou rien. Sur les 7 hordes, deux sont des hordes de 6 petits monstres sans chef (fields-1-4.0, 6 taupes ; fields-4-3.2, 6 abeilles), et tout héros pur bien équipé (830 PV, Air 387) les gagne 154 à 160 fois sur 160, soit déjà 2,86 sur 10. Les autres ont un chef de 815 PV ou 7 à 8 monstres, et il ne les gagne pas. La Garde Air y ajoute fields-1-2.3, une fois sur quatre (38/160), d'où 3,2 à 60 (2,96 à 50).
      - **Choix de Charles : accepter, noter, avancer.** Un plafond de 3,5 pour la Garde Air « craft » a d'abord été écrit, puis remplacé par les hordes solides du paquet final ci-dessous. La règle par le coût en PA (repli 2) n'est pas appliquée.
      - **Pour les Mines :** une horde de 6 et plus a au moins un monstre solide (un chef ou un gabarit au-dessus). C'est un levier de contenu, pas une règle du combat.
    - **Le paquet final (S162)** *(29 octobre 2026, décision du game designer à la demande de Charles, après les mesures de T129)*
      - **Ce que les mesures ont appris.**
        - Le défaut n'est pas où on le cherchait. Avec K = 2 × niveau, la courbe touchait **le héros mixte équipé**, de 2,4 à 3,1 × niveau en « craft » dès le niveau 10. C'est pour ça que le Disjoint du niveau 30 tombait (Mage 18 sur 100, plancher 30).
        - Les cas « craft » au-dessus de 3 viennent de **deux hordes légères** (6 petits monstres sans chef), que tout héros pur équipé gagne. À elles seules, elles font 2,86 sur 10.
        - Les plafonds de l'arbre étaient relatifs au meilleur héros nu, donc au pur. Ils ne passaient avec le bonus linéaire que parce que ce pur était trop fort.
        - Le paquet règle ces trois points, sans toucher le niveau 1 ni le mixte.
      - **1. Le seuil : K = 3 × niveau + 5.** C'est la plus basse formule simple qui reste toujours au-dessus du héros mixte équipé (kit « craft », élément le plus haut) :

        | Niveau | 10 | 20 | 26 | 30 | 35 | 40 | 45 | 50 | 55 | 60 |
        |---|---|---|---|---|---|---|---|---|---|---|
        | K | 35 | 65 | 83 | 95 | 110 | 125 | 140 | 155 | 170 | 185 |
        | Mixte « craft » | 31 | 52 | 73 | 73 | 87 | 98 | 106 | 130 | 147 | 157 |
        | Marge | 4 | 13 | 10 | 22 | 23 | 27 | 34 | 25 | 23 | 28 |

        - Le mixte n'est jamais touché, nu, « zone », « craft » ou « fast » (77 à 30, contre K = 95). Tous les planchers qui le mesurent restent donc tels quels : `BalanceTests`, le duel du niveau 1, les hordes des champs, les styles à 45, `PartyBalanceTests`.
        - Au niveau 1, K vaut 8. Seul un joueur qui met tout dans un élément et porte une pièce de cet élément peut le dépasser, et il y perd au plus 2 de bonus. Aucun plancher ne le mesure, et c'est accepté.
        - **Écarté, K = 2 × niveau :** il touche le mixte dès 10 et casse le Disjoint.
        - **Écarté, la courbe sur les seuls points placés, l'équipement en plus et sans courbe :** le pur « craft » de 60 passerait de +36 à +50 par coup.
      - **2. La part au-delà du seuil : un quart, sans changement** (1 de bonus pour 20 points). Comme le voulait Charles, on n'aplatit pas. Avec un K plus haut, la courbe est même moins plate qu'avec 2 × niveau.
        - **Le pur à 50** (245 points) a 155 + 90 / 4 = 177 points efficaces, soit **+34 par coup**, contre +12 au mixte.
        - **Le pur « craft » à 60** (Air 387) a 185 + 202 / 4 = 235 points efficaces, soit **+46 par coup**.
        - Il reste le build qui frappe le plus fort.
        - L'exemple de la fiche devient « Terre 245, efficace 177 » (fr et en).
      - **3. La Conversion ne change pas :** la somme des deux parents, puis la même courbe avec le même K. Le dépassement de la Sentinelle de Matière ne vient pas de la somme. Il vient de l'**Arc** : 35 % de chaque coup sautent sur un ennemi à 2 cases ou moins, et 52 % avec Signature appuyée. La Garde de Matière, sans Signature appuyée, est à 5,0 ; la Sentinelle, avec, à 6,9. L'Arc est un talent, et il se règle dans l'arbre, pas dans la courbe (point 6).
      - **4. Éboulis : 6 PA, dégâts du barème T27 (12-16), gardé.** C'est le seul réglage de sort qui ait marché : la Sentinelle Terre est passée de 3,9 à 0,0. Avec le K du point 1, elle est à 2,9 au plus (« craft » à 60).
        - **La grande salle de la Combe (69 sur 100, plancher 70) :**
          1. **D'abord, 300 combats** (graines 0 à 299), comme le prévoit la règle du bruit (on mesure plus avant de relâcher). Si elle tient 70 % ou plus, c'est fini.
          2. **Sinon, Éboulis à 13-17**, un cran au-dessus du barème, ses rangs d'autant. On l'écrit comme exception dans `tools/spells_gen.py`, avec la référence D85. +1 de base aide plus le mixte de 41 (+12 de bonus) que le pur (+34) : c'est le bon sens. On remesure ensuite la salle et la Sentinelle Terre `OfOne`.
          3. **Si la salle reste sous 70,** on la note à 69, et Charles tranche. On ne revient pas à 5 PA, qui rendait 3,9 hordes au pur.
        - **Tir de recul et Taillade reviennent à leurs valeurs** (poussée 2, `perTurn` 2 et 3). Le paquet ne touche aucun sort de niveau 1.
      - **5. Les plafonds dans `TemplateTests`.**
        - **Deux sortes de hordes**, calculées depuis les données (rien n'est écrit en dur) :
          - une horde (6 monstres ou plus) est **solide** si elle a 7 monstres ou plus, ou un monstre d'au moins 3 fois les PV du plus faible du groupe (un chef) ;
          - sinon, elle est **légère**. Aujourd'hui, ce sont fields-1-4.0 (6 taupes d'environ 80 PV) et fields-4-3.2 (6 abeilles d'environ 85 PV) : les 5 autres hordes de synthèse sont solides.
        - **Le héros mixte (T41, `AFamilyMadeFromTheTemplates…`) : rien ne change.** Hordes ≤ 3 nu avant 50, ≤ 2 sinon, sur toutes les hordes, et les mêmes planchers pour les petits groupes.
        - **Le héros d'un seul élément (`AHeroOfOneElement…`, à 50 et à 60, 12 cas × 3 kits, 40 combats par horde) :** **≤ 3 sur 10 sur les hordes solides, avec chacun des kits.** La moyenne sur les hordes légères est imprimée, sans assertion.
          - Le cas à part de la Garde Air « craft » (3,5) disparaît : ses 3,2 venaient des deux hordes légères et d'une victoire sur quatre contre fields-1-2.3.
          - Estimation tirée des mesures à 160 combats : le pire cas (Air « craft », 4,3 sur toutes les hordes) tombe autour de 2 sur les hordes solides.
        - **L'arbre à 45 (`TheReferenceBuilds…`, kit « craft ») :**
          - les petits groupes ne changent pas : ≥ 27 et ≤ nu + 3 ;
          - **les hordes solides : chaque build ≤ max(meilleur héros nu, mixte ou `OfOne`, sur les hordes solides + 1,5 ; 3,0).** Le plancher de 3,0 relie l'arbre au plafond du pur : un talent peut rendre un build aussi fort que le pur le mieux équipé, pas plus. Le meilleur nu n'est donc plus le seul repère ;
          - **une seule exception, nommée dans le test :** `sentinel-matter`, plafonné à sa mesure + 0,5, avec un message qui renvoie au point 6. Si `guard-matter` ou `mage-manner` dépassent 3,0 sur les hordes solides (à 5,0 et 4,7 sur toutes les hordes, on les estime vers 3 et 2,6), ils rejoignent l'exception, au même titre, plutôt que de relâcher le plafond pour tous.
      - **6. L'Arc dans les hordes : une story à part avant S163, 1 point**, dans `mesures-arbre.md`. On essaie dans cet ordre, une étape par remesure :
        1. l'Arc saute avec le jet du coup, sans le bonus d'élément ;
        2. Signature appuyée passe de +50 % à +25 % (Arc à 44 %) ;
        3. l'Arc ne saute qu'une fois par sort et par tour.
        - Le but : `sentinel-matter` ≤ 3,0 sur les hordes solides. L'exception du point 5 est alors retirée.
        - Les voies de S163 à S165 se mesurent avec le plafond du point 5. Elles ne reçoivent pas d'exception.
      - **7. Les relevés :** K passe à 3 × niveau + 5 **dans la version 4**, sans nouvelle version, car la version 4 n'est pas encore livrée. On vérifie qu'aucun relevé de version 4 n'est stocké (API locale, Plouf). S'il y en a, on les supprime ou on les refait plutôt que d'ouvrir une version 5. Éboulis à 6 PA est un réglage de données : il ne change pas de version, comme les réglages d'avant.
      - **Ce qu'on accepte et qu'on note :**
        - Un héros pur bien équipé gagne seul les hordes légères de 6 petits monstres. C'est un défaut de contenu, pas de règle. La règle pour les Mines est déjà écrite plus haut (un monstre solide dans chaque horde de 6 et plus). Les hordes légères du chapitre 1 restent telles quelles : à leur niveau, le mixte ne les passe pas.
        - Le pur gagne jusqu'à 3 hordes solides sur 10 avec le meilleur kit. Il en perd encore 7 : les hordes restent faites pour le multi.
        - La Sentinelle de Matière dépasse le plafond jusqu'à la story du point 6.
        - La grande salle de la Combe à 41 peut rester à 69, si les étapes 1 et 2 du point 4 échouent. Charles tranche alors.
        - Le joueur pur de niveau 1 à 10 avec une pièce de son élément perd au plus 2 de bonus.
        - La Mage garde Éboulement à 5 PA. C'est une incohérence avec Éboulis, acceptée tant que la Mage Terre reste à 2,0 ou moins.
        - Le kit « craft » de synthèse ne porte que de l'Air. On le refera élément par élément avec les pièces des Mines.
      - **Appliqué et mesuré (S162, T129)** *(29 octobre 2026)* :
        - le point 6 est fait dans S162 (1 point de plus), à sa 1re étape : l'Arc d'un héros saute sans le bonus d'élément. Le build de Matière de la Sentinelle passe de 6,9 à 0,8 sur les hordes solides à 45 : **aucune exception** dans `TemplateTests` ;
        - le héros d'un seul élément, sur les 4 hordes solides à 50 et 60 : 2,5 au plus (Sentinelle Air « craft »), 1,4 ailleurs. Les 3 hordes légères (fields-1-2.3 compte aussi, ses abeilles de 112 PV n'ont pas 3 fois les PV des autres) vont jusqu'à 8,7 sur 10 (Garde Air « craft ») ;
        - les builds de l'arbre de 35 à 45 : au plus 0,8 sur les hordes solides, petits groupes de 27,0 à 30., dans `TemplateTests` à 50 et 60.**
  - **Planchers, sur le plus faible raisonnable (le mixte avec le paquet automatique) :** moyenne ≥ 29 sur 30 et au plus 2 groupes sous 18 ; hordes ≤ 2 sur 10 nu.
  - **Plafonds, sur le plus fort raisonnable (`OfOne`), pour chaque classe et chaque élément (12 cas) :** moyenne des hordes **≤ 3 sur 10 avec chacun des kits « none », « zone » et « craft »**.
    - Le ≤ 2 avec un kit de T41 avait été fixé sur le héros mixte. Le build le plus fort, au mieux équipé, garde une victoire de marge et perd encore 7 hordes sur 10.
    - Les mesures de T41 aux niveaux 35 à 45 et du chapitre 1 ne changent pas.
  - **20 combats par horde** à ces deux niveaux (bruit d'environ ±1 sur 10 combats).
  - **Pour S162 à S164 :** chaque voie doit tenir, en plus de sa bande de ±8 points, **le plafond de 3 sur 10 avec `OfOne`** dans le meilleur élément de la voie, sorts de voie compris et kit « craft ». Le seuil K ne bouge plus pendant les voies.
- **4. Le découpage.**
  - **Dans S161 :** cette décision et la mesure `OfOne` à 50 et 60 en **indicateur** dans `TemplateTests` (tableau imprimé, sans assertion, pour garder la CI verte). Les planchers du mixte sont, eux, assertés.
  - **Une story à part avant S162, « Le rendement des éléments », environ 4 points.** Les voies se règlent sur cette courbe, sinon on les règle deux fois. Elle contient :
    - la règle et ses tests unitaires : 1 point ;
    - le relevé de version 4 et le rejeu : 0,5 point ;
    - la fiche et l'éditeur de points qui montrent le seuil (« Terre 245, efficace 172 », fr et en) : 1 point ;
    - le réglage et les remesures : 1,5 point. Ce sont `OfOne` à 50 et 60, avec l'assertion du point 3, les builds de l'arbre (`mesures-arbre.md` : la Conversion somme deux parents et peut passer le seuil), les grandes salles des donjons et `PartyBalanceTests`.
- **Limites :**
  - Le héros de deux éléments (environ 2,5 × niveau par élément) n'est pas mesuré. Il est un peu touché par le seuil et se place entre le mixte et le pur.
  - Une famille qui résiste à l'élément d'un joueur pur le punit plus qu'avant. C'est voulu : le bestiaire invite à changer de paquet.

#### S158 (6) : les spécialisations, le moteur et la Sentinelle
- **Expérience visée :** « à 50, je deviens Affût : mon Faucon joue deux fois de plus et mes Sceaux durent un tour de plus ».
- **Stories :**
  - **Le moteur :** `data/specs.json`, `Hero.Spec`, les passifs des héros (`Fight.PassivesOf`), le déclencheur `Kill`, la règle changée (compilée comme un talent), 3 sorts par spécialisation générés au barème ; le rejeu, le serveur, et `admin … --spec` pour Plouf.
  - La quête « Une route à soi » passe à 50 (`talents-arbre.md` §7.4). La première spécialisation est gratuite, les suivantes coûtent 30 × niveau Éclats.
  - Un panneau qui montre les trois voies côte à côte : passif, règle, sorts, et la ligne d'ambiance.
  - **La Sentinelle :** Mire, Affût, Sente (§7.2).
- **Critère de réussite :** au niveau 50, avec ses builds de Matière et de Manière, chaque voie de la Sentinelle est **à ±8 points de la moyenne des trois** contre les hordes de la Forêt (300 combats), et gagne seule la salle 2 des deux donjons de la Forêt au moins 90 fois sur 100.
- **Tests :** unitaires pour chaque passif et chaque règle ; intégration : choisir, changer (et payer), rejouer un combat.

#### S159 (3) : la Garde
- Palissade, Brèche, Étau (§7.2), avec la synergie Palissade et Soutien (§7.3).
- **Critère de réussite :** même bande de ±8 points. Un duo Palissade et dégâts ne gagne pas plus de 1,5 victoire sur 30 de plus qu'un duo dégâts et dégâts (la règle de S144).

#### S160 (3) : la Mage, les 18 builds mesurés
- Archimage, Sorcier et Nécromancien. Le nom de ce dernier revient au scénariste (canon, `talents-arbre.md` §9, point 6).
- `Rpg.Sim --spec` : 3 classes × 3 voies × 2 builds = **18 builds à 50** (§7.3, point 4).
- **Critère de réussite :** chacun des 18 builds est à ±8 points de la moyenne de sa classe contre les hordes, et aucun ne passe sous le plancher T41 des petits groupes.

#### S161 (4) : la tresse de Glace, playtest
- **Expérience visée :** « je transmute mon Air en Glace : mes coups mordent un PA, et je garde mon Eau ».
- **Stories :**
  - Transmutation Eau → Glace ou Air → Glace (un choix), Signature appuyée et Prisme étendus à la paire, Conversion de Glace (clé ; il n'y a toujours **qu'une seule grande bascule** dans l'arbre).
  - La signature Morsure (−1 PA, 1 tour), et 24 jumeaux (8 par classe) générés à 80 %. Les noms et les renommages viennent du scénariste, puis de Charles : c'est le point de plus que prévoyait `talents-arbre.md` §10.2.
  - `TreeLayout` apprend la deuxième tresse : c'est la limite de T115, qui ne prévoyait qu'une tresse.
  - Remesure complète, `mesures-arbre.md` v3.
  - **Playtest** : Charles et un joueur extérieur jouent la Glace à 45.
- **Critère de réussite :** contre les hordes, les trois classes réunies, la Glace avec sa tresse est à moins de 8 points de la Foudre avec la sienne. Aucun nœud de la tresse de Glace ne tombe sous 20 % des builds qui la visent.

### 12.5 Hors classement (à garder en tête)

| Chantier | Pourquoi pas maintenant | Quand |
|---|---|---|
| **Les Mines de Sous-Racine** (45-60, environ 30 points) | la boucle de retour d'abord, puis les spécialisations pour les régler | v9, après S161 (question 91) |
| **Le percepteur** | c'est du JcJ autour d'un objet posé sur la carte ; avec quelques joueurs, il reste seul et n'a pas de sens | jamais dans la Saga I, sauf avis contraire (question 90) |
| **Hôtel des ventes** (5 points, §11.8) | un marché vide dit « jeu mort » | quand une dizaine de joueurs réguliers jouent sur un serveur ouvert |
| Les trois autres paires (Boue, Magma, Vapeur, Sable : 3 points chacune, une étant faite), Lumière et Ténèbres (5), triplets (12) | D80 : avec les chapitres ; les triplets après 50, quand les points de clé sont là | une paire par sprint de socle de chapitre |
| Approfondissement à 75, accomplissements à 100 (3 + 9) | il faut d'abord du contenu à ces niveaux | avec les chapitres concernés |
| Unisson | Saga II | — |
| Direction artistique, apparence, barbier | reportées par Charles à la fin du jeu | à la fin |
| Rejoindre en cours, banque du compte | inchangé depuis §11.8 | — |

### 12.6 Questions à trancher par Charles (v8)

> Réponds « Reco » pour tout prendre, une lettre, ou en texte libre. Les questions **★** sont les plus structurantes : si tu n'as que cinq minutes, réponds à celles-là.

**73 ★. L'ordre des chantiers.**
- a) mesurer (S152), la boucle de retour (S153 à S156), le socle et les spécialisations (S157 à S160), la Glace (S161), puis les Mines en v9 **(Reco)**
- b) les Mines tout de suite après S152, la boucle de retour ensuite
- c) les spécialisations d'abord, puis la boucle de retour
- d) autre ordre

**74 ★. Électrisation.**
- a) la règle du §12.3 : mesurer sans la tresse, puis 35 → 25 % seulement si la réaction seule dépasse les autres paires de plus de 8 points, et 2 sauts par tour au plus si un trio de Foudre domine de plus de 10 points **(Reco)**
- b) 25 % tout de suite, sans mesure de plus
- c) ne rien changer : c'est l'identité de la Foudre
- d) le saut ne part que des coups de celui qui a électrisé (la synergie de groupe disparaît)

**75 ★. Jouer avec d'autres.** La boucle de retour se juge avec plusieurs joueurs.
- a) Charles seul, plus un invité ponctuel
- b) un cercle fermé de 2 à 5 testeurs pour le playtest de S156, sur un serveur ouvert le temps d'une semaine **(Reco)**
- c) un serveur ouvert en continu dès maintenant
- d) personne avant la direction artistique

**76. La cadence des contrats.**
- a) 3 par jour, cumulables jusqu'à 9 **(Reco)**
- b) 3 par jour, non cumulables
- c) seulement à la semaine
- d) répétables sans limite

**77. La récompense des contrats.**
- a) XP (un quart de niveau) et Éclats (5 × niveau), sans nouvelle monnaie **(Reco)**
- b) une monnaie de contrats et un marchand à part
- c) des objets tirés au sort
- d) a) plus une petite chance de clef de donjon

**78. Les contrats de groupe.**
- a) au plus un sur trois, échangeable gratuitement **(Reco)**
- b) jamais : les contrats restent solo
- c) un par jour, imposé

**79 ★. Ce que donne le niveau de guilde.**
- a) seulement du pratique : onglets du coffre, objectif de la semaine, blason ; aucun bonus de combat **(Reco)**
- b) a) plus +5 % d'XP aux membres
- c) pas de niveau de guilde

**80. Le butin dans le bestiaire.**
- a) seulement ce qu'on a vu tomber, avec « commun », « rare » ou « très rare » **(Reco)**
- b) les taux exacts
- c) tout le butin dès la première victoire

**81. Les droits du coffre de guilde.**
- a) tout le monde dépose ; le meneur et les officiers retirent ; le meneur peut ouvrir un onglet aux membres **(Reco)**
- b) tout le monde dépose et retire
- c) un plafond de retraits par jour et par rang

**82. Le blason.**
- a) composé maintenant (forme, pictogramme, deux couleurs de la charte), remplaçable à la direction artistique sans toucher au code **(Reco)**
- b) attendre la direction artistique
- c) pas de blason

**83. Ce que rapportent les succès.**
- a) des points et des titres cosmétiques, aucune puissance **(Reco)**
- b) a) plus des Éclats
- c) a) plus des objets cosmétiques, après la direction artistique

**84. Le titre.**
- a) un seul, choisi, sous le pseudo et au-dessus du nom de guilde, et qu'on peut masquer **(Reco)**
- b) seulement dans la fiche du personnage
- c) pas de titres

**85. Le premier coup d'un glyphe (limite de T100).**
- a) un glyphe neuf épargne, à son prochain début de tour, celui qui s'y trouvait déjà : il a le temps de partir **(Reco)**
- b) laisser tel quel

**86. Les spécialisations.**
- a) au niveau 50, avant les Mines (S158 à S160), avec les valeurs de `talents-arbre.md` §7 déjà validées en D80 **(Reco)**
- b) avec les Mines
- c) après les Mines

**87. La deuxième tresse.**
- a) la Glace, dans l'ordre de `elements.md` (c'est ton exemple, et c'est une tresse de contrôle face à une tresse de dégâts) **(Reco)**
- b) le Sable, la paire la plus faible des mesures, de Terre comme les Mines
- c) la Boue, complément de la Foudre
- d) aucune avant les Mines

**88 ★. Les retours en attente.**
- a) avant S153, Charles fait les trois playtests (tranche, guildes, arbre) et les relectures (textes v7, v8 et v9, Edmée, couches, bandeaux, lignes d'ambiance) **(Reco)**
- b) au fil de l'eau, sans bloquer les sprints
- c) le scénariste relit et Charles valide en bloc

**89. Les compteurs de jeu pour le playtest.**
- a) oui : jours joués, durée et contrats faits, par personnage, sans aucune donnée personnelle, lisibles par l'admin seulement **(Reco)**
- b) non : les retours oraux suffisent

**90. Le percepteur.**
- a) jamais dans la Saga I **(Reco)**
- b) avec un JcJ, plus tard
- c) une version JcE (des monstres l'attaquent, la guilde le défend)

**91. Après S161.**
- a) approfondir : les Mines de Sous-Racine (45 à 60), une zone sur le modèle de la tranche **(Reco)**
- b) élargir : une deuxième région de 30 à 45 (la Côte des Brisants, l'Eau, la copie de sort)
- c) les deux en alternance

**92. Le panneau Menus**, qui passe à 11 fenêtres avec le Bestiaire et les Succès.
- a) 4 rangées de 3 (ce que T112 proposait déjà) **(Reco)**
- b) 3 rangées de 4
- c) une seule fenêtre « Collection » (bestiaire et succès en onglets), soit 10 fenêtres

### 12.7 Planification v8 (si Charles dit « Go »)

- **Ajoutés :** S152 à S161, soit 10 sprints et **40 points**. Le projet passerait de **614 à 654 points** et de **151 à 161 sprints** ; Osmose, de **393 à 433**.
- Il faudra mettre à jour `PLAN.md`, `data/scrum.json`, les fichiers `scrum/sprint-152.md` à `sprint-161.md`, R2, R8 et `scopeDecisions` (règle « ajout = gestion de projet à jour »), et écrire D83.
- **Deux playtests prévus :** S156 (la boucle de retour, une semaine) et S161 (la Glace) ; **un point d'arrêt** après S156.

| Sprint | Pts | Chantier | Contenu |
|---|---|---|---|
| S152 | 3 | 11 | Mesurer avant de régler : Électrisation décomposée, la ligne du héros nu, le trio ; restes des guildes et des glyphes |
| S153 | 5 | 12 | Le tableau des contrats |
| S154 | 3 | 12 | Le bestiaire |
| S155 | 5 | 12 | Les succès et les titres |
| S156 | 5 | 12 | La guilde qui sert (niveau, coffre, blason) ; **playtest de la boucle** |
| S157 | 3 | 13 | Le socle 45-60 |
| S158 | 6 | 13 | Les spécialisations : le moteur et la Sentinelle |
| S159 | 3 | 13 | Les spécialisations de la Garde |
| S160 | 3 | 13 | Les spécialisations de la Mage, les 18 builds mesurés |
| S161 | 4 | 14 | La tresse de Glace ; **playtest** |
