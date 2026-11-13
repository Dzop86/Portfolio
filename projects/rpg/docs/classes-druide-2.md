# Le Druide, deuxième partie : le Gardien de la nature et le Métamorphe *(13 novembre 2026, sprint 217, à valider par Charles)*

Ce document prépare la story du sprint 217 : « En tant que Druide, je deviens Métamorphe ou Gardien de la nature, avec les formes ; xUnit. » Les finales et les bornes du banc viennent avec, comme pour les autres classes. Il part de `classes-conception.md` (§1.3 « Gardien de la nature : terrain qui protège ; Métamorphe : formes, au contact », §1.4 « La forme du Druide est un choix réversible, sans coût en PV ; celle du Berserker se paie et ne se quitte pas »), de `classes-liste.md` §7.1 et §7.2, et de la première partie (`classes-druide.md`). Le Chaman vient au sprint 218 : `Arena.Writing` donne 2 branches au Druide jusque-là.

Charles a demandé d'enchaîner les sprints restants : les choix ci-dessous sont mes recommandations, appliquées ; les questions du §6 restent à trancher.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Le Gardien de la nature fait durer son terrain** : ses glyphes, celles du kit comme les siennes, durent un tour de plus (la règle `glyph-turns` de l'Embuscade, S186 [V]), des deux côtés d'une glyphe partagée. Le Sylvain frappe ceux qui s'y tiennent ; le Protecteur protège ceux qui s'y abritent.
- **Le Métamorphe prend une forme qui passe** : Forme sauvage donne 20 % de dégâts pendant 3 tours, ne coûte aucun PV, s'éteint et revient (relance 4). Elle ouvre ses coups au contact, comme la Rage du Berserker ouvre les siens (`form`, S206 [V]). Celle du Berserker dure tout le combat et se paie de 10 PV.
- **Aucune brique neuve dans le moteur.** La forme réutilise le statut `Rage`, pour 3 tours au lieu de 99. **Une correction de l'IA** : elle comptait une forme de 3 tours comme la Rage du Berserker, à 12 points, moins qu'un coup, et ne la prenait presque jamais (0,1 fois par combat) ; une forme qui passe vaut désormais son bonus sur les coups du héros pendant sa durée (§3).

## 1. Le Gardien de la nature (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Gardien de la nature** (`nature-warden`) | ses glyphes durent **un tour de plus** (`glyph-turns`) | **Haie** (`path-hedge`, 3 PA, 1-4, croix de 1, **partagée** 2 tours : ennemis −1 PM, alliés +20 % de résistance, relance 2) ; **Écorce** (`path-barkskin`, 2 PA, 0-4, bouclier de 20 sur un allié, 2 tours, relance 2) |

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Sylvain** (`sylvan`) | +20 % sur un ennemi debout sur l'une de ses glyphes (`glyph-target`, la Terre brûlée des talents [V]) ; **Colère des bois** (`path-woods-wrath`, 5 PA, 2-4, cercle de 1, relance 2) | **Seigneur des bois** (`woods-lord`, Trace : Colère des bois laisse un terrain partagé, 2 tours : ennemis −1 PM, alliés +10 % de dégâts) ; **Ancien sylvestre** (`sylvan-elder`, Faster : Colère des bois relancée en 1 tour) |
| **Protecteur** (`grove-keeper`) | ses boucliers sur ses alliés valent 25 % de plus (`ally-shield`, le Rempart du Guerrier [V]) ; **Branche gardienne** (`path-guarding-bough`, 4 PA, 1-4, un coup qui lui donne un bouclier de 12, 2 tours, relance 2 ; §4) | **Gardien ancestral** (`ancestral-warden`, Extends : Branche gardienne donne aussi +8 % de résistance, 2 tours) ; **Esprit de la forêt** (`forest-spirit`, Faster : Branche gardienne relancée en 1 tour) |

L'id `protector` est la branche du Paladin : le Protecteur du Druide est `grove-keeper` (Grove Keeper). Le terrain laissé par le Seigneur des bois s'ajoute après la règle du Gardien : il dure 2 tours, pas 3.

## 2. Le Métamorphe (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Métamorphe** (`shapeshifter`) | aucune : la forme est son sort | **Forme sauvage** (`path-wild-shape`, 2 PA, sur soi, **Rage de 20 % pendant 3 tours, sans PV**, relance 4) ; **Griffes** (`path-claws`, 3 PA, au contact, deux fois par tour, **en forme**) |

| Lignée | Sort | Finales (gabarit) |
|---|---|---|
| **Changeforme** (`changeform`) | **Bond du fauve** (`path-pounce`, 4 PA, 2-4 en ligne, bond, croix de 1 où il retombe, relance 2, **en forme**) | **Roi des bêtes** (`beasts-king`, Extends : −1 PM aux ennemis touchés, 1 tour) ; **Prédateur primordial** (`primal-predator`, Faster : Bond du fauve relancé en 1 tour) |
| **Druide sauvage** (`wild-druid`) | **Piétinement** (`path-trample`, 4 PA, tout autour de lui, qu'il épargne, **en forme**, et un terrain **partagé** 2 tours : ennemis −1 PM, alliés +15 % de résistance, relance 2) | **Avatar animal** (`animal-avatar`, Extends : +10 % de dégâts au Druide, 2 tours) ; **Seigneur des métamorphoses** (`shape-lord`, Faster : Piétinement relancé en 1 tour ; §4) |

**Réversible** (§1.4 de la conception) : la forme s'éteint au bout de 3 tours ; ses coups se ferment jusqu'à la suivante, une relance de 4 laissant un tour sur quatre sans forme. Le Druide sauvage garde son terrain en forme : son Piétinement pose une glyphe partagée qui l'accueille aussitôt (la règle `welcomes` de S216). Aucun sort du Métamorphe ne coûte de PV ; tous ses coups demandent la forme.

## 3. Ce qui sépare les branches

| Branche | Son terrain | Sa forme | Vérifié |
|---|---|---|---|
| Gardien de la nature | dure un tour de plus, des deux côtés | aucune | `DruidBranchesTests.TheNatureWardensGlyphs_LastATurnMore_…` |
| Métamorphe | le Piétinement du Druide sauvage seulement | 3 tours, sans PV, réversible | `DruidBranchesTests.TheWildShape_OpensTheFormsBlows_…` |
| Berserker (Sauvage) | aucun | tout le combat, 10 PV | `…AndWhatSetsItsFormApart_FromTheBerserkers` |

**La correction de l'IA** [V, `Ai.Value`] : une Rage de moins de 99 tours (seule la Forme sauvage en pose une) vaut le bonus qu'elle donne à un tour de coups du héros (`RoundOf`), multiplié par sa durée ; au niveau 40, plus de 12. La Rage du Berserker et la forme de l'Hémarque gardent leur estimation : leurs combats ne changent pas (empreintes inchangées).

## 4. Mesures

Banc (`Rpg.Sim --matrix --class druid --simulate 30`, sorts de voie rangés, les 16 groupes de 4 et 5 monstres, 480 combats par voie ; `--level 60` et `--level 100` pour ces niveaux).

| Niveau | Héros sans voie | Gardien de la nature | Métamorphe | Écart à la moyenne |
|---|---|---|---|---|
| 35 | 36,5 | 63,8 | 60,4 | ±1,7 |
| 45 | 32,7 | 65,0 | 62,5 | ±1,3 |
| 50 | 42,1 | Sylvain 73,1, Protecteur 68,1 | Changeforme 58,8, Druide sauvage 65,2 | +6,8, +1,8, −7,5, −1,1 |
| 60 | 17,1 | Sylvain 53,8, Protecteur 52,5 | Changeforme 58,5, Druide sauvage 56,0 | −1,4, −2,7, +3,3, +0,8 |
| 100 | 15,6 | Sylvain 41,7 (finales 42,5 et 49,2), Protecteur 44,0 (44,2 et 50,0) | Changeforme 47,1 (47,1 et 47,1), Druide sauvage 37,3 (39,4 et 35,8) | de −7,9 à +6,2 |

**Ce que l'IA joue** (par combat) : Forme sauvage et Griffes au-dessus de 0,5 à 35 et 45 (la branche est concluante) ; Écorce 0,5 ; Bond du fauve et Piétinement 0,3 à 0,4 ; **Haie 0,0**, comme le Cercle de ronces du kit : une glyphe partagée sert un groupe, et le banc joue seul (ajoutée à `CiLevelsTests.Unplayed`).

**Les réglages en route.**

- **La forme** n'était presque jamais prise (0,1 à 0,2 par combat, Griffes 0,1) : l'IA l'estimait comme la Rage du Berserker, 0,05 × 20 % × 3 tours × 4 = 12, moins qu'un coup. Corrigée dans l'IA (§3), pas dans le sort (la règle du banc, T152).
- **Le Protecteur** à −10,2 de la moyenne au niveau 60 et −10,5 au 100 : son **Rempart de bois** (bouclier de 18 en cercle autour d'un allié, 3 PA, relance 3) n'était jamais lancé seul (0,0 à 0,1), et sa règle ne touche pas le Druide lui-même. Remplacé par la **Branche gardienne**, un coup qui le protège (la Frappe gardienne du Paladin) : −2,7 au 60, +0,2 au 100. La règle de groupe reste sur l'Écorce.
- **Le Gardien ancestral** à +15 % de résistance à chaque Branche : +11,6 de la moyenne au 100, 12 points au-dessus de sa lignée. À **8 %** : +0,4.
- **Le Seigneur des métamorphoses** (Forme sauvage relancée en 3 tours) faisait 5 points de moins que sa lignée : l'IA reprenait la forme plus souvent, et ces 2 PA coûtaient un coup. Il relance le **Piétinement en 1 tour** : 1,5 point sous sa lignée, dans le bruit.

**Les bornes (§7.3 de la conception).**

- **B5** (rang 2 à 35 et 45, ±8 de la moyenne) : **tenue**, ±1,7 au plus.
- **B6** (rang 3 à 50 et 60, 8 points au plus entre lignées, ±8 de la moyenne) : **tenue** ; à 50, 5,0 points entre les lignées du Gardien et 6,4 entre celles du Métamorphe, de −7,5 à +6,8.
- **B8** (rang 4, indicatif) : les finales restent à 7,5 points au plus de leur lignée (l'Ancien sylvestre +7,5, l'Esprit de la forêt +6,0). Le Prédateur primordial et le Roi des bêtes ne changent pas le banc (Bond du fauve 0,3 par combat) ; le Prédateur et le Seigneur des métamorphoses (Piétinement 0,3 à 0,4) ne changent aucun combat de la fumée : ajoutés à `CiLevelsTests.Unchanged`.
- **Le budget de combats** (D96) : la suite Cœur en joue 19 933 sur 20 000 (19 833 au sprint 216). Le Chaman, au sprint 218, doit le surveiller.
- **B4** (héros sans voie entre 20 et 75) : inchangée depuis S216, pas tenue à 60 ni à 100.

**Empreintes** : celles des neuf autres classes et du Druide sans voie ne bougent pas (`TeamAiTests`) : la correction de l'IA ne touche que les formes de moins de 99 tours.

## 5. Ce qui vient ensuite

- **Sprint 218** : le Chaman (esprits sans corps, terrain qui frappe : Invocateur spirituel, Élémentaliste naturel), ses finales ; le Druide a ses trois branches, `Arena.Writing` est vide.
- **Sprint 219** : le jalon 3.

## 6. Questions à Charles

- **Q1. Les noms** des 8 sorts et des 14 rangs anglais (Grove Keeper, Changeform, Lord of Shapes) : noms de travail, au scénariste (Reco).
- **Q2. La forme** : a) **3 tours, relance 4, sans PV (Reco, fait)** ; b) une forme qu'on quitte par un sort, sans durée ; c) une forme qui dure tant qu'on frappe au contact.
- **Q3. La correction de l'IA** (§3) : a) **pour les formes qui passent seulement (Reco, fait)** ; b) aussi pour la Rage du Berserker (ses combats changent, à remesurer).
