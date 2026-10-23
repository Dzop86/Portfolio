# Osmose : classes, sorts, arbre des talents et spécialisations *(proposition v2, 24 octobre 2026)*

> **Demande de Charles** (après lecture de la v1, `docs/talents-specialisations.md`) : « pour l'arbre des talents, je veux un truc par exemple où tu peux choisir de nouveaux éléments (foudre, glace, lumière, ténèbres) ou que tu puisses ajouter des effets à des sorts (par exemple si tu as un talent débloqué, tu tapes un monstre eau puis terre, il devient "enlisé" et perd des pm pour x tour). Des talents qui donnent l'occasion de jouer multi élément par exemple. Ou des talents qui disent "tu perds 2PO sur tes sorts mais tu gagnes 10% dégâts de 0 à 3 cases", ou des talents qui disent "tu perds tes sorts feu et air mais tu gagnes 8 sorts foudre à la place" ou des combinaisons de ce genre. Un arbre de talents similaire à toutes les classes, qui influence les mécaniques. Par contre une sentinelle peut par exemple avoir accès à l'élément ténèbres comme le mage mais ils auront leurs sorts de classes ofc. »
>
> **Complément de Charles** : « je veux les classes, les sorts de classes, l'arbre des talents (communs aux classes) ainsi que les spécialisations (mage devient sorcier ou archimage ou nécromencien par exemple, au niveau 50, puis autre chose au niveau 100 etc.) ».
>
> **Ce document remplace la recommandation de la v1** (une voie à 30, puis une rangée de talents tous les 15 niveaux). Il reprend de la v1 ce qui tient encore : l'état des lieux (§1 de la v1), la règle « toute option se joue seule », le garde-fou chiffré contre le talent dominant, la migration sans perte et Plouf.
>
> **C'est une proposition de game design, pas un plan validé.** Rien n'est implémenté. Les noms sont des **noms de travail** ; leur sens dans le monde et leurs textes reviennent au scénariste (`docs/talents-canon.md`), qui devra revoir plusieurs de ses principes (§9).
>
> Lu avant d'écrire : `docs/talents-specialisations.md`, `docs/talents-canon.md`, `docs/bible.md` (éléments, Primordiaux, Accord), `data/classes.json`, `tools/spells_gen.py` (sorts des trois classes, règle de budget, passifs), `src/Rpg.Core/Effects.cs` (éléments, statuts, glyphes, passifs), `Data.cs` (`Characteristics`, `FighterSpec`), `Fight.cs` (`Damage`, `Resistance`), `Ai.cs` (`BestCast`).

---

## Sommaire

1. Les quatre couches, et ce qui va dans chacune
2. Principes de design, et ce qui est fun ou risqué
3. Couches 1 et 2 : les classes et leurs sorts
4. Couche 3 : l'arbre des talents commun
5. Les éléments composés (Foudre, Glace, Lumière, Ténèbres)
6. Les réactions élémentaires
7. Couche 4 : les spécialisations (50, 75, 100, et après)
8. Ce que ça change dans le moteur et le client
9. Le canon : conflits à trancher par le scénariste
10. Chiffrage, tranche verticale et place dans le plan
11. Questions à Charles

---

## 1. Les quatre couches, et ce qui va dans chacune

| Couche | Ce que c'est | Quand | Ce qu'elle change | Ce qu'elle ne touche **jamais** |
|---|---|---|---|---|
| **1. La classe** | Sentinelle, Garde, Mage (inchangées) | création | PV, sorts de base, rôle de départ | |
| **2. Les sorts de classe** | les 24 sorts de chaque classe, **plus** ses sorts dans chaque élément composé (générés), **plus** les sorts de sa spécialisation | niveaux 1 à 95 ; éléments composés ouverts par l'arbre ; sorts de spécialisation à 50, 75, 100 | ce que le héros peut lancer | |
| **3. L'arbre des talents** | **le même arbre pour les trois classes**, à points | dès **30** | les **éléments** (composés, conversions), les **réactions** entre éléments, les **règles** (portée, distance, PA, deck) | aucun talent ne nomme une classe ni un sort précis : il parle d'éléments, de portées, de coûts |
| **4. La spécialisation** | une branche **de la classe**, choisie parmi 3 | **50**, approfondie à **75**, seconde étape à **100** | le **rôle** : invocations, soins, boucliers, poussées, pièges, économie de PA, passifs de héros | **aucun élément** : une spécialisation ne donne ni n'enlève d'élément, ne touche pas aux réactions |

**La règle de séparation, en une phrase : l'arbre décide « avec quoi je frappe et selon quelles règles », la spécialisation décide « quel est mon métier dans le combat ».** C'est ce qui permet aux deux de se combiner sans se multiplier : un Archimage Foudre et un Archimage Ténèbres ont le même métier (zones, grands sorts) avec des matières différentes ; une Sentinelle Foudre et un Mage Foudre partagent des règles d'élément mais pas un sort.

---

## 2. Principes de design, et ce qui est fun ou risqué

### 2.1 Ce que Charles demande, en principes

1. **Des talents qui changent des règles, pas des chiffres.** Un talent se remarque en combat : un monstre qui s'enlise, un sort qui perd 2 PO, huit sorts qui changent d'élément. Pas de « +3 % ».
2. **Le multi-élément devient payant.** Aujourd'hui il est puni : chaque tranche de 10 points dans un élément ajoute 2 dégâts **à cet élément seul** (D61). À 45, un héros mono-élément ajoute jusqu'à ~88 dégâts à chaque coup ; réparti sur deux éléments, ~44 dans chacun. L'arbre doit donner au joueur hybride quelque chose que le mono-élément n'a pas : les **réactions** (§6) et les **éléments composés** (§5).
3. **Une même grammaire pour toutes les classes**, des sorts propres à chacune. La Sentinelle Ténèbres tire des flèches de Ténèbres, le Mage Ténèbres lance des sorts de Mage de Ténèbres.
4. **Chaque gain a un prix.** « −2 PO, +10 % de 0 à 3 cases » ; « tu perds Feu et Air, tu gagnes Foudre ». Les contreparties sont la règle, pas l'exception.
5. **On ne peut pas tout prendre.** Des points rares, des exclusions mutuelles, des conversions qui retirent quelque chose.
6. **Le groupe compte.** Une empreinte posée par un allié peut être complétée par un autre : la coordination à plusieurs devient un plaisir propre au MMORPG.

### 2.2 Ce qui est fun

- **Le combo à deux temps** (Eau, puis Terre : « Enlisé ! ») est la meilleure idée du lot : une décision par tour (« je prépare ou je déclenche ? »), un retour visible, et une vraie raison de construire un deck de deux ou trois éléments. C'est le moment mémorable.
- **La conversion** (« je deviens une Sentinelle de la Foudre ») est la bascule d'identité qui manquait à la v1, et elle coûte quelque chose de clair.
- **Les échanges de règles** créent des builds absurdes et drôles (une Sentinelle au contact) : de l'expression personnelle, du bouche-à-oreille.
- **À plusieurs**, la Garde qui pose l'Eau et le Mage qui déclenche la Terre : un jeu d'équipe qu'aucun bonus en pourcentage ne donne.

### 2.3 Ce qui est risqué, franchement

| Risque | Pourquoi | Parade proposée |
|---|---|---|
| **L'explosion du contenu** | 4 éléments composés × 3 classes × 8 sorts = **96 sorts**, 192 noms (fr, en), 96 icônes, plus 9 spécialisations et leurs sorts | les sorts composés sont **transposés** des sorts existants par `spells_gen.py` (même forme, nouvel élément, signature de l'élément, dégâts au budget) : le coût est surtout dans les noms. **Un élément à la fois**, Foudre d'abord ; les trois autres viennent avec les chapitres |
| **L'explosion combinatoire** | 3 classes × 3 spécialisations × des milliers de chemins dans l'arbre : `Progression.Built` ne peut pas tout parcourir | **builds de référence** (2 à 3 par classe) mesurés en CI ; le reste hors CI ; un plafond global des bonus conditionnels (§4.4) |
| **La lisibilité** | une empreinte, 6 états de réaction, 4 couleurs d'élément de plus, des sorts modifiés | **une seule empreinte** par combattant (une pastille), des états nommés et icônés, la réaction annoncée dans la prévision **avant** de lancer le sort |
| **L'IA** | le héros simulé doit jouer les combos pour que les mesures veuillent dire quelque chose ; les monstres doivent survivre à des héros qui les enlisent | l'IA est gourmande à un coup : on ajoute à `BestCast` la valeur d'une réaction déclenchée et une petite valeur pour une empreinte posée. Elle jouera des combos simples, pas des plans sur trois tours : c'est assez pour mesurer. Les monstres ne déclenchent **pas** de réactions dans la v2 (§6.4) |
| **L'équilibre T41** | une réaction est un statut **gratuit** ajouté à un coup : c'est de la puissance pure si on ne la paie pas. Le pire cas : un Mage qui enlise toute une horde d'une seule zone | une réaction ne touche que la **cible visée** (la case centrale), pas toute la zone ; l'empreinte se consomme ; un talent par paire ; la cible de la v1 tient : **l'arbre à 45 vaut au plus +10 %** de victoires contre les petits groupes, aucune horde ne passe son plafond |
| **Le rejeu serveur** | l'empreinte et les réactions sont de l'état de combat | elles sont déterministes (aucun tirage) ; les talents et la spécialisation voyagent avec le combat comme le deck (T35) ; le serveur refuse un combat joué avec d'autres talents |
| **Le contenu qui manque** | le jeu s'arrête à 45 ; les spécialisations arrivent à 50 | l'arbre (dès 30) est la partie jouable tout de suite ; les spécialisations arrivent **avec les Mines** (45 à 60). Seul Plouf (niveau 100) pourra les essayer avant |
| **Le canon** | la bible lie les quatre éléments aux quatre Primordiaux, et « le mélange » est le thème du Disjoint ; le scénariste a posé « aucune voie ne porte un élément » | §9 : une lecture qui retourne le conflit en force, à trancher par le scénariste |

---

## 3. Couches 1 et 2 : les classes et leurs sorts

**Les classes ne changent pas** : Sentinelle (distance, poussées, entraves), Garde (contact, attirance, boucliers), Mage (zones, soins, invocation). Le point relevé par le scénariste reste ouvert : la Mage est dite « fragile » mais a le plus de PV (86 contre 74 et 62).

**Les sorts de classe gagnent deux étages :**

| Étage | Contenu | D'où viennent les chiffres |
|---|---|---|
| Les 24 sorts actuels | 4 par élément de base, 8 neutres | inchangés (règle de budget) |
| **Les sorts composés** | pour chaque classe, **8 sorts par élément composé**, transposés de ses 8 sorts des deux éléments parents. Exemple, Sentinelle Foudre (parents Feu et Air) : Flèche ardente, Salve ardente, Tir brûlant, Pluie de braises, Tir de recul, Flèche perçante, Rafale, Chute céleste → 8 sorts de Foudre de même portée, même coût, même zone | `spells_gen.py` : la forme du sort parent, l'élément composé, sa **signature** (§5.3), les dégâts recalculés au budget (la signature coûte comme un effet) |
| **Les sorts de spécialisation** | 3 à 50, 1 à 75, 1 à 100 (§7) | règle de budget ; les effets nouveaux réglés à la main |

**Le deck reste à 12.** Toute la v2 repose sur le choix du deck : ouvrir un élément ajoute des sorts au grimoire, pas des places.

---

## 4. Couche 3 : l'arbre des talents commun

### 4.1 La forme

- **Quatre branches**, les mêmes pour les trois classes :
  - **Tresses** (éléments composés : ouvrir, convertir) ;
  - **Réactions** (combos entre éléments) ;
  - **Distance** (portée, position, déplacement) ;
  - **Rythme** (PA, coûts, relances, deck).
- **Trois étages par branche.** L'étage 1 est ouvert ; l'étage 2 demande **2 points dépensés dans la branche** ; l'étage 3 (les **clés**) en demande **3**.
- **Les points** : **2 au niveau 30**, puis **1 tous les 5 niveaux** : 3 à 35, 4 à 40, **5 à 45**, 6 à 50… **16 à 100**. Une clé est donc accessible dès 45 (3 points dans la branche + 2 pour la clé).
- **Les coûts** : un talent ordinaire coûte **1 point**, une clé **2 points**.
- **Les exclusions** (on ne peut pas tout prendre) :
  - **une seule clé par branche** ;
  - **une seule conversion** dans tout l'arbre ;
  - **deux éléments composés au plus** (ouvertures et conversion comprises) ;
  - **Corps à corps** et **Artillerie** s'excluent (et bloquent leurs contraires de l'étage 1) ;
  - au niveau 100, 16 points pour ~36 talents : un héros en a une douzaine, moins de la moitié de l'arbre.
- **Rien avant 30** : le chapitre 1 et `BalanceTests` ne bougent pas.

*Alternative écartée :* les rangées à choix de la v1 (« 1 talent sur 3 tous les 15 niveaux »). Plus faciles à équilibrer, mais elles ne permettent pas ce que Charles veut : investir dans une direction (trois talents de réactions) ou combiner deux branches.

### 4.2 Le changement de talents et son prix

- **Gratuit jusqu'au niveau 40** : c'est la période où l'on découvre les combos ; essayer fait partie du plaisir.
- **Ensuite, 10 × niveau Éclats** pour tout refaire (500 à 50, 1 000 à 100), le prix des caractéristiques : un prix connu, une seule règle.
- **Dans un village** (Clairval ou Quatre-Chemins), hors combat.
- Une conversion retirée rend les sorts de base ; les sorts perdus **rendent leurs points de sort** (règle d'un sort disparu, `RankList`, T34).

### 4.3 Les talents, chiffrés

Notation : **E1** = étage 1, **E2** = étage 2, **Clé** = étage 3. « Existe » = le moteur sait déjà le faire ; « nouveau » = pièce de moteur à écrire (§8).

#### Branche Tresses (éléments composés, §5)

| # | Talent (nom de travail) | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| T1 | **Ouverture : Foudre** | E1 | 1 | ajoute au grimoire les **4 premiers** sorts de Foudre de ta classe (transposés de ses 2 premiers sorts de Feu et 2 premiers d'Air) | une place de deck à trouver ; compte dans la limite de 2 éléments composés | nouveau (élément) |
| T2 | **Ouverture : Glace** | E1 | 1 | idem, Eau et Air | idem | nouveau |
| T3 | **Ouverture : Lumière** | E1 | 1 | idem, Terre et Feu | idem | nouveau |
| T4 | **Ouverture : Ténèbres** | E1 | 1 | idem, Terre et Eau | idem | nouveau |
| T5 | **Plein grimoire** | E2 | 1 | pour chaque élément ouvert, les **4 autres** sorts composés de ta classe (8 en tout) | aucune, sinon des points | existe (liste de sorts) |
| T6 | **Signature appuyée** | E2 | 1 | la signature de tes sorts composés est **+50 %** (l'arc de Foudre passe de 35 à 50 % des dégâts, §5.3) | tes sorts composés font **−10 %** de dégâts directs | nouveau (paramètre) |
| T7 | **Prisme** | E2 | 1 | tes coups composés **posent l'empreinte** de leur premier parent (Foudre pose Feu, Glace pose Eau…) : ils peuvent préparer une réaction | ils ne la déclenchent toujours pas | nouveau |
| T8 | **Conversion** *(une parmi quatre : Foudre, Glace, Lumière, Ténèbres)* | Clé | 2 | tu **perds tes 8 sorts des deux parents**, tu gagnes **les 8 sorts composés**, à **+15 %** de dégâts (budget) et signature +50 % | tu n'as plus que **3 éléments** : contre un monstre qui résiste à ton composé, tu as moins de recours ; les réactions des parents perdus deviennent inutiles | existe (substitution de sorts) |
| T9 | **Pureté** | Clé | 2 | tant que ton deck ne contient que des sorts d'**un seul** élément (neutres permis) : +20 % de dégâts | **aucune réaction** possible ; incompatible avec une conversion | nouveau (condition sur le deck) |

*Exemple, la conversion de Charles :* une Sentinelle prend « Conversion : Foudre ». Ses 8 sorts de Feu et d'Air disparaissent du grimoire ; elle a 8 sorts de Foudre de mêmes formes, qui comptent ses points de Feu **et** d'Air (§5.2). Elle garde ses 4 sorts de Terre et ses 4 d'Eau.

*T9 Pureté* est là exprès : le joueur qui aime un seul élément doit avoir sa voie dans l'arbre, sinon l'arbre ne parle qu'aux hybrides.

#### Branche Réactions (§6)

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| R1 | **Enlisement** (Terre + Eau) | E1 | 1 | tu peux déclencher **Enlisé** | demande deux éléments dans le deck | nouveau (réactions) |
| R2 | **Transissement** (Eau + Air) | E1 | 1 | tu peux déclencher **Transi** | idem | nouveau |
| R3 | **Fêlure** (Terre + Feu) | E1 | 1 | tu peux déclencher **Fêlé** | idem | nouveau |
| R4 | **Attisement** (Feu + Air) | E1 | 1 | tu peux déclencher **Attisé** | idem | nouveau |
| R5 | **Buée** (Feu + Eau) | E1 | 1 | tu peux déclencher **Embué** | idem | nouveau |
| R6 | **Poudroiement** (Terre + Air) | E1 | 1 | tu peux déclencher **Aveuglé** | idem | nouveau (`Stat.Range`) |
| R7 | **Empreinte tenace** | E2 | 1 | tes empreintes durent **un tour de plus** | | nouveau (paramètre) |
| R8 | **Catalyse** | E2 | 1 | tes réactions infligent en plus **20 % des dégâts du coup qui les déclenche**, en neutre | **une réaction au plus par tour** (la tienne) | nouveau |
| R9 | **Réaction prolongée** | E2 | 1 | les états que tu déclenches durent **+1 tour** | tes coups qui déclenchent une réaction font **−10 %** | nouveau (paramètre) |
| R10 | **Onde** | Clé | 2 | tes sorts de zone déclenchent la réaction sur **2 cibles marquées de plus** dans la zone | tes sorts de zone font **−15 %** | nouveau |
| R11 | **Empreinte partagée** | Clé | 2 | une empreinte **que tu poses** porte tes talents : un allié qui la complète déclenche la réaction **même sans le talent** | tu ne bénéficies plus de Catalyse | nouveau |
| R12 | **Virtuose** | Clé | 2 | quand tu complètes **ta propre** empreinte, le coup qui déclenche fait **+20 %** | les empreintes de tes alliés **ne réagissent plus** pour toi | nouveau |

Les trois clés disent trois façons de jouer : la zone (Onde), le groupe (Empreinte partagée), le duel en solo (Virtuose).

#### Branche Distance

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| D1 | **Au plus près** *(l'exemple de Charles)* | E1 | 1 | **+10 % de dégâts** sur une cible **à 0 à 3 cases** | **−2 PO** à tous tes sorts (jamais sous leur portée minimale, ni sous 1) | existe (portée), nouveau (bonus selon la distance) |
| D2 | **Longue vue** | E1 | 1 | **+2 PO** à tes sorts de portée maximale 4 ou plus | **−10 %** de dégâts sur une cible à 1 ou 2 cases | idem ; exclu avec D1 |
| D3 | **Pied ferme** | E1 | 1 | **+15 %** de dégâts si tu n'as pas dépensé de PM ce tour | **−5 %** sinon | nouveau (PM dépensés) |
| D4 | **Zone serrée** | E2 | 1 | tes zones **rétrécissent d'un cran** (Cercle 2 → Cercle 1 ; Croix 2 → Croix 1 ; Cercle 1 et Croix 1 → une case), dégâts recalculés au budget du nouveau cran (≈ +15 à +40 %) | moins de cibles | existe (budget, à appliquer au chargement) |
| D5 | **Poussée lourde** | E2 | 1 | tes poussées font **+1 case** et le choc contre un obstacle fait **8 par case** au lieu de 4 | tes sorts qui poussent coûtent **+1 PA** | existe |
| D6 | **Pas dansant** | E2 | 1 | chaque case parcourue à pied ce tour donne **+4 %** au prochain sort (au plus +16 %), remis à zéro après le sort | | nouveau |
| D7 | **Corps à corps** | Clé | 2 | portée max de tous tes sorts **ramenée à 2** ; **+25 %** de dégâts | exclu avec D2 et D8 ; la Sentinelle au contact devient un vrai choix, pas une erreur | existe (portée) |
| D8 | **Artillerie** | Clé | 2 | portée **minimale 3** sur tous tes sorts de dégâts, **+2 PO**, **+15 %** | tu ne peux plus frapper à 1 ou 2 cases ; exclu avec D1 et D7 | existe (portée) |
| D9 | **Ligne claire** | Clé | 2 | tes sorts lancés **en ligne** ne demandent plus la vue | tes sorts **qui ne sont pas** en ligne : −1 PO | existe (`LineOfSight`, `InLine`) |

#### Branche Rythme

| # | Talent | Étage | Coût | Effet | Contrepartie | Moteur |
|---|---|---|---|---|---|---|
| Y1 | **Coups appuyés** | E1 | 1 | sorts de **4 PA ou plus** : +15 % | sorts de **3 PA ou moins** : −15 % | nouveau (bonus selon le coût) |
| Y2 | **Petits coups** | E1 | 1 | l'inverse de Y1 | | exclu avec Y1 |
| Y3 | **Premier geste** | E1 | 1 | le **premier** sort de dégâts de chaque tour : +20 % | les suivants : −5 % | nouveau |
| Y4 | **Relance courte** | E2 | 1 | tes sorts à relance : **−1 tour de relance** | leurs dégâts recalculés au budget (−25 % par tour retiré) | existe (budget) |
| Y5 | **Réserve** | E2 | 1 | les PA non dépensés (2 au plus) passent au tour suivant | ton premier tour de combat : −1 PA | nouveau |
| Y6 | **Deck serré** | E2 | 1 | **+1 PA** à chaque tour | ton deck passe de 12 à **10** places | existe (`DeckSize`, `Stat.Ap`) |
| Y7 | **Un geste par sort** | Clé | 2 | **+20 %** de dégâts | chaque sort se lance **une fois par tour** au plus | existe (`PerTurn`) |
| Y8 | **Économie** | Clé | 2 | tes sorts coûtent **−1 PA** (jamais sous 2) | ton deck passe à **8** places | existe |
| Y9 | **Souffle long** | Clé | 2 | **+1 PM et +1 PA** à partir de ton 4e tour | **−1 PA** à tes 3 premiers tours | nouveau (statut selon le tour) |

### 4.4 Les garde-fous de l'arbre

- **Plafond des bonus conditionnels** : la somme des bonus en pourcentage venus de l'arbre et de la spécialisation sur un même coup est plafonnée à **+35 %** (Au plus près + Corps à corps + Premier geste ne font pas +55 %). Les statuts de sorts (Concentration, Cri de guerre) restent hors plafond, comme aujourd'hui.
- **Le talent dominant** : un talent qui, dans le simulateur, donne **plus de 1,5 victoire sur 30** de plus que ses voisins d'étage contre les petits groupes, ou qui fait passer une horde au-dessus de son plafond, est retouché avant d'être livré (règle de la v1).
- **La fausse décision** : un talent pris par moins de 5 % des builds de référence générés par l'outil hors CI est revu (soit trop faible, soit trop étroit).

---

## 5. Les éléments composés (Foudre, Glace, Lumière, Ténèbres)

### 5.1 La proposition : des éléments à deux parents, en cycle

Un élément nouveau qui aurait **sa propre caractéristique** casserait tout : 10 points par niveau à répartir sur 9 caractéristiques au lieu de 5, des builds existants faux, une fiche illisible. Un élément qui **emprunte** la caractéristique d'un seul élément remplacé ne dit rien de plus que « Feu, en jaune ». La proposition : **chaque élément composé a deux parents**, et ces parents forment un cycle où chaque élément de base apparaît **deux fois** :

| Élément composé | Parents | Image | Justesse de l'image |
|---|---|---|---|
| **Foudre** | Feu + Air | le feu porté par le vent, l'orage | naturelle |
| **Glace** | Eau + Air | l'eau saisie par le vent froid | naturelle |
| **Ténèbres** | Terre + Eau | ce qui est enfoui et noyé, les profondeurs | correcte, à faire porter par le scénariste |
| **Lumière** | Terre + Feu | la lave et la forge qui éclairent, le métal chauffé à blanc | la plus faible : au scénariste de dire si elle tient (§9) |

Restent deux paires libres, **Feu + Eau** (vapeur, brume) et **Terre + Air** (sable, poussière) : on les garde pour les **réactions** (§6) ou pour des éléments plus tard.

### 5.2 Les règles dans le moteur

| Question | Règle proposée | Pourquoi |
|---|---|---|
| **Quelle caractéristique ?** | la **somme** des deux parents : Foudre = Feu + Air | un hybride Feu/Air (225 + 225) frappe en Foudre comme un mono-élément (450) frappe dans le sien : **c'est la récompense du multi-élément**. Jamais plus qu'un mono-élément : la somme de deux caractéristiques ne dépasse pas le total des points. Un mono-Feu qui ouvre la Foudre la frappe à pleine force aussi, ce qui est juste (il paie en places de deck et en points) |
| **Quelle résistance ?** | la **moyenne** des résistances aux deux parents, arrondie vers 0, **sauf si la famille en donne une** | aucune donnée à réécrire sur les 50 monstres ; un cerf faible au Feu (−15) et neutre à l'Air est faible à la Foudre (−7) |
| **Les faiblesses des familles** | une famille peut porter une résistance propre à un composé (`"Lightning": 30`) quand le thème le demande : les gardiens moussus de pierre conduisent mal la Foudre | les familles gardent leur « leçon » |
| **Les statuts d'élément** (bonus de dégâts, résistance) | un statut d'un parent ne vaut **pas** pour le composé ; un statut neutre vaut pour tous | une seule règle, lisible |
| **Les soins** | inchangés (le Feu porte le soin) | |
| **Les icônes et couleurs** | 4 couleurs d'élément de plus dans le client (Foudre jaune-violet, Glace cyan pâle, Lumière or blanc, Ténèbres indigo) ; les icônes viennent de game-icons.net, comme les autres sorts | |

### 5.3 La signature de chaque élément composé

Un composé doit se jouer autrement que ses parents, sinon il n'est qu'une couleur. Chaque sort composé porte la signature de son élément, **payée par la règle de budget** comme n'importe quel effet :

| Élément | Signature | Coût au budget | Moteur |
|---|---|---|---|
| **Foudre** | **Arc** : 35 % des dégâts du coup sautent sur l'ennemi le plus proche à 2 cases de la cible (un seul saut) | ×0,8 | nouveau (un effet « saut ») |
| **Glace** | **Morsure** : −1 PA pendant 1 tour | ×0,8 (comme un retrait de PA) | existe (`Stat.Ap`) |
| **Ténèbres** | **Ponction** : le lanceur reprend 20 % des dégâts infligés en PV | ×0,8 | nouveau (soin proportionnel) |
| **Lumière** | **Révélation** : −10 % de résistance à tous les éléments pendant 1 tour | ×0,85 | existe (`Stat.Resistance` neutre) |

**Doublons à régler :** la Mage a déjà « Éclair » (Air), la Sentinelle « Chute céleste » avec une icône d'éclair, et l'Eau a déjà ses sorts de givre (Flèche de givre, Lame de glace, Gel, Glacier). Il faudra soit renommer ces sorts de base (ce sont des noms de travail anciens), soit accepter que l'Air ait ses éclairs « secs » et la Foudre ses orages. Valeur par défaut : **renommer les quatre sorts de base qui portent le mot d'un composé**, au moment où ce composé arrive.

### 5.4 Combien de sorts, et dans quel ordre

- **8 sorts par classe et par composé** (le chiffre de Charles), tous transposés : **24 sorts par élément**, 96 pour les quatre.
- **Ordre proposé** : **Foudre** d'abord (l'image la plus naturelle, l'exemple de Charles, et la signature la plus spectaculaire), dans la tranche qui suit la tranche verticale ; puis **Glace** ; puis **Ténèbres** et **Lumière**, après la décision du scénariste. Un élément par chapitre de contenu, pas tous d'un coup.

---

## 6. Les réactions élémentaires

### 6.1 La règle

1. **L'empreinte.** Un **coup direct** d'un sort de Terre, Feu, Eau ou Air (pas un poison, pas une glyphe, pas un sort neutre, pas un composé sauf Prisme) laisse sur la cible **l'empreinte** de son élément. Un combattant a **une seule** empreinte : la dernière.
2. **Sa durée** : jusqu'à la fin du **prochain tour** de celui qui l'a posée. Le même héros peut donc poser et compléter dans le même tour (3 PA + 4 PA), ou laisser son allié compléter.
3. **Le déclenchement.** Un coup direct d'un **autre** élément de base sur une cible marquée déclenche la réaction de la paire **si le lanceur de ce coup a le talent** de la paire. **L'empreinte peut avoir été posée par n'importe quel combattant de son équipe**, invocations comprises (le Faucon de la Sentinelle et la braise du Mage posent leurs empreintes).
4. **La consommation.** La réaction **efface l'empreinte**, et le coup qui déclenche **n'en laisse pas** de nouvelle. Il faut donc deux coups pour chaque réaction : pas de chaîne infinie.
5. **Sans le talent**, le coup d'un autre élément **remplace** l'empreinte.
6. **Une zone** ne déclenche la réaction que sur la **cible visée** (la case centrale), sauf avec la clé Onde (§4.3). C'est le garde-fou des hordes.
7. **Le cumul** : un même état ne se cumule pas, il se **rafraîchit** (sa durée repart) ; des états différents coexistent.
8. **Les chefs** subissent les réactions (sinon le système disparaît au moment le plus intéressant), mais leurs pertes de PA et de PM sont plafonnées à −2 en tout.

**L'ordre (Eau puis Terre, ou Terre puis Eau) ne compte pas.** Charles a dit « eau puis terre » : des paires **ordonnées** feraient 12 réactions au lieu de 6, deux fois plus à apprendre pour un écart que personne ne retiendra. Valeur par défaut : non ordonnées (question à Charles).

### 6.2 La table

Tous les états s'écrivent avec des statuts **qui existent déjà** (`Poison`, `Ap`, `Mp`, `Damage`, `Resistance`), sauf Aveuglé qui demande un statut de portée (`Stat.Range`, aussi utile aux talents de Distance). Ce qui est nouveau, c'est le **nom** de l'état (un libellé et une icône sur le statut) et la règle de rafraîchissement.

| Paire | État (nom de travail) | Effet | Durée | Ce qu'il apprend au joueur |
|---|---|---|---|---|
| **Terre + Eau** | **Enlisé** *(l'exemple de Charles)* | **−2 PM** | 1 tour de la cible | fixer un monstre au contact ou hors de portée |
| **Eau + Air** | **Transi** | **−2 PA** | 1 tour | couper le gros coup d'un chef |
| **Terre + Feu** | **Fêlé** | **−15 % de résistance** à tous les éléments | 2 tours | préparer la frappe de toute l'équipe |
| **Feu + Air** | **Attisé** | brûlure : au début de ses 2 tours suivants, **25 % des dégâts** du coup qui l'a déclenchée, en Feu | 2 tours | la valeur qui croît avec la puissance du héros (une brûlure fixe serait nulle à 45, où les coups dépassent 100) |
| **Feu + Eau** | **Embué** | **−25 % de dégâts** infligés | 1 tour | protéger un allié avant le tour du monstre |
| **Terre + Air** | **Aveuglé** | **−2 PO** à ses sorts (jamais sous 1) | 1 tour | contrer les tireurs |

**Repère de puissance :** Enlisé vaut le Pieu de la Sentinelle (−2 PM pendant 1 tour, que la règle de budget paie 20 % des dégâts). Une réaction tous les deux coups vaut donc environ **+10 % de dégâts « en effets »** pour un héros qui a payé 1 point et une place de deck dans un second élément. C'est dans la cible.

### 6.3 Le jeu à plusieurs

- **Qui déclenche** : celui qui complète, avec **son** talent (ou, avec Empreinte partagée, le talent de celui qui a posé).
- **Ce que ça crée** : un rôle de « poseur » et un rôle de « déclencheur » sans que personne ne les impose. La Garde pose l'Eau au contact, le Mage déclenche la Terre de loin.
- **L'ordre d'initiative** devient une information utile : qui joue entre mon empreinte et sa fin ? Le client l'affiche déjà (la frise des tours).
- `PartyBalanceTests` passe sur une composition « deux héros qui se complètent » en plus des compositions par classe.

### 6.4 Et les monstres ?

**Pas dans la v2** : les monstres ne posent pas d'empreinte sur les héros et ne déclenchent rien. Plus tard, c'est une **leçon de famille** toute trouvée pour le chapitre 2 (design-familles) : une famille qui marque les héros et les enlise s'ils traînent dans ses deux éléments. Le moteur sera prêt.

---

## 7. Couche 4 : les spécialisations

### 7.1 Les paliers

| Niveau | Palier | Choix | Ce qu'il donne |
|---|---|---|---|
| **50** | **Spécialisation** | **1 parmi 3** pour sa classe | un **titre**, un **passif de spécialisation** (moteur des passifs des monstres, ouvert aux héros), **3 sorts de spécialisation**, **une règle de classe changée** |
| **75** | **Approfondissement** | aucun (automatique) | **1 sort de plus**, et le passif monte d'un cran |
| **100** | **Accomplissement** | **1 parmi 2** pour sa spécialisation | un **sort majeur** (une fois par combat ou relance longue) et une **seconde règle** changée |

- **Pourquoi 50 et pas 30** : c'est le souhait de Charles, et c'est mieux pour le jeu : à 30 le joueur reçoit l'arbre (les éléments, les combos) ; à 50 il choisit son métier. Deux moments forts au lieu d'un seul, séparés de 20 niveaux.
- **Pourquoi un palier automatique à 75** : sans lui, rien ne se passe pendant 50 niveaux côté spécialisation. Il ne coûte presque rien en équilibre (un sort et un chiffre) et ne multiplie pas les mesures.
- **Pourquoi 1 parmi 2 à 100 et pas 1 parmi 3** : 9 × 3 = 27 accomplissements, c'est trop pour un niveau que personne n'atteindra avant longtemps. 18 suffisent.
- **Combien en tout** : 9 spécialisations, 9 approfondissements, 18 accomplissements.

### 7.2 Et au-delà de 100 ?

Le niveau 100 est la fin de la Saga I. Trois pistes :

1. **Rien** dans la Saga I : l'accomplissement est le sommet. *(recommandé)*
2. **Un palier 150 dans la Saga II**, si elle relève le niveau maximum : une troisième étape de spécialisation (le « etc. » de Charles), cohérente avec le reste.
3. **Un prestige** (on redescend au niveau 1 contre un titre ou un bonus) : **déconseillé**. L'arbre, les spécialisations, le deck et les groupes reposent sur le niveau ; un prestige obligerait à tout repenser et pousse à la répétition sans contenu nouveau.

Valeur par défaut : **la piste 1 maintenant, la piste 2 si la Saga II monte le niveau**.

### 7.3 Les neuf spécialisations (noms de travail)

Les noms du Mage sont ceux de Charles. Pour la Sentinelle et la Garde, je reprends les noms de voies du scénariste (une chose ou un lieu, jamais un métier) et j'en ajoute une troisième par classe. Le scénariste devra accorder les deux styles de noms (§9).

| Classe | Spécialisation | Métier dans le combat | Passif (50) | Règle de classe changée | Ses 3 sorts (intention) |
|---|---|---|---|---|---|
| Sentinelle | **Mire** | le tir long et précis | au début de son tour, si aucun ennemi à 3 cases : **+15 % de dégâts** pendant 1 tour | ses sorts **en vue** gagnent **+1 PO** | un tir visé à relance 2 ; une semonce qui retire 1 PA en zone ; un « marquage » qui fait −10 % de résistance |
| Sentinelle | **Affût** | les pièges et le Faucon | son Faucon **joue deux fois** par combat de plus (il revient une fois s'il meurt) | ses **glyphes** (moteur de S130) durent **+1 tour** | un collet posé en glyphe ; un appel du Faucon qui attire une cible ; un piège d'élément au choix du lanceur |
| Sentinelle | **Sente** | la mobilité (l'« acrobate ») | frappée au contact : **+2 PM** pendant 1 tour | ses sauts (`leap`) ne demandent **pas la vue** | un bond à 2 PA ; un tir en reculant (tir puis saut d'une case) ; un échange de place avec un ennemi |
| Garde | **Palissade** | protéger le groupe | au début de son tour : **bouclier de 6** aux alliés à 1 case, elle comprise | ses boucliers sur les alliés **+25 %** | une interposition (échange de place avec un allié, bouclier) ; un ancrage (ne peut être déplacée, +20 % de résistance) ; une provocation de zone |
| Garde | **Brèche** | ouvrir la ligne, frapper fort | à chaque ennemi achevé : **+1 PM** pendant 1 tour (déclencheur `Kill`, nouveau) | ses attirances **+1 case** | une charge (saut au contact + dégâts) ; un coup qui traverse (ligne 2) ; un coup qui repousse et suit |
| Garde | **Étau** | tenir et contrôler | qui la frappe au contact **perd 1 PM** (comme `mud-hide`) | ses retraits de PM et PA **+1 tour** | une prise qui immobilise ; une attirance de zone ; un coup qui retire 1 PA |
| Mage | **Archimage** | les grands sorts et les zones | **+1 PA** au début de chaque tour où elle n'a pas bougé | ses sorts de **6 PA ou plus** : **−1 tour de relance** | un sort de zone en croix 3 ; un « afflux » qui donne 2 PA au prochain tour ; un sort qui grandit d'un cran par tour de relance attendu |
| Mage | **Sorcier** | les états et les malédictions | chaque **état** (réaction, retrait de PA ou PM, poison) qu'elle pose dure **+1 tour** | ses poisons se **cumulent** (deux fois au plus) | une malédiction (−15 % de dégâts infligés, 2 tours) ; un « transfert » qui déplace un état d'un ennemi sur un autre ; une brûlure lente |
| Mage | **Nécromancien** *(canon à trancher, §9)* | les invocations | à la mort d'un ennemi à 3 cases : une **ombre** de lui (sorts réduits, 2 tours) se lève à ses côtés, une fois par combat | elle peut avoir **2 invocations** au lieu d'une | appeler une ombre ; sacrifier une invocation (dégâts de zone) ; renforcer ses invocations |

**Le soin du Mage.** Les trois spécialisations de Charles couvrent les dégâts, le contrôle et l'invocation ; **aucune n'est le soigneur**, alors que le groupe en a besoin (le Baume du scénariste). Les soins actuels restent dans le kit de base de la Mage pour toutes. Question à Charles : faut-il une quatrième spécialisation (Baume) pour la Mage, ou remplacer l'une des trois ?

### 7.4 Les accomplissements à 100 (intentions, 2 par spécialisation)

| Spécialisation | Accomplissement A | Accomplissement B |
|---|---|---|
| Mire | un tir à travers tout le plateau, une fois par combat | chaque tir en vue retire 1 PM |
| Affût | le Faucon devient permanent et plus fort | trois glyphes à la fois |
| Sente | +2 PM permanents, −10 % de PV | après chaque saut, le prochain sort coûte −1 PA |
| Palissade | un rempart de zone, une fois par combat, qui absorbe pour tout le groupe | elle reçoit à la place d'un allié adjacent les coups qui le tueraient |
| Brèche | une charge qui traverse une ligne d'ennemis | ses coups au contact ignorent les boucliers |
| Étau | une prise de zone qui enlise tout à 1 case | un ennemi entravé par elle ne peut pas lancer de sort de déplacement |
| Archimage | un sort de 10 PA qui dépasse le budget, une fois par combat | ses zones gagnent un cran |
| Sorcier | ses états se propagent à un ennemi adjacent à la fin de leur durée | chaque état sur une cible : +5 % de dégâts reçus (4 au plus) |
| Nécromancien | trois invocations | ses ombres gardent les passifs de leur modèle |

### 7.5 Comment l'arbre et la spécialisation se combinent sans exploser

1. **Des axes séparés** (§1) : l'arbre touche les éléments, les réactions, la portée et le rythme ; la spécialisation touche le métier. Aucun talent ne cible une spécialisation, aucune spécialisation ne cible un élément.
2. **Un seul plafond de pourcentages** (+35 % sur un coup, §4.4) pour les deux couches ensemble.
3. **Les synergies sont voulues, mais nommées** : le Sorcier aime les Réactions (ses états durent plus longtemps), l'Archimage aime Rythme, la Sente aime Distance. On les **mesure** comme builds de référence au lieu de les découvrir en jeu.
4. **Les mesures** : 3 classes × 3 spécialisations × **2 builds de l'arbre** (un « réactions », un « élément composé ») = **18 builds de référence** à 50 ; en CI seulement aux niveaux où des monstres existent. Les autres combinaisons sont mesurées hors CI.
5. **Le deck de 12** est le frein naturel : les sorts de spécialisation, les composés et les sorts de base se disputent les mêmes places.

### 7.6 Le changement de spécialisation

- **La première fois, gratuit** ; ensuite **30 × niveau Éclats** (1 500 à 50, 3 000 à 100), chez le maître de la nouvelle spécialisation. Les sorts de l'ancienne quittent le deck et **rendent leurs points de sort**.
- Changer d'accomplissement à 100 : **gratuit dans un village** (deux options à budget égal).
- La quête de passage du scénariste (« Une route à soi ») passe de 30 à **50** ; ses maîtres et ses épreuves restent valables pour la Sentinelle et la Garde.

---

## 8. Ce que ça change dans le moteur et le client

| Pièce | Changement | S'appuie sur |
|---|---|---|
| **Éléments** | `Element` gagne `Lightning`, `Ice`, `Light`, `Dark` et une table des parents ; `Characteristics.For` rend la somme des parents ; `FighterSpec.Resistance` rend la moyenne des parents si la famille n'en donne pas ; la validation des résistances accepte les composés | `Effects.cs`, `Data.cs` |
| **Empreintes et réactions** | une empreinte par combattant (`Element`, poseur, fin) posée dans `Fight` à chaque coup direct ; `data/reactions.json` (paire → statuts, nom, icône) ; un événement `ReactionTriggered` comme `PassiveTriggered` ; un statut **nommé** (`Status` gagne un libellé) qui se rafraîchit au lieu de se cumuler | les statuts, la file des passifs, les événements |
| **Statuts nouveaux** | `Stat.Range` (Aveuglé, et des talents) ; un soin proportionnel (Ténèbres) ; un saut de dégâts (Foudre) | `StatusEffect`, `HealEffect` |
| **Talents** | `data/talents.json` écrit par un générateur Python (comme les sorts) ; `Hero.Talents` ; `Hero.Problem` vérifie points, étages, exclusions, niveau ≥ 30 | `Hero.Problem`, `DeckProblem` |
| **Talents « compilés »** | **la plupart des talents se traduisent avant le combat en sorts modifiés** (portée, zone, coût, relance, liste de sorts) : l'IA, la prévision, les fiches de sort et le rejeu marchent tels quels | la règle de budget, `Spell` (un `record`, modifié par `with`) |
| **Talents « vivants »** | les bonus selon la distance, les PM dépensés, le premier sort : une petite liste de règles sur le `Fighter`, lue par `Fight.Damage` | `Fight.Damage`, `DamageBonusFor` |
| **Spécialisations** | `data/specs.json` ; `Hero.Spec`, `Hero.Feat` (à 100) ; les passifs des héros (`FighterSpec.Passives`, déjà résolus pour tout combattant) ; le déclencheur `Kill` | `Fight.PassivesOf`, la limite de 3 passifs (pour les monstres seulement) |
| **IA** | `BestCast` ajoute la valeur d'une réaction déclenchée et une petite valeur pour une empreinte posée ; **les estimations de dégâts de `Ai.cs` (deux copies aujourd'hui) passent par une seule fonction** qui connaît les règles des talents | `Ai.BestCast` |
| **Héros simulé** | `Progression.Built(hero, data, worn, build)` avec des **builds de référence** nommés | `Progression.Built` |
| **Rejeu et serveur** | talents, spécialisation et accomplissement voyagent avec le combat (comme le deck, T35) ; trois colonnes facultatives ; `PUT …/talents` ; les prix ; la commande d'administration gagne `--talents` et `--spec` (Plouf) | `FightRecord`, `Battles.Rebuild`, `PUT …/points` |
| **Client Godot** | la fenêtre **« Ma route »** (le mot du scénariste) : quatre colonnes, des tuiles, le compteur de points, les exclusions grisées ; la pastille d'empreinte au-dessus des combattants ; le nom de la réaction en texte flottant ; la réaction dans la prévision avant de lancer ; 4 couleurs d'élément ; les fiches de sort des composés ; la valeur composée sur la fiche des caractéristiques (« Foudre : Feu + Air = 450 ») | `PointsPanel`, `Texts.SpellCard`, l'affichage de `PassiveTriggered` |

---

## 9. Le canon : conflits à trancher par le scénariste

Je signale, je ne tranche pas.

1. **« Aucune voie ne porte un élément ni un Primordial »** (talents-canon, question 1). L'arbre de Charles porte des éléments. **Piste** : la règle reste vraie pour les **spécialisations** (aucune ne touche un élément, §1), elle ne s'applique plus à l'arbre. L'arbre n'est pas une voie, c'est la façon dont le héros manie ce qu'il a.
2. **« Le héros est celui chez qui les éléments ne se mêlent pas »** (la réplique de Garance ; « la fusion, c'est le Disjoint »). Les composés ont l'air d'un mélange. **Piste qui retourne le conflit en force** : un élément composé n'est pas une fusion mais une **tresse**, deux éléments qui courent ensemble sans se confondre (comme les spores de Sidonie « côte à côte sans jamais se mêler »). Chez la plupart des gens, dit Garance, « l'un étouffe l'autre » : c'est précisément pour ça que **seul le héros** peut tresser la Foudre. Les composés deviennent une expression discrète de l'indice, pas sa trahison. Les **réactions**, elles, ont lieu **dans la cible**, pas dans le héros : c'est le monstre qui s'enlise quand deux éléments s'y rencontrent. (Le mot « tresse » est un nom de travail ; « Alliage » est à éviter, il sonne forge et métal sombre.)
3. **Les quatre Primordiaux et quatre éléments de plus.** La bible dit que l'Accord « n'est pas un cinquième élément ». Un élément Lumière ou Ténèbres **sans parents** se lirait comme un cinquième et un sixième, et Lumière/Ténèbres comme un bien et un mal que la bible refuse (« pas de camp du bien »). **Piste** : les composés à deux parents restent dans les quatre ; mais l'image de **Lumière = Terre + Feu** est faible. Au scénariste de dire si elle tient, ou de proposer un autre nom pour la paire (et Charles dira s'il tient au mot « Lumière »). Ténèbres ne doit **jamais** toucher le métal sombre ni la Disjonction.
4. **Les Fragments** (la puissance des Primordiaux, chapitre 2) ne doivent pas doubler l'arbre : un Fragment ne donne ni élément composé ni réaction.
5. **« Une voie garde les quatre éléments »** : la **conversion** retire deux éléments de base. Avec la lecture du point 2, le héros les garde **dans** le composé.
6. **La nécromancie.** Dans Osmose, **les morts relevés sont une contrainte** (les zombies de la crypte), et le Baume « ne relève jamais les morts ». Le mot « nécromancien » est aussi dans la liste des titres interdits du scénariste. **Piste** : garder la mécanique (des invocations qui naissent des ennemis tombés) et changer ce qu'elle est dans le monde : une **ombre**, un **écho** ou un **souvenir** du combattant tombé (le thème de Névara, la mémoire), qui s'efface après deux tours, jamais un corps contraint. Le nom final au scénariste ; Charles dit s'il tient au mot.
7. **Les noms.** Charles donne des titres de métier (Sorcier, Archimage, Nécromancien) ; le scénariste a posé « une chose ou un lieu, jamais un métier ». Il faudra une seule règle pour les neuf.
8. **Le moment.** La quête de passage passe de 30 à 50 : au cœur du chapitre 2 (les Mines), et plus au seuil.

---

## 10. Chiffrage, tranche verticale et place dans le plan

### 10.1 La tranche verticale (5 points)

**Les deux exemples de Charles, de bout en bout, pour la Sentinelle seule : la réaction « Enlisé » (R1) et « Au plus près » (D1).** Ce sont les deux pièces les plus neuves (un combo, un échange de règles) ; elles n'ont besoin d'aucun sort nouveau ni d'aucun élément nouveau.

- **Moteur** : l'empreinte, `data/reactions.json` avec une seule paire, le statut nommé, `ReactionTriggered` ; les talents (`Hero.Talents`, points 2 à 30 puis 1 tous les 5 niveaux, `Hero.Problem`) ; un talent compilé (la portée) et un talent vivant (la distance) ; l'IA qui valorise la réaction.
- **Serveur** : la colonne des talents, `PUT …/talents`, le rejeu ; `--talents` dans la commande d'administration.
- **Client** : une **liste** des talents (pas encore l'arbre dessiné), la pastille d'empreinte, « Enlisé ! » en texte flottant, la réaction dans la prévision.
- **Tests** : unitaires (l'empreinte posée par un coup d'Eau, pas par un poison, une glyphe ou un sort neutre ; Terre sur Eau avec le talent : −2 PM ; sans le talent : l'empreinte est remplacée ; une empreinte d'un allié ; l'expiration ; le rafraîchissement ; une zone ne réagit que sur la cible visée ; Au plus près : −2 PO jamais sous la portée minimale, +10 % jusqu'à 3 cases et pas à 4 ; `Hero.Problem` : trop de points, avant 30) ; intégration par l'API (enregistrer des talents, jouer un combat que le serveur rejoue et accepte, refuser un combat joué avec d'autres talents) ; T41 à 35 avec un build de référence ; l'auto-test Godot montre une réaction.
- **Fin de sprint** : Charles joue la Sentinelle de Plouf avec Enlisé et décide **go / pas go** pour la suite.

### 10.2 Le chiffrage complet

| Sprint | Pts | Contenu |
|---|---|---|
| **A1, tranche verticale** | 5 | §10.1 |
| **A2** | 5 | les éléments composés dans le moteur (somme, moyenne, couleurs, l'Arc) ; **Foudre pour la Sentinelle** (8 sorts) ; Ouverture et Conversion |
| **A3** | 3 | Foudre pour la Garde et la Mage (16 sorts), leurs textes ; les sorts de base renommés |
| **A4** | 5 | la fenêtre « Ma route » dessinée ; les 5 autres réactions ; `Stat.Range` ; le prix du changement |
| **A5** | 3 | les branches Distance et Rythme ; la fonction unique d'estimation des dégâts de l'IA |
| **A6** | 3 | l'équilibre : builds de référence dans `TemplateTests` (35 à 45), `PartyBalanceTests` avec empreintes partagées, garde-fous ; court playtest |
| **Arbre, sous-total** | **24** | 6 sprints |
| Glace, Ténèbres, Lumière | 4 chacun, **12** | dans le sprint de « socle » des chapitres suivants, après la décision du scénariste |
| **Spécialisations à 50** | **12** | moteur (passifs des héros, `Kill`, `data/specs.json`, la quête de passage déplacée) 3 ; une classe par sprint, 3 × 3 |
| **Approfondissement à 75** | 3 | avec le chapitre qui porte 60 à 75 |
| **Accomplissements à 100** | 9 | avec le chapitre qui porte 90 à 100 |
| **Total** | **60** | dont **24** maintenant, le reste étalé avec le contenu |

C'est **plus du double** de la v1 (17 points). La v2 fait beaucoup plus (quatre couches, des éléments, des combos) : c'est le prix honnête, et la tranche verticale permet de s'arrêter après 5 points si le combo ne plaît pas.

### 10.3 La place dans le plan

- **S135 (recettes) et S136 (quêtes de la Forêt et playtest) ne bougent pas.** Le playtest de S136 dira si 30 à 45 manque de « quelque chose à construire ».
- **S137 : la tranche verticale** (A1), au lieu des guildes, avec un **go / pas go** de Charles à la fin.
- **S138 à S142 : A2 à A6** si go.
- **Les guildes passent en S143** (une guilde sert quand il y a des joueurs pour la remplir ; l'arbre sert dès ce soir à Charles et à Plouf). *Alternative :* guildes en S137 si un serveur s'ouvre à des testeurs.
- **Les spécialisations à 50 arrivent avec les Mines** (45 à 60), dans leur premier sprint de socle : régler les familles des Mines sans spécialisations, puis tout refaire, serait du travail jeté.
- **Planification** : avec la base de la v1 (551 points, 137 sprints, Osmose 330), l'arbre seul porte le projet à **575 points et 143 sprints**, Osmose à **354** ; les 36 points des spécialisations et des éléments suivants entrent dans les chapitres concernés. `PLAN.md`, `data/scrum.json`, les fichiers de sprint, R2, R8, `scopeDecisions` et une décision dans `DECISIONS.md` sont **à mettre à jour après le go de Charles** ; rien n'est fait ici.

---

## 11. Questions à Charles

| # | Question | Valeur par défaut |
|---|---|---|
| 1 | Les quatre couches et la règle de séparation (l'arbre = éléments et règles ; la spécialisation = métier, jamais d'élément) | **oui** |
| 2 | L'arbre : 4 branches (Tresses, Réactions, Distance, Rythme), 3 étages, 2 points à 30 puis 1 tous les 5 niveaux (16 à 100), clés à 2 points, une clé par branche | **oui** |
| 3 | Le niveau d'ouverture de l'arbre | **30** ; *alternative :* 20, mais il faudrait refaire T41 au Lac et aux Champs |
| 4 | Les éléments nouveaux à deux parents en cycle (Foudre = Feu + Air, Glace = Eau + Air, Ténèbres = Terre + Eau, Lumière = Terre + Feu), caractéristique = somme des parents, résistance = moyenne | **oui** ; Lumière à confirmer avec le scénariste |
| 5 | 8 sorts par classe et par élément composé, transposés ; l'ordre Foudre, Glace, puis Ténèbres et Lumière | **oui** |
| 6 | Deux éléments composés au plus, une seule conversion | **oui** |
| 7 | Les réactions : paires **non ordonnées**, l'empreinte d'un allié compte, le talent est celui de qui complète, seule la cible visée d'une zone réagit | **oui** ; *alternative :* paires ordonnées (12 réactions) |
| 8 | Les monstres déclenchent-ils des réactions sur les héros ? | **pas dans la v2** ; plus tard comme leçon de famille |
| 9 | Le prix des changements | **arbre : gratuit jusqu'à 40, puis 10 × niveau Éclats, dans un village ; spécialisation : la première fois gratuite, puis 30 × niveau** |
| 10 | Les spécialisations : 3 par classe à 50, un approfondissement automatique à 75, 1 accomplissement parmi 2 à 100 | **oui** |
| 11 | Au-delà de 100 | **rien dans la Saga I** ; un palier 150 si la Saga II relève le niveau ; pas de prestige |
| 12 | Les spécialisations de la Sentinelle (Mire, Affût, Sente) et de la Garde (Palissade, Brèche, Étau) | **ces six**, noms de travail |
| 13 | Le soin de la Mage : Sorcier, Archimage, Nécromancien ne couvrent pas le soigneur | **les soins restent dans le kit de base** ; *alternative :* une quatrième spécialisation (Baume) |
| 14 | « Nécromancien » : des ombres ou des échos des ennemis tombés plutôt que des morts relevés (canon) | **oui, la mécanique gardée, le nom au scénariste** |
| 15 | La place dans le plan | **S137 : tranche verticale (Enlisé, Au plus près) avec go / pas go ; S138 à S142 : le reste de l'arbre ; guildes en S143 ; spécialisations avec les Mines** |
| 16 | Plouf | ses trois personnages de niveau 100 reçoivent leurs 16 points et choisissent en jeu ; la commande d'administration gagne `--talents` et `--spec` |
| 17 | La v1 (voie à 30, `talents-specialisations.md`) | **remplacée** par ce document ; on la garde pour l'historique |
