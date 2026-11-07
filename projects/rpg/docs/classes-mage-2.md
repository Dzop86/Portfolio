# Le Mage à quatre rangs, seconde moitié : l'Illusionniste et les 12 classes finales *(7 novembre 2026, sprint 200, à valider par Charles)*

Ce document prépare la story du sprint 200 : « En tant que Mage, je deviens Illusionniste, avec les mécaniques d'illusion, et mes finales sont écrites ; xUnit, bornes du banc. » Il suit `classes-mage.md` (sprint 199), `classes-conception.md` (§1.3, §3 : « Illusions », « Contrôle mental », « Temps, retour ») et `classes-liste.md` §3 (les noms de Charles).

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **L'Illusionniste détourne l'attention des monstres** : son **Image** a son apparence et attire leurs coups, et il frappe plus fort ce qui la regarde. Le Maître des illusions a un **Double** qui frappe ; le Mentaliste **affaiblit l'esprit** (moins de PA, moins de dégâts) et frappe ce qu'il a affaibli.
- **3 briques de moteur**, petites, parce que le Leurre (sprint 142) faisait déjà presque tout [V, `Ai.DecoyPull`, `HpOfSummoner`] :
  1. `SummonSpec.Mirror` : la créature prend l'apparence de son lanceur.
  2. `SummonSpec.Lure` : elle attire les coups des monstres à 2 cases (ce que faisait le seul Leurre, désormais une donnée ; le Leurre la garde).
  3. La règle `lure-target` : +X % sur un ennemi à N cases ou moins d'une de tes créatures qui attirent.
- **Le contrôle mental n'est pas fait** (optionnel dans la conception, §3) : le Mentaliste retire des PA et des dégâts. Voir Q1.
- **Le retour dans le temps non plus** (optionnel aussi) : le Voyageur temporel est fait de données. Voir Q2.
- **5 sorts neufs** : Image, Éblouissement (Illusionniste, 30), Double (Maître des illusions, 50), Emprise (Mentaliste, 50), et Frappe du reflet, le sort du Double.
- **Les 12 finales** remplacent les évolutions d'attente du sprint 199. Aucun Mage n'a de route en base [V, sprint 199] : pas de migration.
- **Aucun passif neuf** : le Gardien du temps reprend `spec-not-today` (le Croisé du Guerrier). Toutes les règles employées existent déjà, sauf `lure-target`.

## 1. L'arbre de l'Illusionniste

| Rang | Nœud (id) | Ce qu'il donne |
|---|---|---|
| 30 | **Illusionniste** (`illusionist`) | +10 % de dégâts sur un ennemi à 2 cases ou moins de ton Image ou de ton Double. Sorts : **Image**, **Éblouissement** |
| 50 | **Maître des illusions** (`mirage-master`) | Sort : **Double**, une image qui frappe |
| 50 | **Mentaliste** (`mentalist`) | +10 % de dégâts sur une cible sous 2 états ou plus. Sort : **Emprise** |

**La décision.** La question de la classe, « où se tient le monstre quand je frappe ? », devient pour l'Illusionniste « où regarde-t-il ? ». Ses lignées : **multiplier les images** (le Double frappe et attire, deux corps à surveiller au lieu d'un), ou **entrer dans la tête** (Emprise et Éblouissement posent chacun leurs états, le Mentaliste frappe ce qu'il a affaibli, sans image du tout s'il le veut).

**Ce qui le distingue de l'Illusionniste du Voleur** (`classes-conception.md`, §2) : celui du Mage attire et frappe avec son image, il n'échange jamais sa place avec elle.

## 2. Les sorts

| Sort | Lignée | Niveau | PA | Portée | Effets | Relance |
|---|---|---|---|---|---|---|
| Image `path-image` | Illusionniste | 30 | 2 | 1-4, sans vue | une **image** à ton apparence : 30 % de tes PV, ne joue pas, **attire** les coups des monstres à 2 cases ; une à la fois | 3 |
| Éblouissement `path-dazzle` | Illusionniste | 30 | 3 | 1-6 | dégâts au barème, **−2 PO** pendant 1 tour | 1 |
| Double `path-double` | Maître des illusions | 50 | 3 | 1-4, sans vue | un **double** à ton apparence : 15 % de tes PV, 3 PA, 3 PM, **attire** et frappe (Frappe du reflet) ; un à la fois | 3 |
| Emprise `path-grip` | Mentaliste | 50 | 3 | 1-6 | dégâts au barème (11 à 15), **−2 PA** pendant 1 tour, **−15 % de dégâts** pendant 2 tours | 2 |
| Frappe du reflet `mirror-strike` | (le Double) | — | 3 | 1-2 | dégâts 6 à 9, dans l'élément le plus fort du Mage | — |

Éblouissement et Emprise sont **adaptatifs** comme tout sort de voie. L'Image et le Double ne comptent pas comme des ennemis pour les règles « achevé » (ce sont des invocations [V, `Trigger.Kill`]).

## 3. Les trois briques du moteur

| Brique | Où | Ce qu'elle fait |
|---|---|---|
| `Mirror` | `SummonSpec`, `Fight.Summon` | l'apparence (`Look`) du lanceur, au lieu de celle de la créature ; le client la dessine sans rien de plus [V, `FighterView.Setup` lit `Spec.Look`] |
| `Lure` | `SummonSpec`, `Fighter.Lure`, `Ai` | `Ai` multiplie par `DecoyPull` (6) la valeur de frapper une créature qui attire, pour un monstre à 2 cases ou moins d'elle. Le Leurre reçoit `lure: true` : rien ne change pour lui, un combat enregistré se rejoue à l'identique |
| `lure-target` | `LureTargetRule(Percent, Cells)`, `HitContext.LureCells` | +`Percent` % si la cible est à `Cells` cases ou moins d'une créature qui attire, invoquée par le lanceur |

## 4. Les 12 classes finales

Gabarits (`FinalTemplate`) : jamais deux sœurs du même gabarit (la validation l'exige [V, `Data.CheckSpecs`]). Les identifiants sont uniques dans le jeu.

### 4.1 Arcaniste

- **Manipulateur arcanique** (`rune-weaver`) : Rune, +10 % dans tes glyphes.
  - **Maître des runes** `rune-master` (Second) : deux Runes à la fois ; Rune se relance en 1 tour.
  - **Architecte magique** `magic-architect` (Extends) : Rune porte à 8 cases et couvre un cercle de 1 ; tes glyphes durent 1 tour de plus.
  - *La décision :* semer des Runes partout, une par tour (Maître des runes), ou dessiner une grande zone, loin, qui dure (Architecte).
- **Érudit mystique** (`scholar`) : Rayon, +10 % à 6 PA et plus.
  - **Sage des arcanes** `arcane-sage` (Faster) : Rayon se relance en 1 tour, Comète aussi.
  - **Archimage** `archmage` (FirstTurn) : +3 PA au premier tour du combat ; tes sorts de 6 PA et plus font +20 % au lieu de +10 %.
  - *La décision :* le gros sort à chaque tour (Sage), ou un premier tour écrasant (Archimage).

### 4.2 Illusionniste

- **Maître des illusions** (`mirage-master`) : Double.
  - **Tisseur de mirages** `mirage-weaver` (Second) : deux Images à la fois ; Image se relance en 2 tours ; près d'elles, +15 % au lieu de +10 %.
  - **Seigneur des illusions** `illusion-lord` (Faster) : ton Double a 25 % de tes PV ; Double se relance en 2 tours.
  - *La décision :* beaucoup d'images qui distraient et que tu frappes autour, ou un double solide qui revient sans cesse.
- **Mentaliste** (`mentalist`) : Emprise, +10 % sur une cible sous 2 états.
  - **Télépathe suprême** `supreme-telepath` (Extends) : Emprise coûte 2 PA et porte à 9 cases.
  - **Dominateur mental** `mind-dominator` (TargetState) : +20 % au lieu de +10 % sur une cible sous 2 états ou plus ; Emprise se relance en 1 tour.
  - *La décision :* affaiblir de loin, souvent, plusieurs cibles l'une après l'autre (Télépathe), ou briser une seule cible, un chef (Dominateur).

### 4.3 Chronomancien

- **Manipulateur temporel** (`time-weaver`) : Accélération, 1 PA de plus en réserve.
  - **Gardien du temps** `time-keeper` (Saves) : une fois par combat, un allié à 2 cases qui tomberait reste à 1 PV (`spec-not-today`) ; Accélération porte à 7 cases.
  - **Seigneur des instants** `instant-lord` (Faster) : Accélération se relance en 1 tour.
  - *La décision :* sauver l'équipe une fois (Gardien), ou la presser à chaque tour (Seigneur).
- **Distorsionniste** (`distorter`) : Point d'orgue, Temps emprunté en 2 tours.
  - **Voyageur temporel** `time-traveller` (Extends) : Point d'orgue porte à 9 cases ; Contretemps retarde de 2 places.
  - **Maître de la distorsion** `distortion-master` (Faster) : Point d'orgue se relance en 1 tour.
  - *La décision :* jouer loin et repousser les ennemis dans l'ordre (Voyageur), ou lancer le gros sort à chaque tour (Maître).

## 5. Banc et fumée

**L'IA.** Un Leurre ne valait pour elle que 8 (une créature qui ne frappe pas) : l'Illusionniste lançait son Image 0,3 fois par combat. Une créature qui attire vaut maintenant, en plus, la moitié des coups qui menacent son lanceur, au plus ses PV (`Ai.LureValue` ; la moitié, parce qu'elle n'attire que les monstres à 2 cases d'elle). Le Leurre des talents en profite aussi. Emprise, sans dégâts, ne valait jamais un sort de 3 PA (0,0 fois par combat) : elle frappe au barème, réduit par ses retraits de PA.

**Mesuré** (`Rpg.Sim --matrix --simulate 30 --class mage`, mêmes graines) :

| Niveau | Sans voie | Arcaniste | Illusionniste | Chronomancien | Manip. arc. | Érudit | Maître des ill. | Mentaliste | Manip. temp. | Distors. |
|---|---|---|---|---|---|---|---|---|---|---|
| 35, premier jet | 41,5 | 58,5 | 43,8 | 58,8 | | | | | | |
| 50, premier jet | 55,4 | | | | 63,3 | 62,3 | **45,2** | **50,4** | 63,3 | 61,9 |
| 35, retenu | 41,5 | 58,5 | 52,7 | 58,8 | | | | | | |
| 50, retenu | 55,4 | | | | 63,3 | 62,3 | 70,0 | 67,5 | 63,3 | 61,9 |

Entre les deux : la valeur du Leurre pour l'IA (d'abord entière : Illusionniste 37,9 et Maître des illusions 73,5), l'Image de 20 à 30 % des PV, le Double de 25 à 15 % et de 4 à 3 PA, Emprise avec des dégâts.

**À 100** (`FinalsBenchTests`, hors CI, bornes indicatives) : le Mage sans voie 38,8. Rune et Point d'orgue presque jamais joués, les lignées qui en dépendent ne sont pas concluantes, ni le Manipulateur temporel (ses finales servent des alliés). Concluants : Sage des arcanes 56,0 (+0,0 sur sa lignée), Tisseur de mirages 62,5 (+1,5), Seigneur des illusions 76,5 (**+15,4**, au-delà du +10 de F1), Télépathe 70,6 (+0,4), Dominateur 75,0 (+4,8) ; le Mentaliste est à +12 de la moyenne de la classe (F4 : 8), tiré par des lignées non concluantes à 44 et 52. Le Seigneur reste à +14 ou +15 quel que soit le partage des caractéristiques de son Double (75, 50, 25 ou 0 %) : c'est la relance plus courte du Double qui compte. Voir Q5.

**La fumée** (`CiLevelsTests`) : Maître des runes, Maître de la distorsion (Rune et Point d'orgue non joués), Gardien du temps et Seigneur des instants (ils servent des alliés que la fumée n'a pas) ne changent aucun combat, et vont dans `Unchanged`. Le Maître des illusions dépense ses PA en Image et Double avant Éblouissement (`UnplayedOn`). Les 18 héros de 100 du Mage partagent le budget de la fumée : 6 combats chacun au lieu de 10, et le Maître-lame du Guerrier (Écrasement, presque jamais joué à 100) n'en change plus aucun.

## 6. À trancher par Charles

- **Q1. Le contrôle mental** (un monstre qui frappe les siens pendant un tour, chefs exclus) : a) **plus tard, quand une autre classe en aura besoin (Reco)** : il touche le choix des cibles de l'IA et le camp de chaque sort, 0,75 point à lui seul ; b) maintenant, au Dominateur mental, à la place de son +20 %.
- **Q2. Le retour dans le temps** (revenir à sa case et à ses PV d'un tour avant, une fois par combat) : a) **plus tard, comme Q1 (Reco)** ; b) au Voyageur temporel, à la place de sa portée.
- **Q3. Les noms** des 5 sorts neufs (Image, Éblouissement, Double, Emprise, Frappe du reflet) et l'anglais : noms de travail, au scénariste.
- **Q4. L'image dessinée comme le héros**, sans rien qui la distingue que l'anneau et ses PV : a) **ainsi (Reco)**, c'est le principe de l'illusion, le joueur la reconnaît à ses PV ; b) un rendu translucide (client seulement).
- **Q5. Le Seigneur des illusions à +15 sur sa lignée au banc de 100** : a) **le garder ainsi pour ce sprint, et le reprendre avec le banc en groupe (Reco)** : le banc de 100 est indicatif, et un Double qui revient plus vite est l'idée même de la finale ; b) lui retirer la relance plus courte (il change alors de gabarit).
