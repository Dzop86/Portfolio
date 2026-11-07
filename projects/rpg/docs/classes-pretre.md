# Le Prêtre, première moitié : les soins *(7 novembre 2026, sprint 201, à valider par Charles)*

Ce document prépare la story du sprint 201 : « En tant que Prêtre, mon kit de 24 sorts soigne ; Soin, Renouveau, Renaissance et Sanctuaire quittent le Mage pour moi, et le Mage est repris avec des sorts neufs ; xUnit, bornes du banc. PloufPrêtre créé. » Les trois branches (Clerc, Inquisiteur, Oracle), la résurrection, les lignées et les finales viennent au sprint 202. Il part de `classes-conception.md` (§1.2 la fiche et le moteur, §4.4 le kit du Mage, Q3 a), de `classes-liste.md` §4 et de `talents-canon.md` §6.2 (le nom de Renaissance).

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Le Prêtre est la première classe vraiment neuve** : aucun héros à reprendre. Il devient jouable en entrant dans `classes.json`, la page de création le dégrise d'elle-même [V, `Lobby.Roster`].
- **Sa fiche** (`classes-conception.md` §1.2) : 66 PV + 3 par niveau, 8 PA, 4 PM, initiative 10, **budget de dégâts 0,8**, portée 1 à 6, **jamais de dégâts au contact** (tous ses sorts de dégâts commencent à 2 cases).
- **3 briques neuves dans le moteur**, le minimum des « soins élaborés » (§5 de la conception) :
  1. **le soin en trop devient un bouclier** (le moteur de la classe, dès le niveau 1) ;
  2. **le soin dans la durée**, un poison inversé (un état neuf, `Regen`) ;
  3. **la purge** : retirer à un allié ses malus, ou à un ennemi ses bonus et ses boucliers (l'Inquisiteur s'en servira au sprint 202).
- **Ses soins grandissent avec son meilleur élément**, comme ses coups : ajouté après la première mesure, où il ne battait le Disjoint que 4 fois sur 100 (§6).
- **Ses 24 sorts** : 16 sorts d'élément (4 par élément) et 8 neutres. Parmi les neutres, Soin, Renouveau, Sanctuaire et Renaissance viennent du Mage ; Renaissance devient **Grâce** (`talents-canon.md` §6.2, point 3).
- **Le Mage reçoit 4 neutres neufs** à la place : Clignement, Lenteur, Barrière de mana, Siphon. Aucune brique neuve.
- **Les Mages déjà créés** : le serveur ne lit déjà que les sorts de la classe ; les rangs et la barre des sorts partis tombent, les points de sort redeviennent libres, sans migration.

## 1. Le moteur de la classe : le soin en trop devient un bouclier

Un soin marqué `overflow` (tous les soins du Prêtre) soigne comme avant ; ce qui dépasse les PV max devient un **bouclier sur le soigné, pendant 2 de ses tours**. Le bouclier ne dépasse jamais le jet du soin. Un soigné **Desséché** (`Parched`) n'en reçoit pas, comme il ne reçoit aucun soin.

La question de la classe, « qui sauver ce tour, et que me reste-t-il pour frapper ? », change ainsi : un soin n'est jamais perdu, il protège l'allié qui va prendre les coups. L'IA le pèse comme un bouclier : ce que les coups à portée prendraient vraiment au soigné (`Incoming`, T154).

**Ses soins grandissent** (`grows`, ajouté après la première mesure, §6). Un coup ajoute à son jet le bonus de sa caractéristique (2 par tranche de 10 points, D61) ; un soin restait fixe. Au niveau 30, Soin (7 à 10) ne valait plus le tiers d'un coup, et l'IA ne le lançait jamais. Un soin du Prêtre ajoute désormais le même bonus, pris sur son **élément le plus fort** (`Fight.HealBonus`). Les autres soins du jeu (monstres, Second souffle, Pansement) ne changent pas.

## 2. Les deux autres briques

| Brique | Données | Ce qu'elle fait | IA |
|---|---|---|---|
| **Soin dans la durée** | `status` avec `"stat": "Regen"` | au début de chacun de ses tours, avant les poisons, le porteur est soigné de la valeur ; les soins reçus (`Healing`) et Desséché s'appliquent | comme un poison inversé : 0,6 × ce qui sera vraiment soigné |
| **Purge** | `{"kind": "purge"}` | sur un allié, retire les malus (poison, valeurs négatives, Desséché, électrisé) ; sur un ennemi, les bonus (valeurs positives, insaisissable, indéplaçable, feinte) et les boucliers | la valeur de chaque état retiré, comme s'il était posé |

**Le relevé des combats.** Les trois briques ne vivent que dans des sorts neufs ou repris par le Prêtre : un combat enregistré avant les rencontre seulement si un Mage y lançait Soin ou Renouveau, sans `overflow`. Il se rejoue donc à l'identique ; `FightRecord` ne change pas de version (même règle que T160).

## 3. Les 24 sorts du Prêtre

Ordre de D62 : un sort de chaque élément et un neutre au niveau 1, puis un sort tous les 5 niveaux. Dégâts au barème × 0,8, portée minimale 2. Noms de travail, au scénariste.

| Niv. | Sort (id) | Élément | PA | Portée | Zone | Effets | Relance |
|---|---|---|---|---|---|---|---|
| 1 | Pierre bénie `blessed-stone` | Terre | 3 | 2-5 | | | |
| 1 | Flamme sainte `holy-flame` | Feu | 3 | 2-5 | | | |
| 1 | Eau bénite `holy-water` | Eau | 3 | 2-5 | | | |
| 1 | Souffle sacré `sacred-breath` | Air | 3 | 2-5 | | recul 1 | |
| 1 | Soin `mend` (du Mage) | — | 3 | 0-5 | | soin 7-10, **en trop : bouclier** | 1 |
| 5 | Prière `prayer` | — | 2 | 0-5 | | **soin de 5 par tour, 3 tours** | 2 |
| 10 | Verdict de pierre `stone-verdict` | Terre | 4 | 2-5 | | −2 PM, 1 tour | 1 |
| 15 | Encensoir `censer` | Feu | 3 | 2-5 | | poison Feu 4 × 3 tours | |
| 20 | Marée bénite `blessed-tide` | Eau | 4 | 2-5 | cercle 1 | soigne aussi les alliés de la zone, 5-7 | |
| 25 | Zéphyr `zephyr` | Air | 4 | 2-6 en ligne | ligne 2 | | |
| 30 | Purification `purify` | — | 2 | 0-5, sans vue | cercle 1 | **purge** des alliés, soin 6-8 avec bouclier | 2 |
| 35 | Colonne de pierre `stone-column` | Terre | 5 | 2-6, sans vue | cercle 1 | | |
| 40 | Bûcher sacré `sacred-pyre` | Feu | 5 | 2-6 | | | 1 |
| 45 | Baptême `baptism` | Eau | 5 | 2-5 | croix 1 | −1 PM, 1 tour | |
| 50 | Auréole `halo` | Air | 5 | 2-6 | cercle 1 | −1 PA, 1 tour | 1 |
| 55 | Renouveau `renewal` (du Mage) | — | 4 | 0-5 | cercle 1 | soin 10-14, en trop : bouclier | 2 |
| 60 | Jugement `judgement` | Terre | 6 | 2-6, sans vue | cercle 1 | | |
| 65 | Brasier céleste `heavenly-blaze` | Feu | 6 | 2-6, sans vue | cercle 2 | | |
| 70 | Crue sacrée `holy-flood` | Eau | 6 | 2-6, sans vue | croix 2 | | |
| 75 | Courroux du ciel `wrath-of-heaven` | Air | 6 | 2-6 | cercle 1 | | 2 |
| 80 | Sanctuaire `sanctuary` (du Mage) | — | 4 | 0-4, sans vue | cercle 2 | +15 % de résistance, 2 tours | 3 |
| 85 | Grâce `grace` (Renaissance du Mage) | — | 6 | 0-5, sans vue | cercle 2 | soin 22-28, en trop : bouclier | 4 |
| 90 | Bénédiction `blessing` | — | 3 | 0-5 | cercle 1 | alliés : +20 % de dégâts, 2 tours | 3 |
| 95 | Pas serein `serene-step` | — | 1 | 0 | | +2 PM, 1 tour | 2 |

Ses dégâts sont plus faibles que ceux des autres classes, et il commence à 2 cases : le Prêtre **frappe peu**, sa valeur est dans ses soins. C'est voulu par la conception (§1.2, budget 0,8) ; la mesure dit si le Prêtre seul tient encore (B1).

## 4. Le Mage repris

Il garde ses 20 autres sorts. Quatre neutres neufs prennent les places des sorts partis, aux mêmes niveaux, sans brique neuve, et tous servent sa tenue seul :

| Niv. | Sort (id) | PA | Portée | Effets | Relance | À la place de |
|---|---|---|---|---|---|---|
| 1 | Clignement `blink` | 2 | 1-3 | bond sur la case | 2 | Soin |
| 55 | Lenteur `slow` | 3 | 1-6, cercle 1 | ennemis : −2 PM et −1 PA, 1 tour | 2 | Renouveau |
| 85 | Barrière de mana `mana-barrier` | 3 | 0 | bouclier de 30 sur soi, 2 tours | 3 | Sanctuaire |
| 90 | Siphon `siphon` | 2 | 1-5 | l'ennemi −2 PA, toi +2 PA, 1 tour | 3 | Grâce |

**Le risque, déjà dit en Q3 de la conception :** le Mage perd ses soins, donc une part de sa tenue seul. La mesure est au §6.

## 5. Les Mages déjà créés

Rien à écrire : le serveur lit déjà les rangs et la barre d'un personnage **en ne gardant que les sorts de sa classe** [V, `Character.RankList` et `DeckList`]. Un Mage qui avait des rangs dans Soin, Renouveau, Sanctuaire ou Renaissance les perd à la lecture, ses points de sort redeviennent libres (ils se comptent sur les rangs), et sa barre perd ces sorts. Le test `Rpg.Api.Tests.PriestTests` le vérifie sur PostgreSQL. La même lecture servira à chaque sort qui change de classe (Égide au Paladin, sprint 206).

## 6. Mesures

**Les exigences en solo de la CI** (`BalanceTests`, chaque classe) :

| | Premier jet | Soins qui grandissent | Règle |
|---|---|---|---|
| Disjoint, seul au niveau 30, kit des artisans | **4 sur 100** | 35 sur 100 | 30 à 100 |
| Grande salle de la Bauge, au niveau de la quête (8) | 88 sur 100 | 88 sur 100 | 70 au moins |

Au premier jet, le Prêtre perdait le Disjoint de peu (36 PV restaient au boss en moyenne, sur 450) en ne soignant que 17 PV par combat : Soin ne se lançait jamais. Avec des soins qui grandissent, il soigne 169 PV par combat, et Soin devient son sort le plus joué.

**Au banc seul** (`Rpg.Sim --matrix --simulate 30`, héros nu, sans voie) :

| Niveau | Archer | Guerrier | Mage | Prêtre |
|---|---|---|---|---|
| 35 | 53,5 | 24,0 | 41,5 | 24,4 (31,9 au premier jet) |
| 50 | 44,2 | 32,3 | 55,4 | 35,6 |

Le Prêtre seul est au rang du Guerrier : il frappe peu, c'est voulu. Le Mage ne bouge pas (41,5 et 55,4, comme au sprint 200) : seul, il ne lançait pas ses soins.

**En groupe** (`CiLevelsTests.Parties`, hordes de synthèse au niveau 45, 40 combats, nus) : les 12 groupes de référence, mesurés à nouveau, n'ont pas bougé. Les 6 nouveaux : Guerrier + Prêtre 57 %, Archer + Prêtre 72 %, Mage + Prêtre 62 %, Guerrier + Archer + Prêtre 95 %, Guerrier + Mage + Prêtre 87 %, les quatre classes 100 %. Le Prêtre vaut le Mage en second héros, sans être une taxe de groupe (B11).

**Ce qui n'est pas mesuré** : le soin dans la durée et la purge ne sont pesés par l'IA que par estimation (0,6 du soin à venir, la valeur des états retirés). Les PV de la classe (66 + 3) restent ceux de la conception, en attendant le balayage du jalon 1 (sprint 203).

## 7. À trancher par Charles

- **Q1. Les noms** des 20 sorts neufs du Prêtre, des 4 du Mage et de Grâce : noms de travail, au scénariste.
- **Q2. Les points rendus en silence** : a) **oui (Reco)**, les points de sort redeviennent libres et la barre perd les sorts partis, sans message, comme le serveur le fait déjà ; b) un message à la connexion, comme pour l'arbre (T146), environ 0,25 point de plus.
- **Q3. Les soins qui grandissent** (§1) : a) **oui, pour le seul Prêtre (Reco)** : sans eux, il ne bat pas le Disjoint ; b) pour tous les soins des héros (Second souffle du Guerrier, Pansement de l'Archer), à mesurer.
