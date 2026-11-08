# Le Paladin, première moitié : la protection et le Protecteur *(8 novembre 2026, sprint 208, à valider par Charles)*

Ce document prépare la story du sprint 208 : « En tant que Paladin, mon kit réutilise les soins et les boucliers, Égide comprise, et je deviens Protecteur ; xUnit. PloufPaladin créé. » Le Templier, le Vengeur et leurs finales viennent au sprint 209. Il part de `classes-conception.md` (§1.2 la fiche et le moteur, §1.4 ce qui le sépare du Prêtre et du Guerrier, §4.2 Égide) et de `classes-liste.md` §2.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Quatrième classe neuve**, id `paladin` (celui de la page de création). 80 PV + 4, 8 PA, **3 PM**, initiative 10, **budget de dégâts 0,85**, au contact, ses effets à 2 cases. La question de son tour : « Qui je couvre, et à côté de qui je me place pour ça ? »
- **Son moteur, la protection** (règle de classe `ward`) : un coup porté au contact donne un bouclier de 30 % des dégâts faits aux ennemis à l'allié le plus blessé à 2 cases, lui compris, pour 2 tours. C'est la signature Bourgeon de la Sève, en bouclier au lieu d'un soin.
- **Aucune brique neuve** au-delà de la règle : boucliers, soins, états et glyphes alliées existent [V].
- **Égide quitte le Guerrier pour le Paladin** (niveau 5, portée 0 à 2) ; le Guerrier reçoit **Provocation de masse** à sa place (niveau 90 : attire de 2 cases les ennemis à 2 cases de lui), fidèle à son rôle. Les Guerriers déjà créés perdent leurs rangs dans Égide à la lecture, leurs points redeviennent libres, sans migration (comme les soins du Mage au sprint 201) : vérifié sur PostgreSQL.
- **Le Protecteur**, ses 2 lignées et ses 4 finales dès ce sprint.

## 1. Ce qui le sépare des autres

| Avec | Règle (§1.4 de la conception) | Vérifié |
|---|---|---|
| Prêtre | aucun soin à plus de 2 cases, aucun soin sans coup porté ; ses soins vont sur lui-même, portés par ses coups | test sur le kit |
| Guerrier | il protège par sa présence ; il n'attire ni ne retire de PM | Égide partie, Provocation de masse au Guerrier |

## 2. Les 24 sorts

| Niv. | Sort (id) | Élément | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|---|
| 1 | Marteau consacré `holy-hammer` | Terre | 4 | 1 | | |
| 1 | Lame radieuse `radiant-blade` | Feu | 3 | 1 | soin sur soi 4-6 | |
| 1 | Coup purifiant `purifying-strike` | Eau | 3 | 1-2 | | |
| 1 | Lance de lumière `light-lance` | Air | 3 | 1-2 en ligne | | |
| 1 | Constance `steadfast` | — | 2 | 0 | +10 % de résistance, 2 tours | 2 |
| 5 | Égide `aegis` (du Guerrier) | — | 4 | 0-2, cercle 1 | bouclier 20 aux alliés, 2 tours | 3 |
| 10 | Frappe du rempart `rampart-strike` | Terre | 4 | 1 | bouclier 10 sur soi | 1 |
| 15 | Frappe solaire `sun-strike` | Feu | 4 | 1-2 | soin sur soi 6-8 | |
| 20 | Flot de foi `tide-of-faith` | Eau | 4 | 1-2 | ennemi −15 % de dégâts, 2 tours | |
| 25 | Lance du jugement `judgement-lance` | Air | 4 | 1-2 en ligne | | |
| 30 | Serment `oath` | — | 3 | 0, cercle 2 | alliés +10 % de résistance, 2 tours | 3 |
| 35 | Foi écrasante `crushing-faith` | Terre | 5 | 1 | | |
| 40 | Lame de l'aube `dawn-blade` | Feu | 5 | 1 | soin sur soi 8-10 | 1 |
| 45 | Frappe de miséricorde `mercy-strike` | Eau | 5 | 1-2 | purge l'ennemi | |
| 50 | Lance céleste `heavens-spear` | Air | 5 | 1-3 en ligne | | |
| 55 | Garde sacrée `sacred-guard` | — | 3 | 0-2 | bouclier 25 sur un allié | 3 |
| 60 | Marteau du jugement `judgement-hammer` | Terre | 6 | 1 | −1 PA | 1 |
| 65 | Serment ardent `blazing-oath` | Feu | 6 | 1 | soin sur soi 12-16 | 1 |
| 70 | Flot de grâce `grace-flood` | Eau | 6 | 1-2 | ennemi −20 % de dégâts | 1 |
| 75 | Zénith `zenith` | Air | 6 | 1-2 | | 2 |
| 80 | Bouclier divin `divine-shield` | — | 3 | 0 | bouclier 30 sur soi | 4 |
| 85 | Ralliement `rally` | — | 3 | 0, cercle 2 | alliés +1 PA, 1 tour | 4 |
| 90 | Pénitence `penance` | — | 3 | 1-2 | ennemi −25 % de dégâts, 2 tours | 3 |
| 95 | Pas gardé `guarded-step` | — | 1 | 0 | +2 PM, 1 tour | 2 |

Seules ses lances d'Air portent à 3 cases ; aucun effet à plus de 2.

## 3. Le Protecteur

| Branche (id), 30 | Règle et sorts | Lignée (id), 50 | Sort | Finales, 100 (gabarit) |
|---|---|---|---|---|
| **Protecteur** (`protector`) | protection +20 % (50 % en tout) ; **Terre sacrée** (glyphe alliée : +15 % de résistance), **Frappe gardienne** (bouclier 12 sur soi) | Rempart (`paladin-rampart`) | **Muraille de foi** (bouclier 20 autour de soi) | Bastion (`holy-bastion`, Extends : cercle 2) ; Gardien éternel (Saves : « Pas aujourd'hui ») |
| | | Défenseur (`defender`) | **Bouclier de la foi** (25 sur un allié à 2 cases) | Égide (`paladin-aegis`, Second : deux fois par tour) ; Champion protecteur (StayOrMove : +15 % sans avoir bougé) |

## 4. Mesures

- **Les exigences en solo de la CI** (Disjoint seul à 30, grande salle de la Bauge, hordes de la Forêt) : tenues au budget de 0,85.
- **Les groupes de référence** (hordes de synthèse au niveau 45, nus) : Mage + Paladin 55 %, Archer + Paladin 60 %, Voleur + Paladin 60 %, Archer + Mage + Paladin 100 %, Prêtre + Berserker + Paladin 95 %, Guerrier + Mage + Voleur + Paladin 100 %.
- **Les autres classes** jouent les mêmes combats qu'avant au niveau 10 (empreintes inchangées) : Égide, partie du Guerrier, était au niveau 90.
- **Le budget de combats de D97** était de nouveau dépassé (20 545) : chaque classe ajoute 6 groupes de 40 combats. Les groupes se partagent désormais un budget fixe de 1 000 combats (27 chacun à 36 groupes, les mêmes premières graines ; toutes les bandes de ±15 tenues sans remesure), et les hordes de la Forêt un échantillon partagé (8 combats par horde à 7 classes, en dixièmes). La suite joue 19 629 combats.
- **La fumée** : les sorts du Protecteur pour ses alliés (Bouclier de la foi ; Terre sacrée à 35) ne sont pas joués seul, et ses trois finales tournées vers les alliés ne changent aucun combat (la fumée se joue seul).

## 5. À trancher par Charles

- **Q1. Les noms** des 24 sorts, des 4 sorts de voie et de Provocation de masse : noms de travail, au scénariste.
- **Q2. La protection sur le Paladin lui-même** : a) **oui, comme le Bourgeon (Reco)**, sinon elle ne sert à rien seul ; b) seulement sur un allié.
- **Q3. Égide au niveau 5** (90 chez le Guerrier) : a) **oui, c'est son sort-type (Reco)** ; b) plus tard dans le kit.
