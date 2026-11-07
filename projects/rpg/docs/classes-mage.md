# Le Mage à quatre rangs, première moitié : l'Arcaniste et le Chronomancien *(6 novembre 2026, sprint 199, à valider par Charles)*

Ce document prépare la story du sprint 199 : « En tant que Mage, je deviens Chronomancien ou Arcaniste, avec les mécaniques du temps ; xUnit par mécanique. PloufMage reprend Plop, sans rien perdre. » L'Illusionniste et les 12 finales viennent au sprint 200. Il part de `classes-conception.md` (§1.3, §3, §4.4), de `classes-liste.md` §3, de `elements-specialisations.md` et `textes-specialisations.md` (Comète, Recueillement, Rayon, Point d'orgue, déjà conçus), et du moteur.

**Légende.** **[V]** : vérifié dans le code. **[S]** : supposé, à mesurer.

---

## 0. En bref

- **Personne n'a de route de Mage** : la page le disait « bientôt », sans un sort écrit [V, base locale : 36 Mages, aucun `Spec`]. Les identifiants sont donc refaits sans migration : `arcanist`, `illusionist`, `chronomancer`. Les textes de l'Archimage, du Maléfice et du Lève-ombres quittent `specs.json` ; leurs idées vont à l'Occultiste et au Nécromancien (`classes-conception.md` §4.4), l'historique git les garde.
- **3 effets neufs dans le moteur**, ce qu'aucune donnée ne pouvait faire : **emprunter** des PA au tour suivant, **décaler** un combattant dans l'ordre de jeu, **hâter** les relances d'un allié.
- **8 sorts de voie** : 4 pour l'Arcaniste (Comète, Recueillement, Rune, Rayon), 4 pour le Chronomancien (Temps emprunté, Contretemps, Accélération, Point d'orgue).
- **À 100** : chaque lignée garde une évolution d'attente, sans effet, jusqu'à ses deux finales au sprint 200. L'Illusionniste est écrit (noms, textes), sans sort : la page le dit « bientôt », comme avant.
- **Les soins restent au Mage** jusqu'au Prêtre (Q3 a de la conception, S201).

## 1. L'arbre

| Branche (id) | 30 | Lignée (id) | 50 |
|---|---|---|---|
| **Arcaniste** (`arcanist`) | +1 PA si tu n'as pas marché au tour d'avant (`spec-steady-mind`). Comète se relance en 2 tours. Sorts : **Comète**, **Recueillement** | **Manipulateur arcanique** (`rune-weaver`) | +10 % sur un ennemi dans une de tes glyphes. Sort : **Rune** |
| | | **Érudit mystique** (`scholar`) | +10 % avec les sorts de 6 PA et plus. Sort : **Rayon** |
| **Illusionniste** (`illusionist`) | sprint 200 | Maître des illusions (`mirage-master`), Mentaliste (`mentalist`) | sprint 200 |
| **Chronomancien** (`chronomancer`) | +1 PA à chaque tour, et tu gardes jusqu'à 2 PA d'un tour au suivant. Sorts : **Temps emprunté**, **Contretemps** | **Manipulateur temporel** (`time-weaver`) | Tu gardes 1 PA de plus. Sort : **Accélération** |
| | | **Distorsionniste** (`distorter`) | Temps emprunté se relance en 2 tours. Sort : **Point d'orgue** |

**Les décisions.** L'Arcaniste **reste en place et prépare** (le PA de plus s'il ne marche pas, Recueillement avant Comète). Ses lignées : **marquer le sol** (Rune pose une glyphe qui laisse la marque de son élément, et le Mage frappe plus fort ce qui s'y tient), ou **tout miser sur le gros sort** (Rayon, +10 % à 6 PA et plus). Le Chronomancien **joue avec les tours** : la question de la classe, « je lance maintenant, ou je garde des PA pour plus gros ? », devient « je prends au tour suivant ». Ses lignées : **jouer pour les autres** (Accélération : un allié joue plus tôt et retrouve ses sorts), ou **pour soi, plus gros** (Point d'orgue à 10 PA, que seuls la réserve et l'emprunt rendent jouable).

## 2. Les sorts

| Sort | Lignée | Niveau | PA | Portée | Zone | Effets | Relance |
|---|---|---|---|---|---|---|---|
| Comète `path-comet` | Arcaniste | 30 | 7 | 3-7 | cercle 1 | dégâts au barème | 3 (2 avec la règle) |
| Recueillement `path-composure` | Arcaniste | 30 | 2 | 0 | — | +25 % de dégâts sur soi, pour le tour | 3 |
| Rune `path-rune` | Manipulateur arcanique | 50 | 3 | 1-6 | — | glyphe ennemie de 2 tours : 10 à 14, laisse la marque de l'élément | 2 |
| Rayon `path-ray` | Érudit mystique | 50 | 5 | 1-5 en ligne, sans vue | ligne de 4 cases | dégâts au barème | 2 |
| Temps emprunté `path-borrowed-time` | Chronomancien | 30 | 1 | 0 | — | **emprunt** : +3 PA tout de suite, −3 au tour suivant | 3 |
| Contretemps `path-offbeat` | Chronomancien | 30 | 3 | 1-6 | — | dégâts au barème, **retard** d'une place | 1 |
| Accélération `path-quicken` | Manipulateur temporel | 50 | 2 | 1-5, sans vue | — | sur un allié : **relances −1**, **avance** d'une place | 3 |
| Point d'orgue `path-crescendo` | Distorsionniste | 50 | 10 | 3-7, sans vue | cercle 2 | dégâts au barème | 2 |

Rune, Comète, Rayon, Contretemps et Point d'orgue sont **adaptatifs** (l'élément le plus fort du héros), comme tout sort de voie. La ligne de 4 cases entre au barème à 0,7 (`spells_gen.py`, `AREA`).

## 3. Les trois mécaniques du temps (le moteur)

| Effet | `kind` | Ce qu'il fait | Bornes |
|---|---|---|---|
| `BorrowEffect(Ap)` | `borrow` | sur le lanceur seul : `Ap += n` tout de suite, et `n` PA de moins au début de son tour suivant (`NextTurnPenalty`, déjà employé par Soulagement) | 1 à 4, `Caster` |
| `DelayEffect(Places)` | `delay` | chaque combattant touché change de place dans l'ordre de jeu : plus tard (`Places` > 0) ou plus tôt (< 0), d'autant de combattants vivants | −3 à 3, non nul, jamais `Caster` |
| `HasteEffect(Turns)` | `haste` | chaque combattant touché voit ses relances baisser de `Turns` (une relance à 0 disparaît) | 1 à 3 |

**La règle du décalage.** Un combattant **ne passe jamais par-dessus celui qui joue** : s'il n'a pas encore joué ce tour-ci, il reste après lui ; s'il a déjà joué, il reste avant (son décalage vaut pour le tour suivant). Il ne fait donc ni deux tours dans le même, ni zéro. Celui qui joue n'est jamais décalé. Une invocation garde sa place. La frise lit l'ordre (`NextTurns`), elle suit d'elle-même ; un événement `OrderChanged` le dit au journal.

**Le relevé des combats.** Les trois effets ne se trouvent que dans des sorts neufs : un combat enregistré avant ne les rencontre pas et se rejoue à l'identique. `FightRecord` ne change pas de version (même règle que T160).

**L'IA.** L'emprunt vaut ce que le tour vaut avec ses PA en plus, moins ce qu'ils auraient valu au tour suivant (`Ai.BorrowWorth`, jamais pesé dans un tour d'emprunt). La Rune et Point d'orgue vont dans `Unplayed` de la fumée : l'une est un piège, comme les Mâchoires, l'autre n'est joué que 0,2 fois par combat. Le retard vaut 3 par place sur un ennemi, l'avance 3 par place sur un allié, chaque tour de relance rendu 2 [S, à mesurer].

## 4. Mesuré au banc (`Rpg.Sim --matrix --simulate 30 --class mage`)

| Niveau | Sans voie | Arcaniste | Chronomancien | Manip. arcanique | Érudit | Manip. temporel | Distorsionniste |
|---|---|---|---|---|---|---|---|
| 35, premier jet | 41,5 | 58,5 | **30,4** | | | | |
| 50, premier jet | 55,4 | | | 63,3 | 62,3 | **45,8** | **41,7** |
| 35, +1 PA par tour | 41,5 | 58,5 | 58,8 | | | | |
| 50, +1 PA par tour | 55,4 | | | 63,3 | 62,3 | 63,3 | 61,9 |

**Le premier jet faisait perdre le Chronomancien** (−11 à 35 sur le Mage sans voie). Ce n'étaient pas ses sorts : un Chronomancien avec Recueillement à la place de l'un, puis de l'autre, restait à 29-30. Deux sorts de voie sans gros dégâts chassent deux sorts de classe de la barre ; l'Arcaniste ne s'en sort que par Comète. Ni la réserve ni le retard n'y changeaient rien (32,3 et 31,9), un Contretemps plus cher et sans relance non plus (24,8). L'IA valorise maintenant l'emprunt (ce que les PA permettent ce tour-ci, moins ce qu'ils coûtent au suivant) : +2 seulement. **+1 PA à chaque tour** le remet au niveau de l'Arcaniste et rend Point d'orgue jouable (0,2 fois par combat). L'Illusionniste, sans sort, vaut le Mage sans voie.

## 5. PloufMage reprend Plop

Un renommage par `admin Plouf --rename Plop:PloufMage`, rien d'autre : Plop n'a pas de route, ses objets, métiers, bestiaire, quêtes et succès ne bougent pas. À la livraison du sprint, jamais en SQL.

## 6. À trancher par Charles

**Tranché le 6 novembre :** Q1 a, Q2 a, Q3 acceptée (noms de travail gardés, relecture du scénariste à venir).

- **Q1. Les textes de l'Archimage, du Maléfice et du Lève-ombres** quittent `specs.json`. a) **Oui, ils reviennent sous l'Occultiste et le Nécromancien (Reco)** ; b) on les garde quelque part dans les données.
- **Q2. L'évolution d'attente à 100**, sans effet, pendant un sprint : a) **acceptée (Reco)** ; b) on attend le sprint 200 pour livrer le Mage.
- **Q3. Les noms** (Rune, Temps emprunté, Contretemps, Accélération) et l'anglais sont des noms de travail, au scénariste.
