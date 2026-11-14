# Le Druide, troisième partie : le Chaman *(14 novembre 2026, sprint 218, à valider par Charles)*

Ce document prépare la story du sprint 218 : « En tant que Druide, je deviens aussi Chaman, et mes finales sont écrites ; xUnit, bornes du banc. » Il part de `classes-conception.md` (§1.3 « Chaman : esprits sans corps, terrain qui frappe », §1.4 « Les esprits du Chaman aident sans occuper de case et durent peu ; les créatures du Conjurateur bloquent le terrain », §3 « Runes, auras, pièges, esprits : `Incorporeal`, rien à ajouter »), de `classes-canon.md` (« Le Chaman parle aux esprits » ; un esprit est « ce qu'un lieu se rappelle », jamais un dieu mineur), de `classes-liste.md` §7.3 et des deux premières parties (`classes-druide.md`, `classes-druide-2.md`).

Charles a demandé d'enchaîner les sprints restants : les choix ci-dessous sont mes recommandations, appliquées ; les questions du §6 restent à trancher.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Le Chaman appelle des esprits qui passent** : un Esprit ne tient aucune case (on le traverse), ne cache aucune ligne de vue, joue 2 tours puis s'efface. Il est nourri par la voie (15 % des PV du Druide, 35 % de ses caractéristiques), comme les créatures de l'Invocateur.
- **Son terrain frappe** : Sol grondant pose une glyphe partagée, 2 tours. Elle frappe les ennemis qui s'y tiennent et donne +10 % de dégâts aux alliés.
- **Une brique dans le moteur, petite** : une invocation peut porter une **durée** (`lasts`, en tours de jeu) et être un **esprit** (`spirit` : le statut `Incorporeal` [V] pour toute sa vie). Ses tours épuisés, elle s'efface au début du suivant : l'événement `Died` existant, sans passif de mort, sans cadavre pour le Nécromancien (une invocation n'en laisse pas [V]), ses glyphes avec elle. **Aucune correction de l'IA.**

## 1. Le Chaman (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Chaman** (`shaman`) | ses Esprits prennent 15 % de ses PV et 35 % de ses caractéristiques (`summon-share`) | **Esprit** (`path-spirit`, 3 PA, 1-3, sans ligne de vue, 2 au plus, relance 3) ; **Sol grondant** (`path-rumbling-ground`, 3 PA, 1-4, croix de 1, **partagé** 2 tours : ennemis 6 à 9 dégâts, alliés +10 % de dégâts, relance 2) |

**L'Esprit** (`spirit`, `data/summons.json`) : 6 PA, 3 PM, initiative 75, **Toucher de l'esprit** (3 PA, 1-3 cases, 5 à 8, deux fois par tour, dans l'élément le plus fort de son Druide). Il porte le modèle du loup, réduit à 65 % : aucun modèle d'esprit n'existe (la limite des modèles, §3 de la conception). Cela se verra jusqu'à la refonte artistique.

## 2. Les lignées (niveau 50) et les finales (niveau 100)

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Invocateur spirituel** (`spirit-caller`) | 3 Esprits au plus (`patch` : `maxSummons`), qui prennent 20 % de ses PV et 40 % de ses caractéristiques ; **Communion** (`path-communion`, 2 PA, sur lui, cercle de 2 : alliés +20 % de dégâts, 2 tours, et +1 PM, 1 tour, relance 3 ; §4) | **Maître des esprits** (`spirit-master`, Extends : Communion donne aussi +2 PA, 1 tour) ; **Ancêtre vivant** (`living-ancestor`, Faster : Communion relancée en 1 tour) |
| **Élémentaliste naturel** (`nature-elementalist`) | +15 % sur un ennemi debout sur l'une de ses glyphes (`glyph-target`, comme le Sylvain [V]) ; **Tempête vivante** (`path-living-storm`, 4 PA, 2-5, cercle de 1, et une glyphe qui frappe les ennemis, 2 tours, relance 2 ; 0,45 du barème, §4) | **Seigneur des éléments** (`elements-lord`, Extends : −10 % de résistance aux ennemis touchés, 2 tours) ; **Avatar primordial** (`druid-primal-avatar`, Faster : Tempête vivante relancée en 1 tour) |

Les ids `elemental-lord` (Seigneur élémentaire) et `primal-avatar` (de l'Élémentaliste, sous le Mage) existent déjà : ceux du Druide sont `elements-lord` et `druid-primal-avatar`.

## 3. Ce qui sépare le Chaman

| Ce qu'il fait | Le Chaman | Ce qui s'en approche | Vérifié |
|---|---|---|---|
| Ses créatures | sans corps, 2 tours, puis s'effacent | les Piliers du Conjurateur bloquent le terrain et restent | `DruidShamanTests.WhatSetsTheSpiritsApart_FromTheSummonersCreatures` |
| Son terrain | frappe les ennemis, pousse les alliés | le Gardien de la nature le fait durer ; le Rôdeur ne frappe que les ennemis | `…TheRumblingGround_StrikesTheEnemies_AndSpursTheAllies_ItIsCastOn` |
| L'esprit en combat | intangible, ne cache rien, joue 2 tours, s'efface, se rappelle | | `…TheSpirit_HoldsNoCell_HidesNothing_PlaysTwoTurns_ThenFades` |

## 4. Mesures

Banc (`Rpg.Sim --matrix --class druid --simulate 30 --casts`, sorts de voie rangés, les 16 groupes de 4 et 5 monstres, 480 combats par voie ; `--level` pour chaque niveau). La nouvelle option `--casts` donne les lancers de chaque sort de voie par combat.

| Niveau | Héros sans voie | Chaman | Écart à la moyenne de la classe |
|---|---|---|---|
| 35 | 36,5 | 64,0 | +1,3 |
| 45 | 32,7 | 67,7 | +2,6 |
| 50 | 42,1 | Invocateur spirituel 73,5, Élémentaliste naturel 68,5 | +5,7, +0,7 |
| 60 | 17,1 | Invocateur spirituel 55,6, Élémentaliste naturel 61,0 | −0,6, +4,8 |
| 100 | 15,6 | Invocateur spirituel 34,4 (finales 36,5 et 38,3), Élémentaliste naturel 41,5 (41,7 et 42,3) | de −7,8 à +0,1 |

**Ce que l'IA joue** (par combat) : Esprit 1,0 à 1,3 jusqu'au niveau 60, 0,3 à 1,0 au 100 ; Communion 0,4 au 50, 1,2 au 60 et 100 (1,9 pour l'Ancêtre vivant) ; Tempête vivante 0,3 au 50, 0,7 à 0,8 ensuite ; **Sol grondant 0,0 à 0,1**, comme la Haie et le Cercle de ronces : une glyphe partagée sert un groupe, et le banc joue seul.

**Les réglages en route.**

- **Le Chaman de base, sans la Tempête** (son coût porté à 99 PA le temps d'une mesure), est déjà à la moyenne de la classe au niveau 60 (−0,2 et +2,5) : ses Esprits, nourris par la voie, valent un sort de lignée. Leur part passe de 20 % des PV et 40 % des caractéristiques à **15 % et 35 %**, celle de l'Invocateur spirituel restant à 20 % et 40 %. Esprit est relancé en **3 tours** au lieu de 2.
- **La Communion** visait un Esprit (`aims`), comme la Surcharge du Conjurateur. Un Esprit ne vivant que 2 tours, l'IA ne la lançait que 0,2 fois par combat au niveau 60, et l'Invocateur spirituel finissait **3 points sous le Chaman de base** (Q4). Elle devient une aura autour du Chaman (cercle de 2, +20 % de dégâts et +1 PM, relance 3) : 1,2 lancer par combat.
- **La Tempête vivante** au plein barème plaçait l'Élémentaliste naturel 14 à 17 points au-dessus de la moyenne, aux niveaux 60 et 100. Allonger sa relance ou son coût n'y changeait rien : le barème paie chaque tour de relance (+25 %) et chaque PA en dégâts. Elle passe à **0,45 du barème** (5 à 7 dégâts au rang 1), relance 2 : +4,8 au 60, −0,8 au 100. Le seuil sur glyphe (+15 %) ne change pas le banc (mesuré à 0 : un point d'écart).

**Les bornes (§7.3 de la conception).**

- **B5** (rang 2 aux niveaux 35 et 45, ±8 de la moyenne) : **tenue**, +1,3 et +2,6.
- **B6** (rang 3 aux niveaux 50 et 60, 8 points au plus entre lignées, ±8 de la moyenne) : **tenue pour le Chaman**, 5,0 points entre ses lignées au 50, 5,4 au 60. **Le Changeforme (S217) passe en dehors** : à −9,1 au niveau 50 (−7,5 au sprint 217), parce que les lignées du Chaman relèvent la moyenne de la classe. C'est dans l'intervalle d'une ligne (±4,5), mais hors borne (Q5).
- **B8** (rang 4, indicatif) : les finales du Chaman restent à 3,9 points au plus de leur lignée (l'Ancêtre vivant, sa Communion lancée 1,9 fois). Celles de l'Élémentaliste naturel changent peu le banc (+0,2 et +0,8) : la Tempête n'est lancée que 0,7 à 0,8 fois par combat.
- **Le budget de combats** (D96) : la fumée se partage un budget fixe ; ses héros de plus ne la font pas grossir (§ suite complète, `FightBudget`).

**Empreintes** : celles des dix classes sans voie ne bougent pas (`TeamAiTests`) : la brique ne touche que les invocations qui portent une durée, et seul l'Esprit en porte une.

## 5. Ce qui vient ensuite

- **Sprint 219** : le jalon 3. Le Druide a ses trois branches, et `Arena.Writing` est vide : toutes les classes sont écrites.

## 6. Questions à Charles

- **Q1. Les noms** des 4 sorts, de l'Esprit et des 7 rangs anglais (Spirit Caller, Nature Elementalist, Lord of the Elements) : des noms de travail, à faire relire par le scénariste (Reco).
- **Q2. La durée d'un esprit** : a) **2 tours, puis il s'efface (Reco, fait)** ; b) 3 tours, avec un esprit de moins ; c) jusqu'à ce que le Chaman en appelle un autre.
- **Q3. Le corps de l'Esprit** : a) **le loup réduit, en attendant la refonte artistique (Reco, fait)** ; b) un modèle humain teinté, comme l'Élémentaire (`male-e`) ; c) un rendu translucide côté client (un chantier de rendu, non chiffré).
- **Q4. La Communion** : a) **une aura autour du Chaman, lui et ses Esprits (Reco, fait)** ; b) un sort sur un seul Esprit (`aims`), comme la Surcharge du Conjurateur : ainsi, elle n'était lancée que 0,2 fois par combat au niveau 60, et l'Invocateur spirituel finissait 3 points sous le Chaman de base.
- **Q5. Le Changeforme au niveau 50** (−9,1 de la moyenne, hors de la borne B6) : a) **l'accepter et le remesurer au jalon 3, à 40 combats par groupe (Reco)** ; b) rendre le Bond du fauve plus souvent joué (0,4 par combat), au sprint 219.

**Réponses de Charles (14 novembre 2026) :** Q1 à Q5, les recommandations. Les noms restent des noms de travail, à faire relire par le scénariste ; un esprit dure 2 tours et porte le loup réduit ; la Communion reste une aura ; le Changeforme sera remesuré au jalon 3.
