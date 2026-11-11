# L'Invocateur, troisième partie : le Démoniste et le Conjurateur *(11 novembre 2026, sprint 213, à valider par Charles)*

Ce document prépare la story du sprint 213 : « En tant qu'Invocateur, je deviens aussi Démoniste ou Conjurateur, et mes finales sont écrites ; xUnit, bornes du banc. » Il part de `classes-conception.md` (§1.3 « Démoniste : des pactes, ses PV contre de la puissance ; Conjurateur : créatures posées, immobiles », §1.4 la paire Chaman / Conjurateur, §7.3 les bornes), de `classes-canon.md` (§1 « un pacte est une contrainte réciproque », « des créatures d'un seul élément, ou les figures du ciel des observatoires ») et de `classes-liste.md` §10.1 et §10.2. Avec lui, l'Invocateur a ses trois branches, comme les sept autres classes.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Le Démoniste paie.** Son Démon est nourri plus que toute autre créature (25 % des PV, 40 % des caractéristiques), mais à chacun de ses tours il prend 3 % des PV maximum de l'Invocateur : c'est **la dîme** (règle `tithe`), la seule brique neuve du sprint. Le Pacte de sang échange 8 PV contre 25 % de dégâts, pour lui et ses alliés à 2 cases, son Démon compris.
- **Le Conjurateur pose.** Ses Piliers ne bougent pas (0 PM) : ils tiennent une case et tirent de 1 à 4 cases. L'Ancrage protège un Pilier et ce qui l'entoure. Aucune brique neuve : une créature sans PM existait déjà (l'Image de l'Illusionniste [V]).
- **Les choix des lignées**, une décision chacun : l'Invocateur démoniaque nourrit son Démon (et le paie plus cher), le Pactisant paie pour ses propres coups ; l'Invocateur élémentaire multiplie les Piliers et les fait frapper, l'Invocateur astral trace ses figures au sol (des glyphes) au lieu de lever des créatures.

## 1. Le Démoniste (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Démoniste** (`demonist`) | Démon nourri à 25 % des PV, 40 % des caractéristiques ; **dîme** de 3 % des PV maximum à chacun de ses tours | **Démon** (`path-demon`, 4 PA, 1-2, relance 4) ; **Pacte de sang** (`path-blood-pact`, 1 PA, 8 PV, cercle de 2 autour de soi : alliés +25 % de dégâts 2 tours, relance 3) |

Le Démon (`demon`) : modèle de cerf, 6 PA, 4 PM, initiative 55, **Lacération** (`demon-rend`, 3 PA, au contact, 8-12, deux fois, dans l'élément de l'Invocateur). Il compte dans les 3 créatures d'un héros.

**La dîme** (`TitheRule`, `Fight.Tithe`) : au début de chaque tour d'une créature dont l'invocateur vivant a une dîme pour son espèce, l'invocateur paie ce pourcentage de ses PV maximum, jamais son dernier PV ; les règles d'une espèce s'additionnent ; le journal le montre comme un sort payé en PV (`HpPaid`). L'IA la compte comme un coût en PV : la moitié de ce qu'elle prendrait sur 2 tours (`Ai.TitheCost`), retirée de ce que vaut la créature.

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Invocateur démoniaque** (`demon-caller`) | Démon nourri à 35 % et 50 % ; dîme +1 % (4 % en tout) ; **Déchaînement** (`path-unleash`, 2 PA, 6 PV, sur le Démon : +30 % de dégâts 2 tours, +2 PM 1 tour) | **Seigneur infernal** (`infernal-lord`, Extends : Déchaînement donne aussi 2 PA) ; **Archidémoniste** (`archdemonist`, Faster : Démon relancé en 2 tours) |
| **Pactisant** (`pactbearer`) | **Prix du sang** (`path-blood-price`, 4 PA, 1-5, 10 PV, relance 1, au budget plein) | **Maître des pactes** (`pact-master`, Extends : Pacte de sang donne aussi 1 PA) ; **Élu des abysses** (`abyss-chosen`, Form : Pacte de sang laisse 15 % de dégâts pour tout le combat) |

## 2. Le Conjurateur (niveau 30)

| Rang | Règles | Sorts |
|---|---|---|
| **Conjurateur** (`conjurer`) | Pilier nourri à 25 % des PV, 40 % des caractéristiques | **Pilier** (`path-pillar`, 3 PA, 1-3, deux au plus, relance 2) ; **Ancrage** (`path-anchor`, 2 PA, 1-6, sur un Pilier, cercle de 1 : bouclier 20 aux alliés, 2 tours, relance 2) |

Le Pilier (`pillar`) : modèle de golem à distance, 6 PA, **0 PM**, initiative 30, **Éclat du pilier** (`pillar-shard`, 3 PA, 1 à 4 cases, 5-8, deux fois, dans l'élément de l'Invocateur).

**La paire Chaman / Conjurateur** (§1.4 de la conception) : les créatures du Conjurateur occupent une case et la gardent ; les esprits du Chaman (Druide, à venir) n'en occupent pas. Vérifié sur les données : le Pilier n'a aucun PM (test).

| Lignée | Règle et sort | Finales (gabarit) |
|---|---|---|
| **Invocateur élémentaire** (`elemental-caller`) | un troisième Pilier (`maxSummons` 3) ; **Surcharge** (`path-overload`, 2 PA, sur un Pilier, cercle de 1 : alliés +35 % de dégâts 2 tours, relance 2) | **Seigneur élémentaire** (`elemental-lord`, Faster : Pilier relancé en 1 tour) ; **Primordial** (`primordial`, Extends : les Piliers grandissent de 5 % par tour, 25 % au plus, la croissance du Familieriste) |
| **Invocateur astral** (`astral-caller`) | **Constellation** (`path-constellation`, 3 PA, 2-6, cercle de 1 : glyphe 2 tours, 9-12 et −1 PM aux ennemis qui y passent, relance 2) | **Maître cosmique** (`cosmic-master`, Extends : −10 % de résistance 2 tours aux ennemis touchés) ; **Architecte astral** (`astral-architect`, Second : deux Constellations à la fois) |

## 3. Réglages en route

- **Le Pacte de sang** sur l'Invocateur seul n'était jamais joué (0,0 par combat) : son budget de 0,75 rend ses coups faibles, et 25 % de peu ne valait pas 8 PV. Étendu aux alliés à 2 cases, son Démon compris : joué.
- **La Surcharge** donnait d'abord 3 PA au Pilier ; mais l'Éclat se lance deux fois par tour au plus : 3 PA de plus ne servaient à rien (0,2 par combat). Devenue +35 % de dégâts autour du Pilier.
- **La dîme** à 4 % et +2 % : au niveau 100, le héros du banc a 889 PV (son équipement), la dîme prenait 53 PV par tour, et l'IA plaçait le Démon (19,5) sous le Molosse (44,7) : jamais appelé. Sans dîme, l'Invocateur démoniaque montait à +53,8 au niveau 100. À 3 % et +1 % : appelé, +27,5.
- **Le Pilier à 30 % des caractéristiques** valait le Feu follet au point près (38,7), et l'Invocateur élémentaire restait à −11,5 de la moyenne au niveau 100 ; à 40 % : +5,9.

## 4. Mesures

`Rpg.Sim --matrix --class summoner --simulate 30` (30 combats sur 16 groupes, sorts de voie rangés), puis `--level 100`.

| Niveau | Voie | Points | Sur le héros sans voie | Sur la moyenne |
|---|---|---|---|---|
| 35 | Démoniste, Conjurateur, Familieriste | 81,3, 74,6, 75,6 | +17,9 à +24,6 | −2,6 à +4,1 |
| 45 | Démoniste, Conjurateur, Familieriste | 89,2, 79,0, 83,3 | +16,9 à +27,1 | −4,9 à +5,3 |
| 50 | Invocateur démoniaque, Pactisant | 84,0, 79,6 | +21,9, +17,5 | +6,1, +1,7 |
| 50 | Invocateur élémentaire, astral | 78,3, 72,1 | +16,3, +10,0 | +0,5, −5,8 |
| 50 | Dompteur, Fusionniste (S212) | 86,7, 66,7 | +24,6, +4,6 | +8,8, −11,2 |
| 100 | Invocateur démoniaque et ses finales | 52,3 à 52,5 | +27,3 à +27,5 | +0,7 à +0,9 |
| 100 | Pactisant et ses finales | 63,8 à 64,0 | +38,8 à +39,0 | +12,2 à +12,4 |
| 100 | Invocateur élémentaire et ses finales | 56,7 à 57,5 | +31,7 à +32,5 | +5,1 à +5,9 |
| 100 | Invocateur astral et ses finales | 48,5 à 51,0 | +23,5 à +26,0 | −3,0 à −0,5 |

**Les bornes (§7.3 de la conception).**

- **B5** (rang 2 à 35 et 45, ±8 de la moyenne de la classe) : **tenue**, de −4,9 à +5,3.
- **B6** (rang 3 à 50, 8 points au plus entre les lignées d'une branche, ±8 de la moyenne) : **tenue pour les deux branches neuves** (4,4 et 6,2 points entre lignées). **Pas pour le Familieriste du S212** : 20 points entre Dompteur et Fusionniste, le Fusionniste à −11,2 (Q4).
- **B8** (rang 4, indicatif) : les finales restent à 2,5 points de leur lignée au plus. Celles de l'Invocateur démoniaque ne changent presque rien au niveau 100 : il y appelle son Démon 0,3 fois par combat, ses 3 créatures étant déjà prises par les Molosses et le Feu follet.
- Le Pactisant est haut au niveau 100 (+12,4 de la moyenne) : ses 10 PV fixes ne pèsent rien sur 889 PV, la limite déjà connue des coûts fixes du Berserker.

## 5. À trancher par Charles

- **Q1. Les noms** : Démon, Lacération, Pacte de sang, Déchaînement, Prix du sang, Pilier, Éclat du pilier, Ancrage, Surcharge, Constellation ; les titres viennent de ta liste.
- **Q2. La dîme** : a) **3 % des PV maximum par tour du Démon, 4 % pour l'Invocateur démoniaque (Reco, fait)** ; b) aucune dîme, un Démon simplement fort (+53,8 au niveau 100 : trop) ; c) un nombre fixe de PV, comme les sorts du Berserker (pèse beaucoup au niveau 30, rien au niveau 100).
- **Q3. L'Invocateur astral trace des glyphes** au lieu de lever des créatures : a) **oui, c'est sa décision face à l'élémentaire : le sol ou les créatures (Reco, fait)** ; b) une créature astrale qui soutient ses alliés : l'IA des créatures ne sait que frapper, à écrire.
- **Q4. Le Fusionniste du S212 sous la borne B6** (−11,2 de la moyenne à 50, 20 points sous le Dompteur) : a) **le régler au jalon 3 (S219), avec toute la classe (Reco)** ; b) maintenant, ce qui rouvre les mesures du S212.
