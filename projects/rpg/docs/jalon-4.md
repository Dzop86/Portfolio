# Jalon 4 : la première zone de 46 à 60 *(25 novembre 2026, sprint 229, validé par Charles : D103)*

La story : « En tant que Charles, je joue la zone de 46 à 60 avec les dix classes, la mesure (banc à 60, banc de groupes, durées) est dans `REVIEW.md`, et je décide de la suite : zones de 61 à 100 ou autre chose ; banc hors CI, build livrée. » Elle vient de D102 (`docs/banc-groupes.md` §5, option a).

## 1. La zone livrée

Les **Mines de Sous-Racine** (`underroot`), sous la Forêt des Aïeux, faites en cinq sprints pour 22 points, **exactement le chiffrage de D102** (5 + 4 + 4 + 6 + 3) :

| Sprint | Contenu | Points | Décision |
|---|---|---|---|
| S224 | 25 cartes de [4, 6] à [8, 10], 10 ressources de 50 et 55, une pierre de passage | 5 | T189 |
| S225 | Les Fouille-rocs (`delver`) : 5 monstres de 46 à 49, l'Écoute-roche (60), tonnelets de poudre, panoplie | 4 | T190 |
| S226 | Les Vers de roche (`borer`) : 4 vers de 49 à 52, le Ronge-montagne (60), Cuir de roche, Anneaux, panoplie | 4 | T191 |
| S227 | Le Filon qui sonne et le Ventre de la montagne, leurs clefs, les quêtes de Berthe (59, 60), deux succès | 6 | T192 |
| S228 | 12 pièces d'artisan (objets de 47, 50, 53), 2 élixirs, 3 pains | 3 | T193 |

## 2. La zone mesurée à dix classes (rappel des sprints 225 à 228)

Chaque mesure est une théorie hors CI de `BalanceTests` (le budget de combats de la suite est plein, D96) ; aucune n'a été changée depuis son sprint.

| Mesure | Règle | Résultat | Hors règle |
|---|---|---|---|
| Petits groupes, niveau 46, 56 groupes (T191) | gagnés seul et nu, au plus 2 groupes sous 18 sur 30 | huit classes de 29,6 à 30,0 sur 30 | **le Prêtre** : 27,7, 3 groupes sous 18 (accepté par Charles le 24 nov.) |
| Hordes, niveau 46 (T190, T191) | au plus 3 sur 10 nu, 2 avec le kit `craft` | neuf classes dedans | **l'Invocateur `craft`** : 2,5 (accepté le 24 nov.) |
| Chefs seuls (T191) | perdus seul | 0 sur 10 pour les dix classes | — |
| Chefs en groupe, niveau 60 (T190, T191, échantillons) | 70 % à trois, 90 % à quatre | Écoute-roche 95 % à deux ; Ronge-montagne 10 % à deux, 91 % à trois, 100 % à quatre | aucun (le chef des Vers reste un combat de groupe, D102) |
| Grandes salles des donjons, au niveau de leur quête (T192) | au moins 70 sur 100 seul et nu | Filon (59) de 76 (Paladin) à 100 ; Ventre (60) 100 pour les dix | **le Nécromancien de 60 sans voie** perd le Filon : 0 sur 100 (§4) |
| Salles des chefs en groupe, niveau 60 (T192) | 70 % à trois, 90 % à quatre | 10 sur 10 en trio et en quatuor ; à deux, Ventre 32 sur 40 | — |
| Hordes à 60 avec l'artisanat de 60 (T193) | gain des pièces au plus 3 par classe, 1,7 en moyenne | de −0,8 à +1,0, moyenne +0,2 | — |

## 3. Le banc à 60 (seul)

`Rpg.Sim --matrix --simulate 30 --casts --level 60` (13 minutes), à côté de la même commande à 50 relancée aujourd'hui sur le même code. Héros nus, groupes de 4 et 5 de la famille synthétique au niveau du héros, sorts de voie rangés, 6 lignées par classe (le rang 3).

| Classe | Sans voie 50 | Sans voie 60 | Moyenne des voies 50 | Moyenne des voies 60 |
|---|---|---|---|---|
| Archer | 46,9 | 41,3 | 69,2 | 59,1 |
| Guerrier | 55,4 | 47,5 | 66,1 | **77,4** |
| Mage | 54,6 | 51,3 | 72,9 | 68,1 |
| Prêtre | 29,6 | **10,2** | 50,1 | **28,7** |
| Voleur | 43,8 | 38,3 | 55,9 | 52,3 |
| Berserker | 74,4 | 72,3 | 70,9 | 61,8 |
| Paladin | 51,3 | 32,7 | 66,3 | 57,8 |
| Invocateur | 62,1 | 47,5 | 78,0 | 67,1 |
| Nécromancien | 44,4 | 21,7 | 62,3 | **39,8** |
| Druide | 42,1 | **17,1** | 67,0 | 56,1 |
| **Toutes les voies** | | | **65,9** | **56,8** |

**Les bornes** (`classes-conception.md` §7.3) :

| Borne | À 50 | À 60 |
|---|---|---|
| **B4**, sans voie entre 20 et 75 | tenue (Prêtre 29,6) | **non tenue** : Prêtre 10,2, Druide 17,1 ; Nécromancien 21,7 au bord |
| **B6**, 8 points au plus entre les deux lignées d'une branche | 1 écart (Sanguimancien et Thanaturge, 10,4) | 4 écarts : Guérisseur et Médecin sacré (10,0), Champion de sang et Exécuteur (10,0), Dompteur et Fusionniste (8,8), Trappeur et Fauconnier (8,5) |
| **B6**, ±8 de la moyenne de la classe | 5 lignées dehors (3 au Berserker) | **15 lignées dehors** : le Prêtre (4 sur 6), le Mage, le Berserker et le Nécromancien (3 chacun), l'Invocateur (2) |
| Couverture (chaque sort de voie à 0,5 par combat) | 20 lignées concluantes sur 60, 36 sorts sous 0,5 | 23 sur 60, 29 sorts sous 0,5 |

**Ce que ça dit :** le banc perd 9 points de 50 à 60, et la chute n'est pas égale : le Prêtre perd 21 points sur ses voies, le Nécromancien 22, quand le Guerrier en gagne 11. B6, tenue à 50 après les réglages des sprints 220 et 221, se défait à 60 surtout dans ces classes-là. La cause est au paragraphe suivant.

## 4. La marche du niveau 60

Les mêmes classes à 55 et 59 (`--class`, 30 combats par groupe) :

| Moyenne des voies | 50 | 55 | 59 | 60 |
|---|---|---|---|---|
| Prêtre | 50,1 | 46,0 | 44,9 | **28,7** |
| Nécromancien | 62,3 | 64,4 | 60,7 | **39,8** |
| Druide | 67,0 | 64,5 | 65,3 | **56,1** |
| Paladin | 66,3 | 61,0 | 60,4 | 57,8 |

| Sans voie | 50 | 55 | 59 | 60 |
|---|---|---|---|---|
| Prêtre | 29,6 | 32,5 | 25,2 | 10,2 |
| Nécromancien | 44,4 | 45,0 | 45,2 | 21,7 |
| Druide | 42,1 | 40,6 | 32,1 | 17,1 |
| Paladin | 51,3 | 54,6 | 36,0 | 32,7 |

**De 59 à 60, le Nécromancien perd 21 points, le Prêtre 16, le Druide 9**, sans rien d'autre qui change qu'un niveau. C'est ce que le sprint 227 avait vu dans la grande salle du Filon : au niveau 60, chaque classe reçoit un sort (le Jugement du Prêtre, la Pierre tombale du Nécromancien, le Chêne abattu du Druide), et **le deck automatique** (`Hero.AutomaticDeck`, sprint 161) le fait entrer à la place d'un sort plus ancien du même élément qu'il « bat » sur un tour (`TurnValue`). Le Nécromancien y perd l'Ossuaire et ne lève plus de morts. La règle compare deux sorts seuls ; elle ne voit ni les combos ni les invocations. Le banc, comme un joueur qui n'a jamais composé son deck, prend ce deck automatique, voie comprise.

*Je n'ai pas encore listé sort par sort ce qui sort du deck pour le Prêtre et le Druide : c'est l'hypothèse la plus simple, la seule chose qui change à 60, et c'est démontré pour le Nécromancien (T192).* Le Paladin, lui, perd surtout entre 55 et 59, sans voie : une autre marche, plus petite.

Ce n'est pas une affaire de zone : la même marche peut revenir à 65, 70, 75 et 80, les niveaux des sorts suivants. Les zones de 61 à 100 la rencontreraient à chaque palier.

## 5. Le banc de groupes à 60

`--matrix --casts --party-size 2|3 --level 60`, contre les hordes de 6 et plus, 20 combats par groupe (10 et 16 minutes).

| Moyenne des voies | 35 (S222) | 45 (S222) | 50 (S222) | 60 |
|---|---|---|---|---|
| Seul, groupes de 4 et 5 | 63,3 | 65,1 | 66,0 | 56,8 |
| Duo, hordes | 68,3 | 70,1 | 72,7 | 70,2 |
| Trio, hordes | 96,3 | 98,0 | 98,8 | 99,4 |

- **Le duo ne voit presque pas la marche** : un allié Guerrier nu compense le deck du Prêtre ou du Nécromancien. B6 en duo : 2 écarts entre lignées (Bretteur et Saboteur 8,9, Briseur de crânes et Carnivore 8,3), 6 lignées hors de ±8.
- **Le trio gagne tout** (99,4) et ne mesure plus rien, comme au S222.
- **Les sorts muets** (sous 0,5 par combat) : 28 en duo, 26 en trio, 29 seul. Des 8 sorts du sprint 223, la Résurrection monte à 0,4 en duo, le Bouclier de la foi et la Haie à 0,3 ; aucun n'atteint 0,5. La Fiole de peste, la Feinte, la Cabriole, le Cercle de fer et le Coup d'arène restent à 0 ou 0,1. C'est le constat du T188 : ils valent moins qu'un coup par PA, et leurs montants fixes ne suivent pas le niveau.

## 6. La durée des combats (B15, B16)

`Rpg.Sim --durations --simulate 60` à 45 et à 60.

- **B15, le combat finit : tenue.** À 60, aucun combat à la limite de 50 tours, médianes de 3 à 6 tours, le plus long 15 tours (3 Prêtres). Seul, à 60, le Prêtre et le Druide gagnent 10 % de leurs combats et le Nécromancien 18 % (35, 30 et 43 % à 45) : la marche du §4.
- **B16, 1 seconde par tour : la mesure ne tranche plus.** Trois mesures du même code au niveau 45, à quelques minutes d'écart : 3 Invocateurs à 1 336, 1 038 et 520 ms ; 3 Nécromanciens à 1 251, 558 et 1 110 ms. Au niveau 60 : de 327 à 814 ms pour les Invocateurs, de 607 à 966 pour les Druides. Pour savoir si l'IA du sprint 223 avait alourdi le tour, j'ai rebâti le simulateur du sprint 222 et l'ai fait tourner en alternance avec celui d'aujourd'hui : **3 Invocateurs à 1 170, 1 522 et 769 ms avec le code du S222** (286 et 313 ms ce jour-là), 1 539, 987 et 660 avec celui d'aujourd'hui. **Le code n'a pas ralenti ; la machine fait varier la mesure du simple au triple.** `--durations` lance les 60 combats en parallèle et divise un temps d'horloge par les tours : il mesure la charge de la machine autant que l'IA.
- **Sprint 231 (Q3 a, T196) : B16 tenue.** `--durations` joue maintenant les combats un à un et compte le temps de calcul. La machine au repos, à 60 : 3 Invocateurs à 97, 95 et 95 ms par tour (2 % d'écart) ; sans les tiers du JIT (`DOTNET_TieredCompilation=0`), 192, 182 et 191 ms. À 45, mesuré pendant une autre suite de tests : 108 à 138 ms, 243 à 255 sans les tiers. Le seuil d'une seconde est loin. La charge de la machine compte encore (20 à 33 % de plus avec une suite de tests à côté) : mesurer au repos.
- Combattants au plus : 31 pour 3 Invocateurs à 60 (27 à 45), la limite de 3 créatures par héros tenue.

## 7. Ce que je n'ai pas fait

- **Le playtest** : la story dit « je joue la zone ». Godot ne tourne chez moi que sans écran ; l'auto-test du village passe (`--town-selftest`, sprint 227) et la CI joue le combat des deux chefs dans le client. Le reste est pour toi, avec la build de ce sprint (§9).
- **Les textes** : les noms des 10 ressources (S224), des donjons, des clefs, des quêtes de Berthe et des succès (S227), des 12 pièces, des élixirs et des pains (S228) sont de moi, pas du scénariste. Seuls les textes des deux familles sont les siens.
- **Aucune quête annexe dans les Mines** ; le fil « Garder ou creuser » (niveau 46, annoncé par Anatole) n'est pas écrit.
- **L'IA ne voit pas les tonnelets** (`KegRisk`, T190) : la mêlée s'y fait sauter.
- **Pas d'échantillon des Mines en CI** ni de ligne dans `PartyBalanceTests` : le budget de combats est plein (19 997 sur 20 000).
- **Les dessins et les portraits** des deux familles n'ont été vus ni par toi ni par le graphiste (pas de captures pour l'instant).

## 8. À trancher par Charles

- **Q1. La marche du niveau 60** (§4) : a) **un sprint (3 points) : le deck automatique ne fait entrer un sort nouveau que s'il ne casse pas un enchaînement ou une invocation de la classe, vérifié au banc à chaque palier de sorts de 45 à 80 (55, 60, 65… ; Reco)** ; b) le laisser : un joueur de 60 compose son deck, et le banc passe à des decks écrits (`--decks`) ; c) les deux : le deck automatique réglé et le banc sur des decks écrits.
- **Q2. B4 et B6 à 60** (Prêtre et Druide sans voie sous 20, 15 lignées hors de ±8) : a) **remesurer après la Q1, sans régler les classes avant (Reco)** : la marche explique l'essentiel ; b) un sprint d'équilibrage des classes à 60 maintenant.
- **Q3. B16** (§6) : a) **mesurer en série, un combat à la fois, en temps de calcul et non d'horloge, et garder le seuil d'une seconde (1 point ; Reco)** ; b) garder la mesure actuelle et prendre la meilleure de trois ; c) mesurer dans le client, en playtest.
- **Q4. Ce qui manque à la zone** (§7) : a) **une passe du scénariste sur les textes de S224, S227 et S228, et le fil « Garder ou creuser » en quêtes annexes (environ 4 points ; Reco)** ; b) les textes seulement (1 point) ; c) plus tard, avec la refonte artistique.
- **Q5. La suite** : a) **Q1, Q3 et Q4, puis la zone de 61 à 75 au même gabarit (22 points) et un jalon 5 (Reco)** : environ 32 points, 9 sprints ; b) les trois zones jusqu'à 100 d'affilée (66 points) et deux jalons, Q1 d'abord ; c) autre chose, après ton playtest de la zone.

**Réponses de Charles (26 novembre 2026, « go », D103) :** Q1 à Q5, les recommandations. Au 230, le deck automatique qui ne casse plus un enchaînement ni une invocation, vérifié de 45 à 80, puis B4 et B6 remesurées à 60 sans régler les classes ; au 231, B16 mesurée en série, en temps de calcul ; au 232, la relecture du scénariste et le fil « Garder ou creuser » en quêtes annexes ; du 233 au 237, la zone de 61 à 75 au même gabarit (cartes, deux familles, donjons, artisanat) ; le jalon 5 au 238.

## 9. La build

Version 1.0.249, run de la CI 45682843983 (`scrum/sprint-229.md`). L'API locale (port 8002) est reconstruite avec elle ; le compte Plouf garde ses trois personnages de niveau 100.
