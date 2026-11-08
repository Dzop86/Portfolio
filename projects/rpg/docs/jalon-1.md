# Jalon 1 : quatre classes *(7 novembre 2026, sprint 203, à valider par Charles)*

La story : « En tant que Charles, je joue 4 classes, les PV sont refaits par balayage, la mesure est dans `REVIEW.md`, et je décide si on continue ; banc hors CI, build livrée. » Charles a déjà dit de continuer jusqu'au sprint 219 et de ne livrer qu'au 219 : la décision de ce jalon est prise, la build attend.

## 1. Les PV, balayés vers la conception

`classes-conception.md` §1.2 donnait une cible aux trois classes converties, pour que **le Guerrier cesse d'être moins résistant que l'Archer** et que **le Mage fragile** ait moins de PV que le Guerrier. Mesuré au banc (`Rpg.Sim --matrix --simulate 30`, héros nu, sans voie) et par les exigences en solo de la CI :

| Classe | Avant | Essayé | Retenu | Ce que l'essai a montré |
|---|---|---|---|---|
| Guerrier | 74 + 3 | 74 + 5 | **74 + 5** | banc 24,0 → 37,9 à 35 ; 32,3 → 50,2 à 50 ; 20,8 → 47,3 à 100. Il bat le Disjoint seul 100 fois sur 100 (41 avant) |
| Mage | 86 + 3 | 72 + 3 | **86 + 3** (inchangé) | à 72, il ne gagne plus que 320 entraînements sur 1000 (la CI en veut 700) et 198 sur 300 dans la grande salle des Racines. Le Guerrier à 74 + 5 le dépasse dès le niveau 7 : l'objectif est atteint sans lui |
| Archer | 62 + 5 | 62 + 4 | **62 + 5** (inchangé) | à + 4, il perd 12 points à 50 (44,2 → 31,9) ; 62 + 5 reste sous 74 + 5 |
| Prêtre | 66 + 3 | 66 + 5 | **66 + 3** (inchangé) | à + 5, pas un combat de plus à 100 (5 sur 30) : il lui manque des coups, pas des PV |

**Le Guerrier sans voie dépasse maintenant beaucoup de ses lignées à 100** (Rempart 31,5, Tenaille 32,5 contre 47,3 sans voie) : elles avaient été réglées avec ses PV d'avant. À reprendre avec la mesure en groupe (Q2).

## 2. Le Prêtre de 100

Seul, il marque 6,7 au banc de 100 sans voie, quand le Guerrier en fait 47,3. Ce que j'ai cherché, sur les groupes de 4 et 5 de synthèse (30 combats) :

| Essai | Victoires sur 30 | Ce que ça dit |
|---|---|---|
| Tel quel | 5 à 6 | inflige 79 dégâts par tour, le Mage 131 ; se soigne de 78 PV par combat pour 961 subis |
| Sans ses soins | 1 | ses soins l'aident |
| Tous ses sorts à 1 case | 5 | sa portée minimale n'y est pour rien |
| 66 + 5 PV | 5 | ses PV non plus |
| Bouclier du soin en trop compté à moitié | 5 | l'IA ne se blindait pas pour rien |
| Budget 0,8 → 0,9 | banc 6,7 → 9,2 | un peu ; mais le Prêtre de 8 ne gagne plus la grande salle de la Bauge que 68 fois sur 100 (la CI en veut 70) : **rejeté** |
| Soins multipliés par (100 + niveau) % | 5 | **retenu** : un soin suit le niveau comme les rangs font suivre un coup ; seul, il pèse peu |

**Effet de bord** : avec ses soins qui suivent le niveau, le Prêtre de 45 profite plus des pièces d'artisan de 40 et 45 contre les hordes de la Forêt (+ 2,6 victoires sur 10, plafond par classe de la CI passé de 2,5 à 3 ; la moyenne des quatre, 1,4, reste sous 1,5).

**Ce qui reste** : seul, le Prêtre consacre environ 30 % de ses PA au soutien, et un soin ne tue rien. Ses voies l'aident (Croisé de la foi, Chasseur de démons, Purificateur entre 25 et 32). En groupe, il vaut le Mage (sprint 201). Je recommande de le juger en groupe (Q1).

## 3. Ce que j'ai joué

Rien en vrai : Godot ne tourne chez moi qu'en tâche de fond, sans écran (règle de Charles). L'auto-test Godot de la CI joue la création d'un Prêtre, la page Spécialisation de chaque classe et un combat. Le playtest des 4 classes est à faire par Charles avec la build du sprint 219.

## 4. À trancher par Charles

- **Q1. Le Prêtre seul de 100** : a) **le juger en groupe, où il vaut le Mage (Reco)** ; b) monter ses coups, au risque du Prêtre de bas niveau (le budget 0,9 lui fait perdre la Bauge) ; c) un gros sort de dégâts neuf au niveau 80.
- **Q2. Les lignées du Guerrier sous son héros sans voie à 100** : a) **les reprendre au jalon 2 avec la mesure en groupe (Reco)** ; b) maintenant.
- **Q3. Le Disjoint, boss du chapitre, battu seul en moyenne 84 fois sur 100** (Archer 79, Guerrier 100, Mage 93, Prêtre 64) : a) **accepté, plafond de la CI à 90 (Reco)**, le Guerrier plus solide était voulu ; b) le Disjoint plus dur, au risque du Prêtre.
