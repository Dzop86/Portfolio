# Les 10 classes d'Osmose : conception et chiffrage *(game designer, 4 novembre 2026, à valider par Charles)*

Ce document part de `docs/classes-liste.md` et des décisions déjà prises par Charles (10 classes, éléments et composés gardés, Garde vers Guerrier, Sentinelle vers Archer, rangs à 30, 50 et 100, modèles Kenney). Il ne les rouvre pas. Rien n'est codé ni planifié par ce document. Les noms de classes sont ceux de Charles ; les mécaniques et les sorts décrits ici n'ont que des noms de travail, à reprendre par le scénariste.

**Légende.** **[V]** : vérifié dans le code ou les données. **[S]** : supposé, à confirmer par la mesure ou le jeu. Aucun chiffre d'équilibre de ce document n'a été mesuré : je n'ai pas lancé le jeu ni les tests.

## Décisions de Charles (4 novembre 2026, D96)

« Go » aux recommandations du §8 : Q1 a, Q2 a, Q3 a, Q4 a, Q5 a ; les 14 noms en double restent ; forme féminine facultative ; Sentinelle et Faucon de guerre sont de simples titres. B12 accepté. B7 accepté **à titre provisoire seulement** : −3 est à revoir après l'IA, un joueur qui choisit une voie ne devrait pas y perdre. Le découpage du §5.4 fait foi ; la forme féminine (environ 2,5 points) puise dans la marge.

## 0. L'avis en une page

**Le plan est tenable en budget, pas en forme.** Ton total de 125 points est proche de mon chiffre nominal (128), mais il ne compte ni marge, ni banc de mesure, et 5 points de mesure pour 10 classes sont très loin du compte. Mon chiffre réaliste : **128 points, 143 avec une marge de 15, soit environ 27 à 30 sprints.**

Ce que je changerais :

1. **Livrer par vagues, avec un point d'arrêt possible** : 4 classes à 53 points, 7 à 86, 10 à 128. Chaque vague donne un jeu complet et jouable, et on ne s'engage pas sur 17 sprints de classes avant d'avoir joué le format.
2. **Commencer par le Guerrier, pas par le Mage.** Ses 6 lignées se transposent presque une à une sur le nouvel arbre : il valide le format à 4 rangs à bas prix. Le Mage devient le vrai pilote juste après, avec un arbre neuf.
3. **Construire le banc de mesure avant le contenu.** T152 a montré qu'un seul défaut de paquet avait faussé les conclusions de deux sprints. Avec 10 classes, la suite de tests actuelle passerait à plusieurs fois son volume (§7).
4. **Ne pas promettre 120 classes finales distinctes.** Dix classes vraiment différentes, 30 métiers, 60 décisions de tour : oui. Les 120 finales sont 120 signatures, bâties sur une douzaine de gabarits (§2).

Trois faits trouvés en lisant le code pèsent sur tout le reste :

- **Aujourd'hui, les classes ne se distinguent presque pas par leurs chiffres.** Toutes les trois ont 8 PA, 4 PM et 10 d'initiative [V, `classes.json`]. Et leurs PV contredisent leur image : au niveau 50, la Sentinelle a 307 PV, la Mage 233, la Garde 221 ; la « Mage fragile » a plus de PV que la Garde à tous les niveaux [V, formule de `Fight.HeroTotals`]. Ces PV ont été réglés par balayage pour équilibrer les duels joués par l'IA (T57). Ils compensent l'IA, ils ne racontent pas la classe.
- **Aucune zone n'existe au niveau 100, ni même au-delà de 60** (`elements-specialisations.md` §v3.5 et §v4.6, M4) ; aucun monstre de `monsters.json` n'a de niveau de 55 ou plus [V, recherche sur le champ `level`]. Les 120 finales ne servent aujourd'hui qu'aux héros de test. Le rang 4 est donc le poste où économiser en premier.
- **Les composés sont déjà générés.** `spells_gen.py` transpose chaque sort élémentaire ; il ne manque à une classe que 16 « gestes » (un mot FR et EN par sort) [V]. Les 96 sorts de composés d'une classe coûtent presque rien à écrire. Ils coûtent à mesurer.

---

## 1. L'identité de chaque classe

### 1.1 Le principe : où loge l'identité

Toutes les classes ont les 4 éléments, les composés, un paquet de 12 places et le même barème de dégâts par PA. L'élément ne distingue donc personne, et le canon le veut ainsi (`talents-canon.md` §2, principe 2). L'identité doit venir de quatre leviers, et d'eux seuls :

| Levier | Ce qu'il porte |
|---|---|
| **Une décision de tour propre à la classe** (le moteur) | la question que le joueur se pose à chaque tour : la seule chose vraiment distinctive |
| **La forme des 16 sorts élémentaires** | portée et zone : c'est là que se joue le placement |
| **Les 8 sorts neutres** | la boîte à outils, sans partage entre classes (`DataTests` l'exige [V]) |
| **Les chiffres** | PV, PM, initiative et un **facteur de budget par classe**, à ajouter à `budget()` |

**Les PA restent à 8 pour tout le monde.** Le barème est en dégâts par PA : changer les PA change tout le rythme de dégâts, et casse les sorts conçus pour tenir dans un tour. C'est trop violent pour servir à différencier. Les PM et le facteur de budget suffisent.

### 1.2 Les fiches

Chiffres de départ, tous **[S]**, à régler par balayage. PV à 50 = base + 49 × gain. Budget : multiplicateur des dégâts des sorts de base, 1 aujourd'hui.

| # | Classe | Rôle | Portée | PV (base + niveau) | PV à 50 | PM | Init. | Budget | La question de son tour |
|---|---|---|---|---|---|---|---|---|---|
| 1 | **Guerrier** | Tenir, déplacer l'ennemi | contact ; attire de 2 à 5 | 74 + 3 (cible 74 + 5) | 221 (319) | 4 | 10 | 1 | **Où est-ce que je fais finir l'ennemi ?** |
| 2 | **Paladin** | Protéger en frappant | contact ; effets à 2 cases | 80 + 4 | 276 | 3 | 10 | 0,85 | **Qui je couvre, et à côté de qui je me place pour ça ?** |
| 3 | **Mage** | Zones et gros sorts | 2 à 8, peu de contact | 86 + 3 (cible 72 + 3) | 233 (219) | 4 | 10 | 1 | **Je lance maintenant, ou je garde des PA pour plus gros ?** |
| 4 | **Prêtre** | Soigner, purger | 1 à 6, jamais au contact | 66 + 3 | 213 | 4 | 10 | 0,8 | **Qui sauver ce tour, et que me reste-t-il pour frapper ?** |
| 5 | **Voleur** | Une cible, vite | 1 à 2, sauts | 58 + 3 | 205 | 5 | 30 | 1,05 | **Comment je l'isole, et par où je ressors ?** |
| 6 | **Archer** | Dégâts à distance, entraves | 2 à 8 | 62 + 5 (cible 62 + 4) | 307 (258) | 4 | 10 | 1 | **Comment je garde la distance encore un tour ?** |
| 7 | **Druide** | Façonner le terrain | 1 à 4, glyphes | 78 + 3 | 225 | 4 | 10 | 0,85 | **Où je pose le terrain, et comment j'y amène tout le monde ?** |
| 8 | **Nécromancien** | Dévorer ce qui tombe | 1 à 6, poisons | 60 + 3 | 207 | 4 | 10 | 0,85 | **Quel cadavre je consomme, et pour quoi ?** |
| 9 | **Berserker** | Dégâts bruts, payés en PV | contact, charges | 90 + 5 | 335 | 5 | 20 | 1,1 | **Combien de mes PV je dépense ce tour ?** |
| 10 | **Invocateur** | Jouer par ses créatures | 1 à 5, faible | 54 + 3 | 201 | 4 | 10 | 0,75 | **Qui je lève, et sur qui je l'envoie ?** |

**La colonne « cible » des trois classes converties.** Au départ, elles gardent leurs PV : les mesures de T152 restent ainsi comparables. Après l'amélioration de l'IA, on refait le balayage vers la cible, pour que le Guerrier cesse d'être moins résistant que l'Archer. Cette retouche touche des chiffres en place : voir la question Q3.

**Le moteur de chaque classe, et ce qu'il demande au code**

| Classe | Moteur (dès le niveau 1) | État du moteur de jeu |
|---|---|---|
| Guerrier | attirer, bloquer, retirer des PM : son kit actuel | existe [V] |
| Paladin | ses coups au contact donnent un bouclier à l'allié le plus blessé à 2 cases | proche de la signature Bourgeon de la Sève [V] |
| Mage | garde jusqu'à 2 PA d'un tour au suivant | `ReserveRule` existe [V] ; l'IA ne sait pas en jouer |
| Prêtre | un soin qui dépasse les PV max devient un bouclier | à ajouter |
| Voleur | +15 % sur une cible qui n'a aucun allié à 2 cases ; ses poisons se cumulent | `TargetStateRule` à étendre ; brique Q prévue, non faite [V] |
| Archer | garder la distance : son kit actuel | existe [V] |
| Druide | des glyphes qui aident les alliés et frappent les ennemis | glyphes alliées déjà jouées par les monstres (Cercle de mousse) [V] |
| Nécromancien | une mort laisse un cadavre sur sa case, ressource de ses sorts | à ajouter |
| Berserker | plus il lui manque de PV, plus il frappe ; certains sorts coûtent des PV | à ajouter |
| Invocateur | 2 créatures dès le début, et un ordre pour les diriger | `MaxSummons` existe [V] ; l'ordre est à ajouter |

### 1.3 Les 30 branches, en une ligne

Ce n'est pas encore la conception détaillée : celle-ci se fait au sprint de chaque classe. C'est ce qui garantit que les 3 branches d'une classe ne se recouvrent pas.

| Classe | Branche 1 | Branche 2 | Branche 3 |
|---|---|---|---|
| Guerrier | **Chevalier** : se placer pour les autres | **Maître d'armes** : retenir une cible et la punir | **Gladiateur** : entrer, achever, arracher |
| Paladin | **Templier** : frappe qui marque et qui rend | **Protecteur** : tenir une position autour des alliés | **Vengeur** : frappe d'autant plus que les siens ont souffert |
| Mage | **Arcaniste** : runes et gros sorts préparés | **Illusionniste** : images, attention des monstres détournée | **Chronomancien** : tours, relances, PA empruntés |
| Prêtre | **Clerc** : le soin | **Inquisiteur** : retirer états et boucliers, punir | **Oracle** : poser ce qui se déclenchera au tour suivant |
| Voleur | **Assassin** : isoler, empoisonner, achever | **Malandrin** : sauts, feintes, pièges | **Ombremage** : disparaître, frapper depuis l'ombre |
| Archer | **Tireur d'élite** : un tir, une cible | **Rôdeur** : Faucon et pièges | **Artilleur** : tenir ou courir (Arbalétrier contre Tireur mobile) |
| Druide | **Gardien de la nature** : terrain qui protège | **Métamorphe** : formes, au contact | **Chaman** : esprits sans corps, terrain qui frappe |
| Nécromancien | **Maître des morts** : un seul grand mort, ou se faire mort soi-même | **Invocateur macabre** : beaucoup de morts jetables | **Occultiste** : aucune invocation, sang et âmes |
| Berserker | **Ravageur** : zones, PV dépensés | **Barbare** : le clan au contact, ou la bête | **Gladiateur** : une cible, l'exécution |
| Invocateur | **Démoniste** : des pactes, ses PV contre de la puissance | **Conjurateur** : créatures posées, immobiles | **Familieriste** : une créature qui grandit, puis la fusion |

### 1.4 Les paires à risque, et ce qui les sépare

Une règle par paire, **testable sur les données** : `DataTests` peut la vérifier, donc elle ne s'érode pas au fil des sprints.

| Paire | Le risque | Ce qui les sépare | La règle testée |
|---|---|---|---|
| **Guerrier / Berserker** | deux corps à corps à PV | Le Guerrier gère la position de l'ennemi, le Berserker gère ses propres PV. L'un fait venir, l'autre y va | Berserker : aucun bouclier, aucune attirance, aucun retrait de PM. Guerrier : aucun sort qui coûte des PV |
| **Paladin / Prêtre** | deux soutiens sacrés | Le Paladin protège *par sa présence* : il doit frapper et être à 2 cases. Le Prêtre soigne de loin, et frappe peu | Paladin : aucun soin à plus de 2 cases, aucun soin sans coup porté. Prêtre : aucun sort de dégâts au contact |
| **Nécromancien / Invocateur** | deux classes à créatures | Le Nécromancien **consomme** : ses morts sont gratuits, fragiles, jetables, et dépendent des morts du combat. L'Invocateur **investit** : ses créatures sont chères, durables, nourries | Nécromancien : sa grande invocation demande un cadavre. Invocateur : aucun sort ne lit un cadavre. La branche Occultiste n'invoque rien |
| **Mage / Voleur Ombremage** | deux lanceurs d'ombre et d'illusions | Mage : zones de loin, immobile. Ombremage : une cible, de près, en mouvement | Ombremage : aucun cercle de 2 ni croix de 2. Le Mage ne saute pas (sauf Chronomancien) |
| **Illusionniste (3.2) / Illusionniste (5.3.2)** | même nom, même mécanique | Celui du Mage déplace **l'attention des monstres**. Celui du Voleur utilise son image **pour sortir** (échange de place, disparition) | Échange avec l'image : Voleur seulement. Image qui frappe ou attire : Mage seulement |
| **Archer Rôdeur / Druide** | bêtes, pièges, nature | Le Rôdeur prépare un terrain **hostile**, à distance, avec un seul Faucon. Le Druide façonne un terrain **partagé**, de près, avec ou sans forme | Glyphes du Rôdeur : ennemis seulement. Glyphes du Druide : aident aussi les alliés |

**Les paires que tu n'as pas citées, et que j'ajoute**

| Paire | Ce qui les sépare |
|---|---|
| **Gladiateur du Guerrier (1.3) / du Berserker (9.3)** | Le premier repose sur les murs et les attirances, le second sur l'exécution payée en PV |
| **Bretteur du Guerrier (1.2.1) / du Voleur (5.2.1)** | Le premier immobilise puis perce, le second saute, frappe, ressort |
| **Métamorphe du Druide / Sauvage du Berserker** (Avatar primal, Avatar animal) | La forme du Druide est un choix réversible, sans coût en PV ; celle du Berserker se paie et ne se quitte pas |
| **Chaman (Invocateur spirituel) / Conjurateur (Invocateur élémentaire)** | Les esprits du Chaman aident sans occuper de case et durent peu ; les créatures du Conjurateur bloquent le terrain |
| **Maître des runes (Mage) / Maître des pièges (Voleur) / pièges du Rôdeur** | Les runes marquent et se déclenchent à la demande ; le Voleur pose au contact et sur soi ; le Rôdeur à distance |
| **Inquisiteur, Fanatique, Zélote, Purificateur, Paladin / Prêtre** | Toujours la même séparation : version Paladin au contact, version Prêtre à distance |

**Le test humain, déjà écrit dans la v4 et à garder :** Charles doit pouvoir dire en une phrase ce qui sépare deux nœuds voisins. S'il ne trouve pas, c'est une fausse décision, quel que soit le chiffre.

---

## 2. Le contenu d'un rang

### 2.1 La réponse franche : 120 finales distinctes, non

**Dix classes peuvent être vraiment distinctes.** Trente branches aussi, parce que chacune a 2 sorts et change ce qu'on fait. Soixante lignées, oui si on tient la règle de la v4 : une différence de décision, jamais de chiffre.

**Cent vingt finales, non, pour trois raisons.**

- **Un jeu tactique n'a pas 120 décisions de tour différentes à offrir.** Il y a un nombre fini de verbes : placer, garder, risquer, préparer, sacrifier. Au-delà d'une soixantaine, on recycle.
- **Personne ne les verra.** Un joueur atteint une finale après 100 niveaux, et elle arrive sans zone pour s'en servir [V, §0]. Dans une guilde, il en croisera quelques-unes.
- **On ne pourra pas les mesurer.** 120 finales × 14 états de composés (aucun, 6 paires, 3 doubles paires, 4 trios) × 300 combats, cela fait 504 000 combats, pour un écart signalé de ±5,7 points.

**Ce qui tient la route : 120 signatures.** Chacune a son nom, son titre, son texte et une règle visible, qu'on voit jouer et qu'on peut raconter en une phrase. Mais ce sont des combinaisons d'une douzaine de gabarits, pas 120 inventions. Ce n'est pas un défaut : c'est ainsi que les grands arbres tiennent, tant que le joueur ne compare que deux finales à la fois.

### 2.2 Ce qu'apporte chaque rang

| Rang | Niveau | Choix | Ce qu'il apporte | Ce qu'il dit au joueur | Sorts |
|---|---|---|---|---|---|
| **1** | 1 | la classe | 24 sorts, le moteur, les chiffres | qui je suis | 24 |
| **2** | 30 | 1 parmi 3 | **2 sorts, 1 passif, 1 règle**, titre | quel métier je fais | 2 |
| **3** | 50 | 1 parmi 2 | **1 sort, 1 règle ou passif** qui fixe la décision, titre | comment je le fais, ce que je regarde à chaque tour | 1 |
| **4** | 100 | 1 parmi 2 | **1 gabarit** : une règle ou un passif, sans sort, titre | ma signature | 0 |

**Pourquoi aucun sort au rang 4** [V] : `Specs.Transferred` transfère les sorts case par case et compte sur une feuille de 3 sorts, le paquet a 12 places, et le livre compte 24 + 3 sorts (D94). Un sort de finale ferait tout déborder, et T149 avait déjà écarté cette option. Le changement de moteur reste donc petit : on ajoute un `Hero.Final` et des `SpecFork.Finals`, sans toucher au transfert.

**Prix de changement proposés** [S] : autre finale, 5 × niveau (500 Éclats à 100) ; le reste inchangé (lignée 10 × niveau, branche 30 × niveau). Le premier choix reste gratuit à chaque rang.

### 2.3 Les gabarits du rang 4

Presque tous existent déjà dans le moteur, surtout par `PatchRule` [V].

| Gabarit | Exemple | Moteur |
|---|---|---|
| Le sort s'étend | Tir visé touche aussi la case derrière | `PatchRule` |
| Le sort laisse une trace | Tir de barrage laisse une glyphe | `PatchRule` |
| Le sort revient plus vite | sans relance, une fois par tour | `PatchRule`, `OncePerTurnRule` |
| Un second exemplaire | deux Faucons, deux pièges | `MaxSummons`, `MaxGlyphs` |
| Une fois par combat, ça sauve | un allié reste à 1 PV | `PerFight`, `AllyFalls` |
| Sous un seuil de PV | sous 30 %, tel effet | `LowHp` |
| Achever rapporte | +2 PA sur ennemi achevé | `Kill` |
| Premier tour | +3 PA au premier tour | `PointsRule`, `FirstCastRule` |
| Rester ou bouger | bonus immobile, ou par case marchée | `StillRule`, `DanceRule` |
| Cible en état | +x % sur cible à 0 PM ou sous 2 états | `TargetStateRule` |
| La mort paie | ce qui meurt explose | `Death` |
| Une forme | transformation durable | nouvelle, §3 |

**Règle de construction :** deux finales sœurs utilisent **deux gabarits différents**. C'est ce qui empêche la fausse décision « +15 % ou +20 % ». La validation peut le vérifier si chaque finale nomme son gabarit dans les données.

### 2.4 Le compte

**Par classe :** 24 de base + 96 de composés + 12 de voie, soit 132 sorts, dont 27 au plus dans le livre d'un héros. Soit 3 × 2 + 6 × 1 = 12 sorts de voie, **autant qu'aujourd'hui** alors que les nœuds passent de 15 à 21.

| Élément | Par classe | Sur 10 classes | À produire |
|---|---|---|---|
| Sorts de base | 24 | 240 | **168** |
| Sorts de voie | 12 | 120 | **96** au plus, moins les réemplois du §4 |
| Sorts de composés (générés) | 96 | 960 | **672** |
| Effets de nœuds (passif ou règle) | environ 24 | environ 240 | environ 200 |
| Titres | 21 | 210 | FR de Charles, **EN à écrire** |
| Descriptions FR et EN | 21 | 210 | **420 textes** |

Soit `spells.json` de 494 entrées [V] à environ **1 430**. Le chargement et les tests qui parcourent tous les sorts sont à surveiller [S].

**Textes FR et EN hors composés :** 528 noms de sorts, 224 gestes, 210 titres anglais, 420 descriptions, et jusqu'à 180 phrases d'ambiance si on en garde pour les rangs 2 et 3. C'est un gros chantier pour le scénariste. Il ne bloque pas un sprint, comme pour les composés.

---

## 3. Les mécaniques nouvelles

Coûts en points, moteur, IA et tests compris, client inclus quand c'est dit, à ±30 % [S]. Chaque mécanique demande une ligne dans `Ai.Value`, sinon la mesure sous-estimera sa classe, et un `case` dans le client si elle crée un événement (`Main.cs`, `Died`, `Summoned` [V]).

**Le constat rassurant :** le moteur contient déjà beaucoup plus qu'il n'y paraît. `Leurre`, `CarriedAp`, `NextTurnPenalty`, `EchoRule`, `Incorporeal`, `Aims`, `Needs`, glyphes alliées, `Replaces` portent à eux seuls une bonne moitié de ce que demande la liste [V].

| Mécanique | Classes qui l'utilisent | Ce qui existe | Ce qu'il faut ajouter | Coût | Verdict |
|---|---|---|---|---|---|
| **Soins élaborés** | Prêtre, Paladin, Druide | `HealEffect`, `Healing` | soin dans la durée (poison inversé), soin en trop devenu bouclier, retrait d'états | 0,75 | **Indispensable** |
| **Résurrection** | Prêtre (Clerc, Gardien de vie), Paladin (Porte-lumière) | un mort reste dans `_fighters`, à 0 PV ; `At` ne le voit plus [V] | viser un allié mort, case libre, une fois par combat, événement client | 0,75 | **Indispensable** pour le fantasme, inutile en solo |
| **Sang** | Berserker, Sanguinomancien, Pactisant, Fanatique | Bourgeon (soin sur dégâts), Prix du sang prévu (R) | sort qui coûte des PV, dégâts selon les PV perdus, vol de vie | 0,75 | **Indispensable, partagée** par 4 classes |
| **Forme** | Métamorphe, Liche, tous les « Avatar », Fusionniste | statuts, `Needs` | état de forme, sorts qui en dépendent, changement d'apparence en combat (`Look` est figé au début du combat [V]). Un modèle de monstre existant sert de corps (`wolf-melee`, `golem-melee`...) | 1,5 | **Indispensable, partagée** par 5 classes |
| **Invocations commandées** | Invocateur, Nécromancien, Rôdeur, Chaman | `SummonRule`, `Needs`, `Aims` ; les invocations sont jouées par l'IA, même pour un joueur [V, `FightController.IsPlayerTurn`] | un ordre : marquer la cible des créatures | 0,5 | **Indispensable** : sans lui, la classe regarde jouer l'IA |
| **Sacrifice et fusion** | Nécromancien, Démoniste, Fusionniste | `Aims`, Détonation conçue | brique R ; fusion : tuer sa créature donne un état au lanceur | 0,75 | **Partagée** |
| **Cadavres** | Nécromancien seul | `Trigger.Kill`, `Death` | liste de cases de mort, sort qui les vise, repère au sol | 0,75 | **Indispensable** : c'est son moteur |
| **Morts-vivants** | Nécromancien | `SummonSpec`, `Scale` ; modèle `zombie-tank` existant [V] | rien de plus que des données : goule, squelette, grand mort | 0 | Données |
| **Illusions** | Illusionniste (Mage, Voleur), Mentaliste, Fantôme | `Leurre`, `HpOfSummoner`, `DecoyPull` dans l'IA [V] | image à l'apparence de son lanceur ; échange avec l'image (`Aims` + `Swap`, comme Chassé-croisé) | 0,5 | **Indispensable, peu chère** |
| **Temps, base** | Chronomancien | `CarriedAp`, `NextTurnPenalty`, `EchoRule`, `CooldownRule`, `PointsRule` [V] | emprunter des PA au tour suivant ; retarder ou avancer un combattant dans `_order`, frise à jour ; réduire les relances d'un allié | 1 | **Indispensable** |
| **Temps, retour** | Voyageur temporel, Gardien du temps | rien | revenir à sa case et à ses PV d'un tour avant : photo de l'état, une fois par combat | 0,75 | **Optionnelle** : à ne faire que si le Mage ne déborde pas |
| **Discrétion** | Assassin, Ombremage, Fantôme, Spectre | `Incorporeal` | ne peut être visé au-delà de 2 cases, s'arrête quand on frappe ; filtres de l'IA ; rendu | 1 | **Indispensable** au Voleur |
| **Poisons cumulés, et les consommer** | Voleur, Occultiste | `SetStatus` renouvelle [V] | brique Q ; un sort qui consomme les poisons en dégâts | 0,75 | **Partagée** |
| **Contrôle mental** | Dominateur mental, Télépathe suprême | rien | un monstre dont l'IA inverse ses cibles pendant un tour, chefs exclus | 0,75 | **Optionnelle** : à défaut, retraits de PA et de résistance |
| **Runes, auras, pièges, esprits** | Arcaniste, Paladin, Saboteur, Chaman | glyphes, marques, `Allies` au début du tour, `Incorporeal` [V] | rien | 0 | Données |

**Total du moteur : environ 9 points, plus 1,5 pour les deux optionnelles**, payés par la première classe qui en a besoin, déjà compris dans le coût de chaque classe au §5.

**Ce que j'exclus, et pourquoi**

| Tentation | Pourquoi non |
|---|---|
| **Terrain destructible** (Démolisseur, Brise-crâne) | cartes figées, chemins, vue, rejeu serveur : un chantier à lui seul. Démolisseur brise boucliers et glyphes : la réaction Dispersé existe déjà [V] |
| **Familier d'équipement en combat** (lié à l'emplacement `Pet`) | mélange équipement et classe, et casse le budget des objets |
| **Contrôle direct des invocations** | un tour de joueur multiplié par 3 ou 4, et un minuteur de tour à revoir : voir Q4 |
| **Dos et flanc** | le combat ne connaît pas d'orientation [V, aucun champ dans `Fighter`]. « Cible isolée » donne la même décision sans cet ajout |
| **Montures, bêtes de grande taille, corps nouveaux** | direction artistique reportée, déjà tranché |

**Un risque à surveiller : la durée d'un combat.** Trois Invocateurs à 3 créatures contre 10 monstres, c'est 22 acteurs par tour. Limite proposée : 3 créatures par héros au plus, et les créatures d'un même héros jouent à la suite. À mesurer en durée de tour réelle au jalon 3 [S].

**Limite des modèles :** ni démon, ni élémentaire, ni esprit n'existent en modèle. L'Élémentaire actuel porte un corps humain, `male-e` [V]. Ces créatures auront un corps de monstre ou d'humain teinté d'ici la refonte artistique. Cela se verra.

---

## 4. La conversion

### 4.1 Ce que je recommande : une transposition, pas une réécriture

Les briques E à P, sans exception, restent : aucune n'est jetée [V, toutes utilisées ci-dessous]. Les textes de titres et de descriptions, eux, sont à reprendre.

| Reste | Part à la poubelle |
|---|---|
| **Les briques E à P** | les 30 titres de la Garde et de la Sentinelle (Muraille, Cloue-au-mur, Longue-vue...), remplacés par ceux de Charles |
| **Les 24 sorts de voie**, dont 2 changent de classe | les descriptions des nœuds, à réécrire |
| **Les passifs et les règles**, qui changent de nœud | les 27 phrases d'ambiance, sauf réemplois |
| **Les évolutions de 100**, qui deviennent des finales | les mesures de T152, à refaire |
| **Les noms de sorts** | les parties de la quête d'Anatole qui nomment les branches |

### 4.2 Le Guerrier : six lignées transposées une à une

Les 6 finales restantes sont nouvelles, soit 1 point environ.

| Rang 2 | Vient de | Rang 3 | Vient de (sort de 50) | Finale reprise | Finale neuve |
|---|---|---|---|---|---|
| **Chevalier** | Palissade : Interposition, Tenir bon, Serrez les rangs | **Gardien** | Garde-corps : Mur de boucliers | **Croisé** : Pas aujourd'hui, le sauvetage | **Sentinelle** |
| | | **Champion** | Bastion d'épines : Rétorsion, Épines | **Duelliste** : Rétorsion qui touche derrière | **Seigneur de guerre** |
| **Maître d'armes** | Étau : Prise, Verrou, Qui s'y frotte | **Bretteur** | Carcan : Écrasement | **Maître des lames** : Écrasement à +50 % | **Lame fantôme** |
| | | **Tacticien** | Tenaille : Cercle de fer | **Stratège** : Personne ne sort | **Général** |
| **Gladiateur** | Brèche : Assaut, Arrachement, Sur la lancée | **Briseur** | Bélier : Trouée | **Exécuteur** : +1 PM et +2 PA par ennemi achevé | **Destructeur** |
| | | **Combattant** | Croc-de-fer : Harpon | **Champion d'arène** : les murs, 15 par case | **Pugiliste** |

**Le moins naturel :** Bretteur, qui reçoit le Carcan. Écrasement sonne lourd pour un escrimeur. Le sort peut changer de nom sans changer d'effet (scénariste).

**Le kit :** il garde 23 de ses 24 sorts. **Égide** passe au Paladin : c'est son sort-type, et le titre 2.2.2.1 porte le même nom. Le Guerrier reçoit un neutre neuf à la place. Le reste ne bouge pas : c'est ce qui rend les mesures de T152 encore utiles comme point de départ.

### 4.3 L'Archer : plus de changements

Le nouvel arbre ne suit pas l'ancien : la Sente n'a pas d'équivalent, et Artilleur demande une branche neuve.

| Rang 2 | Sorts (origine) | Passif | Rang 3 | Sort | Finales |
|---|---|---|---|---|---|
| **Tireur d'élite** | Tir visé, Point faible (Mire) | Œil calme | **Sniper** | Coup au but | **Œil du dragon** (ex-Dernière semonce) ; **Tir parfait** (neuve) |
| | | | **Chasseur de primes** | **neuf** : désigner une proie | **Traqueur légendaire**, **Exécuteur** (neuves) |
| **Rôdeur** | Mâchoires, Piqué (Affût) | Faucon nourri | **Éclaireur** | Appât | **Pisteur** (+15 % dans une glyphe) ; **Maître forestier** (2 Mâchoires) |
| | | | **Chasseur sauvage** | Chassé-croisé | **Seigneur des bêtes** (2 Faucons) ; **Prédateur ancestral** (Piqué sans relance) |
| **Artilleur** | Tir de barrage (Herse, descend de 50 à 30, budget refait), Coup de talon (Sente) | Talons vifs | **Arbalétrier** | **neuf** : tir lourd ; règle `StillRule` | **Baliste vivante** (ex-Route barrée) ; **Maître arbalétrier** (neuve) |
| | | | **Tireur mobile** | Tir en course ; règle `DanceRule` | **Voltigeur** (ex-Cours-toujours, l'écho) ; **Faucon de guerre** (neuve) |

- **Ce que ce schéma gagne :** Arbalétrier et Tireur mobile s'opposent sur une vraie décision, rester ou bouger. C'est la meilleure lignée double de l'arbre.
- **Ce qui part chez le Voleur :** **Cabriole** et **Feinte** (brique I), pour le Malandrin, à qui elles vont mieux. La Cabriole était jouée 0,1 fois par combat [V, T152] : on ne perd rien de mesuré.
- **Le kit reste entier**, Faucon compris. Les poisons de l'Archer aussi : le Voleur se distingue par le cumul.
- **Le nom à clarifier :** Faucon de guerre (6.3.2.2) est sous Tireur mobile, loin du Faucon du Rôdeur. Je le lis comme un titre (celui qui fond et tire), sans lien avec l'oiseau. **Question ouverte, §8.**

### 4.4 Le Mage : rien n'est codé, donc rien n'est perdu

`data/specs.json` a les textes du Mage, ses 12 sorts ne sont pas écrits [V, §v4 et `spells_gen.py`]. Leur **conception** se réemploie :

| Sorts ou lignées conçus | Destination proposée |
|---|---|
| Comète, Recueillement, Rayon | **Arcaniste** (3.1), Archimage (3.1.2.2) reprenant son rôle |
| Point d'orgue, la réserve de PA du Thaumaturge | **Chronomancien** : garder du temps pour plus tard |
| Malédiction, Fiel, Flétrissure | **Occultiste** (8.3) ou Oracle, selon la conception |
| Miasme | **Empoisonneur** (5.1.2) |
| Lever une ombre, Dîme d'ombre, Détonation, Marche funèbre | **Nécromancien** (8.1, 8.2) |
| Textes de Lève-ombres, Mène-ombres, Sème-ombres | à reprendre pour les textes des sorts, le nom de classe restant Nécromancien |

**Le kit du Mage doit changer.** Sa description dit « soigne ses alliés » ; si on garde Soin, Renouveau, Renaissance et Sanctuaire chez lui, le Prêtre n'a plus de terrain propre. Je propose de les **passer au Prêtre au moment où il arrive**, et de donner au Mage 4 neutres neufs. Renaissance change de nom à cette occasion (`talents-canon.md` §6.2, point 3, le demandait déjà). Le risque est réel : le Mage perd ses soins, donc sa tenue en solo. **À mesurer, et c'est la question Q3.**

### 4.5 Ce que coûte la migration [V]

- **Identifiants de classes :** 464 occurrences de `"sentinel"`, `"guard"`, `"mage"` dans 133 fichiers, surtout des tests et des `Designer.cs` de migration. Un remplacement mécanique en un commit vaut mieux qu'un `guard` qui voudrait dire Guerrier, surtout avec un nœud Sentinelle sous le Guerrier. Les fichiers de migration EF restent intacts.
- **Identifiants des branches :** `data.Specs` est un dictionnaire par identifiant, et les noms ne sont pas uniques (14 doublons). Il faut préfixer par la classe : `warrior-gladiator`, `berserker-gladiator`.
- **Base de données :** migration EF sur `Class`, `Spec`, `Fork` ; héros de plus de 30 ramené à son équivalent, finale à choisir gratuitement. Comptes de test restaurés par la commande d'administration, jamais en SQL.
- **Comptes de test :** déjà accordé par Charles, limite de 10 personnages pour cette phase, un Plouf par classe de base (`MaxCharacters` est à 5 [V]).
- **Page de création :** 10 boutons de 150 px font 1 572 px pour un écran de 1 280 [V, `LobbyView.cs`] : une grille de 5 × 2. Les classes pas encore écrites restent grisées, en « Bientôt ».
- **Page Spécialisation :** 12 tuiles de 44 px font 528 px pour une page de 404 [V, maquette §2.1]. **Solution proposée :** les colonnes 30 et 50 restent entières (1 + 3 + 6), et la colonne 100 ne montre que les **4 finales de la branche ouverte**, soit 250 px. Lecture de gauche à droite gardée, aucun défilement. À revoir par le graphiste.
- **Récit :** Anatole présente « les 3 chemins » ; sa scène doit devenir générique ou se décliner en 10 versions. Garance « forme les trois classes à Clairval » (`talents-canon.md` §1.2) : à reprendre.
- **Tests :** `Progression.Builds` a 3 entrées par classe, soit 30 à terme [V].

---

## 5. Le chiffrage et le découpage

### 5.1 Ton estimation contre la mienne

| Poste | Toi | Moi | Pourquoi |
|---|---|---|---|
| Conception, IA, banc de mesure | 5 | **8** | le banc de mesure n'était pas compté, et l'IA demande plus que les 0,5 de la brique S |
| Moteur 4 rangs, page, conversion, création | 15 | **10** | les finales sans sort évitent de toucher au transfert, et la conversion est surtout mécanique |
| Guerrier, Archer, Mage | 21 | **21** | d'accord sur le total, pas sur l'ordre |
| 7 nouvelles classes | 84 | **76** | à 12 chacune, on paie 7 fois les mêmes mécaniques partagées |
| Mesure croisée, playtest | 5 | **13** | 3 jalons, pas un seul |
| **Nominal** | **125** | **128** | |
| Marge (réglages, reprises, composés) | 0 | **15** | les réglages ont toujours débordé : T150, T151, T152 |
| **Réaliste** | | **143** | |

### 5.2 Le détail des fondations (18 points)

| Lot | Contenu | Pts |
|---|---|---|
| **A. Cadrage** | D96, `PLAN.md`, R2 et R8, tableau des chiffres, gabarits, canon et noms avec le scénariste | 2 |
| **B. IA** | la brique S et sa suite : valeur de la glyphe posée, du saut, de la réserve, du bouclier, de l'invocation ; diagnostic de la Sentinelle à ±22 (T152) | 3 |
| **C. Banc de mesure** | séparation des tests (§7), `Rpg.Sim --matrix`, couverture des sorts, paquets de référence par classe | 3 |
| **D. Moteur à 4 rangs** | `Hero.Final`, `SpecFork.Finals`, validation, `PUT …/spec`, prix, migration, `admin --spec`, gabarit nommé | 3 |
| **E. Page** | finales par branche ouverte, panneau, confirmation ; maquette | 3 |
| **F. Conversion** | identifiants, migration des héros, création à 10 classes, limite à 10, Plouf | 3 |
| **G. Récit** | la quête « Une route à soi » et Anatole, génériques | 1 |

### 5.3 Le coût d'une classe

| Lot, pour une classe sans mécanique lourde | Pts |
|---|---|
| Conception : kit et 21 nœuds, en tableau | 1 |
| Kit de 24 sorts, gestes, icônes, chiffres, description | 1,5 |
| 12 sorts de voie et les effets des rangs 2 et 3 | 2 |
| Rang 4 : 12 gabarits, titres | 1 |
| Moteur propre, hors partagé | 1 |
| IA et tests de la classe | 1 |
| Textes et relecture | 0,5 |
| Mesure et réglage | 1 |
| **Total** | **9** |

### 5.4 L'ordre et les sprints

**Pourquoi Guerrier, Archer, Mage.** Le Guerrier valide les 4 rangs sans rien inventer. L'Archer tranche la question de l'IA (±22). Le Mage éprouve alors le pipeline complet sur un arbre neuf, avec des mécaniques neuves : c'est lui, le vrai pilote, mais il arrive une fois le format sûr. Inverser l'ordre obligerait à faire tourner Garde et Sentinelle sur un format provisoire pendant 2 sprints, et à déboguer le format et la mécanique ensemble.

**Pourquoi le Prêtre en quatrième.** Il complète un groupe à quatre rôles (tenir, frapper de loin, zone, soigner), et il faut le faire en même temps que le changement de kit du Mage. C'est aussi le meilleur point d'arrêt : **un vrai jeu de groupe**.

**Ensuite,** les classes qui réutilisent : Voleur (discrétion, poisons), Berserker (sang), Paladin (soins et boucliers du Prêtre et du Guerrier). Puis Invocateur (invocations commandées, fusion), Nécromancien (réemploi, cadavres), Druide en dernier, qui réutilise presque tout.

| Sprint | Contenu | Pts | Cumul |
|---|---|---|---|
| **S193** | A. Cadrage ; C. Banc de mesure | 5 | 5 |
| **S194** | B. IA ; D. Moteur (début) | 5 | 10 |
| **S195** | D. Moteur (fin) ; F. Conversion ; réduction du budget de combats (D97 : 3 points pris sur la marge, non comptés dans le cumul de cette table) | 7 | 14 |
| **S196** | E. Page ; G. Récit | 4 | 18 |
| **S197** | **Guerrier** | 5 | 23 |
| **S198** | **Archer** | 6 | 29 |
| **S199-S200** | **Mage** : temps, illusions, retrait des soins en attente | 10 | 39 |
| **S201-S202** | **Prêtre** : soins, résurrection, passage des soins du Mage | 11 | 50 |
| **S203** | **Jalon 1** : mesure, balayage des PV, playtest des 4 classes. **Décision de Charles : on continue ?** | 3 | **53** |
| **S204-S205** | **Voleur** : discrétion, poisons cumulés | 11 | 64 |
| **S206-S207** | **Berserker** : sang, forme | 9 | 73 |
| **S208-S209** | **Paladin** : réemploi | 9 | 82 |
| **S210** | **Jalon 2** : mesure, playtest. **Décision de Charles** ; IA des bonds, pièges et sorts d'alliés, banc rangé (D98 : 5 points pris sur la marge, non comptés dans le cumul de cette table) | 9 | **86** |
| **S211-S213** | **Invocateur** : invocations commandées, fusion ; IA des pièges, retraits de PM, tractions et sorts d'alliés (D99 : 3 points pris sur la marge, non comptés dans le cumul de cette table) | 16 | 99 |
| **S214-S215** | **Nécromancien** : cadavres | 11 | 110 |
| **S216-S218** | **Druide** : glyphes partagées, formes | 12 | 122 |
| **S219** | **Jalon 3** : mesure croisée, durée des combats, playtest ; le Disjoint plus dur (D100 : 1 point pris sur la marge, non compté dans le cumul de cette table) | 7 | **128** |
| | Marge, à placer là où les jalons le demanderont | 15 | 143 |

Le S193 actuel (Mage, Archimage, Sorcier, Nécromancien) est remplacé. M1 à M6 passent au banc de mesure et aux jalons.

**Le vrai goulot, à dire franchement.** Les sprints avancent vite en calendrier : S176 à S193 ont tenu en quelques jours (mémoire du projet). Ce qui freine, ce n'est pas d'écrire, c'est de **vérifier** : la suite du cœur (25 à 38 minutes en local), la CI qu'un push annule, et un seul joueur humain pour 10 classes. Le playtest de Charles ne pourra pas évaluer 120 finales ; c'est aussi pour ça qu'il y a des jalons.

**Le coût d'opportunité, que je dois aussi nommer.** 128 points, c'est un cinquième de plus que les 602 points de l'Osmose d'aujourd'hui [V, `projects.json`]. Pendant ce temps, rien ne s'ajoute après le niveau 60, alors que les rangs 4 sont au niveau 100. Si les joueurs plafonnent là où les classes finales ne se voient pas, le jalon 2 est le bon moment pour se poser la question.

---

## 6. Les leviers de réduction, classés

Du meilleur rapport gain et coût au pire. Les points sont pris sur les 128.

| # | Levier | Gain | Ce que ça coûte au jeu | Avis |
|---|---|---|---|---|
| 1 | **S'arrêter à un jalon** : 7 classes, ou 4 | **−42** ou **−75** | les classes restantes restent grisées ; la liste de Charles est incomplète, sans être abandonnée | **Le levier principal.** Rien n'est perdu, tout est reporté |
| 2 | **Rang 4 différé pour les 7 nouvelles classes** : titre et texte, l'effet arrive avec les zones de 60 à 100 | **−7** | un héros de 100 a un titre sans effet ; seuls les comptes de test le verront aujourd'hui | **Réserve n° 1**, à activer au jalon 1 si le rythme déçoit. C'est de la dette, pas une économie |
| 3 | **Kits élémentaires tirés d'un gabarit** : 16 sorts dérivés d'un tableau par portée et zone, seuls les 8 neutres écrits à la main | **−3** | deux classes de même portée se ressemblent davantage ; à interdire pour Guerrier et Berserker | **Réserve n° 2** |
| 4 | **Textes réduits** : pas de phrase d'ambiance au rang 4, descriptions générées depuis les règles | **−2** | un peu moins de saveur ; le bilingue reste obligatoire | **Oui**, si besoin |
| 5 | **Mécaniques optionnelles retirées** : retour dans le temps, contrôle mental | **−1,5** | Voyageur temporel et Dominateur mental deviennent des finales de retraits et de relances | **Oui**, dès que le Mage tend |
| 6 | **Rang 3 sans sort** : une règle seulement | −5 | la décision de tour se réduit à un chiffre : la lignée devient une fausse décision | **Non** |
| 7 | **Rang 4 sans règle** : titre seul, pour de bon | −10 | 120 noms vides ; « classe finale » devient une tromperie | **Non** |
| 8 | **Moins de mesure** | −6 | répète T150 et T152 : des conclusions fausses qui reviennent plus cher ensuite | **Non** |
| 9 | **Fusionner des classes** | jusqu'à −30 | contraire à ta liste : je ne le propose que si tu le demandes | **Non** |

**Les deux leviers que tu citais.**

- **« Classes finales avec passif et titre, sans sort » : déjà compté** dans mes 128. Si tu préfères un sort par finale, c'est **+25 points** (2,5 par classe), un paquet saturé et un transfert à reprendre. Je le déconseille.
- **« Composés générés, pas écrits à la main » : déjà le cas** [V]. Le gain restant est négligeable, de l'ordre de 0,25 point par classe, en prenant des noms entièrement automatiques à la place des 16 gestes.

**Rappel sur l'arbitrage réel.** Avec les leviers 2, 3 et 4, le total nominal tombe à **116**. Ajoutés à la marge, cela donne environ 128 : c'est le plan qui se rapproche de ton 125, mais avec une marge et 3 jalons.

---

## 7. Mesurer l'équilibre à 10 classes

### 7.1 Ce qui explose si on ne change rien [V]

| Test ou outil | À 3 classes | À 10 classes |
|---|---|---|
| `AClasssPaths_AreWorthAlike` | 15 cas × 480 combats × 2 classes = 14 400 combats | 72 000 |
| `AtLevel1_EveryClassDuel` | 3 paires × 600 = 1 800 | 45 paires = 27 000 |
| `AtHighLevels_ClassDuels` | 6 × 40 × 2 = 480 | 90 × 40 × 2 = 7 200 |
| `Training_EveryClass` | 3 000 | 10 000 |
| `Rpg.Sim`, groupes de 2 à 4 | 6 + 10 + 15 = 31 groupes | 55 + 220 + 715 = **990 groupes** |
| `PartyBalanceTests`, 30 combats en tournant | couvre chaque groupe | **ne couvre plus rien** : 30 combats pour 220 trios, le test devient du bruit qui passe tout seul |

Le cœur met déjà 25 à 38 minutes en local. Par simple effet de boucle sur `Real.Classes`, il dépasserait une heure [S, à chronométrer d'abord : c'est la première tâche du lot C].

### 7.2 Trois niveaux, et un budget de combats fixe

**La règle : le budget de combats de la CI ne dépend pas du nombre de classes.** Un test le vérifie.

| Niveau | Quand | Ce qu'il vérifie | Volume |
|---|---|---|---|
| **N0, fumée** | chaque push | chaque classe et branche joue : le combat finit, l'IA n'est jamais refusée, aucun sort ne reste jamais joué | environ 1 000 combats |
| **N1, garde-fous** | CI | planchers et plafonds solo, nœuds de rang 2 à 35 et de rang 3 à 50 contre un échantillon de 8 groupes de 4 et 5, à 15 combats par groupe, ±15 points ; 12 compositions de groupe de référence | **20 000 au plus** (rang 2 : 4 800 ; rang 3 : 8 400 ; solo : 3 000 ; le reste en groupe) |
| **N2, le banc** | aux jalons, et à chaque classe pour elle seule, hors CI | les seuils ci-dessous, à 300 ou 480 combats, mêmes graines | sans limite ; résultats dans `docs/mesures-classes.md` |

À 120 combats, l'intervalle est de ±9 points autour de 50 % : d'où un garde-fou à ±15 en CI. Le seuil de ±8, lui, ne se juge qu'au banc.

**Trois changements d'outil :**

1. **Les duels sortent de la CI à 45 paires** : ils ne sont pas le jeu. On garde un échantillon de 10 paires pour la seule question « le combat finit-il ? ».
2. **La mesure utilise un paquet de référence par classe**, pas seulement le paquet automatique. T152 a montré à quel point celui-ci pèse. Pour un Prêtre, dont les meilleurs sorts ne font pas de dégâts, `LeastWorth` joue sûrement mal [S].
3. **Couverture des sorts.** Chaque sort de voie doit être joué au moins 0,5 fois par combat là où il sert. En dessous, le résultat de son nœud se lit **« non concluant »** : on répare l'IA au lieu de régler le sort. C'est la leçon de la Cabriole (0,1 fois par combat).

### 7.3 Les bornes

**Solo, règle de Charles : 1 à 3 monstres faciles, 6 à 10 durs.**

| # | Mesure | Borne | D'où elle vient |
|---|---|---|---|
| **B1** | Groupes de 1 à 3, héros nu au niveau de la zone, chaque classe | **27 sur 30 en moyenne**, 2 groupes au plus sous 18 sur 30 | test actuel (`BalanceTests`), gardé pour chaque classe, Prêtre et Invocateur compris |
| **B2** | Groupes de 6 et plus, solo | **3 sur 10 au plus** sans équipement, 2 sur 10 avec | test actuel |
| **B3** | Plafond avec composés, contre les hordes | 30 % (T41) | **Déjà dépassé** par les composés sans Terre (Sentinelle à 45, T147). Ce chantier doit être réglé au plus tard au jalon 1, sinon toutes les classes en héritent |
| **B4** | Écart entre classes, solo, groupes de 4 et 5 | héros sans voie entre 20 et 75 points, **écart de 30 points au plus** entre classes de même rôle | proposition [S] : un Invocateur et un Berserker n'ont pas à gagner autant seuls |

**Dans une classe**

| # | Mesure | Borne |
|---|---|---|
| **B5** | Rang 2 à 35 et 45 | ±8 de la moyenne de la classe (seuil actuel) |
| **B6** | Rang 3 à 50 et 60 | 8 points au plus entre les 2 lignées d'une branche, ±8 de la moyenne de la classe |
| **B7** | Gain sur le héros sans voie | **+15 au plus** (fixé, testé) ; **−3 au moins**, et non +3 : T152 montre que +3 n'est pas tenu, et un plancher qu'on ne tient jamais n'est pas un plancher. Un gain négatif se lit « l'IA ou le sort » |
| **B8** | Rang 4 | **Indicatif, hors CI**, sur familles de synthèse : +10 au plus sur le rang 3. Pas de zone pour vérifier mieux |
| **B9** | Composés × lignée | 3 cas par classe (aucun, meilleur, pire) : pas de grille complète. Couple à +20 au plus |

**En groupe : le jeu est fait pour ça**

| # | Mesure | Borne |
|---|---|---|
| **B10** | 12 compositions de référence, à 2 et 3 héros, contre les hordes de `PartyBalanceTests` | Reprend les bandes de §11.2.2 : **à 2 : 50 % pour 6 monstres, 20 % pour 7 et 8 ; à 3 : 90 %, 80 %, 60 % pour 9 et 10** (tolérance de 10 points) |
| **B11** | **Pas de soigneur obligatoire** : 3 héros de dégâts contre les mêmes hordes | tient les planchers de B10. **Sinon le Prêtre devient une taxe de groupe** |
| **B12** | **Apport d'une classe de soutien** : classe et Archer, contre 2 Archers | entre −1,5 et +5 victoires sur 30. M5 plafonnait à +1,5 : je le desserre, car un soutien doit se sentir à 6 monstres et plus. **Écart assumé, à discuter si la mesure gêne** |
| **B13** | Tout le monde apporte quelque chose | aucune classe sous −3 victoires sur 30 en duo avec la référence |
| **B14** | Verrouillage (M6) | l'ennemi perd tous ses PM moins d'un tour sur deux, à 2 Guerriers ; pareil pour les retards du Chronomancien sur un chef |
| **B15** | **Le combat finit** | aucun combat ne touche `RoundLimit` (50), médiane sous 12 tours, y compris Prêtre + Paladin. `Erosion` (10 %) est le garde-fou existant [V], à revérifier car la résurrection et les soins cumulés le mettent à l'épreuve |
| **B16** | Durée réelle | groupe de 3 Invocateurs : tour complet sous un seuil à fixer au jalon 3, joué, pas simulé |

**Ce que la mesure ne dira pas.** Tout cela mesure l'IA et ses paquets. Elle sous-estimera toujours les classes à décision différée : Mage, Chronomancien, Invocateur, Nécromancien, pièges. La règle de T152 reste valable : **non concluant veut dire non concluant**, et le playtest tranche. Pour la même raison, je ne descends aucun seuil de plancher pour faire passer une classe, et j'en réfère à Charles.

---

## 8. Les questions pour Charles

**Q1. Vagues et points d'arrêt.**
- a) **3 jalons de décision, à 4, 7 et 10 classes, chacun jouable, classes à venir grisées à la création (Reco).** Tu peux tout faire sans rien changer, et tu joues avant d'engager la suite
- b) Tout d'une traite, le jalon 3 pour seule mesure croisée : même volume, plus de risque
- c) 4 classes, puis les zones de 60 à 100, puis les autres

**Q2. Le rang 4.**
- a) **Titre, texte et une règle tirée d'un gabarit, sans nouveau sort (Reco)**
- b) Comme a) pour les 3 classes converties ; pour les 7 nouvelles, titre et texte d'abord et effet avec les zones de 60 à 100 (−7 points, dette à payer plus tard)
- c) Un sort par finale (+25 points, paquet saturé, transfert à reprendre) : déconseillé

**Q3. Toucher aux kits et aux PV de l'existant.** Les classes ne partagent aucun sort, donc un Prêtre ne peut soigner sans en prendre au Mage, et la Mage « fragile » a plus de PV que la Garde.
- a) **Soin, Renouveau, Renaissance et Sanctuaire passent au Prêtre à son arrivée, Égide au Paladin ; PV refaits par balayage au jalon 1, après l'IA (Reco).** Coût : un Mage moins solide seul, qu'il faudra reprendre avec ses neutres neufs
- b) Le Mage garde ses soins, le Prêtre soigne seulement mieux : aucun risque de mesure, mais une frontière floue
- c) Sorts déplacés, PV inchangés : le Guerrier reste moins résistant que l'Archer

**Q4. Qui joue les invocations.** Aujourd'hui l'IA les joue, même pour un joueur [V].
- a) **L'IA les joue, le joueur leur donne un ordre : un sort marque leur cible (Reco).** 0,5 point ; le tour reste court
- b) Le joueur les joue toutes : plus d'emprise et un vrai jeu pour l'Invocateur, mais +3 points, des tours bien plus longs dans une partie à 3 ou 4, et le minuteur à revoir
- c) L'IA seule : l'Invocateur regarde jouer

**Q5. Noms et canon.** Je ne touche à aucun nom. Mais la liste contredit le canon écrit, et il faut savoir qui l'emporte. `talents-canon.md` écrit noir sur blanc :
- §2, principe 1 : « Une voie est une tradition humaine, pas un don divin » → Paladin, Prêtre, Templier, Croisé divin, Saint, Élu céleste.
- §3 : les morts relevés sont « une contrainte, pas un soin », et la fusion forcée, c'est le Disjoint et les zombies → Nécromancien, Fusionniste, Avatar fusionnel.
- §5 : paladin, druide, rôdeur, berserker, templier, nécromancien, invocateur, assassin, sage, tireur d'élite sont des noms à éviter, car ce sont des noms d'autres jeux. Les mots de colère étaient interdits à la Garde.
- La bible ne connaît ni démons ni abysses (Démoniste, Chasseur de démons, Élu des abysses, Seigneur infernal), ni dragons (Œil du dragon), ni astres (Invocateur astral).

Options :
- a) **Tes noms l'emportent, et le scénariste réécrit le canon des classes, court : Prêtres et Paladins servent les Primordiaux à leur façon, les démons et les morts se rattachent à la Disjonction (Reco).** Cela peut même servir la trame : un Fusionniste qui fait à petite échelle ce que fait le Disjoint est une très bonne question morale
- b) Tes noms, sans justification dans le monde : plus simple, mais un Prêtre sans dieu se remarque
- c) On revoit certains noms avec le scénariste, sur ta demande seulement

**Trois points de noms, sans urgence, à répondre d'un mot :**

1. **Les 14 noms en double** (Croisé, Champion, Duelliste, Bretteur, Gladiateur, Destructeur, Exécuteur, Inquisiteur, Purificateur, Protecteur, Fanatique, Zélote, Illusionniste, Maître des illusions) : ils gênent peu, puisqu'ils ne se croisent jamais dans un même arbre. Je recommande de les garder ; les identifiants sont préfixés par la classe. Seul point délicat : dans une guilde, « Gladiateur » ne dit pas de quelle classe on parle. L'infobulle affichera donc la classe de base.
2. **Le féminin.** Le jeu n'accorde aucun titre, et le scénariste avait tout choisi épicène (T13). Guerrier, Voleur, Prêtre, Chevalier ne le sont pas. Je recommande une forme féminine facultative par titre, lue d'après l'apparence (`female-*`), environ 2,5 points sur l'ensemble. Sinon, on garde le masculin comme nom de classe, et une héroïne s'affichera « Guerrier ».
3. **Deux noms à préciser :** Sentinelle (1.1.1.2) sous le Guerrier, alors que les anciennes Sentinelles deviennent des Archers ; Faucon de guerre (6.3.2.2) sous Tireur mobile, loin du Faucon. Je les lis comme de simples titres. Environ 25 noms dépassent aussi les 18 signes retenus jusqu'ici (Seigneur des métamorphoses en a 26, compté à la main) : ils tiendront sur 2 lignes, à vérifier à l'écran.

---

## 9. Ce qui a été vérifié, et ce qui ne l'a pas été

**Vérifié dans le code et les données :** chiffres de `classes.json` et formule des PV ; `Effects.cs` (états, effets, déclencheurs) ; `Talents.cs` (liste des règles) ; `Specs.cs` ; `Ai.cs` (valeur des effets, `DecoyPull`) ; `Fight.cs` (morts, invocations, ordre, `RoundLimit`, `Erosion`) ; `spells_gen.py` (barème, jumeaux, gestes) ; 494 entrées dans `spells.json` ; `DataTests`, `BalanceTests`, `TemplateTests`, `PartyBalanceTests`, `Rpg.Sim` (volumes de combats calculés depuis leur code) ; `LobbyView.cs` (largeur des boutons) ; `maquette-specialisation.md` (hauteur de page) ; `talents-canon.md` ; D94, D95, T149 à T152. Les 14 doublons sont comptés à la main sur `classes-liste.md`.

**Supposé, non mesuré :** tous les chiffres d'équilibre du §1.2 et les bornes neuves du §7 (B4, B7, B12, B13) ; le temps réel d'un combat et de la suite à 10 classes ; les coûts en points, estimés d'après S190 à S192, qui étaient eux-mêmes des estimations ; le comportement de l'IA sur les nouvelles classes ; l'affirmation qu'aucune zone n'existe au-delà de 60, tirée de la v4 et d'une recherche sur `monsters.json`, sans parcours des cartes.

**Non lu en entier :** la suite de `elements-specialisations.md` après la v3 (v2 et v1, historiques) et les textes d'Anatole, seulement localisés.
