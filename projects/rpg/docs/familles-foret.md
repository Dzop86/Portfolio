# Osmose : les familles de la Forêt des Aïeux (S132 et S133)

> **Version 1, 23 octobre 2026.** Conception du game designer, à remettre à Claude Code pour les sprints 132 (les Sylvecerfs) et 133 (les Gardiens moussus). Plan validé par Charles : D79, `docs/suite-apres-d69.md` §11.6.
> Gabarit suivi : `docs/design-familles.md` §3 (la grammaire) et §4 (les tableaux). Rédigé après lecture de : `src/Rpg.Core/Effects.cs`, `Fight.cs` (glyphes), `Ai.cs` (`GlyphRisk`, `GlyphOdds`), `Templates.cs`, `Monsters.cs` (`MonsterGroups`), `Items.cs`, `Progression.cs` (`Kit`, `LevelGap`), `Data.cs` (`ValidGlyph`), `tests/Rpg.Core.Tests/GlyphTests.cs`, `TemplateTests.cs`, `BalanceTests.cs`, `data/monsters.json` (abeilles, taupes), `tools/spells_gen.py`, `tools/sets_gen.py`, `tools/world_gen.py`, `godot/Creatures.cs`, `DECISIONS.md` (T96 à T99), `docs/chapitre-2-pistes.md` §5.
> Les noms et les descriptions seront relus par le scénariste : ils restent sobres et ne reprennent rien d'Ankama (D54).

## 0. Mode d'emploi pour Claude Code

- **Sources à modifier** (comme en D69) : les sorts et les passifs dans `tools/spells_gen.py` (`S(...)`, `ranks=False`, critique 10 ; `P(...)`), les invocations dans `young(...)` du même fichier ; les monstres dans `data/monsters.json` ; les panoplies dans `tools/sets_gen.py` ; les cartes dans `data/maps/` ; les dessins des monstres dans `godot/Creatures.cs`.
- **Conventions des tableaux de sorts** (ordre des colonnes de `design-familles.md` §4, qui suit les arguments de `S(...)`) : id · nom fr / en · PA · portée min-max · ligne de vue · en ligne · dégâts min-max · lancers par tour · élément · relance (`cd`) · zone · effets. Tous les sorts sont **(new)**.
- **Écriture d'un glyphe** : `glyph(Affects, tours, dégâts min-max, élément[, état, valeur, tours d'état])`, soit `GlyphEffect(Affects, Turns, DamageMin, DamageMax, Element, Status, Value, StatusTurns)`. Les tours sont ceux **du lanceur** (T98).
- **Les chiffres sont ceux des gabarits** de `Templates.cs` au niveau de chaque monstre, sans aucun écart. Ce sont des points de départ : chaque sprint se termine par la mesure T41, et on règle **d'abord les PV, puis les dégâts des glyphes, puis leurs tours**. On ne touche à une mécanique qu'en le notant dans `DECISIONS.md`.
- Les points marqués **[À TRANCHER]** ont une valeur par défaut (§7) : l'utiliser et la noter dans `DECISIONS.md`.

### 0.1 Ce que la lecture du moteur change à la conception

1. **Un glyphe posé sous un héros le frappe toujours à son prochain tour.** Le moteur frappe « celui qui commence son tour » sur le glyphe, et le héros ne joue pas entre la pose et le début de son tour (test `AGlyph_StrikesAtTheStartOfTheTurn_AfterThePoisons_BeforeThePassives`, et le second cas de `TheAi_GoesRoundAGlyph_AndStepsOffOne`). La phrase de T98 « celui qui s'y tient est prévenu et peut partir » est donc inexacte : il est prévenu, mais il prend le premier coup avant de pouvoir partir. Ce qu'on évite vraiment, ce sont **les coups suivants** et **l'entrée** sur un glyphe. **Conséquence :** tous les glyphes ci-dessous comptent le premier coup comme acquis. Leurs dégâts valent environ **40 % du sort principal** du lanceur, et un glyphe ne pose jamais d'état qui empêche de repartir (au plus −2 PM sur 3). L'option moteur qui tiendrait la promesse de §11.6 est au §7, point 1.
2. **Un glyphe peut viser les alliés** (`Affects.Allies` avec un état, accepté par `ValidGlyph`, testé dans `AGlyph_IsChecked_AsTheDataLoads`). `Allies` comprend le lanceur (`Fight.Touches`). C'est la leçon des Gardiens : leurs glyphes ne piègent pas le héros, ils **renforcent les gardiens qui s'y tiennent**.
3. **Un état posé au début du tour de son porteur compte ce tour-là** (§2.1 de `design-familles.md`), puis il est retiré à la fin du tour si ses tours sont à 1. Un glyphe allié de résistance doit donc durer **2 tours d'état** pour couvrir aussi les tours des héros qui suivent.
4. **L'IA** pose un glyphe là où il touche un combattant (60 % de sa valeur, `GlyphOdds`), contourne les glyphes ennemis et quitte ceux qu'elle a sous les pieds. Elle **ne cherche pas** un glyphe allié et ne pousse pas exprès un héros sur un glyphe. Les combos (poussée ou attirance vers un glyphe) arriveront donc par hasard dans la simulation, et plus souvent chez un joueur qui les subit sans les voir venir : on en tient compte dans les dégâts.
5. **Aucun passif ne peut poser de glyphe** (refusé au chargement) : les passifs ci-dessous n'en posent pas.
6. **`tools/spells_gen.py` n'a pas encore de fonction `glyph()`** (T98 n'a écrit de glyphes que dans les tests) : à ajouter (§6.1).
7. **`tools/sets_gen.py` attend 5 pièces par famille, des bonus à 2, 3 et 5 pièces, une pièce par monstre et une clef de donjon dans chaque butin** : à adapter pour 4 pièces et des familles sans clef avant S134 (§6.2).
8. **Les monstres sont dessinés par le jeu** (`godot/Creatures.cs`, une fonction par famille) : il faut deux dessins nouveaux, `Stag(...)` et `Keeper(...)`, sur le modèle de `Wolf` et `Mole` (§7, point 6).

---

## 1. Les deux familles en une page

| Famille | Zone (niv.) | Élément | Faible à | Leçon | Passif de famille |
|---|---|---|---|---|---|
| Sylvecerfs (`stag`) | Forêt des Aïeux (31) | Terre | **Feu** | **lire le sol** : contourner les glyphes, ne pas finir son tour sur une case marquée ni dos à un glyphe face à un cerf qui pousse | `skittish` (`Hit` mêlée) |
| Gardiens moussus (`golem`) | Forêt des Aïeux (31) | Terre | **Air** | **déloger** : sortir un gardien de son cercle (pousser, attirer) ou occuper le cercle avant de frapper | `cairn` (`AllyDies`) |

**Le contraste voulu entre les deux familles :** les cerfs **piègent** le sol (glyphes ennemis : dégâts, PM), les gardiens le **tiennent** (glyphes alliés : résistance, dégâts). Le joueur apprend les glyphes dans les deux sens, et il change de deck : Feu contre les cerfs, Air contre les gardiens.

**Les faiblesses.** Le tableau de §3 donne l'Air aux sangliers et aux crabes, le Feu aux loups et aux champignons, l'Eau aux zombies et aux taupes, la Terre aux grenouilles et aux abeilles. Pour les deux familles de Terre :
- **Sylvecerfs → Feu** : du bois vivant, une pierre qui éclate à la chaleur.
- **Gardiens → Air** : un vent sec dessèche la mousse qui tient leurs pierres ensemble.
- Une faiblesse Eau aurait été illogique pour des êtres de mousse. La Terre est exclue (c'est leur élément).
- **Conséquence pour la suite du chapitre 2 :** si l'on garde « chaque élément est la faiblesse de deux familles » sur les huit familles des premières zones des quatre régions, il restera à placer **Terre ×2, Eau ×2, Feu ×1 et Air ×1** sur les six familles de la Côte, des Plaines et des Falaises.

**Les chiffres de grammaire** (§3) : faiblesse **−15 %** pour les monstres, **−5 %** pour les chefs ; un tank qui protège ou contrôle ; un soutien qui repositionne ou renforce ; un chef à deux phases (`LowHp` à 50 %) avec un signe narratif.

---

## 2. Choix transverses

### 2.1 Les identifiants
- **`stag`** et **`golem`** : un seul mot anglais, comme les huit familles existantes (`boar`, `wolf`, `mole`…), parce que les dérivés se construisent dessus : `stag-melee` pour l'allure, `dungeon-stag-1` pour les cartes de donjon en S134, et ainsi de suite.
- `sylvan-stag` donnerait `dungeon-sylvan-stag-1`, long pour rien. `keeper` est écarté : c'est déjà l'id du gardien de salle dans les cartes de donjon (`"id": "keeper"`, dialogue `dungeon-keeper`), et une recherche dans le dépôt mélangerait les deux.
- Les noms vus par le joueur restent « Sylvecerfs / Sylvan Stags » et « Gardiens moussus / Mossy Keepers ». La quête `mossy-keepers` (S136) comptera la famille `golem`.

### 2.2 Les niveaux [À TRANCHER]
- **Ce qui contraint :** toutes les cartes de la forêt sont au niveau 31 (`world_gen.py`), et la mesure T41 de la zone se fera à ce niveau (`BalanceTests`, `InlineData("ancestors", 31)`). Surtout, un groupe tire sa famille et ses rôles **au hasard sur n'importe quelle carte** (`MonsterGroups.For`) : la profondeur ne change que la taille des groupes. Un monstre de niveau 44 pourrait donc se tenir, en paire, à l'entrée, contre un héros de 31.
- **Ma proposition :** les monstres communs de **31 à 37**, les deux chefs à **45**.
  - Sylvecerfs de 31 à 34 : on les rencontre d'abord, ils apprennent les glyphes.
  - Gardiens de 34 à 37 : la quête `mossy-keepers` est au niveau 36.
- **Ce qui porte la zone jusqu'à 45 :** les hordes du fond (6 à 10 monstres), les deux chefs, les donjons (S134, salle 2 au niveau 40), les quêtes et les ressources 40 et 45.
- **L'XP reste juste :** à 44, un monstre de 34 rapporte 360 points, soit environ 12 monstres par niveau (pas de perte d'XP avant 20 niveaux d'écart, `LevelGap`).
- **Alternative :** étirer les communs jusqu'à 41. La pente des gabarits est douce (+1 PV et +0,4 de dégâts par niveau), mais T41 à 31 deviendrait fragile. À mesurer si Charles le préfère.

### 2.3 Les gabarits (T96), recopiés pour chaque monstre

| Monstre | Rôle | Niv. | PV | Sort principal (jet) | Soin | Initiative (gabarit) |
|---|---|---|---|---|---|---|
| `carving-hind` | Ranged | 31 | 51 | 16-20 | — | 150 |
| `brocket` | Melee | 32 | 62 | 19-25 | — | 126 |
| `lead-doe` | Support | 33 | 65 | 19-25 | 16-20 | 109 |
| `old-hart` | Tank | 34 | 75 | 19-25 | — | 83 |
| `elder-stag` | Boss | 45 | 577 | 38-50 | — | 127 |
| `pebble-keeper` | Ranged | 34 | 54 | 17-21 | — | 157 |
| `cobble-keeper` | Melee | 35 | 65 | 21-27 | — | 131 |
| `moss-tender` | Support | 36 | 68 | 20-26 | 17-21 | 114 |
| `slab-keeper` | Tank | 37 | 79 | 20-26 | — | 86 |
| `menhir-keeper` | Boss | 45 | 577 | 38-50 | — | 127 |

- Les valeurs viennent de `Templates.Monster(rôle, niveau)` (divisions entières), le jet de `Roll` (moyenne ± un septième) et l'initiative de `Templates.Initiative`, que le chargement vérifie à 10 % près.
- Résistance à la Terre selon le gabarit : 10 (25 pour le tank, 30 pour le chef).
- Écart au gabarit : **0** pour chaque monstre, donc la moyenne de famille de `FamilyTests` tient d'office.
- **[À TRANCHER] La faiblesse du chef.** Le gabarit `Boss` de `Templates.cs` porte −20, mais §3 et les huit chefs de Clairval portent −5. J'applique **−5** : le chargement ne vérifie pas la faiblesse, et `TemplateTests` exige seulement qu'elle soit négative.

### 2.4 Les panoplies de 4 pièces
- **Les familles de Clairval en ont 5 :** une pièce par monstre commun (8 %), plus l'arme du chef (30 %), qui laisse aussi les autres pièces à 8 %.
- **Ici, 4 pièces :** celles du mêlée, du distance et du tank, plus l'arme du chef. **Le soutien n'en donne aucune.** C'est le monstre le plus rare : jamais seul, jamais dans un groupe de 2, au plus un tiers du groupe (`MonsterGroups.Draw`). Sa pièce serait la plus longue à obtenir seul. À la place, il laisse les deux ressources de la famille plus souvent.
- **Bonus à 2, 3 et 4 pièces.** La pièce de 4 est l'arme de niveau 45 : la panoplie complète vient avec le niveau 45, comme le chef.
- **Les deux panoplies se chevauchent par les mains.** Celle des cerfs a une arme à deux mains, celle des gardiens une arme à une main et un bouclier, et `Equipment.Problem` interdit de porter les deux. Le joueur choisit donc :
  - **Sylvecerfs 4 + Gardiens 2** : +1 PM, la mobilité qui sert contre les glyphes ;
  - **Gardiens 4 + Sylvecerfs 3** : de la vitalité et de la Terre.
  
  Aucune des deux voies ne domine (calcul au §3.6 et au §4.6). Aucune ne donne de PA.

### 2.5 Les cartes de combat
- **Une carte nouvelle par famille** (§5) : `ancestors-trunks` (« Futaie », S132) et `ancestors-stones` (« Cercle de pierres », S133).
- **Comme les paires de Clairval (§5.1), les deux familles partagent les deux cartes à partir de S133** : `battles` vaut `[ancestors-trunks, ancestors-stones]` pour `stag` et `[ancestors-stones, ancestors-trunks]` pour `golem`. En S132, les cerfs n'ont que la Futaie.
- Arriver avec S133 change la carte de certains groupes de cerfs. Ce n'est pas un problème, parce que la mesure T41 de S133 couvre toute la zone.
- **Les cartes des chefs** (`bossMap`), toutes deux au fond (profondeur 2) :
  - `stag` : `ancestors-0-4` ;
  - `golem` : `ancestors-4-4`.
  
  À vérifier avec `MonsterGroups.Depth` ; à défaut, n'importe quelle carte de profondeur 2.

---

## 3. S132 : les Sylvecerfs (`stag`), Forêt des Aïeux

*Narratif : des cerfs dont le bois est de pierre gravée. Comme tous les cerfs, ils grattent le sol et y frottent leurs bois pour marquer leur territoire ; ici, leurs grattis s'allument de marques. La ramure de l'aïeul porte quatre signes autour d'un creux gratté, celui de la tombe grattée de Clairval et de l'inscription du passage (`the-unmapped-passage`). (Corrigé d'après la relecture du scénariste, `familles-foret-textes.md` §3.)*

**Leçon : lire le sol.** La biche pose des glyphes qui brûlent, le vieux cerf des glyphes qui alourdissent, le daguet et l'aïeul poussent ou attirent vers eux. Le joueur qui a compris :
- regarde le sol avant de marcher (le chemin montré fait déjà le détour, T98) ;
- ne finit plus son tour sur une case marquée ;
- ne reste pas dos à un glyphe face à un daguet.

### 3.1 Les monstres

| Monstre | fr / en | Rôle | Niv. | PV | PA | PM | Init. | Allure |
|---|---|---|---|---|---|---|---|---|
| `brocket` | Daguet de pierre / Stone Brocket | Melee | 32 | 62 | 6 | 4 | 126 | `stag-melee` |
| `carving-hind` | Biche graveuse / Carving Hind | Ranged | 31 | 51 | 6 | 3 | 150 | `stag-ranged` |
| `old-hart` | Vieux cerf / Old Hart | Tank | 34 | 75 | 6 | 3 | 83 | `stag-tank` |
| `lead-doe` | Biche meneuse / Lead Doe | Support | 33 | 65 | 6 | 3 | 109 | `stag-support` |
| `elder-stag` | Cerf-Aïeul / Elder Stag | Boss | 45 | 577 | 8 | 3 | 127 | `stag-boss` |

| Monstre | Sorts (dans l'ordre) | Résistances | Passifs |
|---|---|---|---|
| `brocket` | `antler-jab`, `head-toss` | Earth 10, Fire −15 | `skittish` |
| `carving-hind` | `antler-splinter`, `scrape` | Earth 10, Fire −15 | `skittish` |
| `old-hart` | `antler-butt`, `heavy-ground` | Earth 25, Water 10, Fire −15 | `skittish` |
| `lead-doe` | `warm-breath`, `doe-call`, `hoof-kick` | Earth 10, Fire −15 | `skittish` |
| `elder-stag` | `elder-antlers`, `antler-hook`, `ancestors-ring` | Earth 30, Water 15, Air 15, Fire −5 | `skittish`, `cracked-antlers` |

- **Le daguet a 4 PM** : des cerfs rapides. **Tank qui contrôle** : le vieux cerf et sa Terre lourde (−2 PM). **Soutien qui repositionne** : la meneuse tire un cerf blessé hors du contact. Le chef **n'a pas d'invocation** : six des huit chefs de Clairval en ont une, et sa seconde phase porte plutôt sur le tempo (+2 PA).

### 3.2 Les sorts

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `antler-jab` | Coup d'andouiller / Antler Jab | 3 | 1-1 | oui | non | 19-25 | 2 | Earth | 0 | — | — |
| `head-toss` | Coup de tête / Head Toss | 3 | 1-1 | oui | non | 10-13 | 1 | Earth | 1 | — | push(Enemies, 2) |
| `antler-splinter` | Esquille de bois / Antler Splinter | 3 | 2-6 | oui | non | 16-20 | 2 | Earth | 0 | — | — |
| `scrape` | Grattis / Scrape | 2 | 2-5 | oui | non | 0-0 | 1 | Neutral | 2 | Circle 1 | glyph(Enemies, 2, 7-9, Earth) |
| `antler-butt` | Coup de massacre / Antler Butt | 3 | 1-1 | oui | non | 19-25 | 2 | Earth | 0 | — | — |
| `heavy-ground` | Terre lourde / Heavy Ground | 3 | 1-3 | oui | non | 0-0 | 1 | Neutral | 2 | Cross 1 | glyph(Enemies, 2, 0-0, Neutral, Mp, −2, 1) |
| `warm-breath` | Souffle tiède / Warm Breath | 3 | 0-4 | oui | non | 0-0 | 1 | Neutral | 1 | — | heal(Allies, 16, 20) |
| `doe-call` | Appel de la meneuse / Lead Doe's Call | 2 | 2-4 | oui | non | 0-0 | 1 | Neutral | 2 | — | pull(Allies, 2) |
| `hoof-kick` | Ruade / Hoof Kick | 3 | 1-2 | oui | non | 19-25 | 1 | Earth | 0 | — | — |
| `elder-antlers` | Massacre de l'aïeul / Elder's Antlers | 4 | 1-1 | oui | non | 38-50 | 1 | Earth | 0 | — | push(Enemies, 1) |
| `antler-hook` | Crochet d'andouiller / Antler Hook | 3 | 2-4 | oui | non | 14-18 | 1 | Earth | 1 | — | pull(Enemies, 2) |
| `ancestors-ring` | Cercle des aïeux / Ring of the Ancestors | 3 | 0-0 | oui | non | 0-0 | 1 | Neutral | 2 | Circle 2 | glyph(Enemies, 2, 10-13, Earth) |

**Ce que fait chaque glyphe, vu du joueur :**
- **`scrape`** : 5 cases en losange, à 2-5 cases de la biche, pour 2 de ses tours. Elle le lance d'ordinaire sous un héros, qui prend 7 à 9 au début de son tour, puis 7 à 9 à chaque tour où il y entre ou y reste. Pour en sortir du centre, il faut 2 pas sur 3 PM. Elle échange une Esquille (16-20) contre ce glyphe : sur un tour, elle fait moins de dégâts, mais elle prend du terrain.
- **`heavy-ground`** : une croix de 5 cases, à 1-3 cases du vieux cerf. Qui y entre ou y commence son tour perd **2 PM pour ce tour-là**, et seulement ce tour-là : il garde 1 PM, assez pour quitter la case. Une marche qui y entre peut s'y arrêter si elle n'a plus de PM (test `AGlyphThatTakesTheMovementPoints_StopsTheWalk`). Élément neutre : le jeu le dessine dans une autre couleur que les glyphes qui brûlent.
- **`ancestors-ring`** : les 13 cases autour de l'aïeul, pour 2 de ses tours. Un héros au contact prend 10 à 13 à chaque début de tour : il faut se battre à 3 cases ou payer. Le Crochet (`antler-hook`) le ramène à 2 cases, et l'arrivée sur le cercle frappe aussi (une seule fois par tour de jeu).
- **Les combos de poussée** : le Coup de tête du daguet pousse de 2 cases, le Massacre de l'aïeul d'une. Arriver poussé sur un glyphe frappe. L'IA ne le cherche pas (§0.1, point 4), mais un joueur dos à un glyphe le subira.
- **Le budget du premier coup acquis** (§0.1, point 1) : 7-9 contre 16-20 pour le sort principal de la biche, et 10-13 contre 38-50 pour l'aïeul.

### 3.3 Les passifs

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `skittish` | Ombrageux / Skittish | Hit | Melee | status(Caster, Mp, +2, 1) |
| `cracked-antlers` | Bois fendu / Cracked Antlers | LowHp | Percent 50 | status(Caster, Ap, +2, 10) ; status(Caster, Resistance, −10, 10) |

Descriptions (pour la fiche) :
- `skittish` : fr « Frappé au contact, gagne 2 PM à son prochain tour. » ; en "Struck up close, gains 2 MP on its next turn."
- `cracked-antlers` : fr « Sous la moitié de ses PV, son bois se fend et montre un creux gravé entre quatre signes : +2 PA et −10 % de résistances jusqu'à la fin du combat. » ; en "Below half its HP, its antlers crack open on a carved hollow between four signs: +2 AP and −10% resistances until the end of the fight."

**Pourquoi Ombrageux :**
- C'est un cerf : il s'écarte quand on le frappe au contact. Un cerf qui a reculé laisse ses glyphes entre lui et le héros qui le poursuit ; la leçon est de ne pas courir derrière lui à travers le sol marqué, mais de le laisser revenir ou de le finir à distance.
- Il ne blesse aucune classe et ne pénalise pas le Garde : le héros ne perd rien, c'est le cerf qui gagne du mouvement.
- C'est la mécanique de `scamper` (le marcassin). Ici, elle vaut pour toute la famille et elle sert la leçon des glyphes. C'est noté au §7.

**Le signe narratif du chef** : à 50 %, le bois se fend. Entre les quatre andouillers paraît le creux gratté du cinquième signe, sans un mot de plus. Avec 10 PA, l'aïeul enchaîne Massacre (4), Crochet (3) et Cercle (3) dans le même tour.

### 3.4 Le butin et les ressources

| Ressource | fr / en | Icône |
|---|---|---|
| `stone-antler` | Andouiller de pierre / Stone Antler | un bois de cerf, game-icons.net (CC BY 3.0), choisi par `icons_gen.py` et inscrit dans `credits.json` |
| `stag-velvet` | Velours de cerf / Stag Velvet | une peau (le velours est la peau qui couvre le bois qui pousse), même source |

Ce sont deux ressources de type `Resource`, sans niveau, comme `mole-fur`. Elles serviront à la clef du donjon (S134) et aux recettes 40 et 45 (S135).

| Monstre | Butin |
|---|---|
| `brocket` | `stone-antler` 40 %, `stag-velvet` 40 %, `antler-circlet` 8 % |
| `carving-hind` | `stag-velvet` 60 %, `hind-cape` 8 % |
| `old-hart` | `stone-antler` 60 %, `stonehoof-boots` 8 % |
| `lead-doe` | `stone-antler` 50 %, `stag-velvet` 50 % (pas de pièce, §2.4) |
| `elder-stag` | `stone-antler` 100 % ×2, `stag-velvet` 100 %, `elder-antler-staff` 30 %, `antler-circlet` 8 %, `hind-cape` 8 %, `stonehoof-boots` 8 % |

La clef du donjon (2 %, 10 % pour le chef) s'ajoute en S134, avec le donjon.

### 3.5 La panoplie `stag` : « Panoplie du sylvecerf » / "Sylvan Stag set"

| Pièce | fr / en | Emplacement | Niv. | Monstre | Caractéristiques en jeu | Budget (+1) |
|---|---|---|---|---|---|---|
| `antler-circlet` | Couronne d'andouillers / Antler Circlet | Hat | 32 | `brocket` | Terre 11, Vitalité 9 | 20/20 |
| `hind-cape` | Cape de biche / Hind Cape | Cape | 31 | `carving-hind` | Terre 9, Vitalité 8, Initiative 10 | 20/20 |
| `stonehoof-boots` | Bottes sabot-de-pierre / Stonehoof Boots | Boots | 34 | `old-hart` | Terre 10, Vitalité 8, Initiative 10 | 21/21 |
| `elder-antler-staff` | Bâton-andouiller de l'aïeul / Elder's Antler Staff | TwoHanded | 45 | `elder-stag` | Terre 34, Vitalité 20 | 54/54 |

| Pièces | Bonus en jeu |
|---|---|
| 2 | Terre 10 |
| 3 | Terre 10, Vitalité 10 |
| 4 | Terre 10, Vitalité 15, **+1 PM** |

- **Les règles appliquées :** le budget est `Templates.Budget` (moitié de 8 + niveau, le double à deux mains) ; un point d'initiative compte 0,3 ; l'initiative ne va que sur les bottes, les capes et les anneaux.
- **Dans `sets_gen.py`** (`SCALE = 0.5`), écrire le **double** des valeurs en jeu, sauf pour l'arme à deux mains, déjà comptée double par `scaled(..., two_handed)`. L'initiative s'écrit telle quelle.

### 3.6 Pourquoi les deux voies de panoplie se valent
Le calcul vaut une fois les deux panoplies livrées (S133) :
- **Cerfs 4 + Gardiens 2** : Terre 10, Vitalité 25, +1 PM, sur 6 pièces (l'arme à deux mains prend les deux mains).
- **Gardiens 4 + Cerfs 3** : Terre 20, Vitalité 40, sur 7 pièces.

L'une donne de la mobilité, l'autre du volume. Dans une zone de glyphes, le PM compte : c'est voulu. Si la mesure montre qu'une voie domine (le kit « fast » de la simulation prend toujours la même), on ajuste la Vitalité du bonus à 4 pièces, jamais le PM.

### 3.7 La carte, l'allure et le chef
- **Carte de combat** : `ancestors-trunks` (§5.1). `battles: ["ancestors-trunks"]` en S132, puis les deux en S133. `bossMap: "ancestors-0-4"`.
- **Allures** (`Creatures.cs`, fonction nouvelle `Stag(scale, couleur, …)`) : un corps de cerf en formes simples comme `Wolf`, des bois gris pierre avec une touche de mousse.
  - `stag-melee` : 1,0, brun-gris, bois courts sans ramure ;
  - `stag-ranged` : 0,9, fauve clair, sans bois ;
  - `stag-tank` : 1,3, brun sombre, grands bois ;
  - `stag-support` : 0,95, fauve, sans bois ;
  - `stag-boss` : 1,6, gris très sombre, grands bois, couronne comme les autres chefs.

### 3.8 Fini quand (S132)
- **xUnit :**
  - le chargement accepte la famille (initiative, glyphes, passifs) ;
  - `FamilyTests` : la moyenne de famille dans les gabarits ;
  - `ItemBudgetTests` sur les 4 pièces ;
  - les bonus de la panoplie à 2, 3 et 4 pièces ;
  - le soutien sans pièce et le chef avec les quatre ;
  - un combat d'essai avec `scrape` et `heavy-ground` qui se rejoue à l'identique.
- **T41** (`BalanceTests`, ligne `InlineData("ancestors", 31)`) :
  - **petits groupes** : ≥ 27 sur 30 en moyenne par classe, au plus 2 groupes sous 18 ;
  - **hordes** : ≤ 3 sur 10 sans équipement, ≤ 2 avec les kits « zone » et « craft » ;
  - à 31, le kit « zone » ne trouve qu'**une** pièce avec de l'initiative (la cape) : c'est accepté, et noté dans `DECISIONS.md`.
- **Le chef :**
  - ≤ 10 % seul ;
  - **`--party`** à trois ≥ 70 % sans composition sous 50 %, à quatre ≥ 90 % (§11.2.1) ;
  - un échantillon dans `PartyBalanceTests`.
- **Godot :** l'auto-test vérifie un plateau **avec** glyphes dessinés (la limite laissée par T98).
- **Leviers, dans l'ordre**, si la mesure sort des bornes :
  1. les PV ;
  2. les dégâts de `scrape` et de `ancestors-ring` ;
  3. les tours des glyphes, de 2 à 1 ;
  4. le −2 PM de `heavy-ground`, ramené à −1.
- **Playtest** (question de §6) : « Il contourne les glyphes, ne finit plus son tour sur une case marquée et ne se met plus dos à un glyphe face au daguet. »

---

## 4. S133 : les Gardiens moussus (`golem`), Forêt des Aïeux

*Narratif : de petits golems de pierre que la mousse tient ensemble. Ils attaquent les mineurs depuis l'ouverture du passage (`mossy-keepers`). Ils tracent des cercles où ils se tiennent, et le plus vieux est un menhir qui marche. Sous sa mousse, un cœur gravé : les « cœurs gravés » que Bérold seul sait lire.*

**Leçon : déloger.** Le tank, le soutien et le chef posent des **glyphes alliés**. Un gardien qui y entre ou y commence son tour gagne de la résistance ou des dégâts pour 2 tours. Le joueur qui a compris :
- pousse ou attire un gardien hors de son cercle avant de le frapper ;
- ou se tient lui-même sur les cases du cercle, qu'un gardien ne peut plus occuper ;
- et frappe en Air ce qui reste bien gardé.

C'est la mécanique de `four-standing-stones` (S136 ou plus tard) en petit : des cases au sol qu'il faut tenir.

### 4.1 Les monstres

| Monstre | fr / en | Rôle | Niv. | PV | PA | PM | Init. | Allure |
|---|---|---|---|---|---|---|---|---|
| `cobble-keeper` | Gardien de moellons / Cobble Keeper | Melee | 35 | 65 | 6 | 3 | 131 | `golem-melee` |
| `pebble-keeper` | Gardien lance-galets / Pebble Keeper | Ranged | 34 | 54 | 6 | 3 | 157 | `golem-ranged` |
| `slab-keeper` | Gardien de dalle / Slab Keeper | Tank | 37 | 79 | 6 | 2 | 86 | `golem-tank` |
| `moss-tender` | Jardinier de mousse / Moss Tender | Support | 36 | 68 | 6 | 3 | 114 | `golem-support` |
| `menhir-keeper` | Menhir gardien / Menhir Keeper | Boss | 45 | 577 | 8 | 3 | 127 | `golem-boss` |

| Monstre | Sorts (dans l'ordre) | Résistances | Passifs |
|---|---|---|---|
| `cobble-keeper` | `cobble-punch`, `stone-grip` | Earth 10, Air −15 | `cairn` |
| `pebble-keeper` | `pebble-shot`, `rolled-rock` | Earth 10, Air −15 | `cairn` |
| `slab-keeper` | `slab-slam`, `moss-circle` | Earth 25, Water 10, Air −15 | `cairn` |
| `moss-tender` | `moss-graft`, `standing-ring`, `mossy-fist` | Earth 10, Air −15 | `cairn` |
| `menhir-keeper` | `menhir-crush`, `oath-ring`, `seismic-step` | Earth 30, Water 20, Fire 10, Air −5 | `cairn`, `carved-heart` |

- **Tank qui protège** : le Cercle de mousse donne +20 % de résistances aux gardiens qui s'y tiennent, lui compris (`Allies` inclut le lanceur). **Soutien qui renforce** : le Cercle levé donne +20 % de dégâts.
- Le tank a **2 PM** : la seule lenteur de la famille. Un chef à 2 PM se ferait promener par une Sentinelle, d'où ses 3 PM.

### 4.2 Les sorts

| Sort | fr / en | PA | Portée | LdV | Ligne | Dégâts | /tour | Élém. | cd | Zone | Effets |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `cobble-punch` | Poing de moellon / Cobble Punch | 3 | 1-1 | oui | non | 21-27 | 2 | Earth | 0 | — | — |
| `stone-grip` | Prise de pierre / Stone Grip | 3 | 1-2 | oui | non | 12-15 | 1 | Earth | 1 | — | pull(Enemies, 1) |
| `pebble-shot` | Galet / Pebble Shot | 3 | 2-6 | oui | non | 17-21 | 2 | Earth | 0 | — | — |
| `rolled-rock` | Roche roulée / Rolled Rock | 3 | 2-4 | oui | oui | 10-13 | 1 | Earth | 1 | — | push(Enemies, 1) |
| `slab-slam` | Coup de dalle / Slab Slam | 3 | 1-1 | oui | non | 20-26 | 2 | Earth | 0 | — | — |
| `moss-circle` | Cercle de mousse / Moss Circle | 3 | 0-0 | oui | non | 0-0 | 1 | Neutral | 2 | Circle 1 | glyph(Allies, 2, 0-0, Neutral, Resistance, +20, 2) |
| `moss-graft` | Greffe de mousse / Moss Graft | 3 | 0-4 | oui | non | 0-0 | 1 | Neutral | 1 | — | heal(Allies, 17, 21) |
| `standing-ring` | Cercle levé / Raised Ring | 3 | 1-4 | oui | non | 0-0 | 1 | Neutral | 2 | Cross 1 | glyph(Allies, 2, 0-0, Neutral, Damage, +20, 2) |
| `mossy-fist` | Poing moussu / Mossy Fist | 3 | 1-2 | oui | non | 20-26 | 1 | Earth | 0 | — | — |
| `menhir-crush` | Écrasement du menhir / Menhir Crush | 4 | 1-1 | oui | non | 38-50 | 1 | Earth | 0 | — | — |
| `oath-ring` | Cercle du serment / Oath Ring | 3 | 0-0 | oui | non | 0-0 | 1 | Neutral | 2 | Circle 2 | glyph(Allies, 2, 0-0, Neutral, Resistance, +20, 2) |
| `seismic-step` | Pas sismique / Seismic Step | 4 | 1-2 | oui | non | 24-30 | 1 | Earth | 1 | Cross 1 | push(Enemies, 1) |

**Ce que fait chaque glyphe, vu du joueur :**
- **`moss-circle`** : les 5 cases autour du tank, pour 2 de ses tours. Un gardien qui y commence son tour ou y entre gagne **+20 % à toutes les résistances** (état neutre) pour 2 tours. Il est renouvelé tant qu'il y reste (même lanceur, T57). Avec sa Terre à 25, le tank monte à 45 % de résistance Terre et 20 % en Air (−15 + 20 = +5, puisque l'état neutre touche tous les éléments).
- **Les deux réponses du joueur** : l'Air, ou sortir le tank du cercle (poussée, attirance). Sur les cases du cercle, le héros n'est pas touché (le glyphe vise les alliés du lanceur), et il prend la place d'un gardien.
- **`standing-ring`** : une croix de 5 cases sous un allié. Un gardien qui y commence son tour frappe à +20 % pendant 2 tours. On l'en sort, ou on le tue d'abord.
- **`oath-ring`** : les 13 cases autour du chef ; c'est le Cercle de mousse en grand. Avec ses 30 en Terre, le menhir monte à 50 % de résistance Terre tant qu'il y reste. Le **Pas sismique** pousse d'une case les héros qui le serrent, pour lui garder le cercle libre. C'est l'aperçu de `four-standing-stones`.
- **Ce que l'IA ne fait pas** : elle ne revient pas sur un cercle allié et ne le choisit pas pour s'y arrêter (§0.1, point 4). Le cercle compte surtout quand il est posé autour du lanceur à la fin de son tour (portée 0), ce que l'IA fait d'elle-même : elle bouge, puis lance. C'est noté au §7.

### 4.3 Les passifs

| Passif | fr / en | Déclencheur | Paramètres | Effets |
|---|---|---|---|---|
| `cairn` | Cairn / Cairn | AllyDies | Range 3 | shield(Caster, 15, 2) |
| `carved-heart` | Cœur gravé / Carved Heart | LowHp | Percent 50 | status(Caster, Damage, +20, 10) ; status(Caster, Resistance, −15, 10) ; summon(pebbling, Count 2) |

Descriptions (pour la fiche) :
- `cairn` : fr « Quand un gardien tombe à 3 cases ou moins, ses pierres le couvrent : bouclier de 15 pendant 2 tours. » ; en "When a keeper falls 3 cells away or less, its stones cover this one: a shield of 15 for 2 turns."
- `carved-heart` : fr « Sous la moitié de ses PV, sa mousse tombe et montre un cœur gravé ; deux pierres tombées se lèvent. +20 % de dégâts et −15 % de résistances jusqu'à la fin du combat. » ; en "Below half its HP, its moss falls away from a carved heart, and two fallen stones rise. +20% damage and −15% resistances until the end of the fight."

**Pourquoi Cairn :**
- Il récompense la même chose que la leçon : **isoler** un gardien avant de le tuer, plutôt que tout taper dans le tas.
- Les invocations ne le déclenchent pas (`AllyDies` exige la même famille et pas une invocation).
- C'est le schéma de `herd-grudge` (les sangliers), mais en défense. C'est noté au §7.

**Le signe narratif du chef** : à 50 %, sa mousse tombe. Le cœur gravé paraît, avec les quatre signes et le creux, et deux pierres tombées se lèvent. Le chef s'ouvre (−15 % de résistances) mais frappe plus fort (+20 %).

### 4.4 L'invocation (format de §4.8)
Invocation `pebbling` : Caillou / Pebbling, allure `golem-melee`, scale 70, PV 40, PA 6, PM 3, initiative 100, sorts [`cobble-punch`].
- Les invocations n'ont pas de contrôle d'initiative (`InitiativeFits` ne lit que les `MonsterSpec`). 100 la fait jouer avant le menhir.
- Elle n'a pas de passif : `cairn` ne servirait à rien sur une invocation.
- Le levier de §9, question 3, s'applique : si le chef dépasse 10 % seul, on baisse ses PV, on ne retire pas l'invocation.

### 4.5 Le butin et les ressources

| Ressource | fr / en | Icône |
|---|---|---|
| `mossy-cobble` | Moellon moussu / Mossy Cobble | une pierre, game-icons.net (CC BY 3.0), choisie par `icons_gen.py` et inscrite dans `credits.json` |
| `keeper-moss` | Mousse de gardien / Keeper Moss | une touffe de mousse, même source ; à distinguer de `glowmoss` (`lorc/ground-sprout`) |

| Monstre | Butin |
|---|---|
| `cobble-keeper` | `mossy-cobble` 40 %, `keeper-moss` 40 %, `cobble-belt` 8 % |
| `pebble-keeper` | `mossy-cobble` 60 %, `moss-ring` 8 % |
| `slab-keeper` | `keeper-moss` 60 %, `slab-shield` 8 % |
| `moss-tender` | `mossy-cobble` 50 %, `keeper-moss` 50 % (pas de pièce) |
| `menhir-keeper` | `mossy-cobble` 100 % ×2, `keeper-moss` 100 %, `menhir-mace` 30 %, `cobble-belt` 8 %, `moss-ring` 8 %, `slab-shield` 8 % |

Le « cœur gravé » de la quête `mossy-keepers` n'est **pas** une ressource. C'est un objet de quête (S136) qui tombera sur toute la famille pendant la quête (règle de T81, comme `humming-debris` chez les taupes). Le nom du passif `carved-heart` le prépare, sans le remplacer.

### 4.6 La panoplie `golem` : « Panoplie du gardien moussu » / "Mossy Keeper set"

| Pièce | fr / en | Emplacement | Niv. | Monstre | Caractéristiques en jeu | Budget (+1) |
|---|---|---|---|---|---|---|
| `moss-ring` | Anneau de mousse / Moss Ring | Ring | 34 | `pebble-keeper` | Terre 9, Vitalité 6, Initiative 20 | 21/21 |
| `cobble-belt` | Ceinture de moellons / Cobble Belt | Belt | 35 | `cobble-keeper` | Terre 11, Vitalité 11 | 22/22 |
| `slab-shield` | Bouclier-dalle / Slab Shield | Shield | 37 | `slab-keeper` | Vitalité 13, Terre 10 | 23/23 |
| `menhir-mace` | Masse du menhir / Menhir Mace | OneHanded | 45 | `menhir-keeper` | Terre 17, Vitalité 10 | 27/27 |

| Pièces | Bonus en jeu |
|---|---|
| 2 | Vitalité 10 |
| 3 | Vitalité 10, Terre 10 |
| 4 | Vitalité 30, Terre 10 |

- Le bouclier porte plus de Vitalité que de Terre, comme les boucliers des artisans (`craft_gen.py`). `sets_gen.py` : le double des valeurs en jeu.
- L'arbitrage avec la panoplie des cerfs est au §3.6.

### 4.7 La carte, l'allure et le chef
- **Carte de combat** : `ancestors-stones` (§5.2). `battles: ["ancestors-stones", "ancestors-trunks"]`, et `ancestors-stones` s'ajoute aussi aux cerfs (§2.5). `bossMap: "ancestors-4-4"`.
- **Allures** (`Creatures.cs`, fonction nouvelle `Keeper(scale, couleur, …)`) : un tronc de blocs gris, des bras épais, des touffes de mousse vert sombre.
  - `golem-melee` : 1,0, gris moyen ;
  - `golem-ranged` : 0,9, gris clair, un galet dans la main ;
  - `golem-tank` : 1,3, gris sombre, une dalle sur le dos ;
  - `golem-support` : 0,95, plus de mousse que de pierre ;
  - `golem-boss` : 1,6, un menhir dressé avec des bras, et la couronne.

### 4.8 Fini quand (S133)
- **xUnit :** comme au §3.8, plus :
  - un glyphe allié qui renforce un gardien au début de son tour, sans toucher un héros sur la même case voisine ;
  - `pebbling` posé par `carved-heart`.
- **T41 sur toute la zone** (les deux familles, les deux cartes) : mêmes bornes qu'au §3.8. Le chef des gardiens : ≤ 10 % seul, `--party` à trois ≥ 70 % sans composition sous 50 %, à quatre ≥ 90 %.
- **Leviers, dans l'ordre** :
  1. les PV ;
  2. la résistance des cercles, de +20 à +15 ;
  3. le soin de la Greffe ;
  4. les tours des glyphes, de 2 à 1.
- **Playtest** : « Il pousse ou attire un gardien hors de son cercle avant de le frapper, ou se tient sur le cercle, et il sort ses sorts d'Air. »

---

## 5. Les cartes de combat (11 × 11, 4 places A, 10 places B, toutes reliées)

Légende du format existant : `.` sol, `#` obstacle, `~` trou (on tire par-dessus, on ne passe pas), `A` place des héros, `B` place des monstres. Comptes vérifiés à la main ; **à revérifier par le script de §5.2** de `design-familles.md`.

### 5.1 `ancestors-trunks` : « Futaie » / « Old Growth » (S132)
Des troncs géants de 2 × 2 et quelques fûts isolés, qui tracent des couloirs. Un glyphe posé dans un couloir le ferme, et un Coup de tête pousse contre un tronc (collision) ou sur un glyphe.
```
..#......B.
.A.....#..B
....##..B..
....##....B
.A.......B.
..#.....#.B
.A.......B.
....##....B
....##..B..
.A.....#..B
..#........
```

### 5.2 `ancestors-stones` : « Cercle de pierres » / « Stone Circle » (S133)
Quatre pierres levées en losange autour de la case centrale, et une source qui coupe le haut et le bas du milieu (on tire par-dessus). Les gardiens se battent volontiers autour du centre. Les pierres gênent les poussées et les attirances, ce qui oblige à choisir l'angle. La source garde des lignes de tir pour les lance-galets comme pour la Sentinelle.
```
.....~...B.
.A...~....B
........B..
.....#....B
.A.......B.
...#...#..B
.A.......B.
.....#....B
........B..
.A...~....B
.....~.....
```
Le nom « Cercle de pierres » laisse « Les quatre pierres levées » à la quête de groupe (`four-standing-stones`).

---

## 6. Ce que les générateurs doivent apprendre

### 6.1 `tools/spells_gen.py` : la fonction `glyph()`
```python
def glyph(a, turns, dmin=0, dmax=0, el=None, stat=None, v=0, st=0):
    d = O([("kind", "glyph"), ("affects", a), ("turns", turns)])
    if dmax: d["damageMin"], d["damageMax"] = dmin, dmax
    if el: d["element"] = el
    if stat: d["status"], d["value"], d["statusTurns"] = stat, v, st
    return d
```
Les noms des champs sont ceux que lit `AGlyph_ReadsFromJson` (`turns`, `damageMin`, `damageMax`, `element`, `status`, `value`, `statusTurns`). Exemples :
- `S("scrape","Grattis","Scrape",2,2,5,0,0,1,"Neutral",1,area=("Circle",1),effects=[glyph("Enemies",2,7,9,"Earth")],ranks=False,cd=2)` ;
- `S("moss-circle","Cercle de mousse","Moss Circle",3,0,0,0,0,1,"Neutral",1,area=("Circle",1),effects=[glyph("Allies",2,stat="Resistance",v=20,st=2)],ranks=False,cd=2)`.

### 6.2 `tools/sets_gen.py` : 4 pièces, un soutien sans pièce, pas encore de clef
1. **Le nombre de pièces par famille** : 5 pour Clairval, 4 pour la forêt. Les bonus deviennent `(2, 3, len(pieces))`.
2. **Le soutien sans pièce** : lever `assert drops` pour le monstre de rôle `Support` des familles à 4 pièces.
3. **Le butin sans clef** : si aucun butin ne finit par `-key` (avant S134), ajouter les pièces à la fin du butin. Il ne faut plus d'erreur `StopIteration`.
4. **Ne rien changer pour Clairval** : les fichiers de Clairval doivent ressortir à l'identique. Un `git diff` vide sur leurs entrées suffit à le vérifier.

---

## 7. Points incertains et valeurs par défaut

| # | Point | Valeur par défaut |
|---|---|---|
| 1 | **Le premier coup d'un glyphe posé sous un héros est inévitable** (§0.1, point 1) : la promesse « le glyphe montré avant qu'il ne frappe » ne tient qu'à moitié. Option moteur (environ 1 point) : un glyphe neuf **épargne au premier début de tour** ceux qui se tenaient sur ses cases quand il a été posé (`Glyph` garde ces ids, comme `Struck`). | **Ne pas toucher au moteur** dans ces sprints de données : les dégâts des glyphes comptent ce coup comme acquis (environ 40 % du sort principal). Corriger la phrase de T98 dans `DECISIONS.md`. Reparler de l'option à Charles après le playtest de S136. |
| 2 | Les niveaux : communs de 31 à 37, chefs à 45 (§2.2), plutôt qu'étirés jusqu'à 45 | **31 à 37** ; T41 mesuré au niveau des cartes (31) |
| 3 | La faiblesse des chefs : −5 (§3) ou −20 (gabarit `Boss` de `Templates.cs`) | **−5** ; noter l'écart, et aligner le gabarit ou le document |
| 4 | Les panoplies : 4 pièces, sans pièce pour le soutien, bonus à 2, 3 et 4, l'arme du chef comme pièce de 4 (§2.4) | **comme écrit** ; `sets_gen.py` adapté (§6.2) |
| 5 | Les ids `stag` et `golem` (§2.1) | **comme écrit** |
| 6 | Deux allures dessinées par le jeu (`Stag`, `Keeper` dans `Creatures.cs`) dans des sprints de 3 points | **des formes simples** sur le modèle de `Wolf` et `Mole` ; si le temps manque, reprendre `wolf-*` (cerfs) et `mole-tank` teinté (gardiens) et le noter |
| 7 | La lisibilité : quatre sortes de glyphes, deux qui brûlent en Terre et deux neutres, dont les alliés | **couleur de l'élément** (T98) et fiche au survol ; demander au client un contour à la couleur de l'équipe pour les glyphes alliés si le playtest les confond |
| 8 | L'IA ne cherche ni les cercles alliés ni les poussées vers un glyphe (§0.1, point 4) | **accepté**, et noté dans les Limites ; le cercle à portée 0 (autour du lanceur) compense |
| 9 | `skittish` reprend la mécanique de `scamper`, et `cairn` le schéma de `herd-grudge` (en défense) | **gardés** : les déclencheurs du moteur sont cinq, et ces deux passifs servent la leçon de leur famille |
| 10 | Les cartes partagées à partir de S133 et les cartes des chefs (`ancestors-0-4`, `ancestors-4-4`) | **comme écrit** ; si une carte n'est pas de profondeur 2, prendre une autre carte du fond |
| 11 | Le kit « zone » à 31 ne trouve qu'une pièce avec de l'initiative (la cape) | **accepté** ; les plafonds se mesurent quand même |
| 12 | Les icônes des quatre ressources | **choisies sur game-icons.net** (CC BY 3.0) par `icons_gen.py`, chacune inscrite dans `credits.json` |
| 13 | Les clefs des donjons | **en S134**, avec les donjons (2 % sur les communs, 10 % sur les chefs, comme à Clairval) |
| 14 | Les faiblesses Feu (cerfs) et Air (gardiens) | **comme écrit** ; il reste Terre ×2, Eau ×2, Feu ×1 et Air ×1 pour les six autres familles des premières zones du chapitre 2 |
