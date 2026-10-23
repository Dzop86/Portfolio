# Osmose : les textes de la v8 (les quêtes de la Forêt des Aïeux)

> Proposition du scénariste pour le sprint 136, **à relire par Charles** avant d'être reportée dans les données. Elle répond à la ligne S136 de `suite-apres-d69.md` (§11.6) : 9 quêtes comme une zone de Clairval (4 principales, 5 secondaires en petites suites), un donneur dans la forêt, des indices, un butin de quête, une fin de tranche qui promet les Mines de Sous-Racine sans les ouvrir, niveaux 31 à 45.
> Sources : `docs/bible.md`, `docs/chapitre-2-pistes.md` (§5, §7, §8 et les réponses de Charles au §10.3, D79), `docs/textes-v7.md` (§4 et §6, point 9), `docs/familles-foret.md`, `docs/familles-foret-textes.md`, `DECISIONS.md` (T97 à T102), `data/quests.json`, `data/dungeons.json`, `data/items.json`, `data/towns/ancestors-*.json`, `data/towns/four-ways*.json`, les halls `bellowing-combe-*` et `mother-tree-roots-*`, `tools/zone_people.py`, `src/Rpg.Core/Data.cs` (types d'objectifs).
> **Convention :** **[P]** = proposition, absente de la bible et du jeu. Le canon de la bible n'est pas modifié. Les identifiants nouveaux (habitants, objets posés, objet de quête, quêtes) sont **[P]** ; ceux qui existent sont cités tels quels.
> **Le héros n'a pas de genre dans les textes** (comme en v5 à v7) : aucun accord ne porte sur lui.
> **Rien n'est écrit dans `data/`** : le dev transcrit.

## Sommaire
0. Résumé : les 9 quêtes
1. Le lieu : où vivent les gens, où sont les indices
2. Les habitants : Berthe et Bérold
3. Les objets posés et le butin de quête
4. Les fiches des quêtes (bible §16)
5. Les textes prêts à copier (dialogues, objets, butin, quêtes)
6. Ce qui demande du code ou une donnée nouvelle
7. Points de canon et questions pour Charles
8. Récapitulatif pour la transcription

**Lire un bloc de quête** (comme en v5 à v7) : **Offre** = ce que dit le donneur ; **Réplique** = ce que dit la personne à qui l'on parle pendant l'objectif (`line`) ; **Remerciement** = ce que dit celui qui reprend la quête.

---

## 0. Résumé : les 9 quêtes

**Règle des récompenses** (relevée sur les 30 quêtes données de `quests.json`) : principale **60 XP et 10 Éclats par niveau** ; secondaire **40 XP et 10 Éclats par niveau** ; « Au fond » d'un donjon **100 XP et 15 Éclats par niveau**. La seule exception est `where-roads-part` (40 par niveau, une étape sans combat). Je l'applique sans écart.

| # | id **[P]** | Nom fr / en | Genre | Niv. | Donneur → reprise | `after` | Objectifs (moteur actuel) | XP / Éclats | Objets |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `the-tree-town` | Ceux de l'arbre / The Tree Folk | Main | 31 | `four-ways/anatole` → `ancestors/berthe` | `where-roads-part` | `Talk` Bérold (réplique), `Talk` Berthe (réplique) | 1 860 / 310 | — |
| 2 | `the-unmapped-passage` | Le passage qui n'existait pas / The Passage That Wasn't There | Main | 33 | `ancestors/berthe` | `the-tree-town` | `Interact` ×3 (`trial-pit-1`, `-2`, `-3`), `Talk` Bérold (réplique) | 1 980 / 330 | — |
| 3 | `mossy-keepers` | Les gardiens moussus / The Mossy Keepers | Main | 36 | `ancestors/berthe` → `ancestors/berold` | `the-unmapped-passage` | `Defeat` `golem` ×6, `Bring` `keeper-heart` ×3 | 2 160 / 360 | butin de quête `keeper-heart` |
| 4 | `roots-of-the-mother-tree` | Les Racines de l'Arbre-Mère / The Mother Tree's Roots | Main | **41** | `ancestors/berthe` → `four-ways/anatole` | `mossy-keepers` | `Dungeon` `mother-tree-roots` salle 2, `Interact` `rooted-slab`, `Talk` Bérold (réplique) | 2 460 / 410 | `grants` `mother-tree-roots-key` ×1 |
| 5 | `burning-ground` | Le sol qui brûle / Burning Ground | Side | 32 | `ancestors/berthe` | `the-tree-town` | `Defeat` `stag` ×6 | 1 280 / 320 | `healing-elixir` ×3 |
| 6 | `antler-wedges` | Des coins d'andouiller / Antler Wedges | Side | 34 | `ancestors/berthe` | `burning-ground` | `Bring` `stone-antler` ×5, `Bring` `stag-velvet` ×4 | 1 360 / 340 | `greater-healing-elixir` ×2 |
| 7 | `the-bellow` | Le brame / The Bellow | Side | **40** | `ancestors/berthe` | `antler-wedges` | `Dungeon` `bellowing-combe` salle 2, `Interact` `echo-stone` | 1 600 / 400 | `grants` `bellowing-combe-key` ×1 ; `spelt-bread` ×3 |
| 8 | `bellowing-combe-cleared` | Au fond : Combe du brame / At the bottom: The Bellowing Combe | Dungeon | 44 | `ancestors/berthe` | `the-bellow` | `Dungeon` `bellowing-combe` | 4 400 / 660 | — |
| 9 | `mother-tree-roots-cleared` | Au fond : Racines de l'Arbre-Mère / At the bottom: The Mother Tree's Roots | Dungeon | 45 | `ancestors/berthe` | `roots-of-the-mother-tree` | `Dungeon` `mother-tree-roots` | 4 500 / 675 | `sorghum-flatbread` ×3 |

**Les suites :**
- **Principale :** `where-roads-part` → 1 → 2 → 3 → 4. Le fil part d'Ysaline (qui a donné `where-roads-part`), passe par Anatole, l'érudit des routes, qui ouvre la forêt (1) et la referme sur la promesse des Mines (4) ; il écrit à Ysaline.
- **Secondaires, deux petites suites et une fin de donjon :** 5 → 6 → 7 → 8 (les cerfs, de la chasse à la Combe) ; 4 → 9 (les gardiens, au fond des Racines). Comme au Bois et au Bois moisi : 3 secondaires et 2 « Au fond ».

**Les types d'activité** : dialogue (1), enquête et exploration (2), combat et collecte avec un butin de quête (3), donjon et indice (4), combat qui apprend la leçon des glyphes (5), collecte (6), donjon et indice (7), donjons entiers à plusieurs (8, 9).

**Jouable seul à son niveau** : aucune étape principale ne demande plus que la salle 2 d'un donjon. La seule étape principale en donjon (4) est au **niveau 41**, comme T102 le demande ; la salle 2 de la Combe est demandée par une secondaire (7) au **niveau 40**. Les deux « Au fond » sont des quêtes de groupe, comme à Clairval.

---

## 1. Le lieu : où vivent les gens, où sont les indices

**Ce qui existe** (T99, T102) : 25 cartes `ancestors-X-Y` (niveau 31), toutes nommées « Forêt des Aïeux / Forest of the Ancestors », sans habitant. On arrive de Quatre-Chemins (`four-ways-1-1`, route du sud) sur `ancestors-1-0`, où est la pierre de passage `waystone-ancestors`. La porte de la Combe du brame est sur `ancestors-0-4` (au fond, au sud-ouest), celle des Racines de l'Arbre-Mère sur `ancestors-4-4` (au fond, au sud-est).

**Les profondeurs** (calculées à la main d'après `MonsterGroups.Depth` et les coordonnées de T99, **à vérifier par le dev**) : `ancestors-0-0`, `1-0`, `2-0`, `1-1` sont près de l'entrée ; `0-1`, `2-1`, `1-2`, `2-2`, `3-0`, `3-1`, `0-2`, `1-3`, `4-0` sont au milieu ; le reste est au fond. Les indices des niveaux 31 à 36 restent près de l'entrée ou au milieu.

**Le camp des mineurs [P]** : sur `ancestors-1-1`, une carte près de l'entrée, juste au sud de l'arrivée. Les mineurs d'Ormeval travaillaient au pied de l'Arbre, au fond de la forêt ; depuis que les gardiens les chassent, ils ont reculé jusqu'à la route (Berthe le dit). C'est la règle de `zone_people.py` : « les donneurs vivent là où se font les quêtes », sur une carte près de l'entrée de la zone, comme Mathurin (`woods-3-2`).

**Ormeval et l'Arbre-Mère.** La ville n'est pas dans la tranche (25 cartes de forêt, aucune ville, T99). Je ne la fais pas visiter, et je ne promets pas d'y monter : Bérold dit que **les étrangers n'y montent pas**, « c'est le premier serment de la ville » **[P]**. C'est une règle de la ville, pas un « plus tard ». L'Arbre-Mère est au fond, au sud-est : la porte des Racines est « entre ses racines ». Voir §7.2, question 1.

**Les indices :**

| id **[P]** | Carte | Près de la case | Aspect | Quête |
|---|---|---|---|---|
| `trial-pit-1` | `ancestors-2-1` (milieu) | (24, 12) | `stone` (existe) ou nouveau `pit` | 2 |
| `trial-pit-2` | `ancestors-1-2` (milieu) | (24, 12) | `roots` (existe) | 2 |
| `trial-pit-3` | `ancestors-2-2` (milieu) | (24, 12) | `grave` (existe) ou `stone` | 2 |
| `echo-stone` | `bellowing-combe-2` (grande salle de la Combe) | (10, 4), comme le sceau de la crypte | `stone` | 7 |
| `rooted-slab` | `mother-tree-roots-2` (grande salle des Racines) | (10, 4) | `roots` ou nouveau `slab` | 4 |

Les trois sondages vont de l'entrée vers le sud-est, vers l'Arbre : on suit le chemin des mineurs. Aucun texte ne décrit une tranchée ou un chantier qui ne serait pas dessiné : chaque indice est un trou et sa pierre, rien de plus.

---

## 2. Les habitants : Berthe et Bérold

Deux habitants, les deux noms gardés par Charles (D79, §10.3, réponse 3). Aucun autre prénom nouveau.

| id **[P]** | Nom affiché fr / en | Apparence | Carte | Fiche |
|---|---|---|---|---|
| `berthe` | Berthe, la contremaîtresse / Berthe, the forewoman | `female-b` (au dev) | `ancestors-1-1`, près de (24, 10) | **la donneuse de la forêt** (relais de l'arc, `chapitre-2-pistes.md` §5). Franche, pressée, elle pense en filons, en étais et en salaires. Elle veut exploiter ce qu'elle a trouvé pour faire vivre Ormeval. Elle tutoie tout le monde, même le juge dans son dos. |
| `berold` | Bérold, le juge des serments / Bérold, the oath-judge | `male-a` (au dev) | `ancestors-1-1`, près de (28, 10), à quelques pas de Berthe | **le représentant de Dhoram** (bible §10). Digne, lent, rigide. Il est descendu d'Ormeval pour faire refermer le passage et ne remontera pas avant. Il **vouvoie** le héros **[P]** (le seul de la forêt) : c'est sa marque, et elle le distingue de Mathurin. Il tient les serments sans les comprendre, comme Mathurin, mais en plus haut placé et en plus dur. |

**Pourquoi les deux au même endroit :** leur désaccord est le sujet de l'arc (préserver ou exploiter, bible §8). Les voir à quatre pas l'un de l'autre, chacun parlant de l'autre, le fait sentir sans un mot de plus. Cela prépare « Garder ou creuser » (niveau 46), **que je n'écris pas**.

**Les voix :** Berthe parle vite, en phrases sèches et concrètes (« trente gars », « ma grille », « du granit à grain fin »), et elle plaisante sur le juge. Bérold parle en formules, au présent de vérité, sans exclamation (« Un serment ne se comprend pas, il se tient. »). Anatole reste ironique et compte (« pour la première fois en vingt ans »).

**Leur évolution dans la tranche :**
- **Bérold** dit d'abord que rien n'est plus vieux que ses registres (2). Puis il lit sur les cœurs les premiers mots des serments d'Ormeval, sans savoir lire la suite (3). Enfin, il écrit à Anatole que « la route du sud s'arrête à l'Arbre » (4) : il commence à avoir peur de ce qu'il protège.
- **Berthe** voit d'abord un filon. Elle reconnaît ensuite un travail de carrier dans le creux gratté (2), puis entend dans la Combe un coup qui ne revient pas, c'est-à-dire du vide derrière la roche (7). Elle veut toujours creuser, mais elle sait maintenant qu'elle ne creuse pas dans de la roche vierge.

---

## 3. Les objets posés et le butin de quête

### 3.1 Les indices (textes au §5.2)
- **Les trois sondages** (2) : une écriture de personne ; une racine qui a suivi les traits et le mot « … tiendront… » ; les quatre signes autour d'un **creux gratté** à l'outil. C'est le motif établi (la tombe de `clairval-2-2`, « un signe gratté avec soin ») : le joueur fait seul le lien, et personne ne le dit.
- **La pierre d'écho** (7) : quatre échos, puis « le silence dure un écho de trop ». C'est le même motif que le couplet qui manque au Lac et que le pain rompu en cinq aux Champs, sans le mot « cinquième ».
- **La dalle prise dans les racines** (4) : les mots des cœurs, « Nous tiendrons… », une phrase qui continue sous le bord, et de l'air froid qui monte : ça descend encore. **C'est la promesse des Mines.** Rien ne s'ouvre.

### 3.2 Le butin de quête : `keeper-heart` [P]
- **Nom :** Cœur gravé / Carved Heart. Type `Quest`. Icône au choix d'`icons_gen.py` (une pierre ronde gravée), inscrite dans `credits.json`.
- **Pourquoi pas `carved-heart` :** c'est déjà l'id du passif du Menhir gardien (`passives.json`) ; une recherche dans le dépôt mélangerait les deux (même raison que `golem` au lieu de `keeper`, `familles-foret.md` §2.1). Le nom affiché est le même que celui du passif : c'est voulu, le menhir montre son cœur à 50 %, et les autres en portent un plus petit.
- **Qui le laisse** (règle de T81 : sur toute la famille, seulement pendant la quête) : `cobble-keeper`, `pebble-keeper`, `slab-keeper`, `moss-tender` à **35 %** (comme `marked-fang`), `menhir-keeper` à **100 %**, tous avec `"quest": "mossy-keepers"`. Six gardiens donnent en moyenne 2,1 cœurs : il en faut d'ordinaire un ou deux de plus, ce qui est le rythme de « Les bêtes inquiètes ».
- **Ce qu'il dit** : rien sur lui-même. C'est Bérold qui le lit, au remerciement de 3 (`familles-foret-textes.md` §3, remarque 2 : ni « cinq parties » ni le serment entier, réservés à `dhorams-fragment`).

---

## 4. Les fiches des quêtes (bible §16)

Champs communs à toutes, pour ne pas les répéter :
- **Chapitre :** 2, les Quatre Héritages ; arc de Dhoram, les Racines du passé, zone 1 (`chapitre-2-pistes.md` §5).
- **Drapeaux persistants :** la quête faite, par personnage (règle actuelle). Aucun autre drapeau.
- **Coopération :** `Talk` et `Interact` se valident pour chaque personnage ; `Defeat` compte les monstres du combat pour chacun ; `Dungeon` compte la salle gagnée pour chaque membre du combat (à vérifier, §6, point 8). Un nouveau venu dans un groupe ne perd jamais une étape parce qu'un autre l'a faite.
- **Abandon, reconnexion :** les compteurs sont gardés par le serveur. Une clef offerte (`grants`) n'est donnée qu'une fois, à la prise de la quête.
- **Échec :** impossible. Une salle perdue se rejoue ; une clef usée sans victoire se refait chez le serrurier (niveau 40, T102).

### 4.1 `the-tree-town` · Ceux de l'arbre · Main · 31
- **Résumé :** Anatole envoie le héros par la route du sud jusqu'à la forêt, où les mineurs d'Ormeval ne passent plus. Au camp, deux voix : Bérold, qui veut refermer un passage ouvert sous l'Arbre, et Berthe, qui veut savoir ce qu'elle a trouvé.
- **Donneur :** Anatole (`four-ways/anatole`), qui a vu passer les mineurs dans son livre. **Reprise :** Berthe (`handIn`).
- **Prérequis :** `after: ["where-roads-part"]` (v7 §6, point 9).
- **Objectifs :** `Talk` `ancestors/berold` avec réplique ; `Talk` `ancestors/berthe` avec réplique. Les deux répliques tiennent dans les deux ordres.
- **Pourquoi elle existe :** elle présente la région, le relais et le représentant de Dhoram, et pose le conflit de l'arc dès la première étape. Elle remplace l'étape « monter à Ormeval » du cadre : la ville n'est pas dans la tranche (§7.1). L'id `the-tree-town` est gardé (bible §16 : les ids restent stables) ; seul le titre change.
- **Combats :** aucun obligatoire. Les cerfs se croisent sur la route.

### 4.2 `the-unmapped-passage` · Le passage qui n'existait pas · Main · 33
- **Résumé :** en cherchant le filon, les mineurs ont creusé des sondages jusqu'à l'Arbre et sont tombés sur des pierres gravées. Le héros les examine. Bérold affirme que rien n'est plus vieux que ses registres ; Berthe reconnaît dans le creux gratté le travail d'un bon carrier.
- **Donneuse et reprise :** Berthe. **Prérequis :** `the-tree-town`.
- **Objectifs :** `Interact` `trial-pit-1`, `trial-pit-2`, `trial-pit-3` ; `Talk` `ancestors/berold` avec réplique.
- **Révélation (niveau 1 → 2) :** des inscriptions plus anciennes que les archives d'Ormeval, et un signe effacé **à l'outil**, comme à Clairval. On efface « avec soin ce dont on se souvient trop bien » (Ysaline, `the-registry`).
- **Type :** enquête et exploration, trois cartes du milieu.

### 4.3 `mossy-keepers` · Les gardiens moussus · Main · 36
- **Résumé :** les pierres qui marchent sont des gardiens de mousse, et ils interdisent le filon. Le héros en abat six et rapporte trois cœurs gravés. Bérold les lit : ce sont les premiers mots des plus vieux serments d'Ormeval, « Nous tiendrons », et il ne connaît pas les mots qui suivent. Les gardiens ne se battaient pas contre les mineurs : ils tenaient un serment.
- **Donneuse :** Berthe. **Reprise :** Bérold (`handIn`). **Prérequis :** `the-unmapped-passage`.
- **Objectifs :** `Defeat` `golem` ×6 ; `Bring` `keeper-heart` ×3 (pris à la reprise).
- **Butin de quête :** §3.2.
- **Révélation :** les gardiens appliquent un serment (le gardien ancien de la zone 2 « applique encore un serment »). On ne dit ni qui a juré, ni combien de parties le serment compte.

### 4.4 `roots-of-the-mother-tree` · Les Racines de l'Arbre-Mère · Main · 41
- **Résumé :** Bérold a fait fermer la grille des mineurs à l'entrée du passage, mais Berthe en a encore la clef, pour un seul tour avant le changement de serrure. Le héros descend jusqu'à la grande salle, sous l'Arbre, où les racines serrent une dalle qui porte les mots des cœurs, une phrase qui continue dessous et de l'air froid qui monte. Il rapporte tout à Anatole, qui a reçu un mot du juge : « La route du sud s'arrête à l'Arbre. »
- **Donneuse :** Berthe. **Reprise :** Anatole (`four-ways/anatole`, `handIn`). **Prérequis :** `mossy-keepers`.
- **Objectifs :** `Dungeon` `mother-tree-roots` salle 2 ; `Interact` `rooted-slab` (dans `mother-tree-roots-2`) ; `Talk` `ancestors/berold` avec réplique (elle tient avant et après la descente).
- **Clef :** `grants` `mother-tree-roots-key` ×1. La porte des Racines **est** la grille des mineurs (§7.2, question 4).
- **Niveau :** **41**, la mesure de T102 (la Garde gagne la grande salle 85 fois sur 100 à 41, 59 à 40).
- **Fin de tranche :** le remerciement d'Anatole **promet les Mines de Sous-Racine** sans les ouvrir : une route qui descend, et pas encore de page dans son livre pour elle. Il écrit à Ysaline : la chaîne revient vers elle sans exiger un aller-retour à Clairval.
- **Pourquoi Anatole et pas Bérold :** l'étape referme la tranche là où les routes se séparent. Le juge y est présent par sa lettre, et son geste (écrire qu'une route s'arrête) en dit plus qu'une réplique.

### 4.5 `burning-ground` · Le sol qui brûle · Side · 32
- **Résumé :** les grattis des cerfs s'allument sous les pieds des mineurs. Le héros en chasse six et apprend à regarder où il marche : c'est la leçon des Sylvecerfs, dite par une mineuse.
- **Donneuse et reprise :** Berthe. **Prérequis :** `the-tree-town`. **Objectif :** `Defeat` `stag` ×6. **Récompense :** `healing-elixir` ×3.

### 4.6 `antler-wedges` · Des coins d'andouiller · Side · 34
- **Résumé :** un andouiller de pierre ne se fend jamais : c'est le meilleur coin pour caler un étai. Il faut aussi des peaux pour les genouillères, parce qu'on travaille à genoux là-dessous. La collecte a donc une raison de métier.
- **Donneuse et reprise :** Berthe. **Prérequis :** `burning-ground`. **Objectifs :** `Bring` `stone-antler` ×5, `Bring` `stag-velvet` ×4 (affiché « Peau de cerf »). **Récompense :** `greater-healing-elixir` ×2.

### 4.7 `the-bellow` · Le brame · Side · 40
- **Résumé :** la nuit, les cerfs brament dans la Combe et les étais du camp tremblent. Un mineur a pris une clef sur un vieux cerf. Le héros descend jusqu'à la grande salle et frappe la pierre d'écho : quatre échos, et un silence trop long. Pour Berthe, c'est du vide derrière la roche.
- **Donneuse et reprise :** Berthe. **Prérequis :** `antler-wedges`.
- **Objectifs :** `Dungeon` `bellowing-combe` salle 2 ; `Interact` `echo-stone` (dans `bellowing-combe-2`).
- **Clef :** `grants` `bellowing-combe-key` ×1 **[P]**. À Clairval, seules les étapes principales offrent une clef ; ici, aucune étape principale ne passe par la Combe. Sans clef offerte, la quête attendrait le serrurier (niveau 40) ou le butin (2 %).
- **Niveau :** **40**, la grande salle de la Combe (T102 : Garde 71 sur 100 à 40). Voir §6, point 10 : la Garde n'y gagne plus que 47 fois sur 100 à 43.
- **Récompense :** `spelt-bread` ×3.
- **Indice discret :** la pierre d'écho (§3.1) et la phrase de Berthe sur le vide préparent les Mines, sans les nommer.

### 4.8 `bellowing-combe-cleared` · Au fond : Combe du brame · Dungeon · 44
- **Donneuse et reprise :** Berthe. **Prérequis :** `the-bellow`. **Objectif :** `Dungeon` `bellowing-combe` (salle 0 = tout le donjon, seul objectif, règle du chargement). Groupe conseillé ; clef chez le serrurier ou dans le butin.

### 4.9 `mother-tree-roots-cleared` · Au fond : Racines de l'Arbre-Mère · Dungeon · 45
- **Donneuse et reprise :** Berthe. **Prérequis :** `roots-of-the-mother-tree`. **Objectif :** `Dungeon` `mother-tree-roots`. **Récompense :** `sorghum-flatbread` ×3. Le remerciement prépare « Garder ou creuser » (le juge et la mineuse auront « raison tous les deux ») sans poser le choix.

---

## 5. Les textes prêts à copier

### 5.1 Les dialogues (`data/dialogues/<id>.json`)
Même forme que ceux de `tools/village_dialogues.py` : `hello`, puis `topic1` à `topic3` ; chaque réplique renvoie aux autres sujets et à « À bientôt. ». Toutes les répliques sont atteignables, toutes ont une sortie.

**`berthe.json`** (4 répliques)
```json
{
  "id": "berthe",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Fais vite : j'ai trente gars qui me coûtent une journée chacun à ne rien faire. Berthe, contremaîtresse. Tu veux quoi ?", "en": "Make it quick: I've got thirty lads costing me a day's wages each to do nothing. Berthe, forewoman. What do you want?" },
      "answers": [
        { "text": { "fr": "Le filon ?", "en": "The seam?" }, "next": "topic1" },
        { "text": { "fr": "Le juge ?", "en": "The judge?" }, "next": "topic2" },
        { "text": { "fr": "Le camp ?", "en": "The camp?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "Du granit à grain fin, et dessous, des veines vertes comme je n'en ai jamais vu. Un filon pareil fait vivre Ormeval dix ans. Encore faut-il y descendre, et les pierres ne veulent pas.", "en": "Fine-grained granite, and under it green veins like nothing I've ever seen. A seam like that keeps Ormeval going for ten years. You've still got to get down to it, though, and the stones won't have it." },
      "answers": [
        { "text": { "fr": "Le juge ?", "en": "The judge?" }, "next": "topic2" },
        { "text": { "fr": "Le camp ?", "en": "The camp?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "Bérold fait tenir les serments d'Ormeval depuis quarante ans, sans en avoir changé un mot. Il est juste, je te l'accorde. Il est aussi têtu qu'une dalle.", "en": "Bérold has kept Ormeval's oaths for forty years without changing a single word. He's fair, I'll give him that. He's also as stubborn as a slab." },
      "answers": [
        { "text": { "fr": "Le filon ?", "en": "The seam?" }, "next": "topic1" },
        { "text": { "fr": "Le camp ?", "en": "The camp?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic3": {
      "text": { "fr": "On travaillait au pied de l'Arbre, au fond de la forêt. Depuis que les gardiens nous chassent, on a reculé jusqu'à la route. Au moins, ici, on voit passer les clients de Lucien.", "en": "We used to work at the foot of the Tree, right at the back of the forest. Since the keepers started driving us off, we've pulled back to the road. At least here we see Lucien's customers go by." },
      "answers": [
        { "text": { "fr": "Le filon ?", "en": "The seam?" }, "next": "topic1" },
        { "text": { "fr": "Le juge ?", "en": "The judge?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```
*Les « veines vertes » sont la malachite, la ressource de niveau 45 de la forêt (T99) : un lien sans un mot de plus.*

**`berold.json`** (4 répliques)
```json
{
  "id": "berold",
  "start": "hello",
  "lines": {
    "hello": {
      "text": { "fr": "Voyageur. Vous entrez dans la forêt d'Ormeval, où l'on tient parole. Je suis Bérold, juge des serments.", "en": "Traveller. You are entering the forest of Ormeval, where people keep their word. I am Bérold, judge of oaths." },
      "answers": [
        { "text": { "fr": "Ormeval ?", "en": "Ormeval?" }, "next": "topic1" },
        { "text": { "fr": "Les serments ?", "en": "The oaths?" }, "next": "topic2" },
        { "text": { "fr": "Et Berthe ?", "en": "And Berthe?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic1": {
      "text": { "fr": "Ormeval est dans les branches de l'Arbre-Mère, au fond de la forêt. Les étrangers n'y montent pas : c'est le premier serment de la ville, et personne ne l'a jamais rompu.", "en": "Ormeval lies in the branches of the Mother Tree, at the far end of the forest. Strangers do not go up: that is the town's first oath, and no one has ever broken it." },
      "answers": [
        { "text": { "fr": "Les serments ?", "en": "The oaths?" }, "next": "topic2" },
        { "text": { "fr": "Et Berthe ?", "en": "And Berthe?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic2": {
      "text": { "fr": "Un serment ne se comprend pas : il se tient. Ceux qui ont juré avant nous savaient pourquoi. Il nous suffit de savoir qu'ils ont juré.", "en": "An oath is not there to be understood: it is there to be kept. Those who swore before us knew why. It is enough for us to know that they swore." },
      "answers": [
        { "text": { "fr": "Ormeval ?", "en": "Ormeval?" }, "next": "topic1" },
        { "text": { "fr": "Et Berthe ?", "en": "And Berthe?" }, "next": "topic3" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    },
    "topic3": {
      "text": { "fr": "Berthe creuse pour Ormeval, je le sais. Mais elle creuse là où personne n'aurait dû creuser, et les pierres le lui disent mieux que moi.", "en": "Berthe digs for Ormeval, I know that. But she is digging where no one should ever have dug, and the stones are telling her so more plainly than I can." },
      "answers": [
        { "text": { "fr": "Ormeval ?", "en": "Ormeval?" }, "next": "topic1" },
        { "text": { "fr": "Les serments ?", "en": "The oaths?" }, "next": "topic2" },
        { "text": { "fr": "À bientôt.", "en": "See you." } }
      ]
    }
  }
}
```
*« Ceux qui ont juré avant nous savaient pourquoi » : Bérold croit que les auteurs du serment avaient de bonnes raisons. C'est vrai en partie, et c'est le point que l'étape 8 retournera (« par ceux qui ont juré »). Il ne dit pas qui a juré.*

**Facultatif : un sujet de plus pour Anatole** (`anatole.json`, il passe à 4 sujets, avec « À bientôt. » en 5ᵉ réponse : **trop de réponses**, le format en permet 4). Je propose donc de **remplacer** son sujet « Pourquoi par l'est ? » une fois la forêt ouverte, ou de ne rien changer. Texte, si vous le voulez :
- Réponse : « La forêt ? » / "The forest?"
- Réplique : « Dans mon livre, la route du sud fait onze mille pas jusqu'à la lisière. Après, ce sont des arbres, et les arbres ne se laissent pas compter. » / "In my book, the south road is eleven thousand paces to the edge of the trees. After that it's all trees, and trees won't let themselves be counted."

### 5.2 Les objets posés (`PROPS` de `tools/zone_people.py`)
Même forme que les entrées existantes : id, aspect, nom, texte, carte, case de départ de la recherche.

```python
    # The Forest of the Ancestors' clues (sprint 136, docs/textes-v8.md §5.2).
    ("trial-pit-1", "stone", text("Un sondage", "A test pit"),
     text("Au fond d'un trou carré, une pierre plate couverte de traits serrés. Ce n'est l'écriture de personne : ni celle des registres de Clairval, ni celle des bornes de la route.",
          "At the bottom of a square hole, a flat stone covered in close-set strokes. It's nobody's writing: not the hand of Clairval's registers, nor that of the milestones on the road."), "ancestors-2-1", (24, 12)),
    ("trial-pit-2", "roots", text("Un sondage sous les racines", "A test pit under the roots"),
     text("Une racine a traversé la pierre gravée sans la fendre : elle a suivi les traits, comme on suit un chemin. Quelques signes ressemblent à des lettres : on devine « … tiendront… », puis la racine couvre le reste.",
          "A root has grown right through the carved stone without splitting it: it followed the strokes, the way you'd follow a path. A few marks look like letters: you can make out '…shall hold…', then the root covers the rest."), "ancestors-1-2", (24, 12)),
    ("trial-pit-3", "grave", text("Le dernier sondage", "The last test pit"),
     text("Quatre signes gravés aux coins de la pierre : une graine, une goutte, un marteau, une plume. Au milieu, un creux gratté. Les traces sont nettes et régulières : on a gratté ici avec un outil, pas avec le temps.",
          "Four signs carved at the corners of the stone: a seed, a drop, a hammer, a feather. In the middle, a scraped-out hollow. The marks are clean and even: this was scraped with a tool, not by time."), "ancestors-2-2", (24, 12)),
    ("echo-stone", "stone", text("La pierre d'écho", "The echo stone"),
     text("Une pierre levée, creusée comme une conque, au fond de la combe. Le sol autour est gratté en rond par des générations de sabots. Tu la frappes : quatre échos reviennent des parois, puis le silence dure un écho de trop.",
          "A standing stone, hollowed like a conch, at the far end of the combe. The ground around it is scraped into a ring by generations of hooves. You strike it: four echoes come back off the walls, then the silence lasts one echo too long."), "bellowing-combe-2", (10, 4)),
    ("rooted-slab", "roots", text("La dalle prise dans les racines", "The slab in the roots"),
     text("Une dalle grande comme une porte, que les racines de l'Arbre serrent de tous côtés, comme une main tient un coffre fermé. On y lit les mots des cœurs des gardiens : « Nous tiendrons… ». La phrase continue sous le bord de la dalle, et de dessous monte un air froid : ça descend encore.",
          "A slab the size of a door, gripped on every side by the Tree's roots, the way a hand holds a chest shut. It bears the words from the keepers' hearts: 'We shall hold…'. The sentence carries on under the edge of the slab, and cold air rises from beneath it: the way goes on down."), "mother-tree-roots-2", (10, 4)),
```
*« Tu la frappes » : seul texte d'objet qui tutoie le joueur. Les autres décrivent. Si le dev préfère la description seule : « Frappée, elle renvoie quatre échos des parois, puis le silence dure un écho de trop. » / "Struck, it sends four echoes back off the walls, then the silence lasts one echo too long."*

### 5.3 Le butin de quête

**`data/items.json`**, à la suite des objets de quête :
```json
  { "id": "keeper-heart", "name": { "fr": "Cœur gravé", "en": "Carved Heart" }, "kind": "Quest", "icon": "<au choix d'icons_gen.py>" },
```

**`data/monsters.json`**, dans le butin de `cobble-keeper`, `pebble-keeper`, `slab-keeper` et `moss-tender` :
```json
          { "item": "keeper-heart", "percent": 35, "quest": "mossy-keepers" }
```
et dans celui de `menhir-keeper` :
```json
          { "item": "keeper-heart", "percent": 100, "quest": "mossy-keepers" }
```

### 5.4 Les quêtes (`data/quests.json`, après `where-roads-part`)
Guillemets intérieurs : chevrons « » en français, apostrophes en anglais (comme en v6 et v7).

```json
  {
    "id": "the-tree-town",
    "name": { "fr": "Ceux de l'arbre", "en": "The Tree Folk" },
    "giver": "four-ways/anatole",
    "zone": "ancestors",
    "level": 31,
    "kind": "Main",
    "text": {
      "fr": "La route du sud entre dans la Forêt des Aïeux, et au bout se tient l'arbre qui porte une ville : Ormeval. Ses mineurs passent ici deux fois l'an ; cette saison, plus un seul. Leur contremaîtresse, Berthe, a monté un camp à l'entrée de la forêt : va voir ce qui les retient.",
      "en": "The south road runs into the Forest of the Ancestors, and at the end of it stands the tree that holds a town: Ormeval. Its miners come through here twice a year; this season, not one. Their forewoman, Berthe, has set up camp at the edge of the forest: go and see what's keeping them."
    },
    "thanks": {
      "fr": "Le juge t'a fait son discours ? Il le fait à tout le monde, même aux écureuils. Avant qu'on mure quoi que ce soit, je veux savoir ce qu'on a trouvé. Tu as l'œil neuf : tu vas m'aider.",
      "en": "Did the judge give you his speech? He gives it to everyone, squirrels included. Before anybody walls anything up, I want to know what we've found. You've got fresh eyes: you're going to help me."
    },
    "objectives": [
      {
        "goal": "Talk",
        "target": "ancestors/berold",
        "line": {
          "fr": "Les mineurs ont ouvert sous l'Arbre un passage qu'aucune carte ne montre, et les pierres se sont levées contre eux. Ce passage sera refermé. Je ne discute pas les serments : je les fais tenir.",
          "en": "The miners opened a passage under the Tree that no map shows, and the stones rose up against them. That passage will be closed. I do not argue with oaths: I see that they are kept."
        }
      },
      {
        "goal": "Talk",
        "target": "ancestors/berthe",
        "line": {
          "fr": "Tu viens de Quatre-Chemins ? Bon, deux bras de plus. On a ouvert un filon sous l'Arbre, le plus beau de ma vie, et depuis, les pierres marchent. Le juge veut tout murer ; moi, j'ai trente gars à payer.",
          "en": "You've come from Four Ways? Good, another pair of hands. We opened a seam under the Tree, the best I've ever seen, and ever since, the stones have been walking. The judge wants it all walled up; I've got thirty lads to pay."
        }
      }
    ],
    "handIn": "ancestors/berthe",
    "after": [ "where-roads-part" ],
    "xp": 1860,
    "shards": 310
  },
  {
    "id": "the-unmapped-passage",
    "name": { "fr": "Le passage qui n'existait pas", "en": "The Passage That Wasn't There" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 33,
    "kind": "Main",
    "text": {
      "fr": "Avant de trouver le filon, mes gars ont creusé des sondages, de l'entrée de la forêt jusqu'à l'Arbre. Dans trois d'entre eux, on est tombés sur des pierres gravées. Va les voir, puis demande au juge s'il les connaît : c'est lui qui garde les registres d'Ormeval.",
      "en": "Before they found the seam, my lads sank test pits all the way from the edge of the forest to the Tree. In three of them we hit carved stones. Go and look at them, then ask the judge if he knows them: he keeps Ormeval's registers."
    },
    "thanks": {
      "fr": "Un creux gratté entre quatre signes ? Des coups réguliers, au ciseau large : c'est du travail de carrier, et d'un bon. Quelqu'un a pris le temps d'effacer ça, et ce n'était pas hier.",
      "en": "A scraped-out hollow between four signs? Even strokes, with a broad chisel: that's a quarryman's work, and a good one. Someone took their time rubbing that out, and it wasn't yesterday."
    },
    "objectives": [
      { "goal": "Interact", "target": "trial-pit-1" },
      { "goal": "Interact", "target": "trial-pit-2" },
      { "goal": "Interact", "target": "trial-pit-3" },
      {
        "goal": "Talk",
        "target": "ancestors/berold",
        "line": {
          "fr": "Les mineurs disent avoir trouvé des pierres plus vieilles que nos registres. Nos registres remontent à la première branche d'Ormeval. Rien de ce qui compte n'est plus vieux qu'eux.",
          "en": "The miners claim they have found stones older than our registers. Our registers go back to Ormeval's first branch. Nothing that matters is older than they are."
        }
      }
    ],
    "after": [ "the-tree-town" ],
    "xp": 1980,
    "shards": 330
  },
  {
    "id": "mossy-keepers",
    "name": { "fr": "Les gardiens moussus", "en": "The Mossy Keepers" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 36,
    "kind": "Main",
    "text": {
      "fr": "Les pierres qui marchent, ce sont des gardiens : de la pierre tenue par la mousse, et ils ne laissent plus personne approcher du filon. Couches-en six. Rapporte aussi trois des cœurs gravés qu'ils portent en eux, et donne-les au juge : lui seul sait les lire.",
      "en": "The walking stones are keepers: stone held together with moss, and they won't let anyone near the seam any more. Put six of them down. And bring back three of the carved hearts they carry inside, and give them to the judge: he's the only one who can read them."
    },
    "thanks": {
      "fr": "Trois cœurs… Ce sont les premiers mots de nos serments, les plus anciens : « Nous tiendrons. » La suite, je ne la lis pas ; elle n'est pas effacée, je ne connais pas ces mots. Ces gardiens ne se battaient pas contre les mineurs, voyageur. Ils tenaient un serment.",
      "en": "Three hearts… These are the first words of our oaths, the oldest ones: 'We shall hold.' The rest I cannot read; it is not worn away, I simply do not know those words. Those keepers were not fighting the miners, traveller. They were keeping an oath."
    },
    "objectives": [
      { "goal": "Defeat", "target": "golem", "count": 6 },
      { "goal": "Bring", "target": "keeper-heart", "count": 3 }
    ],
    "handIn": "ancestors/berold",
    "after": [ "the-unmapped-passage" ],
    "xp": 2160,
    "shards": 360
  },
  {
    "id": "roots-of-the-mother-tree",
    "name": { "fr": "Les Racines de l'Arbre-Mère", "en": "The Mother Tree's Roots" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 41,
    "kind": "Main",
    "text": {
      "fr": "Le juge a fait fermer la grille du passage. Ma grille. La clef, je l'ai encore, mais il fait changer la serrure demain : elle ne servira qu'une fois, ensuite il faudra voir Gédéon. Descends jusqu'à la grande salle, sous l'Arbre, et regarde ce que les racines serrent si fort.",
      "en": "The judge has had the passage gate locked. My gate. I've still got the key, but he's having the lock changed tomorrow: it'll only work once, after that you'll need Gédéon. Go down to the great hall, under the Tree, and look at what the roots are gripping so tight."
    },
    "thanks": {
      "fr": "Bérold Serment-Fixe m'a écrit ce matin, pour la première fois en vingt ans : « La route du sud s'arrête à l'Arbre. » Quand un juge prend la peine d'écrire ça, c'est que la route continue. Berthe appelle le dessous Sous-Racine ; je n'ai pas encore de page pour une route qui descend. J'écris à Ysaline ce soir.",
      "en": "Bérold Serment-Fixe wrote to me this morning, for the first time in twenty years: 'The south road ends at the Tree.' When a judge goes to the trouble of writing that, the road carries on. Berthe calls what lies beneath Underroot; I haven't a page yet for a road that goes down. I'll write to Ysaline tonight."
    },
    "objectives": [
      { "goal": "Dungeon", "target": "mother-tree-roots", "room": 2 },
      { "goal": "Interact", "target": "rooted-slab" },
      {
        "goal": "Talk",
        "target": "ancestors/berold",
        "line": {
          "fr": "Berthe vous a donné la clef de sa grille. Je sais tout ce qui se passe sous cet Arbre, voyageur. Ce qui est en bas ne se prend pas et ne se casse pas : cela se lit.",
          "en": "Berthe has given you the key to her gate. I know everything that happens beneath this Tree, traveller. What lies below is not to be taken or broken: it is to be read."
        }
      }
    ],
    "handIn": "four-ways/anatole",
    "after": [ "mossy-keepers" ],
    "grants": [ { "item": "mother-tree-roots-key", "count": 1 } ],
    "xp": 2460,
    "shards": 410
  },
  {
    "id": "burning-ground",
    "name": { "fr": "Le sol qui brûle", "en": "Burning Ground" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 32,
    "kind": "Side",
    "text": {
      "fr": "Les cerfs grattent le sol autour du camp, et leurs grattis s'allument sous les pieds de mes gars : trois brûlés cette semaine. Chasses-en six. Et regarde où tu marches : eux, ils regardent.",
      "en": "The stags scrape the ground round the camp, and their scrapings light up under my lads' feet: three burnt this week. Hunt six of them. And watch where you tread: they do."
    },
    "thanks": {
      "fr": "Six, et tes bottes sont entières. Mes gars vont pouvoir marcher sans regarder par terre. Enfin, presque. Prends ça pour tes brûlures.",
      "en": "Six, and your boots are in one piece. My lads can walk without staring at the ground again. Well, nearly. Take these for your burns."
    },
    "objectives": [
      { "goal": "Defeat", "target": "stag", "count": 6 }
    ],
    "after": [ "the-tree-town" ],
    "items": [ { "item": "healing-elixir", "count": 3 } ],
    "xp": 1280,
    "shards": 320
  },
  {
    "id": "antler-wedges",
    "name": { "fr": "Des coins d'andouiller", "en": "Antler Wedges" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 34,
    "kind": "Side",
    "text": {
      "fr": "Un andouiller de pierre ne se fend jamais : c'est le meilleur coin qui soit pour caler un étai. Rapporte-m'en cinq, et quatre peaux de cerf pour les genouillères de mes gars : là-dessous, on travaille à genoux.",
      "en": "A stone antler never splits: it's the best wedge there is for bracing a prop. Bring me five, and four deerskins for my lads' knee pads: down there, we work on our knees."
    },
    "thanks": {
      "fr": "Cinq coins, quatre paires de genouillères : mes étais tiendront, et mes gars aussi. Tiens, c'est ce que je donne à ceux qui descendent.",
      "en": "Five wedges, four pairs of knee pads: my props will hold, and so will my lads. Here, it's what I give the ones who go down."
    },
    "objectives": [
      { "goal": "Bring", "target": "stone-antler", "count": 5 },
      { "goal": "Bring", "target": "stag-velvet", "count": 4 }
    ],
    "after": [ "burning-ground" ],
    "items": [ { "item": "greater-healing-elixir", "count": 2 } ],
    "xp": 1360,
    "shards": 340
  },
  {
    "id": "the-bellow",
    "name": { "fr": "Le brame", "en": "The Bellow" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 40,
    "kind": "Side",
    "text": {
      "fr": "La nuit, les cerfs brament dans la combe, au sud-ouest, et mes étais tremblent jusqu'ici. Un brame ne fait pas trembler du bois : quelque chose lui répond. Un de mes gars a pris cette clef sur un vieux cerf ; elle ne servira qu'une fois. Descends jusqu'à la grande salle, et écoute.",
      "en": "At night the stags bellow in the combe, off to the south-west, and my props shake all the way over here. A bellow doesn't make timber shake: something's answering it. One of my lads took this key off an old hart; it'll only work once. Go down to the great hall, and listen."
    },
    "thanks": {
      "fr": "Quatre échos et un trou ? Sous terre, quand un coup ne revient pas, c'est qu'il y a du vide derrière la roche. Je n'aime pas ça, et mes gars encore moins. Tiens, mange : tu as la tête de quelqu'un qui a écouté trop longtemps.",
      "en": "Four echoes and a gap? Underground, when a knock doesn't come back, there's empty space behind the rock. I don't like it, and my lads like it even less. Here, eat: you look like someone who's been listening too long."
    },
    "objectives": [
      { "goal": "Dungeon", "target": "bellowing-combe", "room": 2 },
      { "goal": "Interact", "target": "echo-stone" }
    ],
    "after": [ "antler-wedges" ],
    "grants": [ { "item": "bellowing-combe-key", "count": 1 } ],
    "items": [ { "item": "spelt-bread", "count": 3 } ],
    "xp": 1600,
    "shards": 400
  },
  {
    "id": "bellowing-combe-cleared",
    "name": { "fr": "Au fond : Combe du brame", "en": "At the bottom: The Bellowing Combe" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 44,
    "kind": "Dungeon",
    "text": {
      "fr": "Au bout de la combe vit le Cerf-Aïeul, celui dont la ramure porte des signes, à ce qu'on raconte. Mes gars l'ont vu une fois et ont couru jusqu'ici sans s'arrêter. Va jusqu'au fond, à plusieurs : seul, ce n'est plus du courage, c'est de la paperasse pour le juge.",
      "en": "At the far end of the combe lives the Ancestor Stag, whose antlers carry signs, or so the story goes. My lads saw him once and ran all the way back here without stopping. Go all the way down, with others: alone, it isn't bravery any more, it's paperwork for the judge."
    },
    "thanks": {
      "fr": "L'Aïeul est tombé ? Alors la combe va enfin se taire la nuit. Je dormirai une heure de plus, et c'est à toi que je la devrai.",
      "en": "The Ancestor's down? Then the combe will finally keep quiet at night. I'll sleep an hour longer, and I'll have you to thank for it."
    },
    "objectives": [
      { "goal": "Dungeon", "target": "bellowing-combe" }
    ],
    "after": [ "the-bellow" ],
    "xp": 4400,
    "shards": 660
  },
  {
    "id": "mother-tree-roots-cleared",
    "name": { "fr": "Au fond : Racines de l'Arbre-Mère", "en": "At the bottom: The Mother Tree's Roots" },
    "giver": "ancestors/berthe",
    "zone": "ancestors",
    "level": 45,
    "kind": "Dungeon",
    "text": {
      "fr": "Le plus vieux des gardiens, un menhir qui marche, tient le fond des Racines, au-delà de la dalle. Tant qu'il est debout, aucun de mes gars ne redescendra. Prends du monde et va jusqu'au fond : je veux mon passage, même si le juge veut sa grille.",
      "en": "The oldest of the keepers, a walking menhir, holds the far end of the Roots, beyond the slab. While he's still standing, none of my lads will go back down. Take some company and go all the way: I want my passage, even if the judge wants his gate."
    },
    "thanks": {
      "fr": "Le menhir est couché ? Le juge dira que tu as rompu un serment, et moi, que tu as rouvert un filon. On a sans doute raison tous les deux. Tiens, pour la route.",
      "en": "The menhir's down? The judge will say you broke an oath, and I'll say you reopened a seam. We're probably both right. Here, for the road."
    },
    "objectives": [
      { "goal": "Dungeon", "target": "mother-tree-roots" }
    ],
    "after": [ "roots-of-the-mother-tree" ],
    "items": [ { "item": "sorghum-flatbread", "count": 3 } ],
    "xp": 4500,
    "shards": 675
  }
```

**Notes sur les textes :**
- **Les répliques sans ordre.** Celle de Bérold dans `roots-of-the-mother-tree` tient avant comme après la descente (« Berthe vous a donné la clef » : la clef est donnée à la prise de la quête). Celles de `the-tree-town` et de `the-unmapped-passage` aussi.
- **« La phrase continue dessous »** (la dalle) et **« du vide derrière la roche »** (la Combe) font la promesse des Mines ; Anatole la dit en clair (« une route qui descend »), avec le nom des mineurs, **Sous-Racine**. Aucun texte ne dit « plus tard », « quand tu seras prêt » ni « niveau 45 ».
- **« Ensuite, il faudra voir Gédéon »** reprend la formule d'Ysaline (`rumble-under-the-wallow`) : le serrurier fait les clefs des deux donjons au niveau 40 (T102).
- **Le Cerf-Aïeul dont la ramure « porte des signes »** : une rumeur, qui annonce le passif `cracked-antlers` sans dire lesquels.

---

## 6. Ce qui demande du code ou une donnée nouvelle

**Aucun type d'objectif nouveau.** Les neuf quêtes n'utilisent que `Talk` (avec `line`), `Interact`, `Defeat` (famille), `Bring`, `Dungeon` (avec `room`), ainsi que `handIn`, `grants`, `items` et `after`, tous lus par `Data.cs` et déjà servis par d'autres quêtes. Ce qu'il faut ajouter ou vérifier :

1. **Deux habitants nouveaux**, `berthe` et `berold`, sur `ancestors-1-1` : deux lignes dans `PEOPLE` de `tools/zone_people.py` (apparence, couleur de tenue au dev), et leurs dialogues (§5.1) dans `tools/village_dialogues.py` ou directement dans `data/dialogues/`. Références `ancestors/berthe` et `ancestors/berold`, comme `woods/mathurin`. Les noms affichés avec le métier, comme ceux de Quatre-Chemins.
2. **Cinq objets posés** (§5.2) : trois sondages sur des cartes de la forêt, deux dans des grandes salles (`bellowing-combe-2`, `mother-tree-roots-2`). `dungeon_gen.py` garde les objets posés dans les salles depuis S134. Les aspects existent (`stone`, `roots`, `grave`). Les aspects nouveaux `pit` ou `slab` sont facultatifs.
3. **Un objet de quête**, `keeper-heart` (§5.3), son icône (`icons_gen.py`, `credits.json`), et **cinq lignes de butin** avec `"quest": "mossy-keepers"` sur les gardiens. Le chargement vérifie déjà qu'un butin de quête est un objet `Quest` que la quête demande d'apporter (`Data.cs`, sprint 105).
4. **Une quête rendue hors de sa zone** : `roots-of-the-mother-tree` se donne au camp et se rend à Anatole, à Quatre-Chemins. C'est la forme de `where-roads-part`, dans l'autre sens. À vérifier : le fil du journal (`QuestTrail`) mène de la grande salle des Racines jusqu'à `four-ways-1-1`.
5. **Le donneur d'une étape principale hors de la forêt** : `the-tree-town` est offerte par Anatole. À vérifier : Anatole n'a pas d'autre offre en cours (limite de deux offres par habitant, T70) ; il n'en a pas d'autre.
6. **Berthe donne sept quêtes.** Il peut y avoir trois offres ouvertes à la fois (par exemple `mossy-keepers`, `antler-wedges` et une « Au fond »). À vérifier : la limite de deux offres par habitant (T70) montre la troisième dès qu'une des deux est prise, sans la perdre.
7. **La zone `ancestors` dans le tri du journal** : les quêtes portent `"zone": "ancestors"`, qui existe (T99). Si le journal ne connaît pas encore cette zone, on l'y ajoute, ou on retire `"zone"`.
8. **La salle 2 gagnée à plusieurs** : `the-bellow` et `roots-of-the-mother-tree` comptent la salle 2 pour chaque membre du combat. C'est la règle de `Quests.AfterFight` pour un combat seul. À vérifier pour un combat partagé (T88, T89) : chaque personnage doit être crédité, même si un autre ouvre la porte.
9. **La forêt s'ouvre avant ses quêtes.** La route de la forêt attend `four-trails` (T99, `QuestToReach`), les quêtes attendent `where-roads-part`. Un joueur qui n'est pas passé chez Anatole peut entrer, parler à Berthe et à Bérold (dialogues seulement) et chasser, mais aucune offre ne s'affiche. C'est voulu (v7 §6, point 9). À noter dans le test d'intégration de la chaîne : `four-trails` → `where-roads-part` → les quatre étapes.
10. **La grande salle de la Combe et la Garde** (pour le game designer) : T102 mesure 71 victoires sur 100 à 40 et 73 à 41, mais **47 à 43** (le paquet de sorts de la Garde change). `the-bellow` est au niveau 40 ; un joueur qui la fait à 43 avec la Garde la trouvera plus dure qu'à 40. Le test de la règle du §2 pourrait vérifier la salle 2 de la Combe à 40, **et** à 43 pour la Garde.
11. **Les récompenses en objets** : `healing-elixir` (26), `greater-healing-elixir` (34), `spelt-bread` (40) et `sorghum-flatbread` (45) existent. Si S135 ajoute une potion de niveau 40 ou 45, elle peut remplacer le pain de `the-bellow` ou la galette de la dernière quête. Au game designer de voir si un objet de niveau 45 donné au niveau 45 convient.
12. **Rien ne bloque** (bible §17) : chaque prérequis est une quête de la tranche ou `where-roads-part` ; aucun objectif ne dépend des Mines ; les clefs offertes le sont une fois, puis viennent du serrurier (niveau 40) ou du butin (2 %, 10 % sur les chefs) ; le butin de quête tombe sur toute la famille, chef compris ; les deux « Au fond » sont les seules quêtes de groupe ; aucune réponse de dialogue n'a d'effet, donc rien à rejouer.
13. **Décor facultatif** : un arbre plus grand que les autres au fond de `ancestors-4-4`, autour de la porte des Racines, pour qu'on voie l'Arbre-Mère. Sans lui, rien ne se contredit : aucun texte affiché ne dit que la porte est entre ses racines, seul ce document le dit.

---

## 7. Points de canon et questions pour Charles

### 7.1 Ce que je signale (rien n'est corrigé dans les autres documents)
- **Aucun écart avec la bible.** L'Accord n'est ni nommé ni évoqué ; aucun enfant de Primordial n'est montré ni suggéré ; aucun Primordial n'est nommé. Aucun texte affiché ne dit « cinquième ». La réplique du gardien de la Chambre sans nom n'apparaît pas. Le joueur avance du niveau 1 vers le niveau 2 (des inscriptions plus vieilles que les archives, un signe effacé à l'outil, des gardiens qui tiennent un serment, une dalle tenue fermée par un arbre) sans apprendre qui a juré ni combien de parties compte le serment (réservé à `dhorams-fragment`, étape 8).
- **L'étape 1 du cadre est adaptée.** `chapitre-2-pistes.md` §5 dit « Aubin donne la carte du sud ; monter à Ormeval, parler à Bérold puis à Berthe ». Depuis D79, c'est Anatole qui envoie (les pistes bifurquent à Quatre-Chemins), et la ville n'est pas dans la tranche : Bérold et Berthe sont au camp des mineurs, à l'entrée de la forêt. L'id `the-tree-town` est gardé, et le titre devient « Ceux de l'arbre » : « La ville dans l'arbre » promettrait une ville qu'on ne voit pas.
- **Les mineurs et le passage.** Le cadre dit « des mineurs ont ouvert un passage absent des cartes ». Je place ce passage sous l'Arbre et j'en fais **la porte des Racines** : la grille des mineurs, fermée par Bérold, dont Berthe a la clef. Les « sondages » sont des trous en surface, sur le chemin, pour les trois inscriptions de l'étape 2 (niveau 33, trop tôt pour un donjon de niveau 40).
- **Le creux gratté apparaît deux fois dans la zone** : au sondage (étape 2) et sur la ramure du Cerf-Aïeul (passif, à 50 %). C'est ce que demandait la relecture des familles (`familles-foret-textes.md` §3, remarque 2) ; le cœur du Menhir ne montre toujours rien, et ce sont les cœurs des gardiens que Bérold lit.
- **« Nous tiendrons »** est la formule que je donne aux serments d'Ormeval **[P]**. Elle relie le sondage 2 (« … tiendront… »), les cœurs, la dalle et la devise de Dhoram (« Ce qui possède des racines peut survivre aux tempêtes ») sans la citer. Elle laisse ouverte la question « tenir quoi, et contre qui », qui appartient à l'étape 8.
- **La touche du métal sombre** (D79, réponse 8) reste **dans l'arc de Kaor**, comme le prévoit le cadre (§10.1). Aucun métal sombre n'apparaît dans la forêt, donc rien ne la contredit ni ne l'épuise. Si vous la voulez ici (la Terre étant produite en premier), voir la question 6.
- **Kesh** n'apparaît pas ; rien dans ces textes ne contredit « Kesh est un homme ».
- **La Terre d'abord** : les textes ne supposent pas que le joueur a fait un autre arc. Anatole dit toujours « la route du sud », jamais « la première ».
- **Bérold vouvoie le héros**, alors que la v7 dit « tous les habitants le tutoient » (convention de Quatre-Chemins). Ysaline vouvoyait au début du chapitre 1 : le jeu a donc déjà les deux.
- **Le nom anglais de Sous-Racine** n'existe nulle part. Je propose **Underroot** **[P]** (« the Underroot Mines »), un mot composé comme le français.

### 7.2 Les questions (dans l'ordre où je les relirais)
1. **Ormeval n'est pas visitable**, et Bérold le dit par un serment (« les étrangers n'y montent pas ») **[P]**. Ça vous va pour la tranche ? Sinon : soit la ville vient avec les Mines, et ce serment sera rompu (une belle scène), soit on ne dit rien et le joueur se demande où est la ville qu'Aubin a promise.
2. **Berthe et Bérold au même camp**, à l'entrée de la forêt (`ancestors-1-1`). D'accord, ou préférez-vous Bérold au fond, devant la grille (plus logique, mais il enverrait un joueur de niveau 31 au fond de la zone dès la première étape) ?
3. **La fin de tranche chez Anatole**, à Quatre-Chemins, avec la lettre du juge et « J'écris à Ysaline ce soir ». Ça vous va, ou voulez-vous un retour à Ysaline elle-même (un `Talk` de plus, et un aller-retour à Clairval par les pierres de passage) ?
4. **La porte des Racines est la grille des mineurs**, et sa clef unique vient de Berthe, « avant le changement de serrure ». Gardé ?
5. **La Combe dans une secondaire** (`the-bellow`, niveau 40) avec une clef offerte. À Clairval, seules les étapes principales offrent une clef. On la garde offerte, ou le joueur va chez le serrurier ?
6. **La touche du métal sombre** : on la laisse à Kaor (ma recommandation) ? Si vous la voulez dans la forêt, il faudrait un objet de métal sombre au camp, qui tremble dans les mains de Berthe en colère et pas dans celles du héros. On ne pourrait plus s'en servir dans l'arc de Kaor.
7. **Le vouvoiement de Bérold** : gardé, ou le tutoiement comme tout le monde ?
8. **« Nous tiendrons »** comme formule des serments d'Ormeval : d'accord ?
9. **Underroot** pour Sous-Racine en anglais, ou autre chose (« Rootdeep », « Sub-Root ») ?
10. **Le titre « Ceux de l'arbre » / "The Tree Folk"** pour `the-tree-town`, ou garder « La ville dans l'arbre » ?

---

## 8. Récapitulatif pour la transcription

- **Habitants (2)**, sur `ancestors-1-1`, dialogues au §5.1 : `berthe` (« Berthe, la contremaîtresse / Berthe, the forewoman », près de (24, 10)), `berold` (« Bérold, le juge des serments / Bérold, the oath-judge », près de (28, 10)).
- **Objets posés (5)**, au §5.2 : `trial-pit-1` (`ancestors-2-1`), `trial-pit-2` (`ancestors-1-2`), `trial-pit-3` (`ancestors-2-2`), `echo-stone` (`bellowing-combe-2`), `rooted-slab` (`mother-tree-roots-2`).
- **Objet de quête (1)**, au §5.3 : `keeper-heart` (« Cœur gravé / Carved Heart »), laissé par les gardiens à 35 % et par le menhir à 100 %, pendant `mossy-keepers`.
- **Quêtes (9)**, au §5.4 : `the-tree-town`, `the-unmapped-passage`, `mossy-keepers`, `roots-of-the-mother-tree` (Main, 31, 33, 36, 41) ; `burning-ground`, `antler-wedges`, `the-bellow` (Side, 32, 34, 40) ; `bellowing-combe-cleared`, `mother-tree-roots-cleared` (Dungeon, 44, 45).
- **Facultatifs** : le sujet « La forêt ? » d'Anatole (§5.1, en remplacement d'un sujet), les aspects `pit` et `slab`, un grand arbre autour de la porte des Racines (§6, point 13).
