# Le Druide, première partie : le kit et ses glyphes partagées *(13 novembre 2026, sprint 216)*

Ce document prépare la story du sprint 216 : « En tant que Druide, mon kit partage ses glyphes avec le groupe ; xUnit. PloufDruide créé. » Le Gardien de la nature et le Métamorphe viennent au sprint 217, le Chaman et les finales au 218. Il part de `classes-conception.md` (§1.2 la fiche et le moteur, §1.3 les trois branches, §1.4 « Glyphes du Rôdeur : ennemis seulement. Glyphes du Druide : aident aussi les alliés »), de `classes-canon.md` (« il façonne un lieu pour tous ceux qui s'y trouvent ») et de `classes-liste.md` §7.

Charles a demandé d'enchaîner les sprints restants : les choix ci-dessous sont mes recommandations, appliquées ; les questions du §7 restent à trancher.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Dixième et dernière classe neuve**, id `druid` (celui de la page de création [V, `Lobby.Roster`]). 78 PV + 3, 8 PA, 4 PM, initiative 10, **budget de dégâts 1** (0,85 dans la conception, relevé au banc, §4), de 1 à 4 cases. La question de son tour : « Où je pose le terrain, et comment j'y amène tout le monde ? »
- **Son moteur, la glyphe partagée** : un même sort pose sur les mêmes cases une glyphe pour les ennemis (qui frappe ou entrave) et une glyphe pour les alliés (qui protège ou renforce). Le Rôdeur pose un terrain hostile ; le Druide pose un lieu pour tous ceux qui s'y trouvent.
- **Ce qu'elle aide, elle l'aide tout de suite** : un allié debout sur la glyphe quand elle est posée en reçoit l'effet aussitôt. Une glyphe hostile, elle, épargne ceux qu'elle trouve sur sa case jusqu'à leur tour suivant, pour qu'ils puissent en sortir [V, sprint 152].
- **Aucune branche ce sprint** : `Arena.Writing` donne 0 branche au Druide jusqu'au sprint 217. Rien n'est livré avant la build du sprint 219.

## 1. La brique : deux glyphes d'un même sort

**Avant** [V, `Fight.Lay`] : la glyphe d'un sort remplace la glyphe précédente du même sort et du même lanceur. Un sort à deux glyphes ne pouvait en garder qu'une.

**Après** : le remplacement se fait par sort, lanceur **et camp touché** (`Affects`). Un sort qui pose une glyphe `Enemies` et une glyphe `Allies` les garde toutes les deux ; il remplace les siennes à chaque lancer, comme avant. Aucun sort existant ne pose deux glyphes de camps différents [V, test], donc aucun combat déjà joué ne change.

**L'accueil** (champ `welcomes` d'une glyphe) : une glyphe qui accueille donne son effet, à la pose, aux combattants qu'elle touche déjà. Seules les glyphes alliées du Druide accueillent ; le Cercle de mousse des gardiens moussus garde sa règle d'avant, et les empreintes des combats de monstres ne bougent pas.

**L'IA** [V, `Ai.Value`] pesait déjà une glyphe alliée (le Cercle de mousse) : son statut, à 60 % de chances. Une glyphe qui accueille vaut son statut entier pour les alliés déjà dessus.

**Le barème** (`tools/spells_gen.py`) : un sort élémentaire qui pose aussi une glyphe frappe à 0,8 de son barème, la glyphe payant le reste. Les dégâts d'une glyphe hostile sont posés à la main, au tiers du coup, comme ceux des sorts de Cendre.

## 2. Les 24 sorts

| Niv. | Sort (id) | Élément | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|---|
| 1 | Épine `thorn` | Terre | 3 | 1-4 | | |
| 1 | Tison `stump-ember` | Feu | 3 | 1-4 | | |
| 1 | Bruine `drizzle` | Eau | 3 | 1-4 | | |
| 1 | Bruissement `rustle` | Air | 3 | 1-4 | | |
| 1 | Cercle de ronces `bramble-ring` | — | 3 | 1-4, cercle 1 | **partagée**, 2 tours : ennemis −1 PM ; alliés +15 % de résistance | 2 |
| 5 | Bosquet `grove` | — | 3 | 0-3, cercle 1 | **partagée**, 2 tours : ennemis −10 % de dégâts ; alliés soin de 5 par tour | 2 |
| 10 | Fouet de ronces `bramble-lash` | Terre | 4 | 1-4 | −1 PM | |
| 15 | Feu de brousse `brushfire` | Feu | 4 | 1-4 | **partagée**, 2 tours : ennemis brûlés (un tiers du coup) ; alliés +10 % de dégâts | |
| 20 | Source `spring` | Eau | 4 | 1-4 | **partagée**, 2 tours : alliés soin de 5 par tour | |
| 25 | Pollen `pollen` | Air | 4 | 1-4 | −1 PA | |
| 30 | Sève `mending-sap` | — | 3 | 0-4 | soin 14-18 d'un allié | 2 |
| 35 | Tertre `mound` | Terre | 5 | 2-4, cercle 1 | | |
| 40 | Brûlis `slash-and-burn` | Feu | 5 | 2-4, croix 1 | **partagée**, 2 tours : ennemis brûlés ; alliés +10 % de dégâts | |
| 45 | Marécage `marsh` | Eau | 5 | 2-4, cercle 1 | **partagée**, 2 tours : ennemis −1 PM ; alliés soin de 5 par tour | |
| 50 | Vent des cimes `canopy-wind` | Air | 5 | 2-4, cercle 1 | repousse d'une case | 1 |
| 55 | Terre nourricière `fertile-ground` | — | 3 | 0-4, cercle 1 | **partagée**, 3 tours : ennemis −15 % de résistance ; alliés +15 % de résistance | 3 |
| 60 | Chêne abattu `felled-oak` | Terre | 6 | 2-4 | | 1 |
| 65 | Feu de forêt `wildfire` | Feu | 6 | 2-4, cercle 1 | **partagée**, 2 tours : ennemis brûlés ; alliés +10 % de dégâts | 1 |
| 70 | Débâcle `thaw-flood` | Eau | 6 | 2-4, croix 1 | | 1 |
| 75 | Tourbillon de feuilles `leaf-storm` | Air | 6 | 2-4, cercle 1 | −1 PA | 2 |
| 80 | Enchevêtrement `tangle` | — | 3 | 1-4, croix 1 | **partagée**, 2 tours : ennemis −2 PM ; alliés +1 PM | 2 |
| 85 | Appel de la forêt `forest-call` | — | 3 | 0-4, cercle 2 | alliés +20 % de dégâts, 2 tours | 3 |
| 90 | Pas de mousse `moss-step` | — | 1 | 0 | +2 PM, 1 tour | 2 |
| 95 | Cercle sacré `sacred-grove` | — | 4 | 0-3, cercle 2 | **partagée**, 3 tours : ennemis −1 PA ; alliés +20 % de résistance | 4 |

Dix sorts posent une glyphe partagée, dont les deux du niveau 1 et 5 : le moteur se joue dès le début. Aucun sort neutre ne fait de dégâts (D62). Les dégâts suivent le barème à 1 (§4) ; leurs 96 jumeaux de composés aussi. Noms de travail, au scénariste. Silhouettes de game-icons.net (CC BY 3.0).

## 3. Ce qui le sépare des autres

| Avec | Règle | Testée |
|---|---|---|
| Rôdeur, Malandrin, Arcaniste | leurs glyphes ne touchent que les ennemis ; **chaque glyphe d'ennemis du Druide va avec une glyphe d'alliés**, sur les mêmes cases | `DruidTests` |
| Prêtre | le Druide soigne par le lieu (une glyphe qui soigne à chaque tour) et une seule Sève ; il ne relève jamais, ne purge rien | les données |
| Paladin | le Paladin protège en frappant à 2 cases ; le Druide protège là où il a posé, sans frapper | les données |

## 4. Mesures

Banc (`Rpg.Sim --matrix --class druid --simulate 30`, les 16 groupes de 4 et 5 monstres, héros nu, sans voie) et comptes des sorts lancés (15 combats par groupe). La colonne « Druide à 1 » vient du banc lui-même (36,5 au 35, 17,1 au 60, 15,6 au 100) ; l'outil de comptage, à 15 combats, donnait 38,8 et 14,6.

| Niveau | Druide à 0,85 | Druide à 1 (gardé) | Nécromancien | Autres classes sans voie, même banc |
|---|---|---|---|---|
| 35 | 20,6 (18,3) | 36,5 | 37,9 | Invocateur 57,5, Paladin 50,0, Mage 39,4, Prêtre 23,8 |
| 60 | 6,9 | 17,1 | 21,7 | Mage et Invocateur 47,9, Paladin 34,2, Prêtre 9,2 |
| 100 | 0,2 | 15,6 | 12,9 | Mage 42,5, Paladin 31,3, Invocateur 25,4, Prêtre 8,3 |

**Le réglage.** Au barème de la conception (0,85), seul, le Druide ne tenait pas : de 1 à 4 cases, les monstres l'atteignent, et ses coups qui posent une glyphe paient une part que seul il n'utilise guère. Ses glyphes partagées de niveau 1 et 5 ne sont presque pas lancées en solo (Cercle de ronces 0,0, Bosquet 0,0 ; Source 1,7 et Feu de brousse 0,6 par combat au niveau 35) : elles servent un groupe. **Barème relevé à 1**, comme le Guerrier, le Mage et l'Archer : il rejoint le Nécromancien et reste au-dessus du Prêtre. Q3.

**Les bornes.** B4 (héros sans voie entre 20 et 75) : tenue à 35 ; **pas à 60 (17,1) ni à 100 (15,6)**, comme le Nécromancien au 100 et le Prêtre. Le jalon 3 lira les classes de soutien ensemble.

**Les exigences de la CI** (`BalanceTests`, `CiLevelsTests`) : en solo, la grande salle de la Bauge, les hordes de la Forêt et le Disjoint seul au niveau 30, tenus avec le Druide parmi les classes jugées. **Le budget de combats** (D96) était dépassé (20 033 pour 20 000) : le plancher de 50 combats par classe de `Share` faisait grandir la salle de la Bauge et le Disjoint avec les classes ; ils partagent désormais 400 combats, 30 au moins par classe (40 à dix). **Les groupes de référence** (hordes au niveau 45, 15 combats par groupe) : Guerrier + Druide 55 %, Voleur + Druide 55 %, Berserker + Druide 66 %, Paladin + Nécromancien + Druide 88 %, Archer + Mage + Druide et Voleur + Berserker + Invocateur + Druide 100 %. **Les autres classes** jouent les mêmes combats qu'avant (empreinte en solo inchangée) : la glyphe qui accueille n'est qu'au Druide, et un sort à une seule glyphe la remplace comme avant.

## 5. PloufDruide

Le compte de test reçoit un Druide de niveau 100 par la commande admin (`admin Plouf --create druid:PloufDruide --level 100`), quand Docker tourne.

## 6. Ce qui vient ensuite

- **Sprint 217** (fait, `classes-druide-2.md`) : le Gardien de la nature (terrain qui protège : Sylvain, Protecteur) et le Métamorphe (formes au contact, réversibles, sans coût en PV : Changeforme, Druide sauvage), leurs lignées et leurs finales.
- **Sprint 218** : le Chaman (esprits sans corps, terrain qui frappe : Invocateur spirituel, Élémentaliste naturel), ses finales, les bornes du banc.

## 7. Questions à Charles

- **Q1. Les noms** des 24 sorts : noms de travail, au scénariste (Reco).
- **Q2. L'accueil** : a) **seules les glyphes alliées du Druide aident ceux qui s'y tiennent déjà (Reco, fait)** ; b) toutes les glyphes alliées, Cercle de mousse des monstres compris (les combats de la Forêt changent).
- **Q3. Le barème du Druide** : a) **1, mesuré (Reco, fait)** ; b) 0,85 de la conception, et le Druide sans voie à 20,6 au niveau 35.
