# Le deck automatique qui garde le tour *(26 novembre 2026, sprint 230, D103)*

La story : « En tant que joueur qui n'a pas composé son deck, le deck automatique ne fait entrer un sort nouveau que s'il ne casse pas un enchaînement ou une invocation de ma classe, vérifié au banc à chaque palier de sorts de 45 à 80 ; puis B4 et B6 remesurées à 60, sans régler les classes avant. » C'est la Q1 et la Q2 du jalon 4 (`jalon-4.md` §4).

## 1. Ce qui sortait du deck à 60

Les decks de chaque classe et de chaque lignée, sans voie et avec, niveau par niveau de 30 à 100 (outil hors dépôt, sur `Hero.DeckOf`). À 60 :

| Classe | Sort de 60 | Ce qu'il chassait |
|---|---|---|
| Nécromancien | Pierre tombale (6 PA, relance 1) | l'Ossuaire (5 PA, zone), joué avec la Poussière de tombe (3 PA) ; sur ses voies, la Pierre tombale chassait l'Ossuaire puis sortait elle-même (`LeastWorth`) : plus de zone de Terre du tout |
| Prêtre | Jugement (6 PA) | le Verdict de pierre (4 PA, −2 PM), ou la Pierre bénie (3 PA) sur ses voies |
| Druide | Chêne abattu (6 PA, relance 1) | le Tertre (5 PA, zone) |
| Guerrier | Coup de titan (7 PA) | le Fauchage (4 PA, zone), la Lame ardente sur ses voies |
| Invocateur | Obélisque (6 PA) | le Grondement (5 PA, zone) |
| Voleur | Rotule (6 PA, relance 2) | le Croc-en-jambe (3 PA) |

**Le point commun n'est ni la zone ni l'effet : c'est le tour.** Chaque sort chassé laissait la place, dans un tour de 8 PA, d'un second sort qui frappe (5 + 3, 4 + 3) ; le sort de 6 ou 7 PA la prend. `TurnValue` compare deux sorts seuls, chacun sur un tour entier : il ne voit pas les 2 PA perdus.

## 2. La règle

`Hero.Keeps(next, old, others)` : quand l'ancien sort laissait dans un tour de 8 PA la place du sort qui frappe le moins cher du reste du deck, le nouveau doit la laisser aussi ; sinon il attend le choix du joueur, comme un sort qui ne bat pas l'ancien. Rien d'autre ne change dans `AutomaticDeck` (sprint 161) ni dans `LeastWorth` (sprint 193).

**Une première règle, plus large, a été écartée** : garder aussi la zone et les effets du sort chassé. Elle changeait les decks dès le niveau 40, y compris le remplacement voulu par le game designer (le Tir brûlant à la place de la Salve ardente, sprint 161), et touchait cinq classes réglées de 35 à 50 (sprints 220 et 221). La règle du tour seule **ne change aucun deck avant le niveau 60**.

Ce qu'elle change, de 60 à 100 : le Nécromancien, le Prêtre, le Druide, le Guerrier, l'Invocateur et le Voleur à 60 ; l'Archer (Trappeur, Pluie de braises) et le Voleur (Feu grégeois) à 65 ; le Mage (Glacier) et le Prêtre (Déluge sacré) à 70. Le Mage garde la Faille à 60 (5 PA à la place des Racines, 4 PA : la place d'un sort de 3 reste).

## 3. Le banc, ancien deck contre nouveau, de 59 à 80

`Rpg.Sim --matrix --simulate 20 --class C --level L`, les deux codes sur les mêmes graines, les huit classes dont un deck change. Sans voie, puis moyenne des voies.

| Classe | 59 | 60 avant → après | 65 | 70 | 75 | 80 |
|---|---|---|---|---|---|---|
| Nécromancien, voies | 60,4 | **40,0 → 55,7** | 40,6 → 57,9 | 42,7 → 58,9 | 41,6 → 57,2 | 40,6 → 58,4 |
| Nécromancien, sans voie | 45,3 | 20,9 → 45,6 | 19,4 → 48,4 | 21,6 → 50,9 | 15,9 → 49,7 | 15,9 → 45,0 |
| Prêtre, voies | 44,9 | **28,7 → 42,6** | 27,0 → 39,8 | 25,0 → 38,2 | 27,0 → 38,5 | 28,3 → 37,1 |
| Prêtre, sans voie | 24,4 | 8,8 → 16,6 | 9,1 → 13,1 | 10,0 → 21,6 | 10,0 → 28,4 | 10,6 → 33,1 |
| Druide, voies | 66,1 | **56,5 → 64,3** | 57,8 → 66,8 | 54,9 → 64,7 | 51,9 → 67,0 | 46,8 → 64,4 |
| Druide, sans voie | 31,9 | 17,8 → 29,4 | 20,9 → 38,8 | 25,3 → 34,7 | 21,6 → 31,3 | 20,3 → 33,1 |
| Invocateur, voies | 73,2 | 67,4 → 70,0 | 72,3 → 76,6 | 70,0 → 74,2 | 68,2 → 70,8 | 66,3 → 70,5 |
| Voleur, voies | 55,7 | 53,0 → 53,0 | 34,1 → 51,9 | 36,1 → 51,7 | 36,0 → 48,1 | 37,6 → 47,5 |
| Voleur, sans voie | 33,8 | 37,8 → 30,3 | 35,6 → 33,8 | 32,2 → 29,4 | 24,4 → 36,9 | 21,9 → 35,0 |
| Archer, voies | 62,2 | 59,4 → 59,4 | 58,1 → 60,2 | 54,8 → 58,9 | 54,5 → 59,9 | 55,1 → 60,6 |
| Mage, voies | 69,2 | 69,0 → 69,0 | 68,0 → 68,0 | 68,3 → 65,9 | 68,5 → 65,4 | 66,3 → 63,8 |
| **Guerrier, voies** | 58,4 | **77,2 → 54,8** | 79,0 → 58,8 | 80,0 → 56,9 | 78,0 → 61,5 | 73,6 → 59,7 |
| Guerrier, sans voie | 59,1 | 47,2 → 58,4 | 44,7 → 57,5 | 46,9 → 57,5 | 45,6 → 57,5 | 49,1 → 58,1 |

- **La marche vers le bas a disparu.** De 59 à 60, le Nécromancien ne perd plus que 4,7 points sur ses voies (21 avant), le Prêtre 2,3 (16), le Druide 1,8 (9). Le gain tient jusqu'à 80 : la même marche revenait à 65, 70 et 75 (les sorts de 65 du Voleur, de 70 du Prêtre).
- **Une marche vers le haut a disparu aussi : le Guerrier.** Avec le Coup de titan, ses voies montaient de 19 points de 59 à 60 (58,4 → 77,2), la plus forte classe du banc à 60 (`jalon-4.md` §3 : 77,4). Sans lui, elles restent à 55-62 ; le Guerrier sans voie, lui, gagne 11 points. Le Coup de titan reste dans le grimoire : un joueur qui compose son deck le prend.
- Le Mage perd 2 à 3 points sur ses voies de 70 à 80 (le Glacier attend) ; le Voleur sans voie 7,5 points à 60 (la Rotule attend), qu'il reprend à 75.

## 4. B4 et B6 à 60 (Q2)

`Rpg.Sim --matrix --simulate 30 --casts --level 60`, la commande du jalon 4 §3, sur le nouveau code (13 minutes).

| Classe | Sans voie, jalon 4 → S230 | Moyenne des voies, jalon 4 → S230 |
|---|---|---|
| Archer | 41,3 → 41,3 | 59,1 → 59,1 |
| Guerrier | 47,5 → 57,5 | 77,4 → 54,3 |
| Mage | 51,3 → 51,3 | 68,1 → 68,1 |
| Prêtre | 10,2 → **17,1** | 28,7 → 42,7 |
| Voleur | 38,3 → 30,2 | 52,3 → 52,3 |
| Berserker | 72,3 → 72,3 | 61,8 → 61,8 |
| Paladin | 32,7 → 32,7 | 57,8 → 57,8 |
| Invocateur | 47,5 → 57,1 | 67,1 → 70,8 |
| Nécromancien | 21,7 → 45,8 | 39,8 → 56,1 |
| Druide | 17,1 → 30,6 | 56,1 → 63,8 |
| **Toutes les voies** | | **56,8 → 58,7** |

| Borne | Jalon 4 | S230 |
|---|---|---|
| **B4**, sans voie entre 20 et 75 | Prêtre 10,2, Druide 17,1 | **non tenue pour le seul Prêtre, 17,1** |
| **B6**, ±8 de la moyenne de la classe | 15 lignées dehors | **9** : Mage 3 (Maître des mirages +10,7, Mentaliste +13,2, Tisseur du temps −9,3), Berserker 3 (Briseur de crânes +11,5, Carnivore +10,8, Sauvage −14,9), Prêtre 2 (Exorciste +15,6, Mystique −8,9), Invocateur 1 (Dompteur −8,9) |
| **B6**, 8 points au plus entre les deux lignées d'une branche | 4 écarts | 5 : Dompteur et Fusionniste 12,3, Exorciste et Fanatique 11,6, Champion de sang et Exécuteur 10,0, Trappeur et Fauconnier 8,5, Geôlier et Tenaille 8,2 |

**Ce que ça dit :** la marche expliquait l'essentiel de B4 (le Druide et le Nécromancien rentrent) et 6 des 15 lignées de B6. Ce qui reste n'est plus le deck : le Prêtre sans voie ne garde que 17 points sur 100 à 60 et en avait 24 à 59 ; le deck du Mage et celui du Berserker ne changent pas à 60, leurs lignées dehors sont celles du jalon 4, et le Berserker en avait déjà 3 dehors à 50 (`jalon-4.md` §3). C'est un équilibrage de classes, pas du deck : rien n'est réglé dans ce sprint, comme le veut la réponse à la Q2.

## 5. À trancher par Charles

- **Q1. Le Coup de titan du Guerrier** : a) **le laisser attendre le joueur, comme les autres sorts de 6 et 7 PA (Reco)** : la règle est la même pour toutes les classes, et c'est lui qui faisait du Guerrier la plus forte classe à 60 ; b) une exception pour lui.
- **Q2. Ce qui reste hors des bornes à 60** (Prêtre sans voie à 17,1 ; 9 lignées hors de ±8, 5 paires de lignées à plus de 8 points d'écart) : a) **le remesurer au jalon 5, à 75, avec la zone de 61 à 75, et régler alors les classes sur les deux niveaux (Reco)** : un sprint d'équilibrage maintenant serait à refaire après la zone ; b) un sprint d'équilibrage à 60 avant la zone (environ 4 points).

## 6. Limites

- Un seul tirage de graines, 20 combats par groupe de 60 à 80, 30 à 60 : ±5 points environ par case.
- Le banc joue le deck automatique, comme un joueur qui n'a jamais composé le sien ; un joueur de 60 qui compose son deck n'est pas concerné par la règle.
- La règle ne regarde que le sort le moins cher du reste du deck ; un tour de trois sorts (3 + 3 + 2) n'est pas vu.
- Un deck sans sort qui frappe de 3 PA ou moins n'est pas protégé : la place laissée se compte avec le moins cher qu'il a.
