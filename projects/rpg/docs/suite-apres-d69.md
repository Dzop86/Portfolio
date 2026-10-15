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
>
> Rédigée après lecture du dépôt au commit `08cb31c` :
> - `DECISIONS.md` (T29 à T62), `docs/design-familles.md`, `docs/trame-saga-1.md`, `docs/bible.md`, `PLAN.md` (D54 à D70) ;
> - les données `data/items.json`, `sets.json`, `monsters.json`, `quests.json`, `jobs.json`, `classes.json`, `data/towns/clairval.json`, `data/towns/woods-2-2.json` ;
> - le code `src/Rpg.Core/Fight.cs`, `Items.cs`, `Jobs.cs`, `Progression.cs`, `Monsters.cs`, `FightRecord.cs`, `Data.cs`, `Town.cs`, `src/Rpg.Client/ItemStyle.cs`, `GameMenu.cs` (`WindowState.Bar`), `godot/SpellIcon.cs`, `godot/SideBar.cs`, `godot/assets/icons/spells/credits.json`, `src/Rpg.Api/Endpoints/CharacterEndpoints.cs`, `src/Rpg.Api/Data/Entities.cs`, et l'outil `tools/world_gen.py`.
>
> Les points **[À TRANCHER]** ont une **valeur par défaut** (§9), à utiliser tant que Charles n'a pas dit autre chose. **Rien n'est à implémenter avant son accord sur la v3.**

---

## 0. La réponse courte

| Rang | Chantier | Le trou qu'il bouche | Taille | Dépend de |
|---|---|---|---|---|
| 0 | **Correctif** : la quête principale bloquée à la Bauge | la chaîne d'Ysaline exige au niveau 5 un donjon fait pour plusieurs | ≤ 1 pt, dans S95 | rien |
| 1 | **Le butin de Clairval : panoplies et initiative** | il n'y a presque rien à looter, et l'initiative ne compte pas | 2 sprints (5 + 3) | rien |
| 1 bis | **Des icônes d'objets** *(v3)* | les glyphes dessinés par le jeu sont laids, et 150 objets nouveaux arrivent | 1 sprint (3) | 1 (la liste des nouveaux objets) |
| 2 | **Les Éclats servent à quelque chose** | la monnaie s'accumule sans aucun usage | 1 sprint (3) | 1 (les prix des objets) |
| 3 | **Les métiers : récolte, fenêtre Métiers, ateliers, fabrication** *(v2, v3)* | les cartes ne donnent rien hors des combats, et l'établi ne fait que des clefs | 5 sprints (5 + 3 + 3 + 3 + 3) | 1 (budget des objets), 1 bis (icônes), 2 (prix de Rose) |
| 4 | **Jouer ensemble** | les hordes, les chefs, les salles 3 et les quêtes de donjon sont faits pour plusieurs, mais le jeu ne connaît qu'un héros par combat | 4 sprints (3 + 5 + 5 + 5) | 1 ; 3 pour l'échange entre joueurs |
| 5 | **La fin du chapitre 1** | l'histoire s'arrête au niveau 8, alors que 94 % de l'XP jusqu'au niveau 30 vient après | 3 sprints (3 + 3 + 3) | 0 |
| 6 | Le monde après 30 (concept de `design-familles.md` §7) | il n'y a plus rien après le niveau 30 | plusieurs mois | 1 à 5 et une décision de Charles |

**Ma recommandation :** faire les chantiers 0 à 5 dans cet ordre, soit **58 points, sprints 95 à 110**, avec un playtest après le chantier 1 et un autre après le chantier 3.

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
- **S98 (3 points)**, juste après les panoplies et l'étal de Rose (fait au S97 avant la v3), pour que tous les objets du chantier 1 arrivent avec leur icône. Les objets des métiers (S99 à S103) reçoivent la leur en même temps qu'ils sont écrits.
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
- **Puits 3 : les potions en combat.** Il n'est plus en option : il arrive avec l'alchimiste, en S103 (§5.5).
- Le marchand se fait **au village seulement**, et le serveur vérifie tout.

**Tests :** prix calculés par les règles ; achat refusé sans Éclats ; vente d'un objet porté refusée ; réinitialisation facturée au bon prix ; intégration API achat → inventaire → Éclats.

---

## 5. Chantier 3 : les métiers (S99 à 5 points, S100 à S103 à 3 points) *(v2 et v3, demandes de Charles)*

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
| **Tailleur** | fabrication (équipement) | plastrons, épaulettes | tous les combattants | atelier du tailleur |
| **Alchimiste** | fabrication (consommables) | potions de soin et huiles d'élément (utilisables en combat) | tous, surtout en donjon | alambic |
| Serrurier (existant) | fabrication (clefs) | clefs des donjons | tous | établi (existant) |

Comme le veut Charles, **les métiers de fabrication font les équipements (forgeron, tailleur), les clefs de donjon (serrurier) et les consommables (alchimiste)**. Tous les métiers, de récolte comme de fabrication, vont de 1 à 100.

- **Écartés pour l'instant :**
  - **bijoutier et cordonnier** : ils feraient anneaux, amulettes et bottes, les pièces des panoplies tombées et de l'initiative ;
  - **la cuisine** (le pain et les céréales du paysan) : les PV reviennent pleins après chaque combat hors donjon, et la fontaine les rend en donjon, donc un plat n'aurait pas d'usage. Elle reviendra avec un effet qui compte (un bonus d'une heure, par exemple) ;
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
  - Elle porte **la forge, l'atelier du tailleur et l'alambic**, et accueille l'établi du serrurier, déplacé.
  - Un artisan PNJ par atelier, qui explique son métier et donne une quête d'initiation : récolter 5, fabriquer 1 (50 XP de métier).
  - `Town.Workshops` sert déjà pour n'importe quel métier. L'ancien panneau de l'établi (T48) est remplacé par la fenêtre Métiers (§5.9), qui s'ouvre sur le bon métier, en mode « Fabriquer ».
  - *Alternative :* les ateliers dans les zones (la forge au Bois moisi). Écartée : le craft est un moment social, au même endroit que Rose ; les zones gardent la récolte.

### 5.6 Quelles pièces tombent, lesquelles se fabriquent

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
- **T41 :** les planchers restent mesurés **nus, sans consommable**. Les plafonds sont mesurés avec le **kit « fast » + les 4 pièces d'artisan de la zone + le meilleur consommable** (potion ou huile, le pire des deux pour la règle). Si un plafond saute, on baisse d'abord les chiffres des pièces d'artisan : elles sont garanties, donc elles pèsent plus sur l'équilibre que le butin.
- **Initiative :** inchangée par le craft (§3.2). Aucune pièce d'artisan et aucun consommable n'en donne.

### 5.8 Dépendances avec le jeu à plusieurs
- Les points partagés n'ont pas besoin du chantier 4. Le serveur garde leur état. Le client le lit en entrant sur une carte, et un refus « déjà récolté » le met à jour. Quand le chantier 4 diffuse les présences (S104), il diffuse aussi « point récolté » et « point repoussé ».
- **L'échange entre joueurs** (deux joueurs sur la même carte, les deux valident) entre dans le chantier 4, au sprint du groupe (S105, 5 points). C'est ce qui donne un sens économique aux métiers à plusieurs.

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

| Sprint | Pts | Contenu |
|---|---|---|
| S99 | 5 | **La récolte.** `Job` gagne `Kind` (récolte ou fabrication) et `Harvests` (ressource, niveau, nature du point : arbre, roche, eau, parcelle, herbe) ; champ `nodes` des cartes, écrit par `world_gen.py` (ressource selon la profondeur de la carte) ; table serveur des points (carte, case, prêt à) et `POST …/harvest` ; règle des 20 niveaux pour l'XP ; client : points dessinés (les parcelles en carrés de culture), clic, marche, barre de 3 s avec l'outil en main, « +2 Chêne » flottant ; les **40 ressources** (niveaux 1 à 35) et les 5 métiers de récolte. |
| S100 | 3 | **La fenêtre Métiers** (§5.9), touche J, `JobBook` ; elle remplace le panneau de l'établi. |
| S101 | 3 | **Le quartier des artisans** : la carte [1, 0], la porte est, les 4 ateliers et leurs artisans, les 3 quêtes d'initiation ; l'établi du serrurier déplacé. |
| S102 | 3 | **Forgeron et tailleur** : les 16 pièces d'artisan et leurs recettes (niveaux 1 à 35), `Recipe.Xp`, `ItemBudgetTests` étendu ; T41 remesuré (kit « fast » + artisan). |
| S103 | 3 | **L'alchimiste et `UseItem`** : 4 potions, 4 huiles (une par élément à chaque taille), un consommable par combat, rejoué par le serveur, l'IA ne s'en sert pas ; Rose revend les potions ; T41 remesuré. |

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

## 6. Chantier 4 : jouer ensemble (S104 à S107, 18 points)

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
| S104 | 3 | **Se voir.** Le serveur diffuse aux joueurs d'une carte l'arrivée, la case visée et le départ de chacun, ainsi que l'état des points de récolte. **Les groupes de monstres deviennent communs** : leur errance se calcule à partir de l'horloge, et « battu » et « en combat » passent côté serveur. |
| S105 | 5 | **Le groupe et l'échange.** Inviter au groupe (4 au plus), un chef de groupe, le canal de groupe du chat, le bandeau du groupe ; **l'échange entre deux joueurs** de la même carte (objets et Éclats, chacun valide, le serveur fait tout ou rien). |
| S106 | 5 | **Le combat à plusieurs : règles et serveur.** `Fight` prend une liste de héros ; `FightRecord` passe à une nouvelle version ; XP, Éclats, butin (tiré pour chacun) et quêtes par héros ; un consommable par héros ; le hub de combat, le tour de 30 s, la reconnexion ; le simulateur fait jouer plusieurs héros par l'IA. |
| S107 | 5 | **Le combat à plusieurs : client et donjons.** « Rejoindre » pendant 15 s ; la frise et la fin de combat à plusieurs lignes ; les donjons à plusieurs (chacun use sa clef) ; l'équilibre à plusieurs (§6.4). |

### 6.4 Équilibre à plusieurs (validé par Charles)
Mesuré avec des héros joués par l'IA, en compositions de classes mélangées, au niveau de la zone, avec le kit « zone » :
- hordes : **2 joueurs ≈ 50 %, 3 joueurs ≥ 80 %** ;
- chefs et salles 3 : **3 joueurs ≥ 70 %, 4 joueurs ≥ 90 %**.

On règle les PV du chef seul. **XP :** la règle actuelle reste (XP entière pour chacun, +10 % par allié).

### 6.5 Risques
C'est le chantier le plus risqué : il touche le moteur, le serveur (un état en mémoire, un seul processus), le client et l'auto-test. Il faut **un test d'intégration à deux clients WebSocket** dès S106, et accepter qu'un sprint glisse.

---

## 7. Chantier 5 : la fin du chapitre 1 (S108 à S110, 9 points)

- **S108 (3) :** l'objectif `Interact` (un objet posé sur une carte, `Town.Props`) ; la chaîne d'Ysaline après « Quatre pistes », en **option A** de la trame (chapitre 1 = niveaux 1 à 30) : « Ce qui pousse entre » (10), « La crypte que personne n'a creusée » (12), « Crypte oubliée » (14, `Room: 2` puis `Interact` sur le sceau).
- **S109 (3) :** le Lac et les Champs, de « La berceuse que personne n'a apprise » (18) à « Sous les haies » (28). **Liane** est la seule nouvelle PNJ.
- **S110 (3) :** le **Disjoint** (`design-familles.md` §4.9), **gagnable seul au niveau 30 (50 à 70 %)**, « groupe conseillé » ; puis « Quatre routes » (30). Tant que le chantier 6 n'existe pas, le chapitre se termine sur une promesse, sans contenu fantôme.
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
| Bijoutier, cordonnier, paysan et cuisine | élargir l'artisanat quand le butin aura ses panoplies 30+ | 3 pts par métier |

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
| 13 | Quels métiers au départ | **Récolte : les quatre de base demandés par Charles (bûcheron, mineur, pêcheur, paysan, un par élément), plus l'herboriste pour l'alchimie. Fabrication : forgeron, tailleur, alchimiste**, en plus du serrurier. Bijoutier, cordonnier et cuisine plus tard |
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
| 22 | L'échange entre joueurs | **Oui, avec le groupe (S105)** |

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

---

## 10. Planification proposée (si Charles dit « Go » sur la v3)

D71 (v1 et v2, 52 points, sprints 95 à 108) et **D72** (v3 : les icônes, la fenêtre Métiers, une ressource tous les 5 niveaux ; 6 points de plus, sprints 109 et 110) couvrent ensemble les sprints **95 à 110** : **58 points**. Il y a 36 points pour les chantiers de la v1, 14 pour les métiers de la v2 (dont les 3 de la potion, autrefois en option), 2 pour l'échange entre joueurs, 3 pour les icônes et 3 pour la fenêtre Métiers.
- Le projet passe de **381 à 439 points**, et rpg de **160 à 218**.
- Il faut mettre à jour `PLAN.md`, `data/scrum.json`, les fichiers `scrum/sprint-NN.md`, ainsi que R2, R8 et `scopeDecisions` (règle « ajout = gestion de projet à jour »).
- Les entrées de `DECISIONS.md` commencent à **T63**.

| Sprint | Pts | Chantier | Contenu |
|---|---|---|---|
| S95 | 5 | 0 et 1 | Correctif de la Bauge ; moteur de l'initiative ; panoplies du Bois et du Bois moisi ; `--kit` |
| S96 | 3 | 1 | Panoplies du Lac et des Champs ; T41 remesuré ; **playtest** |
| S97 | 3 | 2 | Rose marchande, puits d'Éclats (fait, T65) |
| S98 | 3 | 1 bis | Les icônes d'objets (game-icons.net, cadre selon la provenance) |
| S99 | 5 | 3 | La récolte : 5 métiers (bûcheron, mineur, pêcheur, paysan, herboriste), 40 ressources (niveaux 1 à 35), les points des cartes |
| S100 | 3 | 3 | La fenêtre Métiers (touche J), qui remplace le panneau de l'établi |
| S101 | 3 | 3 | Le quartier des artisans, les ateliers et les quêtes d'initiation |
| S102 | 3 | 3 | Forgeron et tailleur : 16 pièces d'artisan ; T41 remesuré |
| S103 | 3 | 3 | Alchimiste, potions et huiles en combat ; T41 avec consommable ; **playtest** |
| S104 | 3 | 4 | Se voir sur la carte, des monstres et des points communs |
| S105 | 5 | 4 | Le groupe, son canal de chat, l'échange entre joueurs |
| S106 | 5 | 4 | Le combat à plusieurs : règles, serveur, simulation |
| S107 | 5 | 4 | Le combat à plusieurs : client, donjons, équilibre |
| S108 | 3 | 5 | Objectif `Interact`, chaîne du Bois moisi |
| S109 | 3 | 5 | Chaîne du Lac et des Champs |
| S110 | 3 | 5 | Le Disjoint, « Quatre routes » |
