# Le Nécromancien, première partie : les cadavres et le Maître des morts *(12 novembre 2026, sprint 214, validé par Charles : recommandations)*

Ce document prépare la story du sprint 214 : « En tant que Nécromancien, mon kit relève les morts à partir des cadavres, et je deviens Maître des morts ; xUnit. PloufNécromancien créé. » L'Invocateur macabre et l'Occultiste viennent au sprint 215. Il part de `classes-conception.md` (§1.2 la fiche et le moteur, §1.3 « Maître des morts : un seul grand mort, ou se faire mort soi-même », §1.4 ce qui le sépare de l'Invocateur, §4 la brique des cadavres), de `classes-canon.md` (« le Nécromancien tient un mort à la fois, d'un seul élément » ; « un héros tombé n'est pas mort ») et de `classes-liste.md` §8.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Neuvième classe neuve**, id `necromancer` (celui de la page de création [V, `Lobby.Roster`]). 60 PV + 3, 8 PA, 4 PM, initiative 10, **budget de dégâts 0,85**, de 1 à 6 cases, trois poisons. La question de son tour : « Quel cadavre je consomme, et pour quoi ? »
- **Son moteur, les cadavres** : un monstre qui tombe laisse son cadavre sur sa case. Quatre sorts du kit en prennent un : **lever** son mort (Lever un mort), le **faire exploser** (Détonation), le **manger** (Festin), s'en faire une **armure** (Armure d'os). Un cadavre pris disparaît : c'est la décision du tour.
- **Un mort à la fois** (le canon) : le Mort levé, nourri de 35 % des PV et 45 % des caractéristiques du Nécromancien, se relève **dans le corps du monstre** dont il prend le cadavre.
- **Le Maître des morts** (niveau 30) garde ce mort et le fait grandir ; ses deux lignées et leurs quatre finales sont écrites dès ce sprint. Rien n'est livré avant la build du sprint 219.

## 1. Les cadavres

**Qui en laisse un** (`Fight.LeavesCorpse`) : un monstre, jamais une créature, jamais un héros. Un héros tombé n'est pas mort : le Clerc le relève, le Nécromancien ne lève que des ennemis (`classes-canon.md`, confirmé ici par la règle). Un héros et ses créatures se reconnaissent à ce qu'ils posent des marques [V, `Fighter.LaysMarks`].

**Lesquels il prend** (`Fight.CorpseAt`) : ceux du camp adverse, sur la case visée, le dernier tombé s'il y en a plusieurs. Un monstre de son propre camp tombé (un allié de scénario) reste à terre, intouché.

**Le sort qui en prend un** (champ `corpse` d'un sort) : sans cadavre ennemi sur la case, le lancer est refusé (`ActionError.NoCorpse`, « Ce sort demande le cadavre d'un ennemi sur cette case. ») ; lancé, le cadavre est pris **avant** le coup, qui peut en laisser un autre sur la même case. Le journal le dit (`CorpseTaken`). Un cadavre sous un combattant vivant peut exploser ou se manger, pas se lever : il faut une case libre.

**Ce qu'il vaut** : une ressource que le combat donne, comme les PV du Berserker. Un sort qui en prend un frappe d'**une moitié de plus** au barème (`tools/spells_gen.py`).

**À l'écran** : un disque couleur d'os ancien sur la case, dessiné seulement quand un Nécromancien de l'autre camp combat (`BoardView.AddCorpse`), retiré quand le cadavre est pris ; l'auto-test du client compare les disques aux cadavres du combat. L'aperçu de visée n'allume que les cases à cadavre (il passe par `CheckCast` [V]).

**L'IA** (`Ai.BestCast`) : un sort à cadavre vise les cases des cadavres ennemis, vides ou non, et y est pesé comme tout sort : un mort levé vaut deux tours de ses coups (`SummonValue`, la règle de l'Invocateur), une explosion ses dégâts autour, un festin les PV qui manquent. Elle choisit donc, à chaque cadavre, le meilleur usage.

## 2. Le Mort levé

| Créature (id) | Au plus | Modèle | PV | PA, PM, init. | Sort |
|---|---|---|---|---|---|
| **Mort levé** (`risen`) | 1 | **celui du cadavre** (`wearsCorpse`), plus petit | 35 % (45 % pour le Maître des morts) | 6, 3, 45 | Griffe du mort (`risen-claw`) : 3 PA, au contact, 7-10, 2 fois, dans l'élément du Nécromancien |

Caractéristiques : 45 % de celles du Nécromancien (55 % pour le Maître des morts), réglées au banc (§6). Le modèle par défaut (sans cadavre, impossible en jeu) est le `zombie-tank`.

## 3. Les 24 sorts

| Niv. | Sort (id) | Élément | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|---|
| 1 | Poussière de tombe `grave-dust` | Terre | 3 | 1-5 | | |
| 1 | Flamme blême `pale-flame` | Feu | 3 | 1-5 | | |
| 1 | Bile noire `black-bile` | Eau | 3 | 1-5 | poison d'Eau 4, 3 tours | |
| 1 | Râle `death-rattle` | Air | 3 | 1-5 | | |
| 1 | Lever un mort `raise-dead` | — | 3 | 1-4 | **sur un cadavre** : lève le Mort levé (1 au plus) | 1 |
| 5 | Détonation `corpse-blast` | — (son élément) | 3 | 1-6, cercle 1 | **sur un cadavre** : il explose | 1 |
| 10 | Écharde d'os `bone-shard` | Terre | 4 | 1-5 | −1 PM | |
| 15 | Fièvre `fever` | Feu | 4 | 1-5 | poison de Feu 4, 3 tours | |
| 20 | Main du noyé `drowned-hand` | Eau | 4 | 1-4 en ligne | attire d'une case | |
| 25 | Lamentation `wail` | Air | 4 | 1-5 | −1 PA | |
| 30 | Festin `feast` | — | 2 | 1-3 | **sur un cadavre** : soigne 18-24 | 1 |
| 35 | Ossuaire `ossuary` | Terre | 5 | 2-5, cercle 1 | | |
| 40 | Brasier funèbre `funeral-blaze` | Feu | 5 | 2-5, croix 1 | | |
| 45 | Marée noire `black-tide` | Eau | 5 | 2-5, cercle 1 | poison d'Eau 4, 3 tours | |
| 50 | Vent des âmes `soul-wind` | Air | 5 | 2-6, cercle 1 | −1 PA | 1 |
| 55 | Armure d'os `bone-armor` | — | 2 | 1-4 | **sur un cadavre** : bouclier 30 sur soi, 2 tours | 2 |
| 60 | Pierre tombale `tombstone` | Terre | 6 | 2-5 | | 1 |
| 65 | Flammes du tombeau `tomb-flames` | Feu | 6 | 2-5, cercle 1 | | 1 |
| 70 | Eaux mortes `dead-waters` | Eau | 6 | 2-5, croix 1 | | 1 |
| 75 | Dernier souffle `last-breath` | Air | 6 | 2-6 | | 2 |
| 80 | Marche funèbre `funeral-march` | — | 3 | 0-5, cercle 1 | alliés +2 PM 1 tour, +15 % de dégâts 2 tours | 3 |
| 85 | Effroi `dread` | — | 3 | 1-5 | −2 PA | 2 |
| 90 | Froid du tombeau `grave-chill` | — | 3 | 1-5 | −15 % de résistance, 2 tours | 2 |
| 95 | Pas funèbre `grave-step` | — | 1 | 0 | +2 PM, 1 tour | 2 |

Les dégâts suivent le barème à 0,85 ; leurs 96 jumeaux de composés aussi. Noms de travail, au scénariste. Silhouettes de game-icons.net (CC BY 3.0), dont 25 nouvelles.

## 4. Le Maître des morts (niveau 30)

« Un seul grand mort, ou se faire mort soi-même » (§1.3 de la conception).

| Rang | Règles | Sorts |
|---|---|---|
| **Maître des morts** (`dead-master`) | Mort levé nourri à 45 % des PV, 55 % des caractéristiques ; il **grandit** de 5 % de dégâts à chacun de ses tours, 25 % au plus (la croissance du Familieriste [V]) | **Ravaudage** (`path-stitch`, 2 PA, 1-6, sur son mort : soin 20-26 et +15 % de résistance 2 tours, relance 2) ; **Rempart d'os** (`path-bone-rampart`, 2 PA, 1-5, **sur un cadavre**, cercle 1 : bouclier 20 aux alliés, 2 tours, relance 2) |

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Liche** (`lich`) : se faire mort soi-même | **Non-vie** (`path-unlife`, 3 PA, 1-6, sur son mort : il meurt en lui, ses PV en bouclier et 30 % de dégâts, sa croissance en plus, 3 tours, relance 3 ; la fusion du Fusionniste [V]) | **Archiliche** (`archlich`, Form : Non-vie laisse 15 % de dégâts pour tout le combat) ; **Roi des damnés** (`damned-king`, Extends : Non-vie donne aussi 2 PA) |
| **Seigneur des os** (`bone-lord`) : un grand mort | croissance +10 % par tour, +25 % au plus (15 et 50 en tout) ; **Moelle** (`path-marrow`, 2 PA, 1-6, sur son mort : +30 % de dégâts 2 tours, +2 PM 1 tour, relance 2) | **Titan osseux** (`bone-titan`, Extends : croissance +5 et +25 de plus) ; **Colosse mort-vivant** (`undead-colossus`, Faster : Moelle relancée en 1 tour) |

La Liche et le Seigneur des os se séparent en une phrase : l'une **prend** son mort en elle, l'autre le **pousse** à frapper. Aucune brique neuve : la croissance et la fusion viennent du Familieriste (S212).

## 5. Ce qui le sépare des autres

| Avec | Règle | Vérifié |
|---|---|---|
| Invocateur | §1.4 de la conception : le Nécromancien **consomme**. Toute invocation du Nécromancien demande un cadavre ; aucun sort de l'Invocateur n'en lit un, ni aucun autre sort du jeu | `NecromancerTests.WhatSetsTheNecromancerApart_FromTheSummoner` |
| Prêtre, Clerc | il ne relève jamais un héros ; son seul soin est sur soi, et il coûte un cadavre | test sur les cadavres |
| Fusionniste | même brique de fusion, mais sur un mort que le combat a donné, pas sur une créature appelée à volonté | les données |

## 6. Mesures

`Rpg.Sim --matrix --class necromancer --simulate 30` (30 combats sur 16 groupes de 4 et 5 monstres, héros nus, sorts de voie rangés).

| Niveau | Voie | Points | Sur le héros sans voie | Sur la moyenne |
|---|---|---|---|---|
| 30 | sans voie ; Maître des morts | 24,6 ; 29,8 | +5,2 | 0,0 |
| 35 | sans voie ; Maître des morts | 37,9 ; 63,5 | +25,6 | 0,0 |
| 45 | sans voie ; Maître des morts | 44,8 ; 65,8 | +21,0 | 0,0 |
| 50 | sans voie ; Liche, Seigneur des os | 44,4 ; 68,5, 61,9 | +24,2, +17,5 | +3,3, −3,3 |
| 60 | sans voie ; Liche, Seigneur des os | 21,7 ; 38,1, 38,3 | +16,5, +16,7 | ±0,1 |
| 100 | sans voie ; Liche et ses finales | 12,9 ; 36,5 à 38,3 | +23,5 à +25,4 | −1,5 à +0,4 |
| 100 | Seigneur des os et ses finales | 37,9 à 38,8 | +25,0 à +25,8 | 0,0 à +0,8 |

**Les autres classes sans voie, au même banc** : au niveau 35, Invocateur 57,5, Paladin 50,0, Mage 39,4, Prêtre 23,8 ; au 60, Mage et Invocateur 47,9, Paladin 34,2, Prêtre 9,2 ; au 100, Mage 42,5, Paladin 31,3, Invocateur 25,4, Prêtre 8,3. Au niveau 10, le Nécromancien gagne 13 combats sur 100 (Archer 20, Mage 9, Prêtre 6, Guerrier 5).

**Ce qu'il joue** (niveau 35, 10 combats par groupe) : 1,6 cadavre pris par combat ; Lever un mort 1,0, Festin 0,3, Détonation 0,3. Il ne lève pas avant qu'un monstre tombe : ses premiers tours sont ceux d'un lanceur à 0,85.

**Réglages en route.**

- **La Détonation** au barème simple (5-7 au niveau 5) n'était presque jamais jouée (0,11 par combat) : un cadavre pris frappe désormais d'une moitié de plus (8-11), 0,28 par combat.
- **La Moelle** donnait +2 PA au mort, dont la Griffe se lance deux fois par tour au plus : inutile, le défaut de la Surcharge au S213. Devenue +30 % de dégâts 2 tours et +2 PM.
- **Le Mort levé** à 30 % des PV et 35 % des caractéristiques : le héros sans voie tombait à 17,9 au niveau 60 et 12,3 au 100. À 35 et 45 % (45 et 55 % pour le Maître des morts) : 21,7 et 12,9 ; le gain reste petit, le mort étant seul.
- **Le Seigneur des os** à +5 % de croissance : 9,6 points sous la Liche à 50 (borne B6 : 8). À +10 % : 6,6.

**Les bornes (§7.3 de la conception).**

- **B4** (héros sans voie entre 20 et 75) : tenue de 30 à 60 ; **pas au niveau 100 (12,9)**, comme le Prêtre (8,3). Q3.
- **B5** (rang 2 à 35 et 45) : une seule branche ce sprint, la moyenne de la classe est la sienne.
- **B6** (rang 3 à 50 et 60, 8 points au plus entre lignées) : **tenue**, 6,6 et 0,2.
- **B7** (+15 au plus sur le héros sans voie) : **dépassée**, de +16,5 à +25,8, comme pour l'Invocateur et les voies rangées depuis D98 : le mort du Maître est mieux nourri et grandit. Le jalon 3 lira toute la classe.
- **B8** (rang 4) : les finales restent à 2 points de leur lignée. Le **Colosse mort-vivant** ne change aucun combat de la fumée (la Moelle peu lancée au niveau 100) : ajouté à `CiLevelsTests.Unchanged`.
- **Le Rempart d'os** est peu joué (0,0 à 0,3 par combat) : il protège les alliés autour d'un cadavre, et le banc joue seul. Le Ravaudage, de 0,4 à 0,5.

**Les exigences de la CI.**

- En solo : la grande salle de la Bauge, les hordes de la Forêt et le Disjoint seul à 30, tenus avec le Nécromancien parmi les classes jugées.
- **Les groupes de référence** (hordes au niveau 45, 18 combats par groupe) : Guerrier + Nécromancien 55 %, Archer + Nécromancien 61 %, Paladin + Nécromancien 61 %, Prêtre + Voleur + Nécromancien, Mage + Berserker + Nécromancien et Guerrier + Prêtre + Invocateur + Nécromancien 100 %.
- **Les autres classes** jouent les mêmes combats qu'avant (empreintes en solo inchangées) : les cadavres ne touchent que les sorts qui les prennent.

## 7. Tranché par Charles (« go reco », 12 novembre)

- **Q1. Les noms** des 24 sorts, du Mort levé, de la Griffe du mort et des 4 sorts de voie : noms de travail, au scénariste. Les titres viennent de ta liste.
- **Q2. Un sort neutre qui fait des dégâts.** D62 veut 8 sorts neutres sans dégâts par classe ; la Détonation, un cadavre qui explose, frappe dans l'élément de son lanceur. a) **l'exception, pour ce seul sort, écrite dans le test (Reco, fait)** ; b) la Détonation devient un sort d'un élément, et le Nécromancien perd l'un de ses sorts de Terre.
- **Q3. Le Nécromancien sans voie au niveau 100** (12,9, sous la borne de 20 ; le Prêtre est à 8,3) : il dépend des morts du combat, et son mort est seul. a) **le régler au jalon 3 (S219), avec toute la classe, ses trois branches écrites (Reco)** ; b) maintenant, un second mort pour le héros sans voie, contre le canon (« un mort à la fois »).
- **Q4. Les cadavres de qui** : a) **les monstres seulement : un héros tombé n'est pas mort, le Nécromancien ne lève que des ennemis (Reco, fait, le canon)** ; b) ceux des héros adverses aussi, en duel de classes.
- **Q5. Le repère au sol** : a) **un disque couleur d'os, dessiné seulement quand un Nécromancien combat (Reco, fait)** ; b) toujours dessiné ; c) le corps du monstre laissé couché, à la refonte artistique.
