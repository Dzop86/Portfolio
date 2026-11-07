# Le Prêtre, seconde moitié : Clerc, Inquisiteur, Oracle *(7 novembre 2026, sprint 202, à valider par Charles)*

Ce document prépare la story du sprint 202 : « En tant que Prêtre, je deviens Clerc, Inquisiteur ou Oracle, avec la résurrection, puis leurs lignées et leurs finales ; xUnit, bornes du banc. » Il part de `classes-conception.md` (§1.3 : Clerc le soin, Inquisiteur retirer états et boucliers et punir, Oracle poser ce qui se déclenchera au tour suivant ; §5 la résurrection), de `classes-liste.md` §4 (les noms) et de `classes-pretre.md` (le kit, le soin en trop, le soin dans la durée, la purge).

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Deux briques** seulement, le reste vient du sprint 201 : la **résurrection** (`revive`) et le **bonus de soin** d'une voie (`heal`).
- **12 sorts de voie**, 2 à 30 et 1 à 50 par lignée, aucun coup au contact. Les coups de l'Inquisiteur sont au barème plein, comme ceux de toute voie : c'est le Prêtre qui frappe.
- **12 finales**, sans passif neuf : Gardien de vie, Élu céleste et Augure reprennent « Pas aujourd'hui », « Prime touchée » et « Esprit posé ».
- **L'Oracle sans brique** : un poison d'un tour frappe au début du tour suivant de la cible, un soin dans la durée d'un tour soigne au début du tour suivant de l'allié. « Poser ce qui se déclenchera » est déjà dans le moteur.

## 1. L'arbre

| Branche (id), 30 | Règle et sorts | Lignée (id), 50 | Règle et sort | Finales, 100 (gabarit) |
|---|---|---|---|---|
| **Clerc** (`cleric`) | soins +15 %, +1 PA à chaque tour ; **Résurrection**, **Imposition des mains** | Guérisseur (`healer`) | soins +10 % ; **Vague de soin** | Saint (Extends : 7 cases, cercle 3) ; Miracle vivant (Faster : relance 1) |
| | | Médecin sacré (`sacred-medic`) | **Remède** | Chirurgien divin (Second : deux fois par tour) ; Gardien de vie (Saves : « Pas aujourd'hui », Résurrection à 6 cases) |
| **Inquisiteur** (`inquisitor`) | +10 % sur une cible sous un état ; **Censure**, **Marque de l'hérétique** | Exorciste (`exorcist`) | **Exorcisme** | Purificateur (Extends : 7 cases, cercle 2) ; Chasseur de démons (TargetState : +20 %, Marque en 1 tour) |
| | | Fanatique (`fanatic`) | **Zèle** | Zélote (Faster : relance 1) ; Croisé de la foi (FirstTurn : +2 PA au premier tour) |
| **Oracle** (`oracle`) | garde 1 PA d'un tour au suivant ; **Présage**, **Prévoyance** | Prophète (`prophet`) | **Prophétie** | Voyant suprême (Extends : 8 cases, cercle 2) ; Élu céleste (Kill : « Prime touchée ») |
| | | Mystique (`mystic`) | garde 1 PA de plus, +15 % à 4 cases ou plus ; **Vision** | Augure (StayOrMove : « Esprit posé ») ; Sage divin (Faster : Vision en 1 tour) |

## 2. Les sorts et les briques

| Sort | Voie | PA | Portée | Zone | Effets | Relance |
|---|---|---|---|---|---|---|
| Résurrection `path-resurrect` | Clerc | 3 | 1-4, sans vue | | **relève** un allié tombé avec 30 % de ses PV, une fois par combat | |
| Imposition des mains `path-laying-hands` | Clerc | 2 | 0-2 | | soin 12-16, en trop : bouclier | 1 |
| Vague de soin `path-healing-wave` | Guérisseur | 4 | 0-5, sans vue | cercle 2 | soin 10-14, en trop : bouclier | 2 |
| Remède `path-remedy` | Médecin sacré | 2 | 0-5 | | purge l'allié, soin de 8 par tour, 2 tours | 1 |
| Censure `path-censure` | Inquisiteur | 3 | 2-6 | | dégâts, purge l'ennemi (bonus et boucliers) | 1 |
| Marque de l'hérétique `path-brand` | Inquisiteur | 3 | 2-6 | | dégâts, −15 % de résistance, −50 % de soins reçus, 2 tours | 2 |
| Exorcisme `path-exorcism` | Exorciste | 4 | 2-5 | cercle 1 | dégâts, purge les ennemis | 2 |
| Zèle `path-zeal` | Fanatique | 1 | 0 | | +2 PA tout de suite ; +25 % de dégâts, −10 % de résistance, 2 tours | 3 |
| Présage `path-omen` | Oracle | 3 | 2-6 | | 42 dégâts neutres **au début du tour suivant** de la cible | 1 |
| Prévoyance `path-foresight` | Oracle | 2 | 0-5 | | soin de 20 **au début du tour suivant** de l'allié | 1 |
| Prophétie `path-prophecy` | Prophète | 4 | 2-6, sans vue | cercle 1 | 56 dégâts neutres au tour suivant | 2 |
| Vision `path-vision` | Mystique | 1 | 0-5 | cercle 1 | alliés : +2 PO, soin de 16 au tour suivant | 2 |

**La résurrection** (`ReviveEffect`, `revive`). Un mort reste dans les combattants, à 0 PV [V]. Le sort vise sa case : un allié tombé du lanceur, pas une invocation, sa case libre. Il revient avec 30 % de ses PV max (que l'érosion a déjà réduits), sans ses états ni ses boucliers, et rejoue à son tour. **Une fois par combat** pour chaque lanceur et chaque sort. Le client le montre se relever (`Revived`) ; l'IA le lève avant tout autre chose (ses PV rendus, et 40 de plus pour un combattant revenu).

**Le bonus de soin** (`HealRule`, `heal`) : les soins de qui porte la voie sont plus gros d'autant, après le bonus de l'élément (sprint 201).

**Un poison d'un tour** vaut pour l'IA 0,9 de ses dégâts (0,6 pour un poison qui dure) : il frappe à coup sûr au tour suivant, presque comme un coup. Sans cela, l'IA ne jouait le Présage que 0,3 fois par combat.

**Ce qui ne grandit pas.** Les poisons et les soins d'un tour de l'Oracle ont des valeurs fixes : ils ne prennent pas le bonus de la caractéristique, comme tout poison du jeu. À mesurer à 100 (§4).

## 3. Les noms

Ceux de `classes-liste.md` pour l'arbre ; les sorts sont des noms de travail, au scénariste (Q1). Les identifiants ne sont pas préfixés : Inquisiteur, Purificateur, Fanatique et Zélote reviendront chez le Paladin avec `paladin-` devant (`classes-conception.md` §8, 14 doublons).

## 4. Mesures

`Rpg.Sim --matrix --simulate 30 --class priest` (héros nu, paquet automatique).

| | Premier jet | Retenu |
|---|---|---|
| **35** : sans voie | 24,4 | 24,4 |
| Clerc | 10,4 | **39,2** (+1 PA par tour) |
| Inquisiteur | 21,9 | **40,8** (coups au barème plein, Marque qui frappe) |
| Oracle | 10,0 | **40,4** (Présage à 42, poison d'un tour à 0,9 pour l'IA) |
| **50** : sans voie | 35,6 | 35,6 |
| Guérisseur, Médecin sacré | 17,9, 23,3 | 37,5, 41,9 |
| Exorciste, Fanatique | 26,0, 26,7 | 40,0, 42,7 (Zèle donne 2 PA) |
| Prophète, Mystique | 21,5, 21,5 | 39,0, 40,0 (Prophétie à 4 PA et 56 ; +15 % à 4 cases) |

**Le premier jet faisait perdre chaque voie** contre le Prêtre sans voie : ses sorts de voie chassaient du paquet ses meilleurs sorts de classe, sans rien rapporter seul. La Résurrection n'a personne à relever, Imposition des mains ne se visait pas soi-même, et l'IA ne jouait ni le Présage (0,3 fois par combat) ni la Marque (0,1). Le Clerc garde une Résurrection inutile seul : comme le Chronomancien (T162), il reçoit +1 PA à chaque tour.

**À 100** (indicatif, hors CI) : sans voie **6,7**, les finales de 8,5 à 26,3. `FinalsBenchTests.AtHundred_ThePriestsFinals…` passe : seules les lignes concluantes sont jugées. **Le Prêtre seul de 100 est trop faible** (l'Archer sans voie fait 37,7) : ses poisons et ses soins d'un tour ont des valeurs fixes, ses coups sont à 0,8. C'est la mesure du jalon 1 (sprint 203) qui doit le reprendre.

## 5. À trancher par Charles

- **Q1. Les noms** des 12 sorts de voie : noms de travail, au scénariste.
- **Q2. La résurrection, une fois par combat et par lanceur** : a) **ainsi (Reco)**, la conception le demande ; b) une fois par allié, plus généreux à plusieurs Prêtres.
