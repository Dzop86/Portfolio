# Osmose : la suite après D69

> **Version 2, 15 octobre 2026.**
> - **Version 1** (même jour) : les chantiers 0 à 4, l'initiative par l'équipement. **Charles a validé toutes les valeurs par défaut** de la v1 (questions 1 à 11 du §9).
> - **Version 2 :** ajoute, à sa demande, **les métiers** (récolte et fabrication, ateliers, ressources récoltables, craft). C'est le chantier 3, au §5. Le plan, les numéros des chantiers, le total de points et les questions sont mis à jour.
> - **Version 2.1 :** précision de Charles, il faut **au moins quatre métiers de récolte de base : bûcheron, mineur, pêcheur, paysan**. Ils sont au §5.2, un par élément, et l'herboriste reste en cinquième métier de récolte.
>
> Rédigée après lecture du dépôt au commit `08cb31c` :
> - `DECISIONS.md` (T29 à T62), `docs/design-familles.md`, `docs/trame-saga-1.md`, `docs/bible.md`, `PLAN.md` (D54 à D70) ;
> - les données `data/items.json`, `sets.json`, `monsters.json`, `quests.json`, `jobs.json`, `classes.json`, `data/towns/clairval.json`, `data/towns/woods-2-2.json` ;
> - le code `src/Rpg.Core/Fight.cs`, `Items.cs`, `Jobs.cs`, `Progression.cs`, `Monsters.cs`, `FightRecord.cs`, `Data.cs`, `Town.cs`, `src/Rpg.Api/Endpoints/CharacterEndpoints.cs`, `src/Rpg.Api/Data/Entities.cs`, et l'outil `tools/world_gen.py`.
>
> Les points **[À TRANCHER]** ont une **valeur par défaut** (§9), à utiliser tant que Charles n'a pas dit autre chose. **Rien n'est à implémenter avant son accord sur la v2.**

---

## 0. La réponse courte

| Rang | Chantier | Le trou qu'il bouche | Taille | Dépend de |
|---|---|---|---|---|
| 0 | **Correctif** : la quête principale bloquée à la Bauge | la chaîne d'Ysaline exige au niveau 5 un donjon fait pour plusieurs | ≤ 1 pt, dans S95 | rien |
| 1 | **Le butin de Clairval : panoplies et initiative** | il n'y a presque rien à looter, et l'initiative ne compte pas | 2 sprints (5 + 3) | rien |
| 2 | **Les Éclats servent à quelque chose** | la monnaie s'accumule sans aucun usage | 1 sprint (3) | 1 (les prix des objets) |
| 3 | **Les métiers : récolte, ateliers, fabrication** *(v2)* | les cartes ne donnent rien hors des combats, et l'établi ne fait que des clefs | 4 sprints (5 + 3 + 3 + 3) | 1 (budget des objets), 2 (prix de Rose) |
| 4 | **Jouer ensemble** | les hordes, les chefs, les salles 3 et les quêtes de donjon sont faits pour plusieurs, mais le jeu ne connaît qu'un héros par combat | 4 sprints (3 + 5 + 5 + 5) | 1 ; 3 pour l'échange entre joueurs |
| 5 | **La fin du chapitre 1** | l'histoire s'arrête au niveau 8, alors que 94 % de l'XP jusqu'au niveau 30 vient après | 3 sprints (3 + 3 + 3) | 0 |
| 6 | Le monde après 30 (concept de `design-familles.md` §7) | il n'y a plus rien après le niveau 30 | plusieurs mois | 1 à 5 et une décision de Charles |

**Ma recommandation :** faire les chantiers 0 à 5 dans cet ordre, soit **environ 52 points, sprints 95 à 108**, avec un playtest après le chantier 1 et un autre après le chantier 3.

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

## 4. Chantier 2 : les Éclats servent à quelque chose (S97, 3 points)

### 4.1 L'expérience visée
« J'ai 800 Éclats : je refais mes caractéristiques, ou je garde de quoi acheter la clef de la ruche ? »

### 4.2 Contenu
- **Rose devient marchande** (son dialogue `merchant` existe).
  - Elle **achète** les ressources (1 + niveau de la ressource / 10, soit 1 à 4 Éclats) et les équipements (2 × leur niveau).
  - Elle **vend** les ressources à 5 fois leur prix de rachat : celles des monstres comme celles de la récolte (§5).
  - Les potions viennent de l'alchimiste (§5.5). Rose les vend à 10 + niveau Éclats, plus cher que leur fabrication.
- **Puits 1 : refaire ses caractéristiques** coûte **10 × niveau** Éclats, gratuit jusqu'au niveau 10.
- **Puits 2 : les clefs du serrurier** coûtent aussi **5 × le niveau du donjon** en Éclats.
- **Puits 3 : les potions en combat.** Il n'est plus en option : il arrive avec l'alchimiste, en S101 (§5.5).
- Le marchand se fait **au village seulement**, et le serveur vérifie tout.

**Tests :** prix calculés par les règles ; achat refusé sans Éclats ; vente d'un objet porté refusée ; réinitialisation facturée au bon prix ; intégration API achat → inventaire → Éclats.

---

## 5. Chantier 3 : les métiers (S98 à 5 points, S99, S100, S101 à 3 points) *(v2, demande de Charles)*

### 5.1 L'expérience visée
« En traversant le Lac pour aller aux crabes, je coupe deux saules et je pêche une anguille. Ce soir, je forge le bouclier de mon palier ; la panoplie des grenouilles, je l'attends du butin. »

Les métiers ajoutent **une boucle méso sans combat**, avec sa propre progression (motivation de **compétence** et de **collection**). Ils donnent aussi un **plancher d'équipement garanti**, face au butin qui reste aléatoire. Trois principes :
1. **Le butin reste la source des pièces aspirationnelles** (panoplies, initiative, bonus de PA et de PM). Le craft donne les pièces de base, fiables, **sans panoplie ni initiative**. On ne casse ni la direction de Charles (l'initiative par le butin) ni T41.
2. **Chaque recette lie les deux boucles** : des ressources récoltées et des ressources de monstres. Le récolteur va au combat, le combattant va à la récolte.
3. **Périmètre réaliste.** 5 métiers de récolte (les 4 de base demandés par Charles et l'herboriste), 3 nouveaux métiers de fabrication en plus du serrurier, 4 paliers (un par zone). Ensuite, tout est en données.

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
| **Forgeron** | fabrication | armes à une et à deux mains, boucliers | tous les combattants | forge |
| **Tailleur** | fabrication | plastrons, épaulettes | tous les combattants | atelier du tailleur |
| **Alchimiste** | fabrication | potions de soin (utilisables en combat) | tous, surtout en donjon | alambic |
| Serrurier (existant) | fabrication | clefs des donjons | tous | établi (existant) |

- **Écartés pour l'instant :**
  - **bijoutier et cordonnier** : ils feraient anneaux, amulettes et bottes, les pièces des panoplies tombées et de l'initiative ;
  - **la cuisine** (le pain et les céréales du paysan) : les PV reviennent pleins après chaque combat hors donjon, et la fontaine les rend en donjon, donc un plat n'aurait pas d'usage. Elle reviendra avec un effet qui compte (un bonus d'une heure, par exemple) ;
  - **l'herboriste fusionné dans le paysan** : un seul métier pour les fibres et les plantes médicinales nourrirait à la fois le tailleur et l'alchimiste, ce qui en ferait un passage obligé. Séparés, chaque métier de fabrication a son récolteur principal ;
  - **les produits intermédiaires** (planches, lingots) : un étage de plus pour le même plaisir.
- Un personnage peut **apprendre tous les métiers**, sans limite. Le jeu se joue encore surtout seul : une limite à la Dofus obligerait à trouver d'autres joueurs pour s'équiper.

### 5.3 Les ressources récoltables
**Un palier par zone**, une ressource par métier et par palier, soit **20 ressources récoltables** (objets `kind: Resource` nouveaux).

| Palier (niv. de métier) | Zone | Bûcheron | Mineur | Pêcheur | Paysan | Herboriste |
|---|---|---|---|---|---|---|
| 1 | Bois (3) | Chêne | Cuivre | Goujon (mares du bois) | Lin sauvage (clairières) | Ortie |
| 10 | Bois moisi (10) | Bois noir | Fer | Anguille des mares | Chanvre des mares | Mousse-lumière (sur les `M`) |
| 20 | Lac (18) | Saule | Étain | Perche (rives du lac) | Laîche des rives | Nénuphar |
| 30 | Champs (26) | Frêne | Argent | Brochet (eau profonde, depuis les pontons du lac) | Blé de Clairval | Fleur de ruche (sur les `,`) |

- Le brochet se pêche **au Lac** : les Champs n'ont pas d'eau (`STYLES` de `world_gen.py`). Le palier 30 du pêcheur oblige donc à retourner au Lac.
- **Le paysan cueille, il ne sème pas.** Ses parcelles sont des carrés de 2 × 2 cases de culture sur le sol, où l'on marche : lin des clairières, chanvre au bord des mares, laîche des rives. Aux Champs, ce sont de vraies parcelles cultivées : le blé de Clairval, que les paysans du village laissent glaner. Il n'y a ni semis, ni arrosage, ni attente de pousse. Planter serait un système à part entière (des parcelles à soi, des temps de pousse), à garder pour le jour où il y aura des maisons ou des enclos.
- **Les paliers 1, 10, 20 et 30 sont ceux du serrurier** (T48). Le lien avec le niveau des zones est volontairement **décalé** : un herboriste de niveau de personnage 26 doit avoir monté son métier ailleurs pour cueillir aux Champs. Le métier est une progression à part, pas un sous-produit du niveau.

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

Chaque métier de base a au moins un point sur presque chaque carte où il a quelque chose à récolter. Le village n'en a aucun.
- **Partagés entre les joueurs** (un MMO : on voit l'autre récolter). Un point récolté se **repousse en 2 + palier/10 minutes**, soit 2 à 5 minutes, pour tout le monde. Avec peu de joueurs aujourd'hui, la concurrence restera rare, et la repousse courte limite la frustration.

**Comment on récolte :**
- **Clic** sur le point, le personnage **marche jusqu'à une case voisine**, puis une **action de 3 secondes** avec une barre. Bouger l'annule.
- **Aucun outil exigé** : un outil à acheter ou à équiper serait une porte de plus sans décision intéressante. Le pic, la nasse, la hachette et la faucille apparaissent dans la main du personnage pendant l'animation, et ils donnent leur nom au geste. Ils pourront devenir plus tard des objets à améliorer (cosmétique ou bonus de quantité).
- Le serveur vérifie :
  - le niveau de métier ≥ palier ;
  - le point disponible ;
  - le personnage sur cette carte, à une case du point de sa dernière place enregistrée ;
  - une seule récolte par 3 secondes par personnage.
  Il tire ensuite la quantité avec son propre générateur et donne la ressource et l'XP.
- **Quantité :** 1 + (niveau de métier − palier) / 20, bornée à 3. Un bûcheron de niveau 41 coupe 3 chênes d'un coup : l'avance se récompense sans rendre le bas niveau inutile.

### 5.4 La progression des métiers
- **Courbe inchangée** (`Crafting.XpToReach`, 10 × (N − 1) × N) : 900 XP pour le niveau 10, 3 800 pour le 20, 8 700 pour le 30. Elle va jusqu'au niveau 100, mais **le contenu de Clairval s'arrête au palier 30**.
- **XP de récolte :** 25 + 5 × palier par action. Cela donne **environ 30 à 40 récoltes par tranche de 10 niveaux** :
  - palier 1 : 30 XP, soit 30 actions jusqu'au niveau 10 ;
  - palier 10 : 75 XP, soit 39 actions de 10 à 20 ;
  - palier 20 : 125 XP, soit 39 actions de 20 à 30.
  Avec 1 à 3 points du métier par carte, cela fait 15 à 25 minutes de récolte par palier, entre les combats.
- **XP de fabrication :** la règle actuelle (20 + 5 × niveau de la recette) convient aux clefs et aux potions. Pour l'**équipement**, il faut un champ optionnel `xp` sur `Recipe`, à **150 + 15 × niveau de recette** (150, 300, 450, 600 selon le palier). Sinon, il faudrait 36 pièces pour un seul palier, comme pour les clefs, soit des centaines de ressources. Avec ce champ, il faut **6 à 9 pièces par palier**, en comptant les potions et les clefs fabriquées en chemin.

### 5.5 Le craft
- **Recettes déterministes**, ce qui garde la ligne du projet (le serveur rejoue tout, rien n'est caché au joueur) :
  - **réussite à 100 %** : un échec ne ferait que punir le joueur qui a déjà payé ses ingrédients ;
  - **aucun jet aléatoire de qualité** : l'objet fabriqué a les chiffres de sa fiche. Des jets rendraient l'équilibre T41 impossible à garantir et pousseraient à fabriquer en boucle pour un « parfait ».
- **Recette type d'équipement :** 2 ressources récoltées du palier + 1 ressource de monstre de la zone. Exemples :

| Palier | Objet (niv.) | Métier | Ingrédients |
|---|---|---|---|
| 1 | Hache de chêne (3, 2 mains, Terre) | forgeron | 8 chêne, 4 cuivre, 3 défenses de sanglier |
| 1 | Plastron de lin (3, Vitalité) | tailleur | 6 lin sauvage, 2 cuivre, 3 peaux de loup |
| 10 | Bouclier de bois noir (10, Vit + Eau) | forgeron | 8 bois noir, 5 fer, 3 chapeaux de champignon |
| 20 | Épaulettes de laîche (18, Eau) | tailleur | 8 laîche, 4 étain, 4 bave |
| 30 | Plastron de paille tressée (26, Vitalité) | tailleur | 10 blé de Clairval, 4 argent, 4 fourrures de taupe |
| 30 | Lame de frêne (26, 1 main, Air) | forgeron | 10 frêne, 6 argent, 4 ailes d'abeille |
| 1 à 30 | Potion de soin (4 tailles) | alchimiste | 2 herbes du palier + 1 poisson du palier |

- **Les 20 pièces d'artisan**, soit 5 par palier :
  - au tailleur, le plastron (Vitalité) et les épaulettes ;
  - au forgeron, le bouclier, l'arme à une main et l'arme à deux mains.
  Les **5 pièces d'un palier couvrent les quatre éléments une fois chacun, plus la Vitalité** (le plastron) : toute construction trouve au moins une pièce de son élément à chaque palier. Elles respectent le **budget 8 + niveau**, sans initiative, sans PA ni PM, et sans panoplie.
- **Les potions (4 tailles) :** une nouvelle action de combat, `UseItem`. Elle coûte 2 PA, soigne 20 + 2 × niveau, **une fois par combat**, et retire la potion de l'inventaire (vérifié au rejeu). Elle est disponible en donjon comme dehors. C'était l'option S98 de la v1 : elle devient le cœur de l'alchimiste.
- **Les ateliers : un quartier des artisans.** Clairval fait 16 × 12 cases, et T44 le dit « petit à côté des autres ».
  - Une **nouvelle carte du village, « Clairval, quartier des artisans »**, en [1, 0] à l'est (case libre du monde), de 24 × 16 cases. On y entre par la porte est, dont la sortie d'entraînement de Garance est déplacée d'une case.
  - Elle porte **la forge, l'atelier du tailleur et l'alambic**, et accueille l'établi du serrurier, déplacé.
  - Un artisan PNJ par atelier, qui explique son métier et donne une quête d'initiation : récolter 5, fabriquer 1 (50 XP de métier).
  - `Town.Workshops` et le panneau de l'établi (T48) servent déjà pour n'importe quel métier : c'est surtout des données.
  - *Alternative :* les ateliers dans les zones (la forge au Bois moisi). Écartée : le craft est un moment social, au même endroit que Rose ; les zones gardent la récolte.

### 5.6 Quelles pièces tombent, lesquelles se fabriquent

| Emplacement | Source | Initiative possible |
|---|---|---|
| Coiffe, cape, bottes, anneaux, amulette, ceinture | **butin** (panoplies des familles) | oui sur cape, bottes, anneaux |
| Arme | butin (l'arme du chef, dans sa panoplie) **et** craft (forgeron) | non |
| Bouclier | craft (forgeron) ; le bouclier de carapace des crabes en butin | non |
| Plastron, épaulettes | **craft seulement** (tailleur) | non |
| Familier, monture | ni l'un ni l'autre pour l'instant (l'âne et le chaton existants restent) | non |
| Potions | craft (alchimiste) ; Rose en revend plus cher | non |

L'arme fabriquée et l'arme du chef ont le **même budget**. Celle du chef complète sa panoplie (bonus de 5 pièces), celle du forgeron s'obtient à coup sûr. C'est un vrai choix, sans arme dominante.

### 5.7 L'économie et l'équilibre
- **Rose (§4)** rachète les ressources récoltées à 1 à 4 Éclats et les revend à 5 fois ce prix.
  - Pour le joueur pressé, acheter le bois d'une hache coûte environ 40 Éclats au palier 1 et environ 300 au palier 30 : un puits utile.
  - Pour le récolteur, revendre rapporte environ 3 Éclats par action, **bien moins qu'un combat** (28 Éclats par monstre au niveau 26). La récolte n'est donc pas une ferme à Éclats.
- **Revendre une pièce fabriquée** (2 × niveau, 52 au niveau 26) rapporte moins que ses ingrédients au prix de Rose. Il n'y a **pas de boucle infinie** acheter → fabriquer → revendre.
- **T41 :** les planchers restent mesurés **nus, sans potion**. Les plafonds sont mesurés avec le **kit « fast » + les 5 pièces d'artisan du palier de la zone + une potion**. Si un plafond saute, on baisse d'abord les chiffres des pièces d'artisan : elles sont garanties, donc elles pèsent plus sur l'équilibre que le butin.
- **Initiative :** inchangée par le craft (§3.2). Aucune pièce d'artisan et aucune potion n'en donne.

### 5.8 Dépendances avec le jeu à plusieurs
- Les points partagés n'ont pas besoin du chantier 4. Le serveur garde leur état. Le client le lit en entrant sur une carte, et un refus « déjà récolté » le met à jour. Quand le chantier 4 diffuse les présences (S102), il diffuse aussi « point récolté » et « point repoussé ».
- **L'échange entre joueurs** (deux joueurs sur la même carte, les deux valident) entre dans le chantier 4, au sprint du groupe (S103, qui passe à 5 points). C'est ce qui donne un sens économique aux métiers à plusieurs.

### 5.9 Découpage et tests

| Sprint | Pts | Contenu |
|---|---|---|
| S98 | 5 | **La récolte.** `Job` gagne `Kind` (récolte ou fabrication) et `Harvests` (ressource, palier, nature du point : arbre, roche, eau, parcelle, herbe) ; champ `nodes` des cartes, écrit par `world_gen.py` ; table serveur des points (carte, case, prêt à) et `POST …/harvest` ; client : points dessinés (les parcelles en carrés de culture), clic, marche, barre de 3 s avec l'outil en main, « +2 Chêne » flottant ; **onglet Métiers** de la barre latérale (niveaux, XP, ce que chaque palier débloque) ; les 20 ressources et les 5 métiers de récolte (bûcheron, mineur, pêcheur, paysan, herboriste). |
| S99 | 3 | **Le quartier des artisans** : la carte [1, 0], la porte est, les 4 ateliers et leurs artisans, les 3 quêtes d'initiation ; l'établi du serrurier déplacé. |
| S100 | 3 | **Forgeron et tailleur** : les 20 pièces d'artisan et leurs recettes, `Recipe.Xp`, `ItemBudgetTests` étendu ; T41 remesuré (kit « fast » + artisan). |
| S101 | 3 | **L'alchimiste et la potion en combat** (`UseItem`, une par combat, rejouée par le serveur ; l'IA ne s'en sert pas) ; Rose revend les potions ; T41 remesuré avec potion. |

**Tests :**
- **Unitaires :**
  - quantité récoltée selon l'écart de niveau ;
  - repousse ;
  - refus : palier trop haut, point pas prêt, trop loin, trop tôt ;
  - chaque point posé sur le bon décor, voisin d'une case atteignable, à distance des passages, sans couper la carte ;
  - chaque recette d'équipement dans le budget, sans initiative ;
  - les 5 pièces d'un palier couvrent les quatre éléments ;
  - `UseItem` une seule fois, potion retirée, combat rejoué à l'identique.
- **Intégration :** récolter par l'API, le point indisponible pour un second personnage, l'XP de métier gardée ; fabriquer une hache de bout en bout (récolte, butin, forge, équiper) ; acheter une potion à Rose et l'utiliser dans un combat rejoué par le serveur.
- **Auto-test Godot :** une récolte sur la carte du milieu du Bois, une fabrication au quartier.

### 5.10 Risques
- **La corvée.** 30 à 40 récoltes par palier, c'est la limite. Au playtest, la question sera : « La récolte en passant vous a-t-elle semblé un plaisir ou un détour ? » Si c'est un détour, on monte la quantité, pas le nombre de points.
- **Les bots.** Une seule récolte par 3 secondes, la vérification de la place, et pas de récolte hors ligne. Une vraie modération viendra avec le jeu à plusieurs.
- **Le volume de données.** 20 ressources, 20 pièces, 4 potions, environ 850 points : tout est écrit par les outils (`world_gen.py`, `dungeon_gen.py` ou un `craft_gen.py`), jamais à la main.

---

## 6. Chantier 4 : jouer ensemble (S102 à S105, 18 points)

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
| S102 | 3 | **Se voir.** Le serveur diffuse aux joueurs d'une carte l'arrivée, la case visée et le départ de chacun, ainsi que l'état des points de récolte. **Les groupes de monstres deviennent communs** : leur errance se calcule à partir de l'horloge, et « battu » et « en combat » passent côté serveur. |
| S103 | 5 | **Le groupe et l'échange.** Inviter au groupe (4 au plus), un chef de groupe, le canal de groupe du chat, le bandeau du groupe ; **l'échange entre deux joueurs** de la même carte (objets et Éclats, chacun valide, le serveur fait tout ou rien). |
| S104 | 5 | **Le combat à plusieurs : règles et serveur.** `Fight` prend une liste de héros ; `FightRecord` passe à une nouvelle version ; XP, Éclats, butin (tiré pour chacun) et quêtes par héros ; potions par héros ; le hub de combat, le tour de 30 s, la reconnexion ; le simulateur fait jouer plusieurs héros par l'IA. |
| S105 | 5 | **Le combat à plusieurs : client et donjons.** « Rejoindre » pendant 15 s ; la frise et la fin de combat à plusieurs lignes ; les donjons à plusieurs (chacun use sa clef) ; l'équilibre à plusieurs (§6.4). |

### 6.4 Équilibre à plusieurs (validé par Charles)
Mesuré avec des héros joués par l'IA, en compositions de classes mélangées, au niveau de la zone, avec le kit « zone » :
- hordes : **2 joueurs ≈ 50 %, 3 joueurs ≥ 80 %** ;
- chefs et salles 3 : **3 joueurs ≥ 70 %, 4 joueurs ≥ 90 %**.

On règle les PV du chef seul. **XP :** la règle actuelle reste (XP entière pour chacun, +10 % par allié).

### 6.5 Risques
C'est le chantier le plus risqué : il touche le moteur, le serveur (un état en mémoire, un seul processus), le client et l'auto-test. Il faut **un test d'intégration à deux clients WebSocket** dès S104, et accepter qu'un sprint glisse.

---

## 7. Chantier 5 : la fin du chapitre 1 (S106 à S108, 9 points)

- **S106 (3) :** l'objectif `Interact` (un objet posé sur une carte, `Town.Props`) ; la chaîne d'Ysaline après « Quatre pistes », en **option A** de la trame (chapitre 1 = niveaux 1 à 30) : « Ce qui pousse entre » (10), « La crypte que personne n'a creusée » (12), « Crypte oubliée » (14, `Room: 2` puis `Interact` sur le sceau).
- **S107 (3) :** le Lac et les Champs, de « La berceuse que personne n'a apprise » (18) à « Sous les haies » (28). **Liane** est la seule nouvelle PNJ.
- **S108 (3) :** le **Disjoint** (`design-familles.md` §4.9), **gagnable seul au niveau 30 (50 à 70 %)**, « groupe conseillé » ; puis « Quatre routes » (30). Tant que le chantier 6 n'existe pas, le chapitre se termine sur une promesse, sans contenu fantôme.
- **Écriture :** agent `scenariste`, puis relecture de Charles. **Récompenses :** environ 100 × niveau en XP, pas d'équipement (le butin et l'artisanat restent ses sources). Une quête de la chaîne peut demander une ressource récoltée (« trois joncs pour Liane ») : c'est un clin d'œil aux métiers, jamais une porte (Rose la vend).

**Tests :** chaque quête de la chaîne se prend à son niveau et se finit seul (objectifs de donjon ≤ salle 2) ; `Interact` refuse un objet d'une autre carte ; le Disjoint perd 15 PV par tour et explose ; intégration : toute la chaîne de 1 à 30 jouée par l'API.

---

## 8. Chantier 6 et ce qui reste hors classement

### 8.1 Le monde après 30 (conditionnel)
`design-familles.md` §7 reste la référence. Proposition minimale quand Charles le décidera : Quatre-Chemins et **une seule région** de deux zones, avec une seule mécanique nouvelle, soit environ 6 sprints. Les métiers y gagnent les paliers 40 et 50, en données. Il faut d'abord l'initiative des monstres qui monte avec leur niveau (§3.5) et l'équilibre de haut niveau (T29).

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
| 15 | Points de récolte partagés ou par joueur | **Partagés**, repousse de 2 à 5 minutes selon le palier |
| 16 | Combien de points | **6 à 10 par carte de zone**, aucun au village |
| 17 | Comment on récolte | **Clic, marche jusqu'au point, 3 secondes**, l'outil en main sans être exigé ; 1 à 3 ressources selon l'avance du métier |
| 18 | Paliers | **1, 10, 20, 30** (ceux du serrurier), une ressource par métier et par zone ; le contenu de Clairval s'arrête au métier 30 |
| 19 | Réussite et qualité | **100 % de réussite, pas de jets aléatoires** : l'objet a les chiffres de sa fiche |
| 20 | Ce qui se fabrique | **Plastron et épaulettes (craft seulement), boucliers et armes (craft et butin), potions.** Jamais d'initiative, de PA ou de PM, ni de panoplie sur une pièce fabriquée |
| 21 | Où sont les ateliers | **Un nouveau quartier des artisans à l'est de Clairval**, avec forge, tailleur, alambic et l'établi du serrurier déplacé |
| 22 | L'échange entre joueurs | **Oui, avec le groupe (S103)** |

---

## 10. Planification proposée (si Charles dit « Go » sur la v2)

Une nouvelle décision, **D71**, couvre les sprints **95 à 108** : **52 points**. Il y a 36 points pour les chantiers de la v1, 14 pour les métiers (dont les 3 de la potion, autrefois en option) et 2 pour l'échange entre joueurs.
- Le projet passe de **381 à 433 points**, et rpg de **160 à 212**.
- Il faut mettre à jour `PLAN.md`, `data/scrum.json`, les fichiers `scrum/sprint-NN.md`, ainsi que R2, R8 et `scopeDecisions` (règle « ajout = gestion de projet à jour »).
- Les entrées de `DECISIONS.md` commencent à **T63**.

| Sprint | Pts | Chantier | Contenu |
|---|---|---|---|
| S95 | 5 | 0 et 1 | Correctif de la Bauge ; moteur de l'initiative ; panoplies du Bois et du Bois moisi ; `--kit` |
| S96 | 3 | 1 | Panoplies du Lac et des Champs ; T41 remesuré ; **playtest** |
| S97 | 3 | 2 | Rose marchande, puits d'Éclats |
| S98 | 5 | 3 | La récolte : 5 métiers (bûcheron, mineur, pêcheur, paysan, herboriste), 20 ressources, les points des cartes, l'onglet Métiers |
| S99 | 3 | 3 | Le quartier des artisans, les ateliers et les quêtes d'initiation |
| S100 | 3 | 3 | Forgeron et tailleur : 20 pièces d'artisan ; T41 remesuré |
| S101 | 3 | 3 | Alchimiste et potion en combat ; T41 avec potion ; **playtest** |
| S102 | 3 | 4 | Se voir sur la carte, des monstres et des points communs |
| S103 | 5 | 4 | Le groupe, son canal de chat, l'échange entre joueurs |
| S104 | 5 | 4 | Le combat à plusieurs : règles, serveur, simulation |
| S105 | 5 | 4 | Le combat à plusieurs : client, donjons, équilibre |
| S106 | 3 | 5 | Objectif `Interact`, chaîne du Bois moisi |
| S107 | 3 | 5 | Chaîne du Lac et des Champs |
| S108 | 3 | 5 | Le Disjoint, « Quatre routes » |
