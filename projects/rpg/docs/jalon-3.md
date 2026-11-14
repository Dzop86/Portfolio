# Jalon 3 : dix classes *(14 novembre 2026, sprint 219, à valider par Charles)*

La story : « En tant que Charles, je joue 10 classes, la mesure croisée et la durée des combats sont dans `REVIEW.md` ; banc hors CI, build livrée. » Avec elle, la seconde story : le Disjoint plus dur, et le plafond de sa mesure rétabli (D100).

Les dix classes sont écrites : Guerrier, Archer, Mage, Prêtre (vague 1) ; Voleur, Berserker, Paladin (vague 2) ; Invocateur, Nécromancien, Druide (vague 3). Chacune a 3 branches à 30, 2 lignées par branche à 50 et 2 finales par lignée à 100, soit 220 classes. `Arena.Writing` est vide.

## 1. La mesure croisée (le banc, N2)

`Rpg.Sim --matrix --simulate 30 --casts` (480 combats par voie, 16 groupes de 4 et 5 monstres, mêmes graines, héros nus, sorts de voie rangés), aux niveaux 35, 45 et 50 : 120 voies, 23 minutes sur 12 cœurs. Les lignes sont dans `docs/mesures-classes.md`.

**Héros sans voie** (points du banc) :

| Classe | 35 | 45 | 50 |
|---|---|---|---|
| Archer | 51,9 | 52,9 | 46,9 |
| Guerrier | 45,6 | 39,8 | 55,4 |
| Mage | 39,8 | 54,8 | 54,0 |
| Prêtre | 20,8 | 29,8 | 29,6 |
| Voleur | 39,4 | 46,0 | 43,8 |
| Berserker | 66,9 | 72,7 | 73,3 |
| Paladin | 49,0 | 54,0 | 51,3 |
| Invocateur | 56,7 | 62,1 | 62,1 |
| Nécromancien | 37,9 | 44,8 | 44,4 |
| Druide | 36,5 | 32,7 | 42,1 |

**Les voies, classe par classe** (12 voies chacune) :

| Classe | Gain moyen sur le héros sans voie | Concluantes | Gain le plus haut |
|---|---|---|---|
| Archer | +12,9 | 9 | +30,4 |
| Guerrier | +16,1 | 6 | +37,7 |
| Mage | +19,2 | 9 | +26,5 |
| Prêtre | +20,8 | 7 | +31,2 |
| Voleur | +4,3 | 1 | +15,8 |
| Berserker | +3,6 | 0 | +23,1 |
| Paladin | +18,4 | 6 | +31,8 |
| Invocateur | +18,4 | 9 | +27,1 |
| Nécromancien | +20,9 | 2 | +27,3 |
| Druide | +27,5 | 2 | +35,0 |

**Les bornes** (`classes-conception.md` §7.3) :

| Borne | État au jalon 3 |
|---|---|
| **B4**, héros sans voie entre 20 et 75 | **tenue de justesse** : du Prêtre à 20,8 (35) au Berserker à 73,3 (50) |
| **B5**, rang 2 à ±8 de la moyenne de la classe (35, 45) | 5 voies sur 60 dehors : Guerrier Chevalier (`palisade`) +11,3 et Maître d'armes (`grip`) −8,5 (35) ; Archer Artilleur (`trail`) −10,6, Berserker Ravageur (`ravager`) +10,8, Paladin Vengeur (`avenger`) −15,0 (45) |
| **B6**, rang 3 (50) : 8 points au plus entre lignées, ±8 de la moyenne | **27 écarts** ; les plus larges au Guerrier (Briseur et Combattant 23,1, Bretteur et Tacticien 18,2, Gardien et Champion 16,7), à l'Invocateur (Dompteur et Fusionniste 20,0), à l'Archer (Arbalétrier et Tireur mobile 18,8) et au Voleur (Traqueur nocturne et Illusionniste 14,2) ; le Changeforme du Druide à −9,1, et à −9,6 à 40 combats par groupe (Q5 du Chaman) |
| **B7**, +15 au plus sur le héros sans voie | **70 voies au-delà, 33 au-delà de +24** ; 10 sous −3 (surtout le Berserker, dont le héros sans voie est le plus fort) |
| Couverture (chaque sort de voie à 0,5 par combat) | **51 voies concluantes sur 120** ; 13 sorts jamais joués : Arrachement, Cercle de fer, Fiole de peste, Lame enduite, Feinte, Cabriole, Coup d'arène, Rappel, Résurrection, Bouclier de la foi, Rempart d'os, Haie, Sol grondant |

**Ce que ça dit :**

- **Les trois classes de la vague 3 tiennent mieux leurs bornes internes que les deux premières vagues** : elles ont été réglées voie par voie au banc dès leur écriture (S211 à S218), là où le Guerrier et l'Archer datent d'avant le banc rangé (S210).
- **B7 n'est plus le bon garde-fou.** Le jalon 2 l'avait déjà dit (Q1, D98) : un héros sans voie est une référence fragile. Le Druide sans voie vaut 36,5 parce que sa force est dans le terrain qu'il partage, d'où des voies à +27,5 en moyenne. La CI compare les voies entre elles, pas au héros sans voie.
- **Les sorts jamais joués sont, à deux exceptions près, des sorts de groupe** : glyphes partagées (Haie, Sol grondant), sorts d'alliés (Résurrection, Bouclier de la foi, Rempart d'os, Rappel), pièges et tractions. Le banc joue seul. Le Voleur et le Berserker restent les classes que l'IA joue le moins bien.

## 2. La durée des combats (B15, B16)

`Rpg.Sim --durations --simulate 60` (nouveau, S219) au niveau 45 : chaque classe seule contre les groupes du banc, trois héros de chaque classe et le Prêtre avec le Paladin contre les hordes de 6 et plus. « ms par tour » : ce que les règles et l'IA prennent pour jouer un tour de tous les combattants, sans le client, sur 12 cœurs, les 60 combats en parallèle.

| Qui | Contre | Gagné | Tours (médiane) | Le plus | À la limite | Combattants au plus | ms par tour |
|---|---|---|---|---|---|---|---|
| Archer | 4 et 5 | 58 % | 6 | 8 | 0 | 7 | 95 |
| Guerrier | 4 et 5 | 42 % | 5 | 7 | 0 | 6 | 47 |
| Mage | 4 et 5 | 58 % | 5 | 7 | 0 | 6 | 73 |
| Prêtre | 4 et 5 | 35 % | 6 | 8 | 0 | 6 | 55 |
| Voleur | 4 et 5 | 50 % | 4 | 6 | 0 | 6 | 98 |
| Berserker | 4 et 5 | 73 % | 5 | 6 | 0 | 6 | 33 |
| Paladin | 4 et 5 | 58 % | 6 | 8 | 0 | 6 | 47 |
| Invocateur | 4 et 5 | 65 % | 5 | 7 | 0 | 12 | 140 |
| Nécromancien | 4 et 5 | 43 % | 5 | 7 | 0 | 8 | 75 |
| Druide | 4 et 5 | 30 % | 5 | 6 | 0 | 6 | 52 |
| 3 Archers | 6 et plus | 100 % | 4 | 9 | 0 | 18 | 172 |
| 3 Guerriers | 6 et plus | 100 % | 4 | 9 | 0 | 13 | 33 |
| 3 Mages | 6 et plus | 100 % | 3 | 9 | 0 | 13 | 123 |
| 3 Prêtres | 6 et plus | 98 % | 5 | 17 | 0 | 13 | 84 |
| 3 Voleurs | 6 et plus | 90 % | 4 | 10 | 0 | 13 | 195 |
| 3 Berserkers | 6 et plus | 100 % | 3 | 6 | 0 | 13 | 112 |
| 3 Paladins | 6 et plus | 83 % | 5 | 11 | 0 | 13 | 107 |
| **3 Invocateurs** | 6 et plus | 100 % | 4 | 10 | 0 | **27** | **810** |
| 3 Nécromanciens | 6 et plus | 100 % | 4 | 9 | 0 | 17 | 523 |
| 3 Druides | 6 et plus | 87 % | 4 | 9 | 0 | 13 | 235 |
| Prêtre + Paladin | 6 et plus | 52 % | 6 | 11 | 0 | 11 | 122 |

- **B15, le combat finit : tenue.** Aucun combat ne touche la limite de 50 tours ; médianes de 3 à 6 tours (borne : 12), Prêtre + Paladin compris (6). Le plus long : 17 tours, 3 Prêtres.
- **B16, la durée réelle : à fixer.** Trois Invocateurs mettent 27 combattants sur la carte (3 créatures chacun, la limite de D100). Leur tour complet coûte **0,8 seconde de calcul** de l'IA, quatre fois plus que les autres trios hors Nécromanciens, et cela vient avant les animations. Dans le jeu, l'IA ne joue que les monstres et les créatures (le joueur joue son héros) : c'est donc un temps d'attente que le joueur voit. Je propose un seuil de **1 seconde de calcul par tour** sur cette machine (Q3). Tenu aujourd'hui, mais sans marge.
- **Ce que je n'ai pas mesuré** : la durée réelle avec le client, ses animations et le réseau. Godot ne tourne chez moi que sans écran ; le playtest en groupe est pour Charles, avec la build de ce sprint.

## 3. Ce que j'ai joué

L'auto-test de la ville (`--town-selftest`, sans écran, dans l'image `godot-env`) passe avec les dix classes. La CI joue la création, la page Voie de chaque classe et un combat sur les trois OS. Le reste est pour ton playtest.

## 4. Le Disjoint plus dur (D100)

Depuis l'IA de D99 (sprint 211), un héros seul tenait le Disjoint à distance pendant qu'il s'usait : 98,75 victoires sur 100 en moyenne, le plafond de 90 levé en attendant ce jalon.

- **La Langue happante** (`snatching-tongue`, à lui seul) : 4 PA, de 2 à 5 cases en ligne, 14 à 18 dégâts d'Eau, ramène de 3 cases, relance 2. Ses autres coups ne portent qu'à 2 cases. La langue écrasante de la grenouille (`tongue-crush`) n'est pas touchée.
- **480 PV** (450). Avec la langue seule, la moyenne ne descendait qu'à 93 : l'Archer passait de 100 à 80 et le Prêtre de 98 à 60, mais les classes au contact le battaient toujours. À 520 PV, l'écart explosait (le Voleur à 0, le Mage à 12).
- **Mesure** (`BalanceTests`, 40 combats par classe, panoplie d'artisan) : Archer 75, Guerrier 100, Mage 32, Prêtre 95, Voleur 65, les cinq autres 100 ; **moyenne 86,7, plafond de 90 rétabli**, plancher de 30 par classe gardé. **Le Mage, à 32, est près du plancher** : une donnée qui bouge pourra faire échouer ce test.
- Tests : `DisjointTests.ItsTongue_DragsBackToItsClaws_TheHeroWhoKeepsItAway`, `…ItsTongue_IsItsOwn_AndReachesFartherThanItsOtherBlows`.

## 5. À trancher par Charles

- **Q1. Les écarts dans les classes** (B5, B6) : a) **un sprint d'équilibrage des deux premières vagues, réglées avant le banc rangé : Guerrier, Archer, Paladin, Voleur, puis le Dompteur et le Fusionniste de l'Invocateur (Reco)** ; b) laisser le playtest trancher, sans régler au banc ; c) régler classe par classe, au fil des retours.
- **Q2. B7** (+15 sur le héros sans voie) : a) **la retirer des bornes, comme la CI l'a fait au jalon 2 : comparer les voies entre elles (Reco)** ; b) la garder et baisser 70 voies.
- **Q3. La durée d'un tour** (B16) : a) **seuil de 1 seconde de calcul par tour de 3 Invocateurs, mesuré par `--durations` à chaque jalon, et la limite de 3 créatures gardée (Reco)** ; b) 2 créatures au plus par héros ; c) un seuil sur la durée réelle, à mesurer en playtest.
- **Q4. Les 13 sorts jamais joués**, presque tous de groupe : a) **un banc de groupes (2 et 3 héros) qui compte leurs lancers, avant de toucher l'IA (Reco)** ; b) un sprint d'IA sur les sorts d'alliés, les pièges et les tractions.
- **Q5. La suite** : a) **l'équilibrage (Q1) et le banc de groupes (Q4), puis les zones de 60 à 100 (Reco)** ; b) les zones de 60 à 100 d'abord : les rangs 4 y prennent leur sens ; c) autre chose, après ton playtest.
